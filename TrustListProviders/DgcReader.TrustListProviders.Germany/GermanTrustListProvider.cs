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
using DgcReader.Exceptions;

#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER || NET47_OR_GREATER
using Microsoft.Extensions.Options;
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


                    var certData = X509CertificatesUtils.GetCertificateData(data, Logger);

                    if (Options.SaveCertificate)
                    {
                        certData.Certificate = data.RawData;
                    }

                    if (Options.SaveSignature)
                    {
                        certData.Signature = data.Signature;
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

                    if (!X509CertificatesUtils.VerifyTrustlistSignature(content, thumbprint))
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

        #endregion
    }
}
