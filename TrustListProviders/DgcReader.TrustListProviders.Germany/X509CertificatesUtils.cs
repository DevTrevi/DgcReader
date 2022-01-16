using Microsoft.Extensions.Logging;
using DgcReader.TrustListProviders.Germany.Models;
using DgcReader.TrustListProviders.Germany.Backend;
using System;
using System.Text;
using DgcReader.TrustListProviders.Germany.Resources;

#if NET5_0_OR_GREATER || NET47_OR_GREATER
// Recente version of .NET Framework and .NET implements the required cryptographic apis in System.Security.Cryptography
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Linq;
#endif

#if NET452 || NETSTANDARD2_0
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.X509;
using System.IO;
#endif

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.TrustListProviders.Germany
{
    /// <summary>
    /// Utilities for decoding certificates into <see cref="CertificateData"/>
    /// </summary>
    public static class X509CertificatesUtils
    {
#if NET5_0_OR_GREATER || NET47_OR_GREATER

        /// <summary>
        /// Parse the certificate, returning the object that will be stored by the provider
        /// </summary>
        /// <param name="data">Certificate data received from the remote server</param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static CertificateData GetCertificateData(CertificateEntry data, ILogger? logger)
        {

            var cert = new X509Certificate2(data.RawData);
            var keyAlgo = cert.GetKeyAlgorithm();
            var keyAlgoOid = Oid.FromOidValue(keyAlgo, OidGroup.PublicKeyAlgorithm);

            var certData = new CertificateData
            {
                Kid = data.Kid,
                KeyAlgorithm = keyAlgoOid?.FriendlyName,
                SignatureAlgo = cert.SignatureAlgorithm.FriendlyName,
                Thumbprint = data.Thumbprint,
                Timestamp = data.Timestamp,
                Country = data.Country,
            };

            var ecdsa = cert.GetECDsaPublicKey();
            if (ecdsa != null)
            {
                var p = ecdsa.ExportParameters(false);
                certData.EC = new Models.ECParameters
                {
                    Curve = p.Curve.Oid?.Value,
                    CurveFriendlyName = p.Curve.Oid?.FriendlyName,
                    X = p.Q.X?.ToArray() ?? new byte[0],
                    Y = p.Q.Y?.ToArray() ?? new byte[0],
                };

#if NET47_OR_GREATER
                // Fix for .NET Framework
                if (string.IsNullOrEmpty(certData.EC.Curve))
                {
                    certData.EC.Curve = p.Curve.GetOidValue();
                }
#endif
            }

            var rsa = cert.GetRSAPublicKey();
            if (rsa != null)
            {
                var p = rsa.ExportParameters(false);
                certData.RSA = new Models.RSAParameters
                {
                    Exponent = p.Exponent?.ToArray() ?? new byte[0],
                    Modulus = p.Modulus?.ToArray() ?? new byte[0],
                };
            }

            return certData;
        }

#else

        /// <summary>
        /// Parse the certificate, returning the object that will be stored by the provider
        /// </summary>
        /// <param name="data">Certificate data received from the remote server</param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static CertificateData GetCertificateData(CertificateEntry data, ILogger? logger)
        {
            var cert = new X509CertificateParser().ReadCertificate(data.RawData);
            var publicKeyParameters = cert.GetPublicKey();

            var certData = new CertificateData
            {
                Kid = data.Kid,
                SignatureAlgo = cert.SigAlgName,
                Thumbprint = data.Thumbprint,
                Timestamp = data.Timestamp,
                Country = data.Country,
            };

            var ecParameters = publicKeyParameters as ECPublicKeyParameters;
            if (ecParameters != null)
            {
                certData.KeyAlgorithm = "ECC";
                certData.EC = new ECParameters
                {
                    Curve = ecParameters.PublicKeyParamSet.Id,
                    X = ecParameters.Q.XCoord.ToBigInteger().ToByteArray(),
                    Y = ecParameters.Q.YCoord.ToBigInteger().ToByteArray(),
                };
            }

            var rsaKeyParams = publicKeyParameters as RsaKeyParameters;
            if (rsaKeyParams != null)
            {
                certData.KeyAlgorithm = "RSA";
                certData.RSA = new RSAParameters
                {
                    Exponent = rsaKeyParams.Exponent.ToByteArray(),
                    Modulus = rsaKeyParams.Modulus.ToByteArray(),
                };
            };
            return certData;

        }
#endif

        /// <summary>
        /// Check the signature of the downlaoded trustlist
        /// </summary>
        /// <param name="data"></param>
        /// <param name="signature"></param>
        /// <returns></returns>
        public static bool VerifyTrustlistSignature(string data, string signature)
        {

#if NET452 || NETSTANDARD2_0

            // Validating signature using Bouncy Castle
            using (var textReader = new StringReader(Encoding.ASCII.GetString(PublicKeys.dsc_list_signing_key)))
            {
                var pemReader = new PemReader(textReader);
                var pem = pemReader.ReadObject();
                var pubKeyParameters = (ECPublicKeyParameters)pem;

                var keyBytes = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(pubKeyParameters).GetEncoded();
                var pubkey = PublicKeyFactory.CreateKey(keyBytes);


                var signedData = Encoding.ASCII.GetBytes(data);
                var thumbprintData = Convert.FromBase64String(signature);
                // If ECDSA, convert signature in DER format
                thumbprintData = AsnExtensions.ToDerSignature(thumbprintData);

                // Check signature
                var verifier = SignerUtilities.GetSigner("SHA256withECDSA");
                verifier.Init(false, pubkey);
                verifier.BlockUpdate(signedData, 0, signedData.Length);
                var result = verifier.VerifySignature(thumbprintData);
                return result;
            }
#endif

#if NET47_OR_GREATER
            // WORKAROUND for .NET Framework: unfortunately, even latest version of .NET Framework
            // does not implement ecdsa.ImportFromPem method for parsing public keys in PEM format.
            // In order to avoid a dependency to BouncyCastle just to validate the trustlist signature
            // and because I know how the signature key is formatted (nistP256 curve), I get the X and Y parameters of the ECDSa curve
            // directly from the last 64 bytes of the public key data, ignoring the first bytes

            const string publicKeyPemHeader = "-----BEGIN PUBLIC KEY-----";
            const string publicKeyPemFooter = "-----END PUBLIC KEY-----";

            var stringEncoded = Encoding.ASCII.GetString(PublicKeys.dsc_list_signing_key);
            stringEncoded = stringEncoded.Replace(publicKeyPemHeader, "")
                .Replace(publicKeyPemFooter, "")
                .Replace("\n", "");
            var encoded = Convert.FromBase64String(stringEncoded);


            byte[] keyX = new byte[32];
            byte[] keyY = new byte[32];
            Buffer.BlockCopy(encoded, encoded.Length - 64, keyX, 0, keyX.Length);
            Buffer.BlockCopy(encoded, encoded.Length - 32, keyY, 0, keyY.Length);
            var parameters = new System.Security.Cryptography.ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q =
                {
                    X = keyX,
                    Y = keyY,
                },
            };

            var ecdsa = ECDsa.Create();
            ecdsa.ImportParameters(parameters);
            return ecdsa.VerifyData(Encoding.ASCII.GetBytes(data), Convert.FromBase64String(signature), HashAlgorithmName.SHA256);
#endif



#if NET5_0_OR_GREATER
            // Validating signature using System.Security.Cryptography
            var stringEncoded = Encoding.ASCII.GetString(PublicKeys.dsc_list_signing_key);
            var ecdsa = ECDsa.Create();
            ecdsa.ImportFromPem(stringEncoded);
            var derSignature = AsnExtensions.ToDerSignature(Convert.FromBase64String(signature));
            return ecdsa.VerifyData(Encoding.ASCII.GetBytes(data), derSignature, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);

#endif
        }

    }
}
