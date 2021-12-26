using Newtonsoft.Json;
using System;

namespace DgcReader.BlacklistProviders.Italy.Models
{
    public class DrlEntry : IDrlVersionInfo
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
        /// Total number of UVCIs in blacklist
        /// </summary>
        [JsonProperty("totalNumberUCVI")]
        public int TotalNumberUCVI { get; set; }

        #region Full download
        /// <summary>
        /// Revoked UVCIs for this chunk
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
        /// The UVCI list delta from previous version
        /// </summary>
        [JsonProperty("delta")]
        public DrlDelta Delta { get; set; }


        #endregion

        /// <inheritdoc/>
        public override string ToString()
        {
            if (RevokedUcviList != null)
                return $"{nameof(DrlEntry)} id {Id} - Chunk {Chunk}/{TotalChunks} - {RevokedUcviList?.Length ?? 0} entries";
            return $"{nameof(DrlEntry)} id {Id} - Chunk {Chunk}/{TotalChunks} - {Delta}";
        }
    }

    public class DrlDelta
    {
        /// <summary>
        /// UVCIs added from previous version
        /// </summary>
        [JsonProperty("insertions")]
        public string[] Insertions { get; set; }

        /// <summary>
        /// UVCIs deleted from previous version
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

