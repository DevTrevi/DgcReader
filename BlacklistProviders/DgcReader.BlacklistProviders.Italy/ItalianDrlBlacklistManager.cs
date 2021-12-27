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
        private const string ProviderDataFolder = "DgcReaderData\\Blacklist\\Italy";
        private const string FileName = "italian-drl.db";

        private ItalianDrlBlacklistProviderOptions Options { get; }
        private ILogger? Logger { get; }

        private bool _dbVersionChecked;
        private DateTime? _lastCheck;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public ItalianDrlBlacklistManager(ItalianDrlBlacklistProviderOptions options, ILogger? logger)
        {
            Options = options;
            Logger = logger;
        }


        /// <summary>
        /// Return the currently saved SyncStatus from the DB
        /// </summary>
        /// <returns></returns>
        public async Task<SyncStatus> GetSyncStatus(CancellationToken cancellationToken = default)
        {
            using (var ctx = await GetDbContext(cancellationToken))
            {
                return await GetOrCreateSyncStatus(ctx, cancellationToken);
            }
        }

        /// <summary>
        /// Get the datetime of the latest update check
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DateTime> GetLastCheck(CancellationToken cancellationToken = default)
        {
            if(_lastCheck == null)
            {
                var status = await GetSyncStatus(cancellationToken);
                _lastCheck = status.LastCheck;
            }
            return _lastCheck.Value;
        }

        /// <summary>
        /// Updates the target version info with the specified entry
        /// </summary>
        /// <param name="statusEntry"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<SyncStatus> SetTargetVersion(IDrlVersionInfo statusEntry, CancellationToken cancellationToken = default)
        {
            Logger?.LogInformation($"Updating target version to {statusEntry.Version} ({statusEntry.Id})");
            using (var ctx = await GetDbContext(cancellationToken))
            {
                var status = await GetOrCreateSyncStatus(ctx, cancellationToken);

                if (status.TargetVersionId != statusEntry.Id)
                {
                    status.TargetVersion = statusEntry.Version;
                    status.TargetVersionId = statusEntry.Id;

                    status.LastChunkSaved = 0;
                    status.ChunksCount = statusEntry.TotalChunks;

                    status.TotalNumberUCVI = statusEntry.TotalNumberUCVI;
                    status.LastCheck = DateTime.Now;
                    await ctx.SaveChangesAsync(cancellationToken);
                    _lastCheck = status.LastCheck;
                }
                return status;
            }
        }

        /// <summary>
        /// Update the datetime of last check for new versions
        /// </summary>
        /// <param name="lastCheck"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<SyncStatus> SetLastCheck(DateTime lastCheck, CancellationToken cancellationToken = default)
        {
            using (var ctx = await GetDbContext(cancellationToken))
            {
                var status = await GetOrCreateSyncStatus(ctx, cancellationToken);

                status.LastCheck = lastCheck;
                await ctx.SaveChangesAsync(cancellationToken);
                _lastCheck = status.LastCheck;
                return status;
            }
        }

        /// <summary>
        /// Saves the provided chunk of data, adding or deleting blacklist entries and updating the SyncStatus
        /// </summary>
        /// <param name="chunkData"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<SyncStatus> SaveChunk(DrlChunkData chunkData, CancellationToken cancellationToken = default)
        {
            Logger?.LogInformation($"Saving chunk {chunkData.Chunk} of {chunkData.TotalChunks} for Drl version {chunkData.Version}");
            using (var ctx = await GetDbContext())
            {
                var status = await GetOrCreateSyncStatus(ctx, cancellationToken);
                if (status.TargetVersionId != chunkData.Id)
                {
                    if (chunkData.Chunk > 1)
                    {
                        // Version is changed and at least one chunk was downloaded, restart download of chunks targeting the new version
                        Logger?.LogWarning($"Version changed to {chunkData.Version} while downloading chunks for version {status.TargetVersion}. Restarting the download for the new version detected");
                        return await SetTargetVersion(chunkData, cancellationToken);
                    }
                    else
                    {
                        // Version is changed but no chunks where downloaded
                        // Update status with latest version data
                        status.TargetVersion = chunkData.Version;
                        status.TargetVersionId = chunkData.Id;
                        status.ChunksCount = chunkData.TotalChunks;
                        status.TotalNumberUCVI = chunkData.TotalNumberUCVI;
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
                status.TotalNumberUCVI = chunkData.TotalNumberUCVI;
                status.LastChunkSaved = chunkData.Chunk;
                status.LastCheck = DateTime.Now;

                // If last chunk, apply the latest version
                if (chunkData.Chunk == status.ChunksCount)
                {
                    // Confirm new version
                    status.LocalVersion = chunkData.Version;
                    status.LocalVersionId = chunkData.Id;
                }

                // Save changes
                await ctx.SaveChangesAsync(cancellationToken);
                _lastCheck = status.LastCheck;
                return status;
            }
        }

        /// <summary>
        /// Method that clears all the UCVIs downloaded resetting the sync status
        /// </summary>
        /// <returns></returns>
        public async Task ClearUCVIs(CancellationToken cancellationToken = default)
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


                status.LocalVersion = 0;
                status.LocalVersionId = "";
                await ctx.SaveChangesAsync(cancellationToken);

                ctx.Database.CommitTransaction();

            }
        }


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

        private async Task<SyncStatus> GetOrCreateSyncStatus(ItalianDrlBlacklistDbContext ctx, CancellationToken cancellationToken = default)
        {
            var syncStatus = await ctx.SyncStatus
                    .OrderByDescending(r => r.LocalVersion)
                    .FirstOrDefaultAsync(cancellationToken);

            if (syncStatus == null)
            {
                syncStatus = new SyncStatus()
                {
                    LocalVersion = 0,
                    TotalNumberUCVI = 0,
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

                Logger?.LogDebug($"Removing {deleting.Count()} of {ucvis.Length} (page {i+1} of {pages}) UCVIs from the blacklist");
                ctx.Blacklist.RemoveRange(deleting);
            }
        }
    }
}