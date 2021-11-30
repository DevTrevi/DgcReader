using DgcReader.TrustListProviders.Abstractions.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DgcReader.Interfaces.TrustListProviders;
using DgcReader.Exceptions;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.TrustListProviders.Abstractions
{
    /// <summary>
    /// Base class for implementing a trustlist provider optimized for being used by multiple threads.
    /// When the trustlist is expired, the refresh is managed by a single task.
    /// </summary>
    public abstract class ThreadsafeTrustListProvider : ITrustListProvider, IDisposable
    {
        /// <summary>
        /// Logger instance used by the provider
        /// </summary>
        protected readonly ILogger? Logger;

        /// <summary>
        /// Options for the provider
        /// </summary>
        protected readonly ITrustListProviderBaseOptions Options;

        private ITrustList? _currentTrustList = null;
        private DateTimeOffset _lastRefreshAttempt;

        /// <summary>
        /// Semaphore controlling access to <see cref="_refreshTask"/>
        /// </summary>
        private readonly SemaphoreSlim _refreshTaskSemaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// The task that is currently executing the <see cref="RefreshTrustList"/> method
        /// </summary>
        private Task<IEnumerable<ITrustedCertificateData>?>? _refreshTask = null;
        private CancellationTokenSource? _refreshTaskCancellation;

        /// <summary>
        /// Semaphore controlling access to <see cref="_currentTrustList"/>
        /// </summary>
        private readonly SemaphoreSlim _currentTrustlistSemaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        protected ThreadsafeTrustListProvider(
            ITrustListProviderBaseOptions options,
            ILogger? logger)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            Options = options;
            Logger = logger;
        }

        #region Implementation of ITrustListProvider

        /// <inheritdoc/>
        public abstract bool SupportsCountryCodes { get; }

        /// <inheritdoc/>
        public abstract bool SupportsCertificates { get; }

        /// <inheritdoc/>
        public async Task<IEnumerable<ITrustedCertificateData>> GetTrustList(CancellationToken cancellationToken = default)
        {

            // Reading trustlist from cache if the provider supports it
            await _currentTrustlistSemaphore.WaitAsync(cancellationToken);
            try
            {
                // If not loaded, try to load from file
                if (_currentTrustList == null)
                {
                    _currentTrustList = await LoadCache();
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                _currentTrustlistSemaphore.Release();
            }

            // Checking validity of the trustlist:
            // If is null or expired, refresh
            var trustList = _currentTrustList;

            Task<IEnumerable<ITrustedCertificateData>?>? refreshTask = null;
            if (trustList == null)
            {
                Logger?.LogInformation($"TrustList not loaded, refreshing from server");

                // If not present, always try to refresh the list
                refreshTask = await GetRefreshTask(cancellationToken);
            }
            else if (trustList.LastUpdate.Add(Options.RefreshInterval) < DateTime.Now)
            {
                // If refresh interval is expired and the min refresh interval is over, refresh the list
                if (_lastRefreshAttempt.Add(Options.MinRefreshInterval) < DateTime.Now)
                {
                    Logger?.LogInformation($"TrustList refresh interval expired, refreshing from server");
                    refreshTask = await GetRefreshTask(cancellationToken);
                }
            }
            else if (trustList.Expiration != null &&
                     trustList.Expiration < DateTime.Now)
            {
                // If file is expired and the min refresh interval is over, refresh the list
                if (_lastRefreshAttempt.Add(Options.MinRefreshInterval) < DateTime.Now)
                {
                    Logger?.LogInformation($"TrustList expired, refreshing from server");
                    refreshTask = await GetRefreshTask(cancellationToken);
                }
            }

            var certificates = trustList?.Certificates;

            if (refreshTask != null)
            {
                if (certificates == null)
                {
                    Logger?.LogInformation($"No TrustList loaded in memory, waiting for refresh to complete");
                    certificates =  await refreshTask;
                }
                else if (Options.UseAvailableListWhileRefreshing == false)
                {
                    // If UseAvailableListWhileRefreshing, always wait for the task to complete
                    Logger?.LogInformation($"Trustlist is expired, waiting for refresh to complete");
                    certificates = await refreshTask;
                }
            }

            if (certificates == null)
                throw new DgcException($"Can not get a valid TrustList. Make sure that the application can connect to the remote server and try again.");

            return certificates;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ITrustedCertificateData>?> RefreshTrustList(CancellationToken cancellationToken = default)
        {
            try
            {
                _lastRefreshAttempt = DateTimeOffset.Now;

                var trustList = await GetTrustListFromServer(cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();
                await _currentTrustlistSemaphore.WaitAsync(cancellationToken);
                try
                {
                    _currentTrustList = trustList;
                }
                catch (Exception e)
                {
                    Logger?.LogError(e, $"Error updating  currentTrustlist: {e.Message}");
                }
                finally
                {
                    _currentTrustlistSemaphore.Release();
                }

                // Try saving file
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    await UpdateCache(trustList, cancellationToken);
                }
                catch (Exception e)
                {
                    Logger?.LogError(e, $"Error while updating trustlist cache: {e.Message}");
                }

                return trustList.Certificates;
            }
            catch (OperationCanceledException e)
            {
                Logger?.LogWarning($"RefreshTrustList task canceled: {e.Message}");
                return null;
            }
            catch (Exception e)
            {
                Logger?.LogError(e, $"Error refreshing trustlist from server: {e.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public virtual async Task<ITrustedCertificateData?> GetByKid(string kid, string? country, CancellationToken cancellationToken = default)
        {
            var trustList = await GetTrustList(cancellationToken);

            var q = trustList.Where(x => x.Kid == kid);
            if (!string.IsNullOrEmpty(country))
                q = q.Where(x => x.Country == country);
            return q.SingleOrDefault();
        }

        #endregion

        #region Methods to be implemented

        /// <summary>
        /// Executes the request to the server for getting the updated trustlist.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected abstract Task<ITrustList> GetTrustListFromServer(CancellationToken cancellationToken = default);


        /// <summary>
        /// If the provider supports a cache for the trustlist (eg. a file), executest the operations needed to load the required data
        /// This method is called when <see cref="GetTrustList(CancellationToken)"/> is called and the current trust list is not loaded (null)
        /// </summary>
        /// <returns></returns>
        protected virtual Task<ITrustList?> LoadCache(CancellationToken cancellationToken = default) => Task.FromResult<ITrustList?>(null);

        /// <summary>
        /// Store the updated trustlist to the cache of the provider (eg. to a file)
        /// </summary>
        /// <param name="trustList"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected virtual Task UpdateCache(ITrustList trustList, CancellationToken cancellationToken = default) => Task.FromResult(0);

        #endregion

        #region Private

        /// <summary>
        /// If not already started, starts a new task for refreshing the Trustlist
        /// </summary>
        /// <returns></returns>
        private async Task<Task<IEnumerable<ITrustedCertificateData>?>> GetRefreshTask(CancellationToken cancellationToken = default)
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
                            return await RefreshTrustList(_refreshTaskCancellation.Token);
                        }
                        catch (Exception ex)
                        {
                            Logger?.LogError($"Error while executing {nameof(RefreshTrustList)} task: {ex}");
                            return null;
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
        public void Dispose()
        {
            CancelRefreshTaskExecution();
        }
        #endregion
    }

    /// <inheritdoc cref="ThreadsafeTrustListProvider"/>
    public abstract class ThreadsafeTrustListProvider<TOptions> : ThreadsafeTrustListProvider
        where TOptions : class, ITrustListProviderBaseOptions, new()
    {
        /// <inheritdoc cref="ThreadsafeTrustListProvider.Options"/>
        public new TOptions Options => (TOptions)base.Options;

        /// <inheritdoc />
        protected ThreadsafeTrustListProvider(TOptions? options, ILogger? logger) : base(options ?? new TOptions(), logger)
        {

        }
    }

}
