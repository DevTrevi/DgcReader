using System;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.BlacklistProviders.Italy.Entities
{
    /// <summary>
    /// Sync status info of the blacklist
    /// </summary>
    public class SyncStatus
    {
        /// <summary>
        /// The current version of the Crl stored on the DB
        /// </summary>
        public int CurrentVersion { get; set; }

        /// <summary>
        /// The local version Identifier
        /// </summary>
        public string? CurrentVersionId { get; set; }


        /// <summary>
        /// The targeted version
        /// </summary>
        public int TargetVersion { get; set; }

        /// <summary>
        /// The targeted version identifier
        /// </summary>
        public string? TargetVersionId { get; set; }

        /// <summary>
        /// The total number of UCVI expected for the targeted version of the Drl
        /// </summary>
        public int TargetTotalNumberUCVI { get; set; }

        /// <summary>
        /// Total chunks count of the targeted version
        /// </summary>
        public int TargetChunksCount { get; set; }

        /// <summary>
        /// Chunk size of the target version
        /// </summary>
        public int TargetChunkSize { get; set; }

        /// <summary>
        /// The last chunk number saved for the <see cref="TargetVersion"/>
        /// </summary>
        public int LastChunkSaved { get; set; }

        /// <summary>
        /// Date of last check for Clr updates
        /// </summary>
        public DateTime LastCheck { get; set; }
    }
}
