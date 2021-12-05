using DgcReader.RuleValidators.Germany.CovpassDgcCertlogic.Data;
using DgcReader.RuleValidators.Germany.Models.Rules;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Germany.Providers
{

    internal class RuleIdentifiersProvider : GermanyValueSetProviderBase<RulesIdentifiers>
    {
        public RuleIdentifiersProvider(HttpClient httpClient, DgcGermanRulesValidatorOptions options, ILogger? logger)
            : base(httpClient, options, logger)
        {
        }

        protected override string GetFileName()
        {
            return "rule-identifiers.json";
        }
        protected override string GetCacheFolder()
        {
            return Path.Combine(base.GetCacheFolder(), "Rules");
        }

        protected override async Task<RulesIdentifiers?> GetValuesFromServer(CancellationToken cancellationToken = default)
        {
            try
            {
                var start = DateTime.Now;
                Logger?.LogDebug("Fetching rules identifiers...");
                var response = await HttpClient.GetAsync($"{Const.BaseUrl}/rules", cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();

                    var results = JsonConvert.DeserializeObject<RuleIdentifier[]>(content);

                    if (results == null)
                        throw new Exception("Error wile deserializing rules identifiers from server");

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
                Logger?.LogError(ex, $"Error while getting rules identifiers from server: {ex.Message}");
                throw;
            }
        }
    }
}
