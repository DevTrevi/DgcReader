using DgcReader.BlacklistProviders.Italy.Entities;
using System;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.BlacklistProviders.Italy
{
    /// <summary>
    /// Download progress details for <see cref="ItalianDrlBlacklistProvider"/>
    /// </summary>
    public class DownloadProgressEventArgs : EventArgs
    {
        internal DownloadProgressEventArgs(SyncStatus status)
        {
            CurrentVersion = status.CurrentVersion;
            TargetVersion = status.TargetVersion;
            CurrentChunk = status.LastChunkSaved;
            TotalChunks = status.TargetChunksCount;
        }

        /// <summary>
        /// Current version of the Drl stored on DB
        /// </summary>
        public int CurrentVersion { get; internal set; }

        /// <summary>
        /// Target version of the Drl
        /// </summary>
        public int TargetVersion { get; internal set; }

        /// <summary>
        /// Chunk number currently stored on the db
        /// </summary>
        public int CurrentChunk { get; internal set; }

        /// <summary>
        /// Total chunks count
        /// </summary>
        public int TotalChunks { get; internal set; }

        /// <summary>
        /// Total number of insertions for this version
        /// </summary>
        public int TotalInsertions { get; internal set; }

        /// <summary>
        /// Total number of deletions for this version
        /// </summary>
        public int TotalDeletions { get; internal set; }

        /// <summary>
        /// Total updates (insertions + deletions) for this version
        /// </summary>
        public int TotalUpdates => TotalInsertions + TotalDeletions;

        /// <summary>
        /// The progress percentage, from 0 to 1
        /// </summary>
        public float TotalProgressPercent
        {
            get
            {
                if (TotalChunks <= 0)
                    return 1;
                return ((float)CurrentChunk / TotalChunks);
            }
        }

        /// <summary>
        /// Indicates if the update is completed
        /// </summary>
        public bool IsCompleted => CurrentChunk == TotalChunks;

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"Updating version {CurrentVersion}->{TargetVersion}: {TotalProgressPercent:p}";
        }
    }
}