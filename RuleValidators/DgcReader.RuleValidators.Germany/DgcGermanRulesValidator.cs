using GreenpassReader.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DgcReader.Models;
using System.Threading;
using Microsoft.Extensions.Logging;
using DgcReader.Interfaces.RulesValidators;
using DgcReader.Interfaces.BlacklistProviders;
using DgcReader.RuleValidators.Abstractions;
using DgcReader.Exceptions;
using DgcReader.RuleValidators.Germany.Models;
using DgcReader.RuleValidators.Germany.Backend;

#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER || NET47_OR_GREATER
using Microsoft.Extensions.Options;
#endif

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Germany
{


    /// <summary>
    /// Unofficial porting of the German rules from https://github.com/Digitaler-Impfnachweis/covpass-android.
    /// This service is also an implementation of <see cref="IBlacklistProvider"/>
    /// </summary>
    public class DgcGermanRulesValidator : MultiCountryRulesValidatorProvider<RulesIdentifiers, RulesList, DgcGermanRulesValidatorOptions>, IRulesValidator
    {

        /// <summary>
        /// Url used for getting rules and rules identifiers
        /// </summary>
        private const string RulesUrl = "https://distribution.dcc-rules.de/rules/";

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
        /// Constructor
        /// </summary>
        public DgcGermanRulesValidator(HttpClient httpClient,
            DgcGermanRulesValidatorOptions? options = null,
            ILogger<DgcGermanRulesValidator>? logger = null)
            : base(options, logger)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Factory method for creating an instance of <see cref="DgcGermanRulesValidator"/>
        /// whithout using the DI mechanism. Useful for legacy applications
        /// </summary>
        /// <param name="httpClient">The http client instance that will be used for requests to the server</param>
        /// <param name="options">The options for the provider</param>
        /// <param name="logger">Instance of <see cref="ILogger"/> used by the provider (optional).</param>
        /// <returns></returns>
        public static DgcGermanRulesValidator Create(HttpClient httpClient,
            DgcGermanRulesValidatorOptions? options = null,
            ILogger<DgcGermanRulesValidator>? logger = null)
        {
            return new DgcGermanRulesValidator(httpClient, options, logger);
        }

#else
        /// <summary>
        /// Constructor
        /// </summary>
        public DgcGermanRulesValidator(HttpClient httpClient,
            IOptions<DgcGermanRulesValidatorOptions>? options = null,
            ILogger<DgcGermanRulesValidator>? logger = null)
            : base(options?.Value, logger)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Factory method for creating an instance of <see cref="DgcGermanRulesValidator"/>
        /// whithout using the DI mechanism. Useful for legacy applications
        /// </summary>
        /// <param name="httpClient">The http client instance that will be used for requests to the server</param>
        /// <param name="options">The options for the provider</param>
        /// <param name="logger">Instance of <see cref="ILogger"/> used by the provider (optional).</param>
        /// <returns></returns>
        public static DgcGermanRulesValidator Create(HttpClient httpClient,
            DgcGermanRulesValidatorOptions? options = null,
            ILogger<DgcGermanRulesValidator>? logger = null)
        {
            return new DgcGermanRulesValidator(httpClient,
                options == null ? null : Microsoft.Extensions.Options.Options.Create(options),
                logger);
        }
#endif

        #region Implementation of IRulesValidator

        /// <inheritdoc/>
        public override async Task<IRuleValidationResult> GetRulesValidationResult(EuDGC dgc, DateTimeOffset validationInstant, string countryCode = "IT", CancellationToken cancellationToken = default)
        {
            if (!await SupportsCountry(countryCode))
                throw new DgcException($"Rules validation for country {countryCode} is not supported by this provider");

            var result = new DgcRulesValidationResult
            {
                Dgc = dgc,
                ValidationInstant = validationInstant,
            };

            if (result.Dgc == null)
            {
                result.Status = DgcResultStatus.NotEuDCC;
                return result;
            }

            try
            {
                var rulesContainer = await GetRules(countryCode, cancellationToken);
                if (rulesContainer == null)
                    throw new Exception("Unable to get validation rules");

                var rules = rulesContainer.Rules;


                throw new NotImplementedException("TODO: implement rules validation");
            }
            catch (Exception e)
            {
                Logger?.LogError(e, $"Validation failed with error {e.Message}");
                result.Status = DgcResultStatus.NotValid;
            }
            return result;
        }

        #endregion

        #region Implementation of ThreadsafeRulesValidatorProvider

        /// <inheritdoc/>
        protected override async Task<RulesList> GetRulesFromServer(string? countryCode, CancellationToken cancellationToken = default)
        {
            Logger?.LogInformation("Refreshing rules from server...");
            var rulesList = new RulesList()
            {
                LastUpdate = DateTime.Now,

            };

            var identifiersContainer = await GetSupportedCountriesContainer(cancellationToken);
            if (identifiersContainer == null)
                throw new DgcException($"Can not get a valid rules identifiers list");

            var identifiers = identifiersContainer.Identifiers
                .Where(r=>r.Country.Equals(countryCode, StringComparison.InvariantCultureIgnoreCase))
                .ToArray();

            Logger?.LogDebug($"Getting rules for {identifiers.Count()} identifiers");

            var rules = new List<RuleEntry>();
            foreach(var identifier in identifiers)
            {
                try
                {
                    var rule = await FetchRuleEntry(identifier.Country, identifier.Hash, cancellationToken);
                    rules.Add(rule);
                }
                catch (Exception e)
                {
                    Logger?.LogError(e, $"Error getting rule for country {countryCode} - hash {identifier.Hash}: {e.Message}. Rule is skipped");
                }
            }
            rulesList.Rules = rules.ToArray();

            return rulesList;
        }

        /// <inheritdoc/>
        protected override Task<RulesList?> LoadCache(string? countryCode, CancellationToken cancellationToken = default)
        {
            var filePath = GetRulesFilePath(countryCode);
            RulesList? rulesList = null;
            try
            {
                if (File.Exists(filePath))
                {
                    Logger?.LogInformation($"Loading rules for {countryCode} from file");
                    var fileContent = File.ReadAllText(filePath);
                    rulesList = JsonConvert.DeserializeObject<RulesList>(fileContent, JsonSettings);
                }
            }
            catch (Exception e)
            {
                Logger?.LogError(e, $"Error reading trustlist from file: {e.Message}");
            }

            // Check max age and delete file
            if (rulesList != null &&
                rulesList.LastUpdate.Add(Options.MaxFileAge) < DateTime.Now)
            {
                Logger?.LogInformation($"Rules list for {countryCode} expired for MaxFileAge, deleting list and file");
                // File has passed the max age, removing file
                try
                {
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                }
                catch (Exception e)
                {
                    Logger?.LogError(e, $"Error deleting rules list file for {countryCode}: {e.Message}");
                }
                return Task.FromResult<RulesList?>(null);
            }

            return Task.FromResult<RulesList?>(rulesList);
        }

        /// <inheritdoc/>
        protected override Task UpdateCache(RulesList rules, string countryCode, CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(GetCacheFolder()))
                Directory.CreateDirectory(GetCacheFolder());

            var filePath = GetRulesFilePath(countryCode);
            var json = JsonConvert.SerializeObject(rules, JsonSettings);

            File.WriteAllText(filePath, json);
            return Task.FromResult(0);
        }

        /// <inheritdoc/>
        protected override DateTimeOffset GetRulesLastUpdate(RulesList rules) => rules.LastUpdate;

        #endregion

        #region Implementation of MultiCountryRulesValidatorProvider

        /// <inheritdoc/>
        protected override async Task<RulesIdentifiers?> GetSupportedCountriesFromServer(CancellationToken cancellationToken = default)
        {
            try
            {
                var start = DateTime.Now;
                Logger?.LogDebug("Fetching rules identifiers...");
                var response = await _httpClient.GetAsync(RulesUrl, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();

                    var results = JsonConvert.DeserializeObject<RuleIdentifier[]>(content);

                    if (results == null)
                        throw new Exception("Error wile deserializing rules from server");

                    Logger?.LogInformation($"{results.Length} rules read in {DateTime.Now - start}");
                    return new RulesIdentifiers
                    {
                        LastUpdate = start,
                        Identifiers = results
                    };
                }

                throw new Exception($"The remote server responded with code {response.StatusCode}: {response.ReasonPhrase}");
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, $"Error while getting rules from server: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        protected override DateTimeOffset GetSupportedCountriesLastUpdate(RulesIdentifiers cache)
        {
            return cache.LastUpdate;
        }

        /// <inheritdoc/>
        protected override IEnumerable<string> GetCountryCodes(RulesIdentifiers cache)
        {
            return cache.Identifiers.Select(r => r.Country)
                .Distinct().OrderBy(r => r).ToArray();
        }

        /// <inheritdoc/>
        protected override Task<RulesIdentifiers?> LoadSupportedCountriesCache(CancellationToken cancellationToken = default)
        {
            var filePath = GetRulesIdentifiersFilePath();
            RulesIdentifiers? rulesList = null;
            try
            {
                if (File.Exists(filePath))
                {
                    Logger?.LogInformation($"Loading rules identifiers from file");
                    var fileContent = File.ReadAllText(filePath);
                    rulesList = JsonConvert.DeserializeObject<RulesIdentifiers>(fileContent, JsonSettings);
                }
            }
            catch (Exception e)
            {
                Logger?.LogError(e, $"Error reading rules identifiers from file: {e.Message}");
            }

            // Check max age and delete file
            if (rulesList != null &&
                rulesList.LastUpdate.Add(Options.MaxFileAge) < DateTime.Now)
            {
                Logger?.LogInformation($"Rules identifiers list expired for MaxFileAge, deleting list and file");
                // File has passed the max age, removing file
                try
                {
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                }
                catch (Exception e)
                {
                    Logger?.LogError(e, $"Error deleting rules identifiers file: {e.Message}");
                }
                return Task.FromResult<RulesIdentifiers?>(null);
            }

            return Task.FromResult<RulesIdentifiers?>(rulesList);
        }

        /// <inheritdoc/>
        protected override Task UpdateSupportedCountriesCache(RulesIdentifiers countryCodes, CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(GetCacheFolder()))
                Directory.CreateDirectory(GetCacheFolder());

            var filePath = GetRulesIdentifiersFilePath();
            var json = JsonConvert.SerializeObject(countryCodes, JsonSettings);

            File.WriteAllText(filePath, json);
            return Task.FromResult(0);
        }
        #endregion

        #region Private
        private async Task<RuleEntry> FetchRuleEntry(string countryCode, string hash, CancellationToken cancellationToken = default)
        {
            try
            {
                var start = DateTime.Now;
                Logger?.LogDebug($"Fetching rule for country {countryCode} - hash {hash}");
                var url = $"{RulesUrl}{countryCode}/{hash}";

                var response = await _httpClient.GetAsync(url, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();

                    var result = JsonConvert.DeserializeObject<RuleEntry>(content);

                    if (result == null)
                        throw new Exception("Error wile deserializing rule from server");

                    return result;
                }

                throw new Exception($"The remote server responded with code {response.StatusCode}: {response.ReasonPhrase}");
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private string GetCacheFolder() => Path.Combine(Options.BasePath, Options.FolderName);
        private string GetRulesIdentifiersFilePath()
        {
            return Path.Combine(GetCacheFolder(), Options.RulesIdentifiersFileName);
        }

        private string GetRulesFilePath(string? countryCode)
        {
            if (string.IsNullOrEmpty(countryCode))
                countryCode = "undefined";

            return Path.Combine(GetCacheFolder(), $"rules-{countryCode}.json");
        }

        #endregion
    }
}
