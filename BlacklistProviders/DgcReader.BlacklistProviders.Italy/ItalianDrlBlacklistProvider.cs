using DgcReader.Interfaces.BlacklistProviders;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DgcReader.BlacklistProviders.Italy.Models;
using Newtonsoft.Json;
using System;
using DgcReader.Providers.Abstractions;

#if !NET452
using Microsoft.Extensions.Options;
#endif

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.BlacklistProviders.Italy
{
    /// <summary>
    /// Blacklist provider using the Italian backend
    /// </summary>
    public class ItalianDrlBlacklistProvider : IBlacklistProvider
    {

        private const string BlacklistStatusUrl = "https://get.dgc.gov.it/v1/dgc/drl/check";
        private const string BlacklistChunkUrl = "https://get.dgc.gov.it/v1/dgc/drl";


        private readonly ItalianDrlBlacklistProviderOptions _options;
        private readonly HttpClient HttpClient;
        private readonly ILogger<ItalianDrlBlacklistProvider>? Logger;
        private readonly ItalianDrlBlacklistManager BlacklistManager;
        private readonly SingleTaskRunner<bool> RefreshBlacklistTaskRunner;
        private DateTime lastRefreshAttempt;


        #region Constructor
#if NET452
        /// <summary>
        /// Constructor for the provider
        /// </summary>
        /// <param name="httpClient">The http client instance that will be used for requests to the server</param>
        /// <param name="options">The options for the provider</param>
        /// <param name="logger">Instance of <see cref="ILogger"/> used by the provider (optional).</param>
        public ItalianDrlBlacklistProvider(HttpClient httpClient,
            ItalianDrlBlacklistProviderOptions? options = null,
            ILogger<ItalianDrlBlacklistProvider>? logger = null)
        {
            _options = options ?? new ItalianDrlBlacklistProviderOptions();
            HttpClient = httpClient;
            Logger = logger;

            BlacklistManager = new ItalianDrlBlacklistManager(_options, logger);
            RefreshBlacklistTaskRunner = new SingleTaskRunner<bool>(async ct =>
            {
                await UpdateFromServer(ct);
                return true;
            }, Logger);
        }

        /// <summary>
        /// Factory method for creating an instance of <see cref="ItalianDrlBlacklistProvider"/>
        /// whithout using the DI mechanism. Useful for legacy applications
        /// </summary>
        /// <param name="httpClient">The http client instance that will be used for requests to the server</param>
        /// <param name="options">The options for the provider</param>
        /// <param name="logger">Instance of <see cref="ILogger"/> used by the provider (optional).</param>
        /// <returns></returns>
        public static ItalianDrlBlacklistProvider Create(HttpClient httpClient,
            ItalianDrlBlacklistProviderOptions? options = null,
            ILogger<ItalianDrlBlacklistProvider>? logger = null)
        {
            return new ItalianDrlBlacklistProvider(httpClient, options, logger);
        }
#else
        /// <summary>
        /// Constructor for the provider
        /// </summary>
        /// <param name="httpClient">The http client instance that will be used for requests to the server</param>
        /// <param name="options">The options for the provider</param>
        /// <param name="logger">Instance of <see cref="ILogger"/> used by the provider (optional).</param>
        public ItalianDrlBlacklistProvider(HttpClient httpClient,
            IOptions<ItalianDrlBlacklistProviderOptions>? options = null,
            ILogger<ItalianDrlBlacklistProvider>? logger = null)
        {
            _options = options?.Value ?? new ItalianDrlBlacklistProviderOptions();
            HttpClient = httpClient;
            Logger = logger;

            BlacklistManager = new ItalianDrlBlacklistManager(_options, logger);
            RefreshBlacklistTaskRunner = new SingleTaskRunner<bool>(async ct =>
            {
                await UpdateFromServer(ct);
                return true;
            }, Logger);
        }

        /// <summary>
        /// Factory method for creating an instance of <see cref="ItalianDrlBlacklistProvider"/>
        /// whithout using the DI mechanism. Useful for legacy applications
        /// </summary>
        /// <param name="httpClient">The http client instance that will be used for requests to the server</param>
        /// <param name="options">The options for the provider</param>
        /// <param name="logger">Instance of <see cref="ILogger"/> used by the provider (optional).</param>
        /// <returns></returns>
        public static ItalianDrlBlacklistProvider Create(HttpClient httpClient,
            ItalianDrlBlacklistProviderOptions? options = null,
            ILogger<ItalianDrlBlacklistProvider>? logger = null)
        {
            return new ItalianDrlBlacklistProvider(httpClient,
                options == null ? null : Options.Create(options),
                logger);
        }
#endif
        #endregion

        #region Implementation of IBlacklistProvider

        /// <inheritdoc/>
        public async Task<bool> IsBlacklisted(string certificateIdentifier, CancellationToken cancellationToken = default)
        {
            // Get latest check datetime
            var lastCheck = await BlacklistManager.GetLastCheck();


            if (lastCheck.Add(_options.MaxFileAge) < DateTime.Now)
            {
                // MaxFileAge expired

                var refreshTask = await RefreshBlacklistTaskRunner.RunSingleTask(cancellationToken);

                // Wait for the task to complete
                await refreshTask;
            }
            else if (lastCheck.Add(_options.RefreshInterval) < DateTime.Now)
            {
                // Normal expiration

                // If min refresh expired
                if (lastRefreshAttempt.Add(_options.MinRefreshInterval) < DateTime.Now)
                {
                    var refreshTask = await RefreshBlacklistTaskRunner.RunSingleTask(cancellationToken);
                    if (!_options.UseAvailableValuesWhileRefreshing)
                    {
                        // Wait for the task to complete
                        await refreshTask;
                    }
                }
            }

            return await BlacklistManager.ContainsUCVI(certificateIdentifier, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task RefreshBlacklist(CancellationToken cancellationToken = default)
        {
            var task = await RefreshBlacklistTaskRunner.RunSingleTask(cancellationToken);
            await task;
        }
        #endregion

        #region Private

        /// <summary>
        /// Updates the local blacklist if a new version is available from the remote server
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task UpdateFromServer(CancellationToken cancellationToken = default)
        {

            lastRefreshAttempt = DateTime.Now;
            var syncStatus = await BlacklistManager.GetSyncStatus(cancellationToken);

            var remoteStatus = await GetDrlStatus(syncStatus.LocalVersion, cancellationToken);

            if (syncStatus.LocalVersion == remoteStatus.Version)
            {
                await BlacklistManager.SetLastCheck(DateTime.Now, cancellationToken);
            }
            else
            {
                if (syncStatus.TargetVersion != remoteStatus.Version)
                {
                    Logger?.LogInformation($"New Clr version {remoteStatus.Version} available");
                    syncStatus = await BlacklistManager.SetTargetVersion(remoteStatus, cancellationToken);
                }
                else
                {
                    Logger?.LogInformation($"Resuming download of version {remoteStatus.Version} from chunk {syncStatus.LastChunkSaved}");
                }

                // Downloading chunks
                while (syncStatus.LastChunkSaved < syncStatus.ChunksCount)
                {
                    Logger?.LogInformation($"Downloading chunk {syncStatus.LastChunkSaved + 1} of {syncStatus.ChunksCount} " +
                        $"for updating Clr from version {syncStatus.LocalVersion} to version {syncStatus.TargetVersion}");
                    var chunk = await GetDrlChunk(syncStatus.LocalVersion, syncStatus.LastChunkSaved + 1, cancellationToken);

                    syncStatus = await BlacklistManager.SaveChunk(chunk, cancellationToken);
                }
            }

        }

        /// <summary>
        /// Get the Drl status from the server, given the current version stored on the local DB
        /// </summary>
        /// <param name="localVersion">The currently stored version on the local DB</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<DrlStatusEntry> GetDrlStatus(int localVersion, CancellationToken cancellationToken = default)
        {
            try
            {
                Logger?.LogDebug($"Checking Drl for updates...");

                var response = await HttpClient.GetAsync($"{BlacklistStatusUrl}?version={localVersion}", cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();

                    var result = JsonConvert.DeserializeObject<DrlStatusEntry>(content);

                    if (result == null)
                        throw new Exception($"Error wile deserializing {nameof(DrlStatusEntry)} from server");

                    return result;
                }
                throw new Exception($"The remote server responded with code {response.StatusCode}: {response.ReasonPhrase}");
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, $"Error while Checking Drl version {localVersion} from server: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get a Drl chunk of data
        /// </summary>
        /// <param name="localVersion">The local version of the Drl</param>
        /// <param name="chunk">The chunk to be downloaded for the requested version</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<DrlChunkData> GetDrlChunk(int localVersion, int chunk, CancellationToken cancellationToken = default)
        {
            try
            {
                Logger?.LogDebug($"Checking Drl for updates...");

                var response = await HttpClient.GetAsync($"{BlacklistChunkUrl}?version={localVersion}&chunk={chunk}", cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();

                    var result = JsonConvert.DeserializeObject<DrlChunkData>(content);

                    if (result == null)
                        throw new Exception($"Error wile deserializing {nameof(DrlChunkData)} from server");

                    return result;
                }
                throw new Exception($"The remote server responded with code {response.StatusCode}: {response.ReasonPhrase}");
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, $"Error while getting Drl chunk {chunk} version {localVersion} from server: {ex.Message}");
                throw;
            }
        }

        #endregion
    }
}