using DgcReader.BlacklistProviders.Italy.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.BlacklistProviders.Italy
{
    /// <summary>
    /// Client for getting data from the italian Drl backend
    /// </summary>
    public class ItalianDrlBlacklistClient
    {
        private const string BlacklistStatusUrl = "https://get.dgc.gov.it/v1/dgc/drl/check";
        private const string BlacklistChunkUrl = "https://get.dgc.gov.it/v1/dgc/drl";

        private readonly HttpClient HttpClient;
        private readonly ILogger? Logger;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="logger"></param>
        public ItalianDrlBlacklistClient(HttpClient httpClient, ILogger? logger)
        {
            HttpClient = httpClient;
            Logger = logger;
        }

        /// <summary>
        /// Get the Drl status from the server, given the current version stored on the local DB
        /// </summary>
        /// <param name="localVersion">The currently stored version on the local DB</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DrlStatusEntry> GetDrlStatus(int localVersion, CancellationToken cancellationToken = default)
        {
            try
            {
                Logger?.LogDebug($"Checking Drl for updates...");

                var response = await HttpClient.GetWithSdkUSerAgentAsync($"{BlacklistStatusUrl}?version={localVersion}", cancellationToken);
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
        public async Task<DrlChunkData> GetDrlChunk(int localVersion, int chunk, CancellationToken cancellationToken = default)
        {
            try
            {
                Logger?.LogDebug($"Checking Drl for updates...");

                var response = await HttpClient.GetWithSdkUSerAgentAsync($"{BlacklistChunkUrl}?version={localVersion}&chunk={chunk}", cancellationToken);
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
    }
}