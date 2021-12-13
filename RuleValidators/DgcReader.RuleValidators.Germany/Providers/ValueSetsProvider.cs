using DgcReader.RuleValidators.Germany.Models.ValueSets;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Germany.Providers
{
    internal class ValueSetsProvider : GermanyValueSetProviderBase<ValueSet, string>
    {
        private readonly ValueSetIdentifiersProvider valueSetIdentifiersProvider;

        public ValueSetsProvider(HttpClient httpClient,
            DgcGermanRulesValidatorOptions options,
            ValueSetIdentifiersProvider valueSetIdentifiersProvider,
            ILogger? logger)
            : base(httpClient, options, logger)
        {
            this.valueSetIdentifiersProvider = valueSetIdentifiersProvider;
        }

        protected override string GetFileName(string key)
        {
            return $"{key}.json";
        }

        protected override string GetCacheFolder()
        {
            return Path.Combine(base.GetCacheFolder(), "ValueSets");
        }

        protected override async Task<ValueSet?> GetValuesFromServer(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                var start = DateTime.Now;
                Logger?.LogDebug($"Fetching value set {id}...");

                var identifiersValueSet = await valueSetIdentifiersProvider.GetValueSet(cancellationToken);
                var identifier = identifiersValueSet?.Identifiers.SingleOrDefault(r=> r.Id == id);

                if (identifier == null)
                {
                    Logger?.LogWarning($"Valueset identifier id {id} was not found");
                    return null;
                }

                var response = await HttpClient.GetAsync($"{Const.BaseUrl}/valuesets/{identifier.Hash}", cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();

                    var results = JsonConvert.DeserializeObject<ValueSet>(content);

                    if (results == null)
                        throw new Exception($"Error wile deserializing value set {id} from server");

                    Logger?.LogInformation($"Value set {id} read in {DateTime.Now - start}");

                    results.LastUpdate = start;
                    return results;
                }

                throw new Exception($"The remote server responded with code {response.StatusCode}: {response.ReasonPhrase}");
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, $"Error while getting value set {id} from server: {ex.Message}");
                throw;
            }
        }
    }
}
