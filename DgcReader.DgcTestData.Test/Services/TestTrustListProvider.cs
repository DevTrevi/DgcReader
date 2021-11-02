using DgcReader.DgcTestData.Test.Models;
using DgcReader.TrustListProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Threading;

#if NET452
using Org.BouncyCastle.X509;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Crypto.Parameters;
#endif

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.DgcTestData.Test.Services
{

    /// <summary>
    /// Implementation of <see cref="ITrustListProvider"/> that uses the certificates
    /// stored in each json file provided by countries to rebuild a TrustList,
    /// in order to validate the test certificates signatures
    /// </summary>
    public class TestTrustListProvider : ITrustListProvider
    {
        private CertificatesTestsLoader Loader { get; }
        private IEnumerable<ITrustedCertificateData> TrustList { get; set; }

        public TestTrustListProvider(CertificatesTestsLoader loader)
        {
            Loader = loader;
        }

        #region Implementation of ITrustListProvider
        /// <inheritdoc/>
        public bool SupportsCountryCodes => true;

        /// <inheritdoc/>
        public bool SupportsCertificates => true;

        /// <inheritdoc/>
        public async Task<ITrustedCertificateData> GetByKid(string kid, string country = null, CancellationToken cancellationToken = default)
        {
            var trustList = await GetTrustList(cancellationToken);
            if (trustList == null)
                return null;

            var q = trustList.Where(x => x.Kid == kid);
            if (!string.IsNullOrEmpty(country))
                q = q.Where(x => x.Country == country);
            return q.SingleOrDefault();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ITrustedCertificateData>> GetTrustList(CancellationToken cancellationToken = default)
        {
            if (TrustList == null)
                await RefreshTrustList(cancellationToken);
            return TrustList;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ITrustedCertificateData>> RefreshTrustList(CancellationToken cancellationToken = default)
        {
            var entries = await Loader.LoadTestEntries();
            TrustList = ToTrustListData(entries.SelectMany(e => e.Value.Select(r => (e.Key, r))));
            return TrustList;
        }
        #endregion

        #region Private
        private IEnumerable<ITrustedCertificateData> ToTrustListData(IEnumerable<(string Country, TestEntry Entry)> testEntries)
        {


            var results = testEntries
                .Where(r => r.Entry.TestContext.Certificate != null)
                .GroupBy(r => new { r.Country, Certificate = Convert.ToBase64String(r.Entry.TestContext.Certificate) })
                .Select(r => new TestTrustedCertData(Convert.FromBase64String(r.Key.Certificate), r.Key.Country, r.FirstOrDefault().Entry.Filename))
                .ToArray();

            return results;
        }
        #endregion

        #region Models
        public class TestTrustedCertData : ITrustedCertificateData
        {

            public TestTrustedCertData(byte[] certificateData, string country, string filename)
            {
                Filename = filename;
                Country = country;
                Certificate = certificateData;


                var cert = new X509Certificate2(Certificate);
                X509Certificate2 = cert;


                var sha256 = SHA256.Create();
                var hashBytes = sha256.ComputeHash(Certificate);
                var kidBytes = hashBytes.Take(8).ToArray();
                var kid = Convert.ToBase64String(kidBytes);

                Kid = kid; //Convert.ToBase64String(cert.PublicKey.EncodedParameters.RawData);

                var keyAlgo = cert.GetKeyAlgorithm();
                var keyAlgoOid = Oid.FromOidValue(keyAlgo, OidGroup.PublicKeyAlgorithm);
                KeyAlgorithm = keyAlgoOid.FriendlyName;


#if NET452

                var certB = new X509CertificateParser().ReadCertificate(Certificate);
                var publicKeyParameters = certB.GetPublicKey();


                var ecParameters = publicKeyParameters as ECPublicKeyParameters;
                if (ecParameters != null)
                {
                    //var curveOid = System.Security.Cryptography.Oid.FromOidValue(ecParameters.PublicKeyParamSet.Id, System.Security.Cryptography.OidGroup.HashAlgorithm);
                    EC = new ECParams()
                    {
                        Curve = ecParameters.PublicKeyParamSet.Id,
                        //CurveFriendlyName= keyAlgoOid.FriendlyName,
                        X = ecParameters.Q.XCoord.ToBigInteger().ToByteArray(),
                        Y = ecParameters.Q.YCoord.ToBigInteger().ToByteArray(),
                    };
                }

                var rsaKeyParams = publicKeyParameters as RsaKeyParameters;
                if (rsaKeyParams != null)
                {
                    RSA = new RSAParams()
                    {
                        Exponent = rsaKeyParams.Exponent.ToByteArray(),
                        Modulus = rsaKeyParams.Modulus.ToByteArray(),
                    };
                }

#else
                var ecdsa = cert.GetECDsaPublicKey();
                if (ecdsa != null)
                    EC = new ECParams(ecdsa.ExportParameters(false));
                

                var rsa = cert.GetRSAPublicKey();
                if (rsa != null)
                    RSA = new RSAParams(rsa.ExportParameters(false));
#endif
            }


            public string Filename { get; set; }
            public X509Certificate2 X509Certificate2 { get; }

            public string Kid { get; set; }

            public string KeyAlgorithm { get; set; }

            public string Country { get; set; }

            public byte[] Certificate { get; set; }

            public IECParameters GetECParameters()
            {
                return EC;
            }

            public IRSAParameters GetRSAParameters()
            {
                return RSA;
            }

            public ECParams EC { get; set; }

            public RSAParams RSA { get; set; }
        }

        public class ECParams : IECParameters
        {

#if !NET452
            public ECParams(ECParameters p)
            {
                Curve = p.Curve.Oid.Value;
                CurveFriendlyName = p.Curve.Oid.FriendlyName;
                X = p.Q.X?.ToArray();
                Y = p.Q.Y?.ToArray();
            }
#endif

            public string Curve { get; set; }

            public string CurveFriendlyName { get; set; }

            public byte[] X { get; set; }

            public byte[] Y { get; set; }
        }

        public class RSAParams : IRSAParameters
        {
            public RSAParams()
            {

            }
            public RSAParams(RSAParameters p)
            {
                Modulus = p.Modulus?.ToArray();
                Exponent = p.Exponent?.ToArray();
            }
            public byte[] Exponent { get; set; }

            public byte[] Modulus { get; set; }
        }

#endregion
    }

}

