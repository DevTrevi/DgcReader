using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DgcReader.Providers.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.Providers.Abstractions;

/// <summary>
/// Base class for implementing valueset providers, optimized for downloading values on a single background task for each valueset
/// </summary>
/// <typeparam name="T">The valueset type</typeparam>
/// <typeparam name="TKey">The partitioning key, identifiyng each valueset</typeparam>
public abstract class ThreadsafeMultiValueSetProvider<T, TKey> : IMultiValueSetProvider<T, TKey>, IDisposable
{
    /// <summary>
    /// Logger instance
    /// </summary>
    protected readonly ILogger? Logger;

    private DateTimeOffset _lastRefreshAttempt;

    private readonly IDictionary<TKey, T> _currentValueSets = new Dictionary<TKey, T>();

    /// <summary>
    /// Semaphore controlling access to <see cref="_currentValueSets"/>
    /// </summary>
    private readonly SemaphoreSlim _currentValueSetsSemaphore = new SemaphoreSlim(1, 1);


    /// <summary>
    /// The task that is currently executing the <see cref="RefreshValueSet"/> method
    /// </summary>
    private readonly IDictionary<TKey, SingleTaskRunner<T?>> _refreshTasks = new Dictionary<TKey, SingleTaskRunner<T?>>();

    /// <summary>
    /// Semaphore controlling access to <see cref="_refreshTasks"/>
    /// </summary>
    private readonly SemaphoreSlim _refreshTaskSemaphore = new SemaphoreSlim(1, 1);

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger">The logger instance</param>
    protected ThreadsafeMultiValueSetProvider(ILogger? logger)
    {
        Logger = logger;
    }

    /// <inheritdoc/>
    public virtual async Task<T?> GetValueSet(TKey key, CancellationToken cancellationToken = default)
    {
        T? currentValueSet = default;

        // Reading valueset from cache if the provider supports it
        await _currentValueSetsSemaphore.WaitAsync(cancellationToken);
        try
        {
            _currentValueSets.TryGetValue(key, out currentValueSet);

            // If not loaded, try to load from cache
            if (currentValueSet == null)
            {
                currentValueSet = await LoadFromCache(key, cancellationToken);
                if (currentValueSet != null)
                    _currentValueSets.Add(key, currentValueSet);
            }
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            _currentValueSetsSemaphore.Release();
        }

        // Checking validity of the valueset:
        // If is null or expired, refresh
        var valueSet = currentValueSet;

        DateTimeOffset? expiration = null;
        if (valueSet != null)
            expiration = GetExpiration(valueSet);

        Task<T?>? refreshTask = null;
        if (valueSet == null)
        {
            Logger?.LogInformation($"{GetValuesetName(key)} not loaded, refreshing from server");

            // If not present, always try to refresh the list
            refreshTask = await GetRefreshTask(key, cancellationToken);
        }
        else if (NeedsUpdate(key, valueSet))
        {
            // Update needed

            // Optional: try to reload cache first
            if (TryReloadFromCacheWhenExpired)
            {
                // Try to reload values from cache before downloading from server
                var fromCache = await LoadFromCache(key, cancellationToken);

                // Check if loaded values are already updated
                if (fromCache != null && !NeedsUpdate(key, fromCache, false))
                {
                    Logger?.LogInformation($"{GetValuesetName(key)} reloaded from cache is up to date. No need to refresh from server");
                    await _currentValueSetsSemaphore.WaitAsync(cancellationToken);
                    try
                    {
                        _currentValueSets[key] = fromCache;
                        return fromCache;
                    }
                    finally
                    {
                        _currentValueSetsSemaphore.Release();
                    }
                }
            }

            // Starting request to the remote server if the min refresh interval is over
            if (_lastRefreshAttempt.Add(MinRefreshInterval) < DateTime.Now)
            {
                Logger?.LogInformation($"{GetValuesetName(key)} refreshing from server");
                refreshTask = await GetRefreshTask(key, cancellationToken);
            }
        }

        if (refreshTask != null)
        {
            if (valueSet == null)
            {
                Logger?.LogInformation($"No values loaded in memory for {GetValuesetName(key)}, waiting for refresh to complete");
                return await refreshTask;
            }
            else if (UseAvailableValuesWhileRefreshing == false)
            {
                // If not UseAvailableRulesWhileRefreshing, always wait for the task to complete
                Logger?.LogInformation($"Values for {GetValuesetName(key)} are expired, waiting for refresh to complete");

                try
                {
                    valueSet = await refreshTask;
                }
                catch (Exception e)
                {
                    if (valueSet != null)
                    {
                        var lastUpdate = GetLastUpdate(valueSet);
                        Logger?.LogWarning(e, $"Can not refresh {GetValuesetName(key)} from remote server: {e.Message}. Current values downloaded on {lastUpdate} will be used");
                    }
                    else
                    {
                        Logger?.LogError(e, $"Can not refresh {GetValuesetName(key)} from remote server. No values available to be used");
                    }
                }
            }
        }



        return valueSet;
    }

    /// <summary>
    /// Method that executes the download of the valueset as implemented by <see cref="GetValuesFromServer"/>,
    /// storing them in cache.
    /// The method starts a new task, waiting for the previous to finish
    /// </summary>
    /// <param name="key">The key of the valueset to be refreshed</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public virtual async Task<T?> RefreshValueSet(TKey key, CancellationToken cancellationToken = default)
    {
        try
        {
            _lastRefreshAttempt = DateTimeOffset.Now;

            var valueSet = await GetValuesFromServer(key, cancellationToken);

            if (valueSet == null)
            {
                Logger?.LogWarning($"Null {GetValuesetName(key)} returned from server");
                return valueSet;
            }

            // Try to update the in-memory value
            await _currentValueSetsSemaphore.WaitAsync(cancellationToken);
            try
            {
                _currentValueSets[key] = valueSet;
            }
            catch (Exception e)
            {
                Logger?.LogError(e, $"Error updating currentList: {e.Message}");
            }
            finally
            {
                _currentValueSetsSemaphore.Release();
            }

            // Try to update the cache
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await UpdateCache(key, valueSet, cancellationToken);
            }
            catch (Exception e)
            {
                Logger?.LogError(e, $"Error while updating {GetValuesetName(key)} cache: {e.Message}");
            }
            return valueSet;
        }
        catch (OperationCanceledException e)
        {
            Logger?.LogWarning($"{nameof(RefreshValueSet)} task canceled: {e.Message}");
            return default;
        }
        catch (Exception e)
        {
            Logger?.LogError(e, $"Error refreshing {GetValuesetName(key)} from server: {e.Message}");
            throw;
        }
    }

    // Methods to be implemented

    /// <summary>
    /// Executes the request to the server for getting the updated valueset
    /// </summary>
    /// <param name="key">The key of the valueset to be downloaded</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected abstract Task<T?> GetValuesFromServer(TKey key, CancellationToken cancellationToken = default);

    /// <summary>
    /// If the provider supports a cache for the values (i.e. a file), tries to load them
    /// </summary>
    /// <param name="key">The key of the valueset to be loaded</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected virtual Task<T?> LoadFromCache(TKey key, CancellationToken cancellationToken = default) => Task.FromResult<T?>(default);

    /// <summary>
    /// Store the updated valueset to the cache
    /// </summary>
    /// <param name="key">The key of the valueset to be updated</param>
    /// <param name="valueSet"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected virtual Task UpdateCache(TKey key, T valueSet, CancellationToken cancellationToken = default) => Task.FromResult(0);

    /// <summary>
    /// Returns the last update datetime of the valueset
    /// </summary>
    /// <param name="valueSet"></param>
    /// <returns></returns>
    protected abstract DateTimeOffset GetLastUpdate(T valueSet);

    /// <summary>
    /// Return the expiration date of the valueset if available
    /// </summary>
    /// <param name="valueSet"></param>
    /// <returns></returns>
    protected virtual DateTimeOffset? GetExpiration(T valueSet) => null;

    /// <summary>
    /// Returns the name of the valueset that will be used in logs
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    protected virtual string GetValuesetName(TKey key) => $"ValueSet {key}".Trim();

    /// <summary>
    /// Duration of the stored valueset before a refresh is requested. Default is 24 hours
    /// </summary>
    public virtual TimeSpan RefreshInterval => TimeSpan.FromHours(24);

    /// <summary>
    /// If specified, prevent that every request causes a refresh attempt when the current valueset is expired. Default is 5 minutes
    /// </summary>
    public virtual TimeSpan MinRefreshInterval => TimeSpan.FromMinutes(5);

    /// <summary>
    /// If true, allows to use the current values without waiting for the refresh task to complete.
    /// Otherwise, if the values are expired, every request will wait untill the refresh task completes.
    /// </summary>
    public virtual bool UseAvailableValuesWhileRefreshing => true;

    /// <summary>
    /// If true, try to reload values from cache before downloading from the remote server.
    /// This can be useful if values are refreshed by a separate process, i.e. when the same valueset cached file is shared by
    /// multiple instances for reading
    /// </summary>
    public virtual bool TryReloadFromCacheWhenExpired => false;
    

    // Private

    /// <summary>
    /// If not already started, starts a new task for refreshing the rules from the remote server
    /// </summary>
    /// <returns></returns>
    private async Task<Task<T?>> GetRefreshTask(TKey key, CancellationToken cancellationToken = default)
    {
        await _refreshTaskSemaphore.WaitAsync(cancellationToken);
        try
        {
            SingleTaskRunner<T?>? runner = null;
            if (!_refreshTasks.TryGetValue(key, out runner))
            {
                runner = new SingleTaskRunner<T?>(ct => RefreshValueSet(key, ct), Logger);
                _refreshTasks.Add(key, runner);
            }

            return await runner.RunSingleTask();

        }
        catch (Exception e)
        {
            Logger?.LogError(e, $"Error while getting Refresh Task: {e.Message}");
            throw;
        }
        finally
        {
            _refreshTaskSemaphore.Release();
        }

    }

    /// <summary>
    /// Check if the valueset needs to be updated, checking last update and expiration
    /// </summary>
    /// <param name="key"></param>
    /// <param name="valueSet"></param>
    /// <param name="writeLog">If true, write to log why values should be updated</param>
    /// <returns></returns>
    private bool NeedsUpdate(TKey key, T? valueSet, bool writeLog = true)
    {
        DateTimeOffset? expiration = null;
        if (valueSet != null)
            expiration = GetExpiration(valueSet);

        if (valueSet == null)
        {
            if (writeLog)
                Logger?.LogInformation($"{GetValuesetName(key)} not loaded, update required");

            // If not present, always try to refresh the list
            return true;
        }

        if (GetLastUpdate(valueSet).Add(RefreshInterval) < DateTime.Now)
        {
            if (writeLog)
                Logger?.LogInformation($"{GetValuesetName(key)} refresh interval expired");
            return true;
        }
        else if (expiration != null &&
                 expiration < DateTime.Now)
        {
            if (writeLog)
                Logger?.LogInformation($"{GetValuesetName(key)} expired");
            return true;
        }
        return false;
    }

    /// <inheritdoc/>
    public virtual void Dispose()
    {
        foreach (var runner in _refreshTasks.Values)
        {
            runner.Dispose();
        }
    }
}
