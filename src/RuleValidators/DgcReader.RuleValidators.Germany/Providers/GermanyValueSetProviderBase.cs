using DgcReader.Providers.Abstractions;
using DgcReader.RuleValidators.Germany.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Germany.Providers;

internal abstract class GermanyValueSetProviderBase<T> : ThreadsafeValueSetProvider<T>
    where T: ValueSetBase
{
    protected readonly HttpClient HttpClient;
    protected readonly DgcGermanRulesValidatorOptions Options;


    public GermanyValueSetProviderBase(HttpClient httpClient, DgcGermanRulesValidatorOptions options, ILogger? logger)
        : base (logger)
    {
        HttpClient = httpClient;
        Options = options;
    }

    protected virtual string GetCacheFolder() => Path.Combine(Options.BasePath, Const.ProviderDataFolder);

    protected abstract string GetFileName();

    private string GetFilePath()
    {
        return Path.Combine(GetCacheFolder(), GetFileName());
    }

    protected override DateTimeOffset GetLastUpdate(T valueSet)
    {
        return valueSet.LastUpdate;
    }

    protected override Task<T?> LoadFromCache(CancellationToken cancellationToken = default)
    {
        var filePath = GetFilePath();
        T? valueSet = null;
        try
        {
            if (File.Exists(filePath))
            {
                Logger?.LogInformation($"Loading rules identifiers from file");
                var fileContent = File.ReadAllText(filePath);
                valueSet = JsonConvert.DeserializeObject<T>(fileContent, DgcGermanRulesValidator.JsonSerializerSettings);
            }
        }
        catch (Exception e)
        {
            Logger?.LogError(e, $"Error reading rules identifiers from file: {e.Message}");
        }

        // Check max age and delete file
        if (valueSet != null &&
            valueSet.LastUpdate.Add(Options.MaxFileAge) < DateTime.Now)
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
            return Task.FromResult<T?>(null);
        }

        return Task.FromResult<T?>(valueSet);
    }

    /// <inheritdoc/>
    protected override Task UpdateCache(T valueSet, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(GetCacheFolder()))
            Directory.CreateDirectory(GetCacheFolder());

        var filePath = GetFilePath();
        var json = JsonConvert.SerializeObject(valueSet, DgcGermanRulesValidator.JsonSerializerSettings);

        File.WriteAllText(filePath, json);
        return Task.FromResult(0);
    }


    public override TimeSpan RefreshInterval => Options.RefreshInterval;
    public override TimeSpan MinRefreshInterval => Options.MinRefreshInterval;
    public override bool UseAvailableValuesWhileRefreshing => Options.UseAvailableValuesWhileRefreshing;
    public override bool TryReloadFromCacheWhenExpired => Options.TryReloadFromCacheWhenExpired;
}

internal abstract class GermanyValueSetProviderBase<T, TKey> : ThreadsafeMultiValueSetProvider<T, TKey>
    where T : ValueSetBase
{
    protected readonly HttpClient HttpClient;
    protected readonly DgcGermanRulesValidatorOptions Options;

    protected static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
    {
        MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
        DateParseHandling = DateParseHandling.None,
        Converters = {
            new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.None }
        },
    };

    public GermanyValueSetProviderBase(HttpClient httpClient, DgcGermanRulesValidatorOptions options, ILogger? logger)
        : base(logger)
    {
        HttpClient = httpClient;
        Options = options;
    }

    protected virtual string GetCacheFolder() => Path.Combine(Options.BasePath, Const.ProviderDataFolder);

    protected abstract string GetFileName(TKey key);

    private string GetFilePath(TKey key)
    {
        return Path.Combine(GetCacheFolder(), GetFileName(key));
    }

    protected override DateTimeOffset GetLastUpdate(T valueSet)
    {
        return valueSet.LastUpdate;
    }


    protected override Task<T?> LoadFromCache(TKey key, CancellationToken cancellationToken = default)
    {
        var filePath = GetFilePath(key);
        T? valueSet = null;
        try
        {
            if (File.Exists(filePath))
            {
                Logger?.LogInformation($"Loading rules identifiers from file");
                var fileContent = File.ReadAllText(filePath);
                valueSet = JsonConvert.DeserializeObject<T>(fileContent, JsonSettings);
            }
        }
        catch (Exception e)
        {
            Logger?.LogError(e, $"Error reading rules identifiers from file: {e.Message}");
        }

        // Check max age and delete file
        if (valueSet != null &&
            valueSet.LastUpdate.Add(Options.MaxFileAge) < DateTime.Now)
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
            return Task.FromResult<T?>(null);
        }

        return Task.FromResult<T?>(valueSet);
    }

    /// <inheritdoc/>
    protected override Task UpdateCache(TKey key, T valueSet, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(GetCacheFolder()))
            Directory.CreateDirectory(GetCacheFolder());

        var filePath = GetFilePath(key);
        var json = JsonConvert.SerializeObject(valueSet, JsonSettings);

        File.WriteAllText(filePath, json);
        return Task.FromResult(0);
    }
}
