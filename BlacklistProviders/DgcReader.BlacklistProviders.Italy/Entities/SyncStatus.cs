// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

using System;

namespace DgcReader.BlacklistProviders.Italy.Entities
{
    public class SyncStatus
    {

        /// <summary>
        /// The current version of the Crl stored on the DB
        /// </summary>
        public int LocalVersion { get; set; }

        /// <summary>
        /// The local version Identifier
        /// </summary>
        public string? LocalVersionId { get; set; }

        /// <summary>
        /// The total number of UVCI expected for the current version of the Crl
        /// </summary>
        public int TotalNumberUVCI { get; set; }

        /// <summary>
        /// Total chunks count of the current version
        /// </summary>
        public int ChunksCount { get; set; }

        /// <summary>
        /// The targeted version
        /// </summary>
        public int TargetVersion { get; set; }

        /// <summary>
        /// The targeted version identifier
        /// </summary>
        public string? TargetVersionId { get; set; }

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
