using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using DgcReader.Models;
using System.Text;

#if NETSTANDARD
using System.Text;
using Org.BouncyCastle.X509;
#else
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
#endif

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Italy
{
    internal static class CertificateExtendedKeyUsageUtils
    {
        /// <summary>
        /// Read the extended key usage identifiers from the signer certificate
        /// </summary>
        /// <param name="signatureValidation"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetExtendedKeyUsages(SignatureValidationResult? signatureValidation, ILogger? logger)
        {
            if (signatureValidation == null)
            {
                logger?.LogWarning("Unable to get extended key usage: No signature validation result available");
                return Enumerable.Empty<string>();
            }

            if (signatureValidation.PublicKeyData?.Certificate == null)
            {
                logger?.LogWarning("Unable to get extended key usage: Certificate is not available. " +
                    "Try to use a TrustListProvider capable of returning signer certificates, or enable the sotrage of certificates in the current TrustListProvider");

                return Enumerable.Empty<string>();
            }
            try
            {

#if NETSTANDARD
                // For netstandard, use BouncyCastle in order to be compatible with Xamarin/Mono

                var certificate = new X509CertificateParser().ReadCertificate(AddPemHeaders(signatureValidation.PublicKeyData.Certificate));
                var enhancedKeyExtensions = certificate.GetExtendedKeyUsage();
                if (enhancedKeyExtensions != null)
                {
                    return enhancedKeyExtensions.OfType<string>().ToArray();
                }
                return Enumerable.Empty<string>();

#else
                var certificate = new X509Certificate2(Convert.FromBase64String(Encoding.ASCII.GetString(signatureValidation.PublicKeyData.Certificate)));
                var enhancedKeyExtensions = certificate.Extensions.OfType<X509EnhancedKeyUsageExtension>();

                return enhancedKeyExtensions
                    .SelectMany(e => e.EnhancedKeyUsages.OfType<Oid>().Select(r => r.Value))
                    .ToArray();
#endif
            }
            catch (Exception e)
            {
                logger?.LogError(e, $"Error while parsing signer certificate: {e.Message}");
                return Enumerable.Empty<string>();
            }
        }


#if NETSTANDARD
        private static byte[] AddPemHeaders(byte[] certificateData)
        {
            const string PemHeader = "-----BEGIN CERTIFICATE-----";
            const string PemFooter = "-----END CERTIFICATE-----";

            var decoded = Encoding.ASCII.GetString(certificateData);
            if (!decoded.StartsWith(PemHeader) && !decoded.EndsWith(PemFooter))
            {
                decoded = PemHeader + "\n" + decoded + "\n" + PemFooter;
                return Encoding.ASCII.GetBytes(decoded);
            }
            return certificateData;
        }
#endif
    }
}