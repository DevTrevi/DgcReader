using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DgcReader.RuleValidators.Abstractions.Interfaces;
using DgcReader.Interfaces.RulesValidators;
using GreenpassReader.Models;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Abstractions
{
    /// <summary>
    /// Base class for implementing a rules validator optimized for being used by multiple threads.
    /// When the rules are expired, the refresh is managed by a single task.
    /// </summary>
    /// <typeparam name="TRules">The type of object holding the list of rules for a country</typeparam>
    public abstract class ThreadsafeRulesValidatorProvider<TRules> : IRulesValidator, IDisposable
    {
        /// <summary>
        /// Logger instance used by the provider
        /// </summary>
        protected readonly ILogger? Logger;

        /// <summary>
        /// Options for the provider
        /// </summary>
        protected readonly IRuleValidatorBaseOptions Options;

        //private TRules? _currentRules = default;
        private readonly IDictionary<string, TRules> _currentRulesDictionary = new Dictionary<string, TRules>();
        private DateTimeOffset _lastRefreshAttempt;

        /// <summary>
        /// Semaphore controlling access to <see cref="_refreshTaskDictionary"/>
        /// </summary>
        private readonly SemaphoreSlim _refreshTaskSemaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// The task that is currently executing the <see cref="RefreshRulesMethod"/> method
        /// </summary>
        //private Task<TRules?>? _refreshTask = null;
        private readonly IDictionary<string, Task<TRules?>> _refreshTaskDictionary = new Dictionary<string, Task<TRules?>>();
        private readonly CancellationTokenSource _refreshTaskCancellation = new CancellationTokenSource();

        /// <summary>
        /// Semaphore controlling access to <see cref="_currentRulesDictionary"/>
        /// </summary>
        private readonly SemaphoreSlim _currentRulesSemaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        protected ThreadsafeRulesValidatorProvider(
            IRuleValidatorBaseOptions? options,
            ILogger? logger)
        {
            Options = options ?? new RuleValidatorBaseOptions();
            Logger = logger;
        }

        #region Implementation of IRulesValidator

        /// <inheritdoc/>
        public abstract Task<IRuleValidationResult> GetRulesValidationResult(EuDGC dgc, DateTimeOffset validationInstant, string countryCode, CancellationToken cancellationToken = default);

        /// <inheritdoc/>
        public async Task RefreshRules(string? countryCode = null, CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrEmpty(countryCode))
            {
                await RefreshRulesMethod(countryCode, cancellationToken);
            }
            else
            {
                var supportedCountries = (await GetSupportedCountries()).Distinct().ToArray();
                foreach (var country in supportedCountries)
                {
                    await RefreshRulesMethod(country, cancellationToken);
                }
            }
        }

        /// <inheritdoc/>
        public abstract Task<IEnumerable<string>> GetSupportedCountries(CancellationToken cancellationToken = default);

        /// <inheritdoc/>
        public async Task<bool> SupportsCountry(string countryCode, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(countryCode))
                return false;

            var supportedCountries = await GetSupportedCountries(cancellationToken);

            return supportedCountries.Any(c => c.Equals(countryCode, StringComparison.InvariantCultureIgnoreCase));
        }

        #endregion

        #region Methods consumed by the implementing class
        /// <inheritdoc/>
        protected async Task<TRules?> GetRules(string countryCode, CancellationToken cancellationToken = default)
        {
            countryCode = countryCode?.ToUpperInvariant() ?? "";
            TRules? currentRules = default;

            // Reading rules from cache if the provider supports it
            await _currentRulesSemaphore.WaitAsync(cancellationToken);
            try
            {
                _currentRulesDictionary.TryGetValue(countryCode, out currentRules);

                // If not loaded, try to load from file
                if (currentRules == null)
                {
                    currentRules = await LoadCache(countryCode);
                    if (currentRules != null)
                        _currentRulesDictionary.Add(countryCode, currentRules);
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                _currentRulesSemaphore.Release();
            }

            // Checking validity of the rules:
            // If is null or expired, refresh
            var rules = currentRules;

            DateTimeOffset? rulesExpirationDate = null;
            if (rules != null)
                rulesExpirationDate = GetRulesExpiration(rules);

            Task<TRules?>? refreshTask = null;
            if (rules == null)
            {
                Logger?.LogInformation($"Rules not loaded, refreshing from server");

                // If not present, always try to refresh the list
                refreshTask = await GetRefreshTask(countryCode, cancellationToken);
            }
            else if (GetRulesLastUpdate(rules).Add(Options.RefreshInterval) < DateTime.Now)
            {
                // If refresh interval is expired and the min refresh interval is over, refresh the list
                if (_lastRefreshAttempt.Add(Options.MinRefreshInterval) < DateTime.Now)
                {
                    Logger?.LogInformation($"Rules refresh interval expired, refreshing from server");
                    refreshTask = await GetRefreshTask(countryCode, cancellationToken);
                }
            }
            else if (rulesExpirationDate != null &&
                     rulesExpirationDate < DateTime.Now)
            {
                // If file is expired and the min refresh interval is over, refresh the list
                if (_lastRefreshAttempt.Add(Options.MinRefreshInterval) < DateTime.Now)
                {
                    Logger?.LogInformation($"Rules expired, refreshing from server");
                    refreshTask = await GetRefreshTask(countryCode, cancellationToken);
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

        #endregion

        #region Methods to be implemented

        /// <summary>
        /// Executes the request to the server for getting the updated rules.
        /// </summary>
        /// <param name="countryCode">The 2-letter ISO country code for which downloading the rules.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected abstract Task<TRules> GetRulesFromServer(string? countryCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// If the provider supports a cache for the rules (eg. a file), executest the operations needed to load the required data
        /// This method is called when <see cref="GetRules(string, CancellationToken)"/> is called and the current rules are not loaded (null)
        /// </summary>
        /// <returns></returns>
        protected virtual Task<TRules?> LoadCache(string countryCode, CancellationToken cancellationToken = default) => Task.FromResult<TRules?>(default);

        /// <summary>
        /// Store the updated rules to the cache of the provider (eg. to a file)
        /// </summary>
        /// <param name="rules"></param>
        /// <param name="countryCode">The 2-letter ISO country code of the rules being saved to the cache</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected virtual Task UpdateCache(TRules rules, string countryCode, CancellationToken cancellationToken = default) => Task.FromResult(0);

        /// <summary>
        /// Returns the download date of the specified rules
        /// </summary>
        /// <param name="rules"></param>
        /// <returns></returns>
        protected abstract DateTimeOffset GetRulesLastUpdate(TRules rules);

        /// <summary>
        /// If the rules provides an expiration date, returns the expiration date value
        /// </summary>
        /// <param name="rules"></param>
        /// <returns></returns>
        protected virtual DateTimeOffset? GetRulesExpiration(TRules rules) => null;
        #endregion


        #region Private

        /// <summary>
        /// Private method that executes the download of the rules as implemented by <see cref="GetRulesFromServer"/>, returning them as result.
        /// </summary>
        /// <param name="countryCode"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<TRules?> RefreshRulesMethod(string? countryCode, CancellationToken cancellationToken = default)
        {
            try
            {
                countryCode = countryCode?.ToUpperInvariant() ?? string.Empty;
                _lastRefreshAttempt = DateTimeOffset.Now;

                var rules = await GetRulesFromServer(countryCode, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();
                await _currentRulesSemaphore.WaitAsync(cancellationToken);
                try
                {
                    _currentRulesDictionary[countryCode] = rules;
                }
                catch (Exception e)
                {
                    Logger?.LogError(e, $"Error updating currentRules: {e.Message}");
                }
                finally
                {
                    _currentRulesSemaphore.Release();
                }

                // Try saving file
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    await UpdateCache(rules, countryCode, cancellationToken);
                }
                catch (Exception e)
                {
                    Logger?.LogError(e, $"Error while updating rules cache: {e.Message}");
                }

                return rules;
            }
            catch (OperationCanceledException e)
            {
                Logger?.LogWarning($"{nameof(RefreshRulesMethod)} task canceled: {e.Message}");
                return default;
            }
            catch (Exception e)
            {
                Logger?.LogError(e, $"Error refreshing rules from server: {e.Message}");
                return default;
            }
        }

        /// <summary>
        /// If not already started, starts a new task for refreshing the rules from the remote server
        /// </summary>
        /// <returns></returns>
        private async Task<Task<TRules?>> GetRefreshTask(string countryCode, CancellationToken cancellationToken = default)
        {
            await _refreshTaskSemaphore.WaitAsync(cancellationToken);
            try
            {
                _refreshTaskDictionary.TryGetValue(countryCode, out var refreshTask);
                if (refreshTask == null)
                {
                    refreshTask = Task.Run(async () =>
                    {
                        try
                        {
                            return await RefreshRulesMethod(countryCode, _refreshTaskCancellation.Token);
                        }
                        finally
                        {
                            await _refreshTaskSemaphore.WaitAsync(_refreshTaskCancellation?.Token ?? default);
                            try
                            {
                                _refreshTaskDictionary.Remove(countryCode);
                                _refreshTaskCancellation?.Dispose();
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
                    _refreshTaskDictionary.Add(countryCode, refreshTask);
                }
                return refreshTask;
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
        public virtual void Dispose()
        {
            CancelRefreshTaskExecution();
        }
        #endregion
    }

    /// <inheritdoc cref="ThreadsafeRulesValidatorProvider{TRules}"/>
    public abstract class ThreadsafeRulesValidatorProvider<TRules, TOptions> : ThreadsafeRulesValidatorProvider<TRules>
        where TRules : class
        where TOptions : class, IRuleValidatorBaseOptions, new()
    {
        /// <inheritdoc cref="ThreadsafeRulesValidatorProvider{TRulesList}.Options"/>
        public new TOptions Options => (TOptions)base.Options;

        /// <inheritdoc />
        protected ThreadsafeRulesValidatorProvider(TOptions? options, ILogger? logger)
            : base(options ?? new TOptions(), logger)
        {

        }
    }
}
