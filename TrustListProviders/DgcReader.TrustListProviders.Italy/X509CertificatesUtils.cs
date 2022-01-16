using DgcReader.TrustListProviders.Italy.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Extensions.Logging;

#if NET5_0_OR_GREATER || NET47_OR_GREATER
// Recente version of .NET Framework and .NET implements the required cryptographic apis in System.Security.Cryptography
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
#else
// Older version of .NET Framework, Xamarin, Mono requires BoucyCastle
using Org.BouncyCastle.X509;
using Org.BouncyCastle.Crypto.Parameters;
using System.Text;
#endif

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.TrustListProviders.Italy
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
        /// <param name="kid">The certificate KID</param>
        /// <param name="certificateBytes">Certificate bytes received from server</param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static CertificateData GetCertificateData(string kid, byte[] certificateBytes, ILogger? logger)
        {

            var cert = new X509Certificate2(certificateBytes);
            var keyAlgo = cert.GetKeyAlgorithm();
            var keyAlgoOid = Oid.FromOidValue(keyAlgo, OidGroup.PublicKeyAlgorithm);

            var certData = new CertificateData
            {
                Kid = kid,
                KeyAlgorithm = keyAlgoOid.FriendlyName,
                SignatureAlgo = cert.SignatureAlgorithm.FriendlyName,
            };

            var subjectComponents = ParseCertSubject(cert.Subject, logger);
            if (subjectComponents.ContainsKey("C"))
                certData.Country = subjectComponents["C"][0] ?? "";

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
        /// <param name="kid">The certificate KID</param>
        /// <param name="certificateBytes">Certificate bytes received from server</param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static CertificateData GetCertificateData(string kid, byte[] certificateBytes, ILogger? logger)
        {
            var cert = new X509CertificateParser().ReadCertificate(AddPemHeaders(certificateBytes));
            var publicKeyParameters = cert.GetPublicKey();

            var certData = new CertificateData
            {
                Kid = kid,
                SignatureAlgo = cert.SigAlgName,
            };

            var subjectComponents = ParseCertSubject(cert.SubjectDN.ToString(), logger);
            if (subjectComponents.ContainsKey("C"))
                certData.Country = subjectComponents["C"][0] ?? "";


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

        private static IDictionary<string, string?[]> ParseCertSubject(string subject, ILogger? logger)
        {
            try
            {
                var entries = subject.Split(',')
                    .Select(r =>
                    {
                        if (!r.Contains('='))
                        {
                            return new KeyValuePair<string, string?>(r.Trim(), null);
                        }
                        var idx = r.IndexOf('=');
                        return new KeyValuePair<string, string?>(r.Remove(idx).Trim(),
                            r.Substring(idx + 1).Trim());
                    });

                return new ReadOnlyDictionary<string, string?[]>(
                    entries.GroupBy(r => r.Key)
                    .ToDictionary(r => r.Key, r => r.Select(g => g.Value).ToArray()));
            }
            catch (Exception e)
            {
                logger?.LogError(e, $"Error while parsing certificate subject: {e.Message}");
                throw;
            }
        }
    }
}
