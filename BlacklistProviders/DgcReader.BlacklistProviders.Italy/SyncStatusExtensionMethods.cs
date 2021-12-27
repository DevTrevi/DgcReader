using DgcReader.BlacklistProviders.Italy.Entities;
using DgcReader.BlacklistProviders.Italy.Models;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.BlacklistProviders.Italy
{
    internal static class SyncStatusExtensionMethods
    {
        /// <summary>
        /// Check if the remote version is the currently stored version
        /// </summary>
        /// <param name="remoteStatus"></param>
        /// <param name="localStatus"></param>
        /// <returns></returns>
        public static bool IsSameVersion(this SyncStatus localStatus, IDrlVersionInfo remoteStatus) => remoteStatus.Version == localStatus.CurrentVersion;

        /// <summary>
        /// Check if the version number of the target snapshot match the remote one
        /// </summary>
        /// <param name="remoteStatus"></param>
        /// <param name="localStatus"></param>
        /// <returns></returns>
        public static bool IsTargetVersion(this SyncStatus localStatus, IDrlVersionInfo remoteStatus) => remoteStatus.Version == localStatus.TargetVersion;

        /// <summary>
        /// Check if the target version is consistent with the remote version
        /// </summary>
        /// <param name="remoteStatus"></param>
        /// <param name="localStatus"></param>
        /// <returns></returns>
        public static bool IsTargetVersionConsistent(this SyncStatus localStatus, IDrlVersionInfo remoteStatus)
            => localStatus.IsTargetVersion(remoteStatus) &&
                    localStatus.TargetVersion == remoteStatus.Version &&
                    localStatus.TargetVersionId == remoteStatus.Id &&
                    localStatus.TargetTotalNumberUCVI == remoteStatus.TotalNumberUCVI &&
                    localStatus.TargetChunksCount == remoteStatus.TotalChunks &&
                    localStatus.TargetChunkSize == remoteStatus.SingleChunkSize;

        /// <summary>
        /// Check if at least one chunk has already been stored for the current version
        /// </summary>
        /// <param name="localStatus"></param>
        /// <returns></returns>
        public static bool AnyChunkDownloaded(this SyncStatus localStatus) => localStatus.LastChunkSaved > 0;

        /// <summary>
        /// Check if a new version is available but not all the chunks where downloaded
        /// </summary>
        /// <param name="localStatus"></param>
        /// <returns></returns>
        public static bool HasPendingDownload(this SyncStatus localStatus)
            => localStatus.LastChunkSaved < localStatus.TargetChunksCount;

        /// <summary>
        /// Check if the DB is empty according to the current status
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public static bool HasCurrentVersion(this SyncStatus status) => status.CurrentVersion > 0;

        /// <summary>
        /// Check if the current version of the Dlr is aligned with the targeted version
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public static bool CurrentVersionMatchTarget(this SyncStatus status)
            => status.CurrentVersion == status.TargetVersion && status.CurrentVersionId == status.TargetVersionId;
    }
}