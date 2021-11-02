using DgcReader.TrustListProviders.Italy.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DgcReader.TrustListProviders.Abstractions;
using DgcReader.TrustListProviders.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;

#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER || NET47_OR_GREATER
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
#endif

#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER || NET47_OR_GREATER
using Microsoft.Extensions.Logging;
#endif

#if NET452
using Org.BouncyCastle.X509;
using Org.BouncyCastle.Crypto.Parameters;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
#endif



// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.TrustListProviders.Italy
{
    /// <summary>
    /// Italian trust list provider for DgcReader
    /// </summary>
    public class ItalianTrustListProvider : ThreadsafeTrustListProvider<ItalianTrustListProviderOptions>
    {
        private const string HeaderResumeToken = "X-RESUME-TOKEN";
        private const string HeaderKid = "X-KID";
        private const string CertUpdateUrl = "https://get.dgc.gov.it/v1/dgc/signercertificate/update";
        private const string CertStatusUrl = "https://get.dgc.gov.it/v1/dgc/signercertificate/status";

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
        public ItalianTrustListProvider(HttpClient httpClient,
            ItalianTrustListProviderOptions? options = null,
            ILogger<ItalianTrustListProvider>? logger = null)
            : base(options, logger)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Factory method for creating an instance of <see cref="ItalianTrustListProvider"/> 
        /// whithout using the DI mechanism. Useful for legacy applications
        /// </summary>
        /// <param name="httpClient">The http client instance that will be used for requests to the server</param>
        /// <param name="options">The options for the provider</param>
        /// <param name="logger">Instance of <see cref="ILogger"/> used by the provider (optional).</param>
        /// <returns></returns>
        public static ItalianTrustListProvider Create(HttpClient httpClient,
            ItalianTrustListProviderOptions? options = null,
            ILogger<ItalianTrustListProvider>? logger = null)
        {
            return new ItalianTrustListProvider(httpClient, options, logger);
        }
#else
        /// <summary>
        /// Constructor for the provider
        /// </summary>
        /// <param name="httpClient">The http client instance that will be used for requests to the server</param>
        /// <param name="options">The options for the provider</param>
        /// <param name="logger">Instance of <see cref="ILogger"/> used by the provider (optional).</param>
        public ItalianTrustListProvider(HttpClient httpClient,
            IOptions<ItalianTrustListProviderOptions>? options = null,
            ILogger<ItalianTrustListProvider>? logger = null)
            : base(options?.Value, logger)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Factory method for creating an instance of <see cref="ItalianTrustListProvider"/> 
        /// whithout using the DI mechanism. Useful for legacy applications
        /// </summary>
        /// <param name="httpClient">The http client instance that will be used for requests to the server</param>
        /// <param name="options">The options for the provider</param>
        /// <param name="logger">Instance of <see cref="ILogger"/> used by the provider (optional).</param>
        /// <returns></returns>
        public static ItalianTrustListProvider Create(HttpClient httpClient,
            ItalianTrustListProviderOptions? options = null,
            ILogger<ItalianTrustListProvider>? logger = null)
        {
            return new ItalianTrustListProvider(httpClient,
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
                var trustList = new TrustList()
                {
                    LastUpdate = DateTime.Now,
                };
                var certificatesData = await GetCertificatesFromServer(cancellationToken);


                var certificates = new List<CertificateData>();
                foreach (var data in certificatesData)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var cert = new X509Certificate2(data.Value);
                    var keyAlgo = cert.GetKeyAlgorithm();
                    var keyAlgoOid = Oid.FromOidValue(keyAlgo, OidGroup.PublicKeyAlgorithm);

                    var certData = new CertificateData
                    {
                        Kid = data.Key,
                        KeyAlgorithm = keyAlgoOid.FriendlyName,
                        SignatureAlgo = cert.SignatureAlgorithm.FriendlyName,
                    };

                    if (Options.SaveCertificate)
                    {
                        certData.Certificate = data.Value;
                    }

                    var subjectCOmponents = ParseCertSubject(cert.Subject);
                    if (subjectCOmponents.ContainsKey("C"))
                        certData.Country = subjectCOmponents["C"][0];



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
            var filePath = GetTrustListFilePath();
            TrustList trustList = null;
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
                    Logger?.LogError(e, $"Error deleting rules list file: {e.Message}");
                }
                return Task.FromResult<ITrustList?>(null);
            }

            return Task.FromResult<ITrustList?>(trustList);
        }

        /// <inheritdoc/>
        protected override Task UpdateCache(ITrustList trustList, CancellationToken cancellationToken = default)
        {
            var filePath = GetTrustListFilePath();
            var json = JsonConvert.SerializeObject(trustList, JsonSettings);

            File.WriteAllText(filePath, json);
            return Task.FromResult(0);
        }
#endregion

#region Private

        private async Task<string[]> FetchCertificatesStatus(CancellationToken cancellationToken = default)
        {
            try
            {
                var start = DateTime.Now;
                Logger?.LogDebug("Fetching valid key identifiers...");
                var response = await _httpClient.GetAsync(CertStatusUrl, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();

                    var results = JsonConvert.DeserializeObject<string[]>(content);

                    Logger?.LogDebug($"{results?.Length} read in {DateTime.Now - start}");
                    return results ?? new string [0];
                }

                throw new Exception($"The remote server responded with code {response.StatusCode}: {response.ReasonPhrase}");
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, $"Error while getting status list from server: {ex.Message}");
                throw;
            }
        }

        private async Task<IDictionary<string, byte[]>> GetCertificatesFromServer(CancellationToken cancellationToken = default)
        {
            var start = DateTime.Now;
            Logger?.LogInformation("Updating certificates list from server...");

            var validKeys = await FetchCertificatesStatus(cancellationToken);

            int? resumeToken = 0;
            var certificates = new Dictionary<string, byte[]>();
            while (resumeToken != null)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var certStart = DateTime.Now;
                var result = await FetchCertificate(resumeToken, cancellationToken);
                Logger?.LogDebug($"Cert. with resume token {resumeToken}, kid {result.Kid} downloaded in {DateTime.Now - certStart} ");
                if (!string.IsNullOrEmpty(result.Kid) && result.Certificate != null)
                {
                    if (validKeys.Contains(result.Kid))
                    {
                        if (certificates.Keys.Contains(result.Kid))
                        {
                            // Sometimes the backend returns the same certificate multiple times
                            var len = certificates[result.Kid].Length;
                            if (len == result.Certificate.Length)
                                Logger?.LogInformation($"Certificate with kid {result.Kid} already exist with same lenght. Skip");
                            else
                                Logger?.LogWarning($"Certificate with kid {result.Kid} (size {result.Certificate.Length}) already exist (lenght {len}). Skip");
                        }
                        else
                        {
                            certificates.Add(result.Kid, result.Certificate);
                        }
                    }
                    else
                    {
                        Logger?.LogWarning($"Cert. with resume token {resumeToken}, kid {result.Kid} is not listed as valid and will be discarded");
                    }
                }

                resumeToken = result.ResumeToken;
            }

            Logger?.LogInformation($"{certificates.Count} certificates downloaded in {DateTime.Now - start}");

            return certificates;
        }

        private async Task<(int? ResumeToken, string? Kid, byte[] Certificate)> FetchCertificate(int? resumeToken, CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, CertUpdateUrl);
                if (resumeToken != null)
                    request.Headers.Add(HeaderResumeToken, resumeToken.ToString());

                var response = await _httpClient.SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsByteArrayAsync();

                    int? newToken = null;
                    if (response.Headers.TryGetValues(HeaderResumeToken, out var newTokens))
                    {
                        var newTokenStr = newTokens.SingleOrDefault();
                        newToken = string.IsNullOrEmpty(newTokenStr) ? null : int.Parse(newTokenStr);
                    }

                    string? kid = null;
                    if (response.Headers.TryGetValues(HeaderKid, out var newKids))
                    {
                        kid = newKids.SingleOrDefault();
                    }

                    return (newToken, kid, content);
                }

                throw new Exception($"The remote server responded with code {response.StatusCode}: {response.ReasonPhrase}");
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, $"Error while updating certificate from server (resume token: {resumeToken}): {ex.Message}");
                throw;
            }
        }

        private IDictionary<string, string[]> ParseCertSubject(string subject)
        {
            try
            {
                var entries = subject.Split(',')
                    .Select(r =>
                    {
                        if (!r.Contains('='))
                        {

                            return new KeyValuePair<string, string>(r.Trim(), null);
                        }
                        var idx = r.IndexOf('=');
                        return new KeyValuePair<string, string>(r.Remove(idx).Trim(),
                            r.Substring(idx + 1).Trim());
                    });

                return new ReadOnlyDictionary<string, string[]>(
                    entries.GroupBy(r => r.Key)
                    .ToDictionary(r => r.Key, r => r.Select(g => g.Value).ToArray()));
            }
            catch (Exception e)
            {
                Logger?.LogError(e, $"Error while parsing certificate subject: {e.Message}");
                throw;
            }
        }

        private string GetTrustListFilePath()
        {
            return Path.Combine(Options.BasePath, Options.TrustListFileName);
        }

        
#endregion
    }
}
