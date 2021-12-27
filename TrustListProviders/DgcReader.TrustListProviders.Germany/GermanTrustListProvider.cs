using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DgcReader.TrustListProviders.Abstractions;
using DgcReader.TrustListProviders.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;
using DgcReader.TrustListProviders.Germany.Models;
using DgcReader.TrustListProviders.Germany.Backend;
using System.Text;
using DgcReader.TrustListProviders.Germany.Resources;
using DgcReader.Exceptions;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER || NET47_OR_GREATER
using Microsoft.Extensions.Options;
#endif

#if NETFRAMEWORK || NETSTANDARD2_0
using Org.BouncyCastle.X509;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
#endif



// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.TrustListProviders.Germany
{
    /// <summary>
    /// German trust list provider for DgcReader
    /// </summary>
    public class GermanTrustListProvider : ThreadsafeTrustListProvider<GermanTrustListProviderOptions>
    {
        private const string CertUpdateUrl = "https://de.dscg.ubirch.com/trustList/DSC/";

        private const string ProviderDataFolder = "DgcReaderData\\TrustLists\\Germany";
        private const string FileName = "trustlist-de.json";

        private readonly HttpClient _httpClient;

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters = {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.None }
            },
        };
#if NET452
        /// <summary>
        /// Constructor for the provider
        /// </summary>
        /// <param name="httpClient">The http client instance that will be used for requests to the server</param>
        /// <param name="options">The options for the provider</param>
        /// <param name="logger">Instance of <see cref="ILogger"/> used by the provider (optional).</param>
        public GermanTrustListProvider(HttpClient httpClient,
            GermanTrustListProviderOptions? options = null,
            ILogger<GermanTrustListProvider>? logger = null)
            : base(options, logger)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Factory method for creating an instance of <see cref="GermanTrustListProvider"/>
        /// whithout using the DI mechanism. Useful for legacy applications
        /// </summary>
        /// <param name="httpClient">The http client instance that will be used for requests to the server</param>
        /// <param name="options">The options for the provider</param>
        /// <param name="logger">Instance of <see cref="ILogger"/> used by the provider (optional).</param>
        /// <returns></returns>
        public static GermanTrustListProvider Create(HttpClient httpClient,
            GermanTrustListProviderOptions? options = null,
            ILogger<GermanTrustListProvider>? logger = null)
        {
            return new GermanTrustListProvider(httpClient, options, logger);
        }
#else
        /// <summary>
        /// Constructor for the provider
        /// </summary>
        /// <param name="httpClient">The http client instance that will be used for requests to the server</param>
        /// <param name="options">The options for the provider</param>
        /// <param name="logger">Instance of <see cref="ILogger"/> used by the provider (optional).</param>
        public GermanTrustListProvider(HttpClient httpClient,
            IOptions<GermanTrustListProviderOptions>? options = null,
            ILogger<GermanTrustListProvider>? logger = null)
            : base(options?.Value, logger)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Factory method for creating an instance of <see cref="GermanTrustListProvider"/>
        /// whithout using the DI mechanism. Useful for legacy applications
        /// </summary>
        /// <param name="httpClient">The http client instance that will be used for requests to the server</param>
        /// <param name="options">The options for the provider</param>
        /// <param name="logger">Instance of <see cref="ILogger"/> used by the provider (optional).</param>
        /// <returns></returns>
        public static GermanTrustListProvider Create(HttpClient httpClient,
            GermanTrustListProviderOptions? options = null,
            ILogger<GermanTrustListProvider>? logger = null)
        {
            return new GermanTrustListProvider(httpClient,
                options == null ? null : Microsoft.Extensions.Options.Options.Create(options),
                logger);
        }
#endif

        #region Implementation of ITrustListProvider

        /// <inheritdoc/>
        public override bool SupportsCountryCodes => true;

        /// <inheritdoc/>
        public override bool SupportsCertificates => true;

        #endregion

        #region Implementation of TrustListProviderBase

        /// <inheritdoc/>
        protected override async Task<ITrustList> GetTrustListFromServer(CancellationToken cancellationToken = default)
        {
            try
            {
                Logger?.LogInformation("Getting trustlist from server...");
                var certificatesData = await GetCertificatesFromServer(cancellationToken);
                var trustList = new TrustList()
                {
                    LastUpdate = DateTime.Now,
                };

                var certificates = new List<CertificateData>();
                foreach (var data in certificatesData.Certificates)
                {
                    cancellationToken.ThrowIfCancellationRequested();

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

                    if (Options.SaveCertificate)
                    {
                        certData.Certificate = data.RawData;
                    }

                    if (Options.SaveSignature)
                    {
                        certData.Signature = data.Signature;
                    }


#if NET452

                    var certBouncyCastle = new X509CertificateParser().ReadCertificate(cert.RawData);
                    var publicKeyParameters = certBouncyCastle.GetPublicKey();


                    var ecParameters = publicKeyParameters as ECPublicKeyParameters;
                    if (ecParameters != null)
                    {
                        certData.EC = new ECParameters(ecParameters);
                    }

                    var rsaKeyParams = publicKeyParameters as RsaKeyParameters;
                    if (rsaKeyParams != null)
                    {
                        certData.RSA = new Models.RSAParameters()
                        {
                            Exponent = rsaKeyParams.Exponent.ToByteArray(),
                            Modulus = rsaKeyParams.Modulus.ToByteArray(),
                        };
                    }
#else

                    var ecdsa = cert.GetECDsaPublicKey();
                    if (ecdsa != null)
                    {

                        certData.EC = new Models.ECParameters(ecdsa.ExportParameters(false));
                    }

                    var rsa = cert.GetRSAPublicKey();
                    if (rsa != null)
                    {
                        certData.RSA = new Models.RSAParameters(rsa.ExportParameters(false));
                    }
#endif

                    certificates.Add(certData);
                }

                trustList.Certificates = certificates
                    .OrderBy(r => r.Country)
                    .ThenBy(r => r.Kid)
                    .ToArray();


                return trustList;
            }
            catch (Exception e)
            {
                Logger?.LogError(e, $"Error getting trustlist from server: {e.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        protected override Task<ITrustList?> LoadCache(CancellationToken cancellationToken = default)
        {
            var filePath = GetCacheFilePath();
            TrustList? trustList = null;
            try
            {
                if (File.Exists(filePath))
                {
                    Logger?.LogInformation($"Loading trustlist from file");
                    var fileContent = File.ReadAllText(filePath);
                    //var fileContent = await File.ReadAllTextAsync(filePath);   Only > .net5.0
                    trustList = JsonConvert.DeserializeObject<TrustList>(fileContent, JsonSettings);
                }
            }
            catch (Exception e)
            {
                Logger?.LogError(e, $"Error reading trustlist from file: {e.Message}");
            }

            // Check max age and delete file
            if (trustList != null &&
                trustList.LastUpdate.Add(Options.MaxFileAge) < DateTime.Now)
            {
                Logger?.LogInformation($"TrustList expired for MaxFileAge, deleting list and file");
                // File has passed the max age, removing file
                try
                {
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                }
                catch (Exception e)
                {
                    Logger?.LogError(e, $"Error deleting trustlist file: {e.Message}");
                }
                return Task.FromResult<ITrustList?>(null);
            }

            return Task.FromResult<ITrustList?>(trustList);
        }

        /// <inheritdoc/>
        protected override Task UpdateCache(ITrustList trustList, CancellationToken cancellationToken = default)
        {
            var filePath = GetCacheFilePath();
            if (!Directory.Exists(GetCacheFolder()))
                Directory.CreateDirectory(GetCacheFolder());
            var json = JsonConvert.SerializeObject(trustList, JsonSettings);

            File.WriteAllText(filePath, json);
            return Task.FromResult(0);
        }
        #endregion

        #region Private

        private async Task<GermanyTrustList> GetCertificatesFromServer(CancellationToken cancellationToken = default)
        {
            try
            {
                var start = DateTime.Now;
                Logger?.LogDebug("Fetching certificates...");
                var response = await _httpClient.GetAsync(CertUpdateUrl, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    string rawContent = await response.Content.ReadAsStringAsync();

                    var thumbprint = rawContent.Remove(rawContent.IndexOf("{")).Trim();
                    var content = rawContent.Substring(thumbprint.Length).Trim();

                    if (!VerifyTrustlistSignature(content, thumbprint))
                        throw new DgcException("Invalid Trustlist signature");

                    var result = JsonConvert.DeserializeObject<GermanyTrustList>(content);

                    if (result == null)
                        throw new DgcException("Unable to deserialize the Trustlist");

                    Logger?.LogDebug($"{result.Certificates.Count()} read in {DateTime.Now - start}");

                    return result;
                }

                throw new Exception($"The remote server responded with code {response.StatusCode}: {response.ReasonPhrase}");
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, $"Error while getting status list from server: {ex.Message}");
                throw;
            }
        }

        private string GetCacheFolder() => Path.Combine(Options.BasePath, ProviderDataFolder);
        private string GetCacheFilePath() => Path.Combine(GetCacheFolder(), FileName);

        private bool VerifyTrustlistSignature(string data, string signature)
        {
#if NETFRAMEWORK || NETSTANDARD2_0
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
#else
            using (var textReader = new StringReader(Encoding.ASCII.GetString(PublicKeys.dsc_list_signing_key)))
            {
                var ecdsa = ECDsa.Create();
                ecdsa.ImportFromPem(textReader.ReadToEnd());
                var derSignature = AsnExtensions.ToDerSignature(Convert.FromBase64String(signature));
                return ecdsa.VerifyData(Encoding.ASCII.GetBytes(data), derSignature, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
            }
#endif
        }

        #endregion
    }
}
