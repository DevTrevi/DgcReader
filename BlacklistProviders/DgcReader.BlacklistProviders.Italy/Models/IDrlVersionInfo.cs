// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.BlacklistProviders.Italy.Models
{
    /// <summary>
    /// Informations of a Drl version
    /// </summary>
    public interface IDrlVersionInfo
    {
        /// <summary>
        /// Identifier of the blacklist version
        /// </summary>
        string Id { get; set; }

        /// <summary>
        /// The version of the blacklist
        /// </summary>
        int Version { get; set; }

        /// <summary>
        /// Total chunks count
        /// </summary>
        int TotalChunks { get; set; }

        /// <summary>
        /// Total number of UCVIs in blacklist
        /// </summary>
        int TotalNumberUCVI { get; set; }

        /// <summary>
        /// Single chunk size in bytes
        /// </summary>
        public int SingleChunkSize { get; set; }
    }
}

