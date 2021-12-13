using DgcReader.Exceptions;
using DgcReader.RuleValidators.Germany.CovpassDgcCertlogic.Data;
using DgcReader.RuleValidators.Germany.Models.Rules;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Germany.Providers
{
    internal class RulesProvider : GermanyValueSetProviderBase<RulesList, string>
    {
        private readonly RuleIdentifiersProvider ruleIdentifiersProvider;

        public RulesProvider(HttpClient httpClient,
            DgcGermanRulesValidatorOptions options,
            RuleIdentifiersProvider ruleIdentifiersProvider,
            ILogger? logger)
            : base(httpClient, options, logger)
        {
            this.ruleIdentifiersProvider = ruleIdentifiersProvider;
        }

        protected override string GetFileName(string countryCode)
        {
            return $"rules-{countryCode}.json";
        }

        protected override string GetCacheFolder()
        {
            return Path.Combine(base.GetCacheFolder(), "Rules");
        }

        protected override async Task<RulesList?> GetValuesFromServer(string countryCode, CancellationToken cancellationToken = default)
        {
            try
            {
                Logger?.LogDebug($"Fetching rules for {countryCode}...");
                var rulesList = new RulesList()
                {
                    LastUpdate = DateTime.Now,

                };

                var identifiersValueSet = await ruleIdentifiersProvider.GetValueSet(cancellationToken);
                if (identifiersValueSet == null)
                    throw new DgcException($"Can not get a valid rules identifiers list");

                var identifiers = identifiersValueSet.Identifiers
                    .Where(r => r.Country.Equals(countryCode, StringComparison.InvariantCultureIgnoreCase))
                    .ToArray();

                Logger?.LogDebug($"Getting rules for {identifiers.Count()} identifiers");

                var rules = new List<RuleEntry>();
                foreach (var identifier in identifiers)
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
            catch (Exception ex)
            {
                Logger?.LogError(ex, $"Error while getting rules for {countryCode} from server: {ex.Message}");
                throw;
            }
        }

        private async Task<RuleEntry> FetchRuleEntry(string countryCode, string hash, CancellationToken cancellationToken = default)
        {
            Logger?.LogDebug($"Fetching rule for country {countryCode} - hash {hash}");
            var url = $"{Const.BaseUrl}/rules/{countryCode}/{hash}";

            var response = await HttpClient.GetAsync(url, cancellationToken);
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

    }
}
