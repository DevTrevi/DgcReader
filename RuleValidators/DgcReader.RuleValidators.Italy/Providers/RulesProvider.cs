using DgcReader.Providers.Abstractions;
using DgcReader.RuleValidators.Italy.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DgcReader.RuleValidators.Italy.Providers
{
    internal class RulesProvider : ThreadsafeValueSetProvider<RulesList>
    {
        private static readonly string ProviderDataFolder = Path.Combine("DgcReaderData", "RuleValidators", "Italy");
        private const string FileName = "rules-it.json";

        private readonly HttpClient _httpClient;
        private readonly DgcItalianRulesValidatorOptions Options;

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters = {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.None }
            },
        };

        public RulesProvider(HttpClient httpClient,
            DgcItalianRulesValidatorOptions options,
            ILogger? logger)
            : base(logger)
        {
            _httpClient = httpClient;
            Options = options;
        }



        protected override DateTimeOffset GetLastUpdate(RulesList rules) => rules.LastUpdate;

        protected override async Task<RulesList?> GetValuesFromServer(CancellationToken cancellationToken = default)
        {
            Logger?.LogInformation("Refreshing rules from server...");
            var rulesList = new RulesList()
            {
                LastUpdate = DateTime.Now,
            };
            var rules = await FetchSettings(cancellationToken);


            rulesList.Rules = rules.ToArray();

            return rulesList;
        }

        protected override Task<RulesList?> LoadFromCache(CancellationToken cancellationToken = default)
        {
            var filePath = GetCacheFilePath();
            RulesList? rulesList = null;
            try
            {
                if (File.Exists(filePath))
                {
                    Logger?.LogInformation($"Loading rules from file");
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
                Logger?.LogInformation($"Rules list expired for MaxFileAge, deleting list and file");
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
                return Task.FromResult<RulesList?>(null);
            }

            return Task.FromResult<RulesList?>(rulesList);
        }

        protected override Task UpdateCache(RulesList rules, CancellationToken cancellationToken = default)
        {
            var filePath = GetCacheFilePath();
            if (!Directory.Exists(GetCacheFolder()))
                Directory.CreateDirectory(GetCacheFolder());
            var json = JsonConvert.SerializeObject(rules, JsonSettings);

            File.WriteAllText(filePath, json);
            return Task.FromResult(0);
        }

        public override TimeSpan RefreshInterval => Options.RefreshInterval;
        public override TimeSpan MinRefreshInterval => Options.MinRefreshInterval;
        public override bool UseAvailableValuesWhileRefreshing => Options.UseAvailableValuesWhileRefreshing;

        private async Task<RuleSetting[]> FetchSettings(CancellationToken cancellationToken = default)
        {
            try
            {
                var start = DateTime.Now;
                Logger?.LogDebug("Fetching rules...");
                var response = await _httpClient.GetAsync(SdkConstants.ValidationRulesUrl, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();

                    var results = JsonConvert.DeserializeObject<RuleSetting[]>(content);

                    if (results == null)
                        throw new Exception("Error wile deserializing rules from server");

                    Logger?.LogInformation($"{results.Length} rules read in {DateTime.Now - start}");
                    return results;
                }

                throw new Exception($"The remote server responded with code {response.StatusCode}: {response.ReasonPhrase}");
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, $"Error while getting rules from server: {ex.Message}");
                throw;
            }
        }

        private string GetCacheFolder() => Path.Combine(Options.BasePath, ProviderDataFolder);
        private string GetCacheFilePath() => Path.Combine(GetCacheFolder(), FileName);

    }
}
