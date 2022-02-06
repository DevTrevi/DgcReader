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
using Microsoft.Extensions.Options;
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

        private static readonly string ProviderDataFolder = Path.Combine("DgcReaderData", "TrustLists", "Italy");
        private const string FileName = "trustlist-it.json";

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


        #region Implementation of ThreadsafeValueSetProvider
        /// <inheritdoc/>
        protected override async Task<ITrustList?> GetValuesFromServer(CancellationToken cancellationToken = default)
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

                    var certData = X509CertificatesUtils.GetCertificateData(data.Key, data.Value, Logger);

                    if (Options.SaveCertificate)
                    {
                        certData.Certificate = data.Value;
                    }

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
        protected override Task<ITrustList?> LoadFromCache(CancellationToken cancellationToken = default)
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
                    return results ?? new string[0];
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
                if (result.Kid != null && !string.IsNullOrEmpty(result.Kid) && result.Certificate != null)
                {
                    if (validKeys.Contains(result.Kid))
                    {
                        if (certificates.Keys.Contains(result.Kid))
                        {
                            // Sometimes the backend returns the same certificate multiple times
                            var len = certificates[result.Kid].Length;
                            if (len == result.Certificate.Length)
                                Logger?.LogInformation($"Certificate with kid {result.Kid} already exist with same length. Skip");
                            else
                                Logger?.LogWarning($"Certificate with kid {result.Kid} (size {result.Certificate.Length}) already exist (length {len}). Skip");
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
                    var stringContent = await response.Content.ReadAsStringAsync();
                    var content = Convert.FromBase64String(stringContent);
                 
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

        private string GetCacheFolder() => Path.Combine(Options.BasePath, ProviderDataFolder);
        private string GetCacheFilePath() => Path.Combine(GetCacheFolder(), FileName);


        #endregion
    }
}
