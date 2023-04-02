using Newtonsoft.Json;
using System;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace DgcReader.BlacklistProviders.Italy.Models
{
    /// <summary>
    /// One chunk of data of a Drl version
    /// </summary>
    public class DrlChunkData : IDrlVersionInfo
    {
        /// <summary>
        /// Identifier of the blacklist version
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// The version of the blacklist
        /// </summary>
        [JsonProperty("version")]
        public int Version { get; set; }

        /// <summary>
        /// Chunk number
        /// </summary>
        [JsonProperty("chunk")]
        public int Chunk { get; set; }

        /// <summary>
        /// Last chunk number
        /// </summary>
        [JsonProperty("lastChunk")]
        public int TotalChunks { get; set; }

        /// <summary>
        /// Single chunk size in bytes
        /// </summary>
        [JsonProperty("sizeSingleChunkInByte")]
        public int SingleChunkSize { get; set; }

        /// <summary>
        /// Total number of UCVIs in blacklist
        /// </summary>
        [JsonProperty("totalNumberUCVI")]
        public int TotalNumberUCVI { get; set; }

        #region Full download
        /// <summary>
        /// Revoked UCVIs for this chunk
        /// </summary>
        [JsonProperty("revokedUcvi")]
        public string[] RevokedUcviList { get; set; }


        /// <summary>
        /// Creation date of the current blacklist
        /// </summary>
        [JsonProperty("creationDate")]
        public DateTimeOffset CreationDate { get; set; }

        /// <summary>
        /// The first element in current chunk
        /// </summary>
        [JsonProperty("firstElementInChunk")]
        public string FirstElementInChunk { get; set; }

        /// <summary>
        /// The first element in current chunk
        /// </summary>/// <summary>
        /// The first element in current chunk
        /// </summary>
        [JsonProperty("lastElementInChunk")]
        public string LastElementInChunk { get; set; }

        #endregion

        #region Incremental download
        /// <summary>
        /// Checked version of the blacklist
        /// </summary>
        [JsonProperty("fromVersion")]
        public int? FromVersion { get; set; }

        /// <summary>
        /// The UCVI list delta from previous version
        /// </summary>
        [JsonProperty("delta")]
        public DrlDelta Delta { get; set; }


        #endregion

        /// <inheritdoc/>
        public override string ToString()
        {
            if (RevokedUcviList != null)
                return $"{nameof(DrlChunkData)} id {Id} - Chunk {Chunk}/{TotalChunks} - {RevokedUcviList?.Length ?? 0} entries";
            return $"{nameof(DrlChunkData)} id {Id} - Chunk {Chunk}/{TotalChunks} - {Delta}";
        }
    }

    /// <summary>
    /// Delta containing new and removed UCVIs from the previous version
    /// </summary>
    public class DrlDelta
    {
        /// <summary>
        /// UCVIs added from previous version
        /// </summary>
        [JsonProperty("insertions")]
        public string[] Insertions { get; set; }

        /// <summary>
        /// UCVIs deleted from previous version
        /// </summary>
        [JsonProperty("deletions")]
        public string[] Deletions { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"Delta: {Insertions?.Length ?? 0} insertions - {Deletions?.Length ?? 0} deletions";
        }
    }
}

