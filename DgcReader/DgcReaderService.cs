using System;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using PeterO.Cbor;
using System.Threading.Tasks;
using GreenpassReader.Models;
using DgcReader.TrustListProviders;
using DgcReader.Cwt.Cose;
using DgcReader.Exceptions;
using DgcReader.Models;
using Microsoft.Extensions.Logging;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader
{
    /// <summary>
    /// Service for decoding and verifying the signature of the European Digital Green Certificate (Green pass)
    /// </summary>
    public class DgcReaderService
    {
        private readonly ITrustListProvider CertificatesProvider;
        private readonly ILogger? Logger;



        /// <summary>
        /// Instantiate the DGCDecoderService
        /// </summary>
        /// <param name="certificatesProvider">The provider used to retrieve the valid public keys for signature validations</param>
        /// <param name="logger"></param>
        public DgcReaderService(ITrustListProvider certificatesProvider, ILogger<DgcReaderService>? logger = null)
        {
            CertificatesProvider = certificatesProvider;
            Logger = logger;
        }

        /// <summary>
        /// Decodes the DGC data, trowing exceptions only if data is in invalid format
        /// Informations about signature validity and expiration can be found in the returned result
        /// </summary>
        /// <param name="codeData">DGC raw data from the QRCode</param>
        /// <returns></returns>
        public async Task<DgcResult> Decode(string codeData)
        {
            try
            {
                var cose = DecodeCoseObject(codeData);

                var dgc = GetSignedDgc(cose);

                try
                {
                    await VerifySignature(cose);
                    dgc.HasValidSignature = true;
                }
                catch (Exception e)
                {
                    Logger?.LogWarning($"Verify signature failed: {e.Message}");
                    dgc.HasValidSignature = false;
                }
                return dgc;
            }
            catch (Exception e)
            {
                Logger?.LogError($"Error decoding Dgc data: {e.Message}");
                throw;
            }
        }


        /// <summary>
        /// Decodes the DGC data, verifying the signature
        /// and the expiration of the object
        /// </summary>
        /// <param name="codeData">The QRCode data of the DGC</param>
        /// <returns></returns>
        public Task<DgcResult> Verify(string codeData)
        {
            return Verify(codeData, DateTimeOffset.Now);
        }


        /// <summary>
        /// Decodes the DGC data, verifying the signature
        /// and the expiration of the object against the specified validationClock
        /// This overload is intended for testing purposes only
        /// </summary>
        /// <param name="codeData">The QRCode data of the DGC</param>
        /// <param name="validationClock">The instant for the expiration check of the object (for testing purposes)</param>
        /// <returns></returns>
        public async Task<DgcResult> Verify(string codeData, DateTimeOffset validationClock)
        {
            try
            {
                var cose = DecodeCoseObject(codeData);

                // Checking signature first, throwing exceptions if not valid
                await VerifySignature(cose);

                // Decoding data
                var dgc = GetSignedDgc(cose);

                // Signature already checked
                dgc.HasValidSignature = true;

                // Check expiration
                if (dgc.ExpirationDate == null)
                {
                    Logger?.LogWarning($"Expiration is not set, assuming is not expired");
                }
                else if (dgc.ExpirationDate < validationClock)
                {
                    throw new DgcExpiredException($"DGC has expired on {dgc.ExpirationDate}",
                        dgc.ExpirationDate.Value);
                }
                else
                {
                    Logger?.LogDebug($"Expiration check succeded ({dgc.ExpirationDate} < {validationClock})");
                }

                return dgc;
            }
            catch (Exception e)
            {
                Logger?.LogError($"Error verifying Dgc data: {e.Message}");
                throw;
            }
        }

#region Private

        /// <summary>
        /// Executes all the decoding steps to get the Cose object from the QR code data
        /// </summary>
        /// <param name="codeData"></param>
        /// <returns></returns>
        /// <exception cref="DgcException"></exception>
        private CoseSign1_Object DecodeCoseObject(string codeData)
        {
            // The base45 encoded data should begin with HC1
            if (codeData.StartsWith("HC1:"))
            {
                string base45CodedData = codeData.Substring(4);

                // Base 45 decode data
                byte[] base45DecodedData = Base45Decoding(Encoding.GetEncoding("UTF-8").GetBytes(base45CodedData));

                // zlib decompression
                byte[] uncompressedData = ZlibDecompression(base45DecodedData);

                // Decoding the COSE_Sign1 object
                var cose = GetCoseObject(uncompressedData);

                return cose;
            }

            throw new DgcException("Invalid data");
        }

        /// <summary>
        /// Verify the signature of the COSE object
        /// </summary>
        /// <param name="cose"></param>
        /// <returns></returns>
        /// <exception cref="DgcSignatureValidationException"></exception>
        /// <exception cref="DgcUnknownSignerException"></exception>
        private async Task VerifySignature(CoseSign1_Object cose)
        {

            var cwt = cose.GetCwt();

            if (cwt == null)
                throw new DgcSignatureValidationException($"Unable to get Cwt object");

            var issuer = cwt.GetIssuer();

            var kid = cose.GetKeyIdentifier();
            if (kid == null)
            {
                throw new DgcSignatureValidationException("Signed DGC does not contain kid - cannot find certificate");
            }
            string kidStr = Convert.ToBase64String(kid);

            // Try by kid and country
            var publicKeyData = await CertificatesProvider.GetByKid(kidStr, issuer);

            // If not found, try Kid only
            // Sometimes the issuer of the CBOR is different from the ISO code fo the country
            if (publicKeyData == null)
                publicKeyData = await CertificatesProvider.GetByKid(kidStr);

            if (publicKeyData == null)
                throw new DgcUnknownSignerException($"No signer certificate could be found for kid {kidStr}", kidStr, issuer);

            try
            {
                // Checking signature
                cose.VerifySignature(publicKeyData);

                Logger?.LogDebug($"HCERT signature verification succeeded using certificate {publicKeyData.Kid}");
            }
            catch (Exception e)
            {
                Logger?.LogWarning($"HCERT signature verification failed using certificate {publicKeyData.Kid} - {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// Extract the data from the COSE object
        /// </summary>
        /// <param name="cose"></param>
        /// <returns></returns>
        private DgcResult GetSignedDgc(CoseSign1_Object cose)
        {
            var cwt = cose.GetCwt();
            if (cwt == null)
                throw new DgcSignatureValidationException($"Unable to get Cwt object");

            var dgcData = cwt.GetDgcV1();

            if (dgcData == null)
                throw new DgcSignatureValidationException($"Unable to get DGC data from cwt");

            var dgc = GetEuDGCFromCbor(dgcData);

            var result = new DgcResult
            {
                Issuer = cwt.GetIssuer(),
                IssuedDate = cwt.GetIssuedAt(),
                ExpirationDate = cwt.GetExpiration(),
                Dgc = dgc,
            };

            return result;
        }

        private static byte[] Base45Decoding(byte[] encodedData)
        {
            byte[] uncodedData = Base45.Decode(encodedData);
            return uncodedData;
        }

        private static byte[] ZlibDecompression(byte[] compressedData)
        {
            if (compressedData[0] == 0x78)
            {
                var outputStream = new MemoryStream();
                using (var compressedStream = new MemoryStream(compressedData))
                using (var inputStream = new InflaterInputStream(compressedStream))
                {
                    inputStream.CopyTo(outputStream);
                    outputStream.Position = 0;
                    return outputStream.ToArray();
                }
            }
            else
            {
                // The data is not compressed
                return compressedData;
            }
        }

        private static CoseSign1_Object GetCoseObject(byte[] uncompressedData)
        {
            var cose = CoseSign1_Object.Decode(uncompressedData);
            return cose;
        }

        private static EuDGC? GetEuDGCFromCbor(byte[] cborData)
        {
            var cbor = CBORObject.DecodeFromBytes(cborData, CBOREncodeOptions.Default);

            var json = cbor.ToJSONString();
            var vacProof = EuDGC.FromJson(json);

            return vacProof;
        }

#endregion
    }
}
