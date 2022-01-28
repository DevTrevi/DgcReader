using DgcReader.BlacklistProviders.Italy.Entities;
using DgcReader.BlacklistProviders.Italy.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.BlacklistProviders.Italy
{


    /// <summary>
    /// Class for manage the local blacklist database
    /// </summary>
    public class ItalianDrlBlacklistManager
    {
        private const int MaxConsistencyTryCount = 3;


        internal static readonly string ProviderDataFolder = Path.Combine("DgcReaderData", "Blacklist", "Italy");
        private const string FileName = "italian-drl.db";



        private ItalianDrlBlacklistProviderOptions Options { get; }
        private ItalianDrlBlacklistClient Client { get; }

        private ILogger? Logger { get; }

        private bool _dbVersionChecked;
        private SyncStatus? _syncStatus;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="options"></param>
        /// <param name="client"></param>
        /// <param name="logger"></param>
        public ItalianDrlBlacklistManager(ItalianDrlBlacklistProviderOptions options, ItalianDrlBlacklistClient client, ILogger? logger)
        {
            Options = options;
            Client = client;
            Logger = logger;
        }

        #region Public methods
        /// <summary>
        /// Check if the specified UCVI is blacklisted
        /// </summary>
        /// <param name="ucvi"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<bool> ContainsUCVI(string ucvi, CancellationToken cancellationToken = default)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedUCVI = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(ucvi)));

                using (var ctx = await GetDbContext(cancellationToken))
                {
                    return await ctx.Blacklist.Where(r => r.HashedUCVI == hashedUCVI).AnyAsync(cancellationToken);
                }
            }
        }

        /// <summary>
        /// Return the currently saved SyncStatus from the DB
        /// </summary>
        /// <returns></returns>
        public async Task<SyncStatus> GetSyncStatus(bool useCache, CancellationToken cancellationToken = default)
        {
            if (useCache && _syncStatus != null)
                return _syncStatus;

            using (var ctx = await GetDbContext(cancellationToken))
            {
                return _syncStatus = await GetOrCreateSyncStatus(ctx, cancellationToken);
            }
        }

        /// <summary>
        /// Get the datetime of the latest update check
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DateTime> GetLastCheck(CancellationToken cancellationToken = default)
        {
            var status = await GetSyncStatus(true, cancellationToken);
            return status.LastCheck;
        }

        /// <summary>
        /// Method that clears all the UCVIs downloaded resetting the sync status
        /// </summary>
        /// <returns></returns>
        public async Task<SyncStatus> ClearDb(CancellationToken cancellationToken = default)
        {
            Logger?.LogInformation("Clearing database");
            using (var ctx = await GetDbContext(cancellationToken))
            {
                await ctx.Database.BeginTransactionAsync(cancellationToken);

                var status = await GetOrCreateSyncStatus(ctx, cancellationToken);
                var entityModel = ctx.Model.FindEntityType(typeof(BlacklistEntry));

#if NET452
                var tableName = entityModel.FindAnnotation("Relational:TableName")?.Value;
                var affected = await ctx.Database.ExecuteSqlCommandAsync($"DELETE FROM {tableName}", cancellationToken);

                Logger?.LogInformation($"{affected} rows removed");
#endif

#if NETSTANDARD2_0_OR_GREATER
                var tableName = entityModel.GetTableName();
                var schemaName = entityModel.GetSchema();
                var affected = await ctx.Database.ExecuteSqlRawAsync($"DELETE FROM {schemaName}.{tableName}", cancellationToken);
#endif


                status.CurrentVersion = 0;
                status.CurrentVersionId = "";
                status.LastChunkSaved = 0;

                await ctx.SaveChangesAsync(cancellationToken);

                ctx.Database.CommitTransaction();
                return status;
            }
        }

        /// <summary>
        /// Updates the local blacklist if a new version is available from the remote server
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<SyncStatus> UpdateFromServer(CancellationToken cancellationToken = default)
        {
            return this.UpdateFromServer(0, cancellationToken);
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Updates the local blacklist if a new version is available from the remote server
        /// </summary>
        /// <param name="tryCount">Execution try count</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<SyncStatus> UpdateFromServer(int tryCount = 0, CancellationToken cancellationToken = default)
        {
            if (tryCount > MaxConsistencyTryCount)
            {
                throw new Exception($"Unable to get a consistent state of the Dlr after {tryCount} attempts");
            }

            // Read the status of the local drl from DB
            var localStatus = await GetSyncStatus(false, cancellationToken);

            // Get the updated status from server
            var remoteStatus = await Client.GetDrlStatus(localStatus.CurrentVersion, cancellationToken);

            if (localStatus.IsSameVersion(remoteStatus))
            {
                // Version is not outdated, checking if data from server matches local data
                if (await IsCurrentVersionConsistent(localStatus, remoteStatus))
                {
                    // Updates the last check datetime
                    await SetLastCheck(DateTime.Now, cancellationToken);
                }
                else
                {
                    Logger?.LogWarning($"Database is in an inconsistent state, clearing DB");
                    await ClearDb();

                    // Try again
                    return await UpdateFromServer(tryCount++, cancellationToken);
                }
            }
            else
            {
                if (localStatus.IsTargetVersionConsistent(remoteStatus))
                {
                    if (localStatus.HasPendingDownload() || !localStatus.CurrentVersionMatchTarget())
                    {
                        // Target version info matches the remote version. Resume download
                        Logger?.LogInformation($"Resuming download of version {remoteStatus.Version} from chunk {localStatus.LastChunkSaved}");
                    }
                }
                else
                {
                    // If target version does not match the remote info, updates the target version and restart the download of the latest snapshot.
                    // This strategy differs from the offical one, but should keep the results consistent anyway, beeing more efficient.
                    // When the download completes, a consistency check will be done, eventually resetting the DB and restarting the download

                    if (!localStatus.CurrentVersionMatchTarget())
                        Logger?.LogWarning($"Target version {remoteStatus.Version} changed, setting new target version");
                    localStatus = await SetTargetVersion(remoteStatus, cancellationToken);
                }

                // Downloading chunks
                while (localStatus.HasPendingDownload())
                {
                    Logger?.LogInformation($"Downloading chunk {localStatus.LastChunkSaved + 1} of {localStatus.TargetChunksCount} " +
                        $"for updating Dlr from version {localStatus.CurrentVersion} to version {localStatus.TargetVersion}");

                    var chunk = await Client.GetDrlChunk(localStatus.CurrentVersion, localStatus.LastChunkSaved + 1, cancellationToken);

                    if (!localStatus.IsTargetVersionConsistent(chunk))
                    {
                        // Update the target version
                        // This will cause the localStatus to change, resetting the while cycle and downloading the whole new version from chunk 1
                        Logger?.LogWarning($"Target version {chunk.Version} changed, setting new target version");
                        localStatus = await SetTargetVersion(chunk, cancellationToken);

                        if (!localStatus.AnyChunkDownloaded() && chunk.Chunk == 1)
                        {
                            // If no chunks where downloaded for the inconsistent version, continue the download keeping the downloaded chunk
                            Logger?.LogWarning($"The downloaded chunk is the first for version {chunk.Version}, download can proceed");
                            localStatus = await SaveChunk(chunk, cancellationToken);
                        }
                    }
                    else
                    {
                        // Everything is good, save chunk data
                        localStatus = await SaveChunk(chunk, cancellationToken);
                    }

                    // If last chunk, apply the latest version
                    if (!localStatus.HasPendingDownload())
                    {
                        localStatus = await FinalizeUpdate(localStatus, chunk);
                        if (!localStatus.HasCurrentVersion())
                        {
                            // If failed, db is resetted and a new download attempt will be made
                            return await UpdateFromServer(tryCount++, cancellationToken);
                        }
                    }
                }

                // If finalization is missing, finalize the update
                if (!localStatus.CurrentVersionMatchTarget())
                {
                    // Consistency check:
                    // Getting updated status from server
                    remoteStatus = await Client.GetDrlStatus(localStatus.CurrentVersion, cancellationToken);

                    // If still same version, finalize
                    if (localStatus.IsTargetVersion(remoteStatus))
                    {
                        localStatus = await FinalizeUpdate(localStatus, remoteStatus);
                        if (!localStatus.HasCurrentVersion())
                        {
                            // If failed, db is resetted and a new download attempt will be made
                            return await UpdateFromServer(tryCount++, cancellationToken);
                        }
                    }
                }
            }

            return localStatus;
        }


        /// <summary>
        /// Update the datetime of last check for new versions
        /// </summary>
        /// <param name="lastCheck"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<SyncStatus> SetLastCheck(DateTime lastCheck, CancellationToken cancellationToken = default)
        {
            using (var ctx = await GetDbContext(cancellationToken))
            {
                var status = await GetOrCreateSyncStatus(ctx, cancellationToken);

                status.LastCheck = lastCheck;
                await ctx.SaveChangesAsync(cancellationToken);
                return status;
            }
        }

        /// <summary>
        /// Updates the target version info with the specified entry
        /// </summary>
        /// <param name="statusEntry"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<SyncStatus> SetTargetVersion(IDrlVersionInfo statusEntry, CancellationToken cancellationToken = default)
        {
            Logger?.LogInformation($"Updating target version to {statusEntry.Version} ({statusEntry.Id})");
            using (var ctx = await GetDbContext(cancellationToken))
            {
                var status = await GetOrCreateSyncStatus(ctx, cancellationToken);

                if (!status.IsTargetVersionConsistent(statusEntry))
                {
                    // Copy target data to current status
                    SetTargetData(status, statusEntry);

                    await ctx.SaveChangesAsync(cancellationToken);
                }

                return status;
            }
        }

        /// <summary>
        /// Saves the provided chunk of data, adding or deleting blacklist entries and updating the SyncStatus
        /// </summary>
        /// <param name="chunkData"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<SyncStatus> SaveChunk(DrlChunkData chunkData, CancellationToken cancellationToken = default)
        {
            Logger?.LogInformation($"Saving chunk {chunkData.Chunk} of {chunkData.TotalChunks} for Drl version {chunkData.Version}");
            using (var ctx = await GetDbContext())
            {
                var status = await GetOrCreateSyncStatus(ctx, cancellationToken);
                if (!status.IsTargetVersionConsistent(chunkData))
                {

                    if (!status.AnyChunkDownloaded() && chunkData.Chunk == 1)
                    {
                        // If no chunks where downloaded for the inconsistent version, continue the download keeping the downloaded chunk
                        Logger?.LogWarning($"The downloaded chunk is the first for version {chunkData.Version}, download can proceed");

                        // Updating target with data from chunk
                        SetTargetData(status, chunkData);
                    }
                    else
                    {
                        // Version is changed and at least one chunk was downloaded, restart download of chunks targeting the new version
                        Logger?.LogWarning($"Version changed to {chunkData.Version} while downloading chunks for version {status.TargetVersion}. Restarting the download for the new version detected");

                        // Updating target with data from chunk
                        SetTargetData(status, chunkData);
                        await ctx.SaveChangesAsync();
                        return status;
                    }
                }



                // Full list of additions
                if (chunkData.RevokedUcviList != null)
                {
                    await AddMissingUcvis(ctx, chunkData.RevokedUcviList, cancellationToken);
                }
                else if (chunkData.Delta != null)
                {
                    // Add the new UCVIs
                    await AddMissingUcvis(ctx, chunkData.Delta.Insertions, cancellationToken);

                    // Removes deleted UCVIs
                    await RemoveUcvis(ctx, chunkData.Delta.Deletions, cancellationToken);
                }

                // Update status
                status.TargetTotalNumberUCVI = chunkData.TotalNumberUCVI;
                status.LastChunkSaved = chunkData.Chunk;

                // Save changes
                await ctx.SaveChangesAsync(cancellationToken);
                return status;
            }
        }

        private async Task<SyncStatus> FinalizeUpdate(SyncStatus status, IDrlVersionInfo chunkData, CancellationToken cancellationToken = default)
        {
            // If last chunk, apply the latest version
            if (status.IsTargetVersionConsistent(chunkData) &&
                !status.HasPendingDownload())
            {
                using (var ctx = await GetDbContext())
                {

                    var count = await ctx.Blacklist.CountAsync();
                    if (count == status.TargetTotalNumberUCVI)
                    {
                        Logger?.LogInformation($"Finalizing update for version {status.TargetVersion}");
                        // Apply target version as current version
                        status.CurrentVersion = status.TargetVersion;
                        status.CurrentVersionId = status.TargetVersionId;
                        status.LastCheck = DateTime.Now;
                        ctx.Attach(status).State = EntityState.Modified;
                        await ctx.SaveChangesAsync(cancellationToken);

                        Logger?.LogInformation($"Version {status.TargetVersion} finalized for {count} total blacklist entries");

                        return status;
                    }
                    else
                    {
                        Logger?.LogWarning($"Consistency check failed when finalizing update for version {status.TargetVersion}: " +
                            $"expected count {status.TargetTotalNumberUCVI} differs from actual count {count}. Resetting DB");
                        return await ClearDb();
                    }

                }

            }
            return status;
        }

        /// <summary>
        /// Copy Version info from status to the Target properties of current resetting the LastChunkSaved property
        /// </summary>
        /// <param name="current"></param>
        /// <param name="status"></param>
        private void SetTargetData(SyncStatus current, IDrlVersionInfo status)
        {
            // Reset chunk download status
            current.LastChunkSaved = 0;

            // Copy target data to current status
            current.TargetVersion = status.Version;
            current.TargetVersionId = status.Id;
            current.TargetTotalNumberUCVI = status.TotalNumberUCVI;
            current.TargetChunksCount = status.TotalChunks;
            current.TargetChunkSize = status.SingleChunkSize;
        }

        /// <summary>
        /// Check if the remote version equals the currently stored version, and matches the size
        /// </summary>
        /// <param name="remoteStatus"></param>
        /// <param name="localStatus"></param>
        /// <returns></returns>
        private async Task<bool> IsCurrentVersionConsistent(SyncStatus localStatus, IDrlVersionInfo remoteStatus)
        {
            if (!localStatus.IsSameVersion(remoteStatus))
                return false;
            // Check the actual count on the DB
            var currentCount = await GetActualBlacklistCount();
            return remoteStatus.TotalNumberUCVI == currentCount;
        }

        /// <summary>
        /// Get the actual count of entries in the blacklist
        /// </summary>
        /// <returns></returns>
        private async Task<int> GetActualBlacklistCount()
        {
            using (var ctx = await GetDbContext())
            {
                return await ctx.Blacklist.CountAsync();
            }
        }

        private async Task<SyncStatus> GetOrCreateSyncStatus(ItalianDrlBlacklistDbContext ctx, CancellationToken cancellationToken = default)
        {
            var syncStatus = await ctx.SyncStatus
                    .OrderByDescending(r => r.CurrentVersion)
                    .FirstOrDefaultAsync(cancellationToken);

            if (syncStatus == null)
            {
                syncStatus = new SyncStatus()
                {
                    CurrentVersion = 0,
                    TargetTotalNumberUCVI = 0,
                };
                ctx.SyncStatus.Add(syncStatus);
                await ctx.SaveChangesAsync(cancellationToken);
            }
            return syncStatus;
        }

        private async Task<ItalianDrlBlacklistDbContext> GetDbContext(CancellationToken cancellationToken = default)
        {
            // Check directory
            if (!Directory.Exists(GetCacheFolder()))
                Directory.CreateDirectory(GetCacheFolder());

            // Configuring db context options
            if (!Options.DbContext.IsConfigured)
            {
                var connString = $"Data Source={GetCacheFilePath()}";
                Options.DbContext.UseSqlite(connString);
            }

            var ctx = new ItalianDrlBlacklistDbContext(Options.DbContext.Options);

            // Check if db should be updated
            if (!_dbVersionChecked)
            {
                await ctx.Database.MigrateAsync(cancellationToken);
                _dbVersionChecked = true;
            }

            return ctx;

        }
        private string GetCacheFolder() => Path.Combine(Options.BasePath, ProviderDataFolder);
        private string GetCacheFilePath() => Path.Combine(GetCacheFolder(), FileName);

        /// <summary>
        /// Add the missing UCVIs passed to the context tracked entries in Add
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="ucvis"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task AddMissingUcvis(ItalianDrlBlacklistDbContext ctx, string[] ucvis, CancellationToken cancellationToken = default)
        {
            if (ucvis?.Any() != true)
                return;

            int pageSize = 1000;
            var pages = (int)Math.Ceiling(((decimal)ucvis.Length) / pageSize);

            Logger?.LogInformation($"Adding {ucvis.Length} UCVIs to the blacklist");

            for (int i = 0; i < pages; i++)
            {
                var pageData = ucvis.Skip(i * pageSize).Take(pageSize).ToArray();
                var existing = await ctx.Blacklist
                    .Where(r => pageData.Contains(r.HashedUCVI))
                    .Select(r => r.HashedUCVI).ToArrayAsync(cancellationToken);

                if (existing.Any())
                {
                    Logger?.LogWarning($"{existing.Count()} UCVIs entries already in database, skipping add for these entries");
                }

                var newEntries = pageData.Except(existing).Distinct().Select(r => new BlacklistEntry() { HashedUCVI = r }).ToArray();
                Logger?.LogDebug($"Adding {newEntries.Count()} of {ucvis.Length} (page {i + 1} of {pages}) UCVIs to the blacklist");
                ctx.AddRange(newEntries);
            }
        }

        /// <summary>
        /// Add the missing UCVIs passed to the context tracked entries in Add
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="ucvis"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task RemoveUcvis(ItalianDrlBlacklistDbContext ctx, string[] ucvis, CancellationToken cancellationToken = default)
        {
            if (ucvis?.Any() != true)
                return;

            int pageSize = 1000;
            var pages = (int)Math.Ceiling(((decimal)ucvis.Length) / pageSize);

            Logger?.LogInformation($"Removing {ucvis.Length} UCVIs from the blacklist");

            for (int i = 0; i < pages; i++)
            {
                var pageData = ucvis.Skip(i * pageSize).Take(pageSize).ToArray();

                var deleting = await ctx.Blacklist.Where(r => pageData.Contains(r.HashedUCVI)).ToArrayAsync(cancellationToken);

                if (deleting.Length != pageData.Length)
                {
                    Logger?.LogWarning($"Found {deleting.Length} out of {pageData.Length} deleted UCVIs entries");
                }

                Logger?.LogDebug($"Removing {deleting.Count()} of {ucvis.Length} (page {i + 1} of {pages}) UCVIs from the blacklist");
                ctx.Blacklist.RemoveRange(deleting);
            }
        }
        #endregion
    }
}