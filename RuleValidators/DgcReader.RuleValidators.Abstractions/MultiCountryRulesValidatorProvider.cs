using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DgcReader.RuleValidators.Abstractions.Interfaces;
using System.Collections.Generic;
using System.Linq;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Abstractions
{
    /// <summary>
    /// Base class for implementing a rules validator optimized for being used by multiple threads, supporting multiple countries.
    /// It expose methods for managing the list of supported countries
    /// </summary>
    /// <typeparam name="TCountryCodesList">The type of the object holding the list of supported countries</typeparam>
    /// <typeparam name="TRules">The type of object holding the list of rules for a country</typeparam>
    public abstract class MultiCountryRulesValidatorProvider<TCountryCodesList, TRules>
        : ThreadsafeRulesValidatorProvider<TRules>
    {

        private TCountryCodesList? _currentList = default;
        private DateTimeOffset _lastRefreshAttempt;

        /// <summary>
        /// Semaphore controlling access to <see cref="_refreshTask"/>
        /// </summary>
        private readonly SemaphoreSlim _refreshTaskSemaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// The task that is currently executing the <see cref="RefreshSupportedCountriesMethod(CancellationToken)"/> method
        /// </summary>
        private Task<TCountryCodesList?>? _refreshTask = null;
        private CancellationTokenSource? _refreshTaskCancellation;

        /// <summary>
        /// Semaphore controlling access to <see cref="_currentList"/>
        /// </summary>
        private readonly SemaphoreSlim _currentCacheSemaphore = new SemaphoreSlim(1, 1);


        /// <inheritdoc/>
        protected MultiCountryRulesValidatorProvider(IRuleValidatorBaseOptions? options, ILogger? logger)
            : base(options, logger)
        {
        }

        /// <inheritdoc/>
        public override async Task<IEnumerable<string>> GetSupportedCountries(CancellationToken cancellationToken = default)
        {
            var list = await GetSupportedCountriesContainer(cancellationToken);
            if (list == null)
                return Enumerable.Empty<string>();

            return GetCountryCodes(list);
        }

        #region Methods to be implemented for supported countries

        /// <summary>
        /// Executes the request to the server for getting the updated list of supported countries.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected abstract Task<TCountryCodesList?> GetSupportedCountriesFromServer(CancellationToken cancellationToken = default);

        /// <summary>
        /// If the provider supports a cache for the rules (eg. a file), executest the operations needed to load the required data
        /// This method is called when <see cref="GetSupportedCountries"/> is called and the current rules are not loaded (null)
        /// </summary>
        /// <returns></returns>
        protected virtual Task<TCountryCodesList?> LoadSupportedCountriesCache(CancellationToken cancellationToken = default) => Task.FromResult<TCountryCodesList?>(default);

        /// <summary>
        /// Store the updated rules to the cache of the provider (eg. to a file)
        /// </summary>
        /// <param name="countryCodes"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected virtual Task UpdateSupportedCountriesCache(TCountryCodesList countryCodes, CancellationToken cancellationToken = default) => Task.FromResult(0);

        /// <summary>
        /// Returns the download date of the specified rules
        /// </summary>
        /// <param name="rules"></param>
        /// <returns></returns>
        protected abstract DateTimeOffset GetSupportedCountriesLastUpdate(TCountryCodesList rules);

        /// <summary>
        /// Method that returns the list of country codes from its container
        /// </summary>
        /// <param name="cache"></param>
        /// <returns></returns>
        protected abstract IEnumerable<string> GetCountryCodes(TCountryCodesList cache);
        #endregion

        /// <summary>
        /// Returns the object containing the list of supported country codes
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected async Task<TCountryCodesList?> GetSupportedCountriesContainer(CancellationToken cancellationToken = default)
        {

            // Reading rules from cache if the provider supports it
            await _currentCacheSemaphore.WaitAsync(cancellationToken);
            try
            {
                // If not loaded, try to load from file
                if (_currentList == null)
                {
                    _currentList = await LoadSupportedCountriesCache(cancellationToken);
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                _currentCacheSemaphore.Release();
            }

            // Checking validity of the rules:
            // If is null or expired, refresh
            var rules = _currentList;
            Task<TCountryCodesList?>? refreshTask = null;
            if (rules == null)
            {
                Logger?.LogInformation($"Rules not loaded, refreshing from server");

                // If not present, always try to refresh the list
                refreshTask = await GetRefreshTask(cancellationToken);
            }
            else if (GetSupportedCountriesLastUpdate(rules).Add(Options.RefreshInterval) < DateTime.Now)
            {
                // If refresh interval is expired and the min refresh interval is over, refresh the list
                if (_lastRefreshAttempt.Add(Options.MinRefreshInterval) < DateTime.Now)
                {
                    Logger?.LogInformation($"Rules refresh interval expired, refreshing from server");
                    refreshTask = await GetRefreshTask(cancellationToken);
                }
            }

            if (refreshTask != null)
            {
                if (rules == null)
                {
                    Logger?.LogInformation($"No rules loaded in memory, waiting for refresh to complete");
                    return await refreshTask;
                }
                else if (Options.UseAvailableRulesWhileRefreshing == false)
                {
                    // If UseAvailableRulesWhileRefreshing, always wait for the task to complete
                    Logger?.LogInformation($"Rules are expired, waiting for refresh to complete");
                    return await refreshTask;
                }
            }
            return rules;
        }

        #region Private

        /// <summary>
        /// Private method that executes the download of the rules as implemented by <see cref="GetSupportedCountriesFromServer(CancellationToken)"/>, returning them as result.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<TCountryCodesList?> RefreshSupportedCountriesMethod(CancellationToken cancellationToken = default)
        {
            try
            {
                _lastRefreshAttempt = DateTimeOffset.Now;

                var list = await GetSupportedCountriesFromServer(cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();
                await _currentCacheSemaphore.WaitAsync(cancellationToken);
                try
                {
                    _currentList = list;
                }
                catch (Exception e)
                {
                    Logger?.LogError(e, $"Error updating currentList: {e.Message}");
                }
                finally
                {
                    _currentCacheSemaphore.Release();
                }

                // Try saving file
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    await UpdateSupportedCountriesCache(list, cancellationToken);
                }
                catch (Exception e)
                {
                    Logger?.LogError(e, $"Error while updating supported countries list cache: {e.Message}");
                }

                return list;
            }
            catch (OperationCanceledException e)
            {
                Logger?.LogWarning($"{nameof(RefreshSupportedCountriesMethod)} task canceled: {e.Message}");
                return default;
            }
            catch (Exception e)
            {
                Logger?.LogError(e, $"Error refreshing supported countries list from server: {e.Message}");
                return default;
            }
        }

        /// <summary>
        /// If not already started, starts a new task for refreshing the rules from the remote server
        /// </summary>
        /// <returns></returns>
        private async Task<Task<TCountryCodesList?>> GetRefreshTask(CancellationToken cancellationToken = default)
        {
            await _refreshTaskSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (_refreshTask == null)
                {
                    _refreshTask = Task.Run(async () =>
                    {
                        try
                        {
                            _refreshTaskCancellation = new CancellationTokenSource();
                            return await RefreshSupportedCountriesMethod(_refreshTaskCancellation.Token);
                        }
                        finally
                        {
                            await _refreshTaskSemaphore.WaitAsync(_refreshTaskCancellation?.Token ?? default);
                            try
                            {
                                _refreshTask = null;
                                _refreshTaskCancellation?.Dispose();
                                _refreshTaskCancellation = null;
                            }
                            catch (Exception e)
                            {
                                Logger?.LogError(e, $"Error while checking refresh semaphore: {e.Message}");
                            }
                            finally
                            {
                                _refreshTaskSemaphore.Release();
                            }

                        }
                    });
                }
                return _refreshTask;
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

        private void CancelRefreshTaskExecution()
        {
            if (_refreshTaskCancellation == null)
                return;

            if (!_refreshTaskCancellation.IsCancellationRequested &&
                _refreshTaskCancellation.Token.CanBeCanceled)
            {
                Logger?.LogWarning($"Requesting cancellation for the RefreshListTask");
                _refreshTaskCancellation.Cancel();
            }
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            base.Dispose();
            CancelRefreshTaskExecution();
        }
        #endregion
    }

    /// <inheritdoc cref="MultiCountryRulesValidatorProvider{TCountryCodesList, TRules}"/>
    public abstract class MultiCountryRulesValidatorProvider<TCountryCodesList, TRules, TOptions> : MultiCountryRulesValidatorProvider<TCountryCodesList, TRules>
        where TRules : class
        where TOptions : class, IRuleValidatorBaseOptions, new()
    {
        /// <inheritdoc cref="ThreadsafeRulesValidatorProvider{TRulesList}.Options"/>
        public new TOptions Options => (TOptions)base.Options;

        /// <inheritdoc />
        protected MultiCountryRulesValidatorProvider(TOptions? options, ILogger? logger)
            : base(options ?? new TOptions(), logger)
        {

        }
    }
}
