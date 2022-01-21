using System;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using PeterO.Cbor;
using System.Threading.Tasks;
using GreenpassReader.Models;
using DgcReader.Cwt.Cose;
using DgcReader.Exceptions;
using DgcReader.Models;
using Microsoft.Extensions.Logging;
using DgcReader.Interfaces.RulesValidators;
using DgcReader.Interfaces.TrustListProviders;
using DgcReader.Interfaces.BlacklistProviders;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader
{
    /// <summary>
    /// Service for decoding and verifying the signature of the European Digital Green Certificate (Green pass)
    /// </summary>
    public class DgcReaderService
    {
        #region Services
        /// <summary>
        /// The registered TrustList providers
        /// </summary>
        public readonly IEnumerable<ITrustListProvider> TrustListProviders;

        /// <summary>
        /// The registered BlackList providers
        /// </summary>
        public readonly IEnumerable<IBlacklistProvider> BlackListProviders;

        /// <summary>
        /// The registered rule validators
        /// </summary>
        public readonly IEnumerable<IRulesValidator> RulesValidators;
        private readonly ILogger? Logger;
        #endregion

        #region Main public methods

        /// <summary>
        /// Decodes the DGC data, trowing exceptions only if data is in invalid format
        /// Informations about signature validity and expiration can be found in the returned result
        /// </summary>
        /// <param name="qrCodeData">DGC raw data from the QRCode</param>
        /// <param name="validationInstant">The validation instant of the DGC</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<SignedDgc> Decode(string qrCodeData, DateTimeOffset validationInstant, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = new SignedDgc() { ValidationInstant = validationInstant };

                // Step 1: decode
                var cose = DecodeCoseObject(qrCodeData);
                result.Dgc = GetDgc(cose);

                // Step 2: check signature
                try
                {
                    var signatureValidation = await GetSignatureValidationResult(cose, validationInstant, false, cancellationToken);
                    result.HasValidSignature = signatureValidation.HasValidSignature;
                }
                catch (Exception e)
                {
                    Logger?.LogWarning($"Verify signature failed: {e.Message}");
                    result.HasValidSignature = false;
                    return result;
                }



                return result;
            }
            catch (Exception e)
            {
                Logger?.LogError($"Error decoding Dgc data: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// Decodes the DGC data, verifying signature, blacklist and rules if a provider is available.
        /// </summary>
        /// <param name="qrCodeData">The QRCode data of the DGC</param>
        /// <param name="acceptanceCountryCode">The 2-letter ISO country of the acceptance country. This information is mandatory in order to perform the rules validation</param>
        /// <param name="validationInstant">The validation instant of the DGC</param>
        /// <param name="rulesValidatorFunction">The specific function to be called for rules validation</param>
        /// <param name="throwOnError">If true, throw an exception if the validation fails</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="DgcException"></exception>
        public async Task<DgcValidationResult> Verify(
            string qrCodeData,
            string acceptanceCountryCode,
            DateTimeOffset validationInstant,
            Func<EuDGC, string, DateTimeOffset, SignatureValidationResult, BlacklistValidationResult, bool, CancellationToken, Task<IRulesValidationResult?>>? rulesValidatorFunction,
            bool throwOnError = true,
            CancellationToken cancellationToken = default)
        {
            var result = new DgcValidationResult()
            {
                ValidationInstant = validationInstant,
                AcceptanceCountry = acceptanceCountryCode,
            };

            try
            {
                // Inner try/catch block to calculate exception and result
                try
                {
                    var cose = DecodeCoseObject(qrCodeData);

                    // Step 1: Decoding data
                    result.Dgc = GetDgc(cose);

                    // Step 2: check signature
                    result.Signature = await GetSignatureValidationResult(cose, validationInstant, throwOnError);

                    // Step 3: check blacklist
                    result.Blacklist = await GetBlacklistValidationResult(result.Dgc, throwOnError);

                    // Step 4: check country rules
                    if (rulesValidatorFunction != null)
                    {
                        result.RulesValidation =
                            await rulesValidatorFunction.Invoke(
                                result.Dgc,
                                acceptanceCountryCode,
                                validationInstant,
                                result.Signature,
                                result.Blacklist,
                                throwOnError,
                                cancellationToken);

                        if (result.RulesValidation != null)
                        {
                            var rulesResult = result.RulesValidation;

                            // If result is not valid and throwOnError is true, throw an exception
                            if (rulesResult.Status != DgcResultStatus.Valid && throwOnError)
                            {
                                var message = rulesResult.StatusMessage;
                                if (string.IsNullOrEmpty(message))
                                    message = GetDgcResultStatusDescription(rulesResult.Status);

                                throw new DgcRulesValidationException(message, rulesResult);
                            }

                            // If no message set, use message from rules validation
                            if (string.IsNullOrEmpty(result.StatusMessage))
                                result.StatusMessage = rulesResult.StatusMessage;
                        }
                    }

                }
                catch (DgcException)
                {
                    // Managed exception, rethrow as is
                    throw;
                }
                catch (Exception e)
                {
                    throw new DgcException(e.Message, e);
                }
            }
            catch (DgcException e)
            {
                result.StatusMessage = e.Message;
                if (throwOnError)
                {
                    Logger?.LogError($"Validation failed: {e.Message}");
                    throw;
                }
            }

            // Fallback message if not specified before
            if (string.IsNullOrEmpty(result.StatusMessage))
                result.StatusMessage = GetDgcResultStatusDescription(result.Status);

            if (result.Status == DgcResultStatus.Valid)
                Logger?.LogInformation($"Validation succeded: {result.StatusMessage}");
            else if (result.Status == DgcResultStatus.NeedRulesVerification)
                Logger?.LogWarning($"Validation succeded without rules verification: {result.StatusMessage}");
            else
                Logger?.LogError($"Validation failed: {result.StatusMessage}");

            return result;
        }

        /// <summary>
        /// Return the list of 2-letter iso country codes for the supported acceptance countries for rules verification
        /// The array is computed by checking all the countries supported by every registered IRulesValidator
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<string>> GetSupportedCountries(CancellationToken cancellationToken = default)
        {
            if (RulesValidators?.Any() != true)
                return Enumerable.Empty<string>();

            var temp = new List<string>();
            foreach (var ruleValidator in RulesValidators)
            {
                try
                {
                    var countryCodes = await ruleValidator.GetSupportedCountries(cancellationToken);
                    temp.AddRange(countryCodes.Select(r => r.ToUpperInvariant()));
                }
                catch (Exception e)
                {
                    Logger.LogWarning($"Error while getting supported countries for provider {ruleValidator}: {e}");
                }
            }

            return temp.Where(r => !string.IsNullOrEmpty(r)).OrderBy(r => r).ToArray();
        }

        #endregion

        #region Overloads

        /// <summary>
        /// Decodes the DGC data, trowing exceptions only if data is in invalid format
        /// Informations about signature validity and expiration can be found in the returned result
        /// </summary>
        /// <param name="qrCodeData">DGC raw data from the QRCode</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<SignedDgc> Decode(string qrCodeData, CancellationToken cancellationToken = default)
        {
            return Decode(qrCodeData, DateTimeOffset.Now, cancellationToken);
        }

        /// <summary>
        /// Decodes the DGC data, verifying signature, blacklist and rules if a provider is available.
        /// </summary>
        /// <param name="qrCodeData">The QRCode data of the DGC</param>
        /// <param name="acceptanceCountryCode">The 2-letter ISO country of the acceptance country. This information is mandatory in order to perform the rules validation</param>
        /// <param name="throwOnError">If true, throw an exception if the validation fails</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<DgcValidationResult> Verify(string qrCodeData, string acceptanceCountryCode, bool throwOnError = true, CancellationToken cancellationToken = default)
        {
            return Verify(qrCodeData, acceptanceCountryCode, DateTimeOffset.Now, throwOnError, cancellationToken);
        }

        /// <summary>
        /// Decodes the DGC data, verifying signature, blacklist and rules if a provider is available.
        /// </summary>
        /// <param name="qrCodeData">The QRCode data of the DGC</param>
        /// <param name="acceptanceCountryCode">The 2-letter ISO country of the acceptance country. This information is mandatory in order to perform the rules validation</param>
        /// <param name="validationInstant">The validation instant of the DGC</param>
        /// <param name="throwOnError">If true, throw an exception if the validation fails</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="DgcException"></exception>
        public async Task<DgcValidationResult> Verify(
            string qrCodeData,
            string acceptanceCountryCode,
            DateTimeOffset validationInstant,
            bool throwOnError = true,
            CancellationToken cancellationToken = default)
        {
            return await Verify(qrCodeData,
                acceptanceCountryCode,
                validationInstant,
                GetRulesValidationResult,
                throwOnError,
                cancellationToken);
        }

        /// <summary>
        /// Decodes the DGC data, verifying signature, blacklist and rules if a provider is available.
        /// A result is always returned
        /// This is equivalent to <see cref="Verify(string, string, bool, CancellationToken)"/> with throwOnError = false
        /// </summary>
        /// <param name="qrCodeData">The QRCode data of the DGC</param>
        /// <param name="acceptanceCountryCode">The 2-letter ISO country of the acceptance country. This information is mandatory in order to perform the rules validation</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<DgcValidationResult> GetValidationResult(string qrCodeData, string acceptanceCountryCode, CancellationToken cancellationToken = default)
        {
            return Verify(qrCodeData, acceptanceCountryCode, false, cancellationToken);
        }

        /// <summary>
        /// Decodes the DGC data, verifying signature, blacklist and rules if a provider is available.
        /// A result is always returned.
        /// This is equivalent to <see cref="Verify(string, string, DateTimeOffset, bool, CancellationToken)"/> with throwOnError = false
        /// </summary>
        /// <param name="qrCodeData">The QRCode data of the DGC</param>
        /// <param name="acceptanceCountryCode">The 2-letter ISO country of the acceptance country. This information is mandatory in order to perform the rules validation</param>
        /// <param name="validationInstant">The validation instant of the DGC</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<DgcValidationResult> GetValidationResult(string qrCodeData, string acceptanceCountryCode, DateTimeOffset validationInstant, CancellationToken cancellationToken = default)
        {
            return Verify(qrCodeData, acceptanceCountryCode, validationInstant, false, cancellationToken);
        }
        #endregion

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
        /// <param name="validationInstant">The validation instant of the DGC</param>
        /// <param name="throwOnError">If true, throw an exception if the validation fails</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="DgcSignatureValidationException"></exception>
        private async Task<SignatureValidationResult> GetSignatureValidationResult(CoseSign1_Object cose, DateTimeOffset validationInstant, bool throwOnError, CancellationToken cancellationToken = default)
        {
            var context = new SignatureValidationResult();
            try
            {
                var cwt = cose.GetCwt();

                if (cwt == null)
                    throw new DgcSignatureValidationException($"Unable to get Cwt object");

                context.Issuer = cwt.GetIssuer();
                context.IssuedDate = cwt.GetIssuedAt();
                context.SignatureExpiration = cwt.GetExpiration();

                var kid = cose.GetKeyIdentifier();
                if (kid == null)
                {
                    throw new DgcSignatureValidationException("Signed DGC does not contain kid - cannot find certificate", result: context);
                }
                string kidStr = Convert.ToBase64String(kid);
                context.CertificateKid = kidStr;

                // Search for the public key from the registered TrustList providers
                if (TrustListProviders?.Any() != true)
                {
                    throw new DgcException($"No trustlist provider is registered for signature validation");
                }
                var publicKeyData = await GetSignaturePublicKey(kidStr, context.Issuer, cancellationToken);
                if (publicKeyData == null)
                    throw new DgcUnknownSignerException($"No signer certificate could be found for kid {kidStr}", result: context);

                context.PublicKeyData = publicKeyData;

                // Checking signature
                cose.VerifySignature(publicKeyData);

                // Check signature validity dates
                if (context.IssuedDate != null && context.IssuedDate > validationInstant)
                {
                    throw new DgcSignatureExpiredException($"The signed object is not valid yet", result: context);
                }
                if (context.SignatureExpiration == null)
                {
                    Logger?.LogWarning($"Expiration is not set, assuming is not expired");
                }
                else if (context.SignatureExpiration < validationInstant)
                {
                    throw new DgcSignatureExpiredException($"The signed object has expired on {context.SignatureExpiration}", context);
                }

                Logger?.LogDebug($"HCERT signature verification succeeded using certificate {publicKeyData.Kid}");
                context.HasValidSignature = true;
            }
            catch (Exception e)
            {
                // Wrap unmanaged exceptions as DgcSignatureValidationException
                Logger?.LogWarning($"HCERT signature verification failed: {e.Message}");
                if (throwOnError)
                {
                    if (e is DgcSignatureValidationException)
                        throw;

                    throw new DgcSignatureValidationException(e.Message, e, context);
                }
            }

            return context;
        }

        /// <summary>
        /// Verify if te certificate is included in a blacklist. If true, throws an exception
        /// </summary>
        /// <param name="dgc">The DGC</param>
        /// <param name="throwOnError">If true, throw an exception if the validation fails</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="DgcBlackListException"></exception>
        private async Task<BlacklistValidationResult> GetBlacklistValidationResult(EuDGC dgc, bool throwOnError, CancellationToken cancellationToken = default)
        {
            var context = new BlacklistValidationResult()
            {
                BlacklistVerified = false,
                IsBlacklisted = null,
            };

            if (BlackListProviders?.Any() == true)
            {
                var certEntry = dgc.GetCertificateEntry();

                // Tracking the validated CertificateIdentifier
                context.CertificateIdentifier = certEntry.CertificateIdentifier;

                foreach (var blacklistProvider in BlackListProviders)
                {
                    var blacklisted = await blacklistProvider.IsBlacklisted(certEntry.CertificateIdentifier, cancellationToken);

                    // At least one check performed
                    context.BlacklistVerified = true;

                    if (blacklisted)
                    {
                        context.IsBlacklisted = true;
                        context.BlacklistMatchProviderType = blacklistProvider.GetType();

                        Logger?.LogWarning($"The certificate is blacklisted");
                        if (throwOnError)
                            throw new DgcBlackListException($"The certificate is blacklisted", context);

                        return context;
                    }
                }
                context.IsBlacklisted = false;
            }
            else
            {
                context.BlacklistVerified = false;
                context.IsBlacklisted = null;
                Logger?.LogWarning($"No blacklist provider is registered, blacklist validation is skipped");
            }

            return context;
        }

        /// <summary>
        /// Validates the rules for the specified acceptance country.
        /// This overload is intended for internal use only, and should not be used directly
        /// </summary>
        /// <param name="dgc">The DGC</param>
        /// <param name="acceptanceCountryCode">The 2-letter iso code of the acceptance country</param>
        /// <param name="validationInstant">The validation instant of the DGC</param>
        /// <param name="signatureValidationResult">The result from the signature validation step</param>
        /// <param name="blacklistValidationResult">The result from the blacklist validation step</param>
        /// <param name="throwOnError">If true, throw an exception if the validation fails</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="DgcRulesValidationException"></exception>
        private async Task<IRulesValidationResult?> GetRulesValidationResult(
            EuDGC dgc,
            string acceptanceCountryCode,
            DateTimeOffset validationInstant,
            SignatureValidationResult signatureValidationResult,
            BlacklistValidationResult blacklistValidationResult,
            bool throwOnError,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(acceptanceCountryCode))
            {
                Logger?.LogWarning($"No acceptance country code specified, rules validation is skipped");
                return null;
            }

            if (RulesValidators == null)
            {
                Logger?.LogWarning($"No rules validators registered, rules validation is skipped");
                return null;
            }

            // While result is open, try all the registered validators
            IRulesValidationResult? rulesResult = null;
            foreach (var validator in RulesValidators)
            {
                if (await validator.SupportsCountry(acceptanceCountryCode, cancellationToken))
                {
                    rulesResult = await validator.GetRulesValidationResult(dgc,
                        validationInstant,
                        acceptanceCountryCode,
                        signatureValidationResult,
                        blacklistValidationResult,
                        cancellationToken);

                    if (rulesResult != null)
                    {
                        switch (rulesResult.Status)
                        {
                            case DgcResultStatus.NeedRulesVerification:
                            case DgcResultStatus.OpenResult:
                                // If result is "Open", try next validator if multiple validators for this country exists
                                continue;
                            default:
                                // With any other result, return the result
                                return rulesResult;
                        }
                    }
                }
            }

            // No result or Open result after checking supported validators
            if (rulesResult == null)
            {
                Logger?.LogWarning($"No rules validator is registered for acceptance country {acceptanceCountryCode}, rules validation is skipped");
                return null;
            }

            // Result is not null, it will certainly be an Open result
            return rulesResult;
        }

        /// <summary>
        /// Extract the DGC data from the COSE object
        /// </summary>
        /// <param name="cose"></param>
        /// <returns></returns>
        private EuDGC GetDgc(CoseSign1_Object cose)
        {
            var cwt = cose.GetCwt();
            if (cwt == null)
                throw new DgcException($"Unable to get Cwt object");

            var dgcData = cwt.GetDgcV1();

            if (dgcData == null)
                throw new DgcException($"Unable to get DGC data from cwt");

            var dgc = GetEuDGCFromCbor(dgcData);
            if (dgc == null)
                throw new DgcException($"Unable to get DGC data from cwt");

            return dgc;
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

        private static string GetDgcResultStatusDescription(DgcResultStatus status)
        {
            switch (status)
            {
                case DgcResultStatus.NotEuDCC:
                    return "Certificate is not a valid EU DGC";
                case DgcResultStatus.InvalidSignature:
                    return "Invalid signature";
                case DgcResultStatus.Blacklisted:
                    return "Certificate is blacklisted";
                case DgcResultStatus.NeedRulesVerification:
                    return "Country rules has not been verified for the certificate";
                case DgcResultStatus.OpenResult:
                    return "Validation could not determine a definitive result";
                case DgcResultStatus.NotValid:
                    return "Certificate is not valid";
                case DgcResultStatus.Valid:
                    return "Certificate is valid";
                default:
                    return $"Status {status} not supported";
            }
        }

        /// <summary>
        /// Return the public key data for the signing certificate
        /// </summary>
        /// <param name="kid">The KID of the certificate</param>
        /// <param name="issuingCountryCode">2-letter iso code of the issuer country</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<ITrustedCertificateData?> GetSignaturePublicKey(string kid, string? issuingCountryCode, CancellationToken cancellationToken = default)
        {
            // If multiple providers are registered, search the public key in every provider
            foreach (var provider in TrustListProviders)
            {
                // Try by kid and country
                var publicKeyData = await provider.GetByKid(kid, issuingCountryCode, cancellationToken);

                // If not found, try Kid only
                // Sometimes the issuer of the CBOR is different from the ISO code fo the country
                if (publicKeyData == null)
                    publicKeyData = await provider.GetByKid(kid, cancellationToken: cancellationToken);


                if (publicKeyData == null)
                {
                    Logger?.LogWarning($"Public key data for {kid} from country {issuingCountryCode} not found using {provider}");
                }
                else
                {
                    Logger?.LogDebug($"Public key data for {kid} from country {issuingCountryCode} found using {provider}");
                    return publicKeyData;
                }
            }
            return null;
        }

        #endregion

        #region Factory methods and constructor

        /// <summary>
        /// Instantiate the DgcReaderService
        /// </summary>
        /// <param name="trustListProviders">The provider used to retrieve the valid public keys for signature validations</param>
        /// <param name="blackListProviders">The provider used to check if a certificate is blacklisted</param>
        /// <param name="rulesValidators">The service used to validate the rules for a specific country</param>
        /// <param name="logger"></param>
        public DgcReaderService(IEnumerable<ITrustListProvider>? trustListProviders = null,
            IEnumerable<IBlacklistProvider>? blackListProviders = null,
            IEnumerable<IRulesValidator>? rulesValidators = null,
            ILogger<DgcReaderService>? logger = null)
        {
            TrustListProviders = trustListProviders ?? Enumerable.Empty<ITrustListProvider>();
            BlackListProviders = blackListProviders ?? Enumerable.Empty<IBlacklistProvider>();
            RulesValidators = rulesValidators ?? Enumerable.Empty<IRulesValidator>();
            Logger = logger;
        }

        /// <summary>
        /// Instantiate the DgcReaderService
        /// </summary>
        /// <param name="trustListProvider">The provider used to retrieve the valid public keys for signature validations</param>
        /// <param name="blackListProvider">The provider used to check if a certificate is blacklisted</param>
        /// <param name="rulesValidator">The service used to validate the rules for a specific country</param>
        /// <param name="logger"></param>
        public static DgcReaderService Create(ITrustListProvider? trustListProvider = null,
            IBlacklistProvider? blackListProvider = null,
            IRulesValidator? rulesValidator = null,
            ILogger<DgcReaderService>? logger = null)
        {
            var trustListProviders = new List<ITrustListProvider>();
            if (trustListProvider != null)
                trustListProviders.Add(trustListProvider);

            var blackListProviders = new List<IBlacklistProvider>();
            if (blackListProvider != null)
                blackListProviders.Add(blackListProvider);

            var rulesValidators = new List<IRulesValidator>();
            if (rulesValidator != null)
                rulesValidators.Add(rulesValidator);


            return new DgcReaderService(trustListProviders, blackListProviders, rulesValidators, logger);
        }

        /// <summary>
        /// Instantiate the DgcReaderService
        /// </summary>
        /// <param name="trustListProviders">The providers used to retrieve the valid public keys for signature validations</param>
        /// <param name="blackListProviders">The providers used to check if a certificate is blacklisted</param>
        /// <param name="rulesValidators">The services used to validate the rules for a specific country</param>
        /// <param name="logger"></param>
        public static DgcReaderService Create(IEnumerable<ITrustListProvider>? trustListProviders = null,
            IEnumerable<IBlacklistProvider>? blackListProviders = null,
            IEnumerable<IRulesValidator>? rulesValidators = null,
            ILogger<DgcReaderService>? logger = null)
        {
            return new DgcReaderService(trustListProviders, blackListProviders, rulesValidators, logger);
        }
        #endregion
    }
}