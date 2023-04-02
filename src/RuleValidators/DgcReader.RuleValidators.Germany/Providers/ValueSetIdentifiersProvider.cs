using DgcReader.RuleValidators.Germany.Models.ValueSets;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Germany.Providers;

internal class ValueSetIdentifiersProvider : GermanyValueSetProviderBase<ValueSetIdentifiers>
{
    public ValueSetIdentifiersProvider(HttpClient httpClient, DgcGermanRulesValidatorOptions options, ILogger? logger)
        : base(httpClient, options, logger)
    {
    }

    protected override string GetFileName()
    {
        return "identifiers.json";
    }
    protected override string GetCacheFolder()
    {
        return Path.Combine(base.GetCacheFolder(), "ValueSets");
    }

    protected override async Task<ValueSetIdentifiers?> GetValuesFromServer(CancellationToken cancellationToken = default)
    {
        try
        {
            var start = DateTime.Now;
            Logger?.LogDebug("Fetching value set identifiers...");
            var response = await HttpClient.GetAsync($"{Const.BaseUrl}/valuesets", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();

                var results = JsonConvert.DeserializeObject<ValueSetIdentifier[]>(content, DgcGermanRulesValidator.JsonSerializerSettings);

                if (results == null)
                    throw new Exception("Error wile deserializing value set identifiers from server");

                Logger?.LogInformation($"{results.Length} value set identifiers read in {DateTime.Now - start}");
                return new ValueSetIdentifiers
                {
                    LastUpdate = start,
                    Identifiers = results
                };
            }

            throw new Exception($"The remote server responded with code {response.StatusCode}: {response.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, $"Error while getting value set identifiers from server: {ex.Message}");
            throw;
        }
    }
}
