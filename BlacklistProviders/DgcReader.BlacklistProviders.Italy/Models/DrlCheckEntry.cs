using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DgcReader.BlacklistProviders.Italy.Models
{
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
        /// Total number of UVCIs in blacklist
        /// </summary>
        int TotalNumberUCVI { get; set; }
    }

    public class DrlCheckEntry : IDrlVersionInfo
    {
        /// <summary>
        /// Identifier of the Dlr version
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Checked version of the blacklist
        /// </summary>
        [JsonProperty("fromVersion")]
        public int? FromVersion { get; set; }

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
        /// Number of entries added from version <see cref="FromVersion"/> relative to <see cref="Version"/>
        /// </summary>
        [JsonProperty("numDiAdd")]
        public int NumDiAdd { get; set; }

        /// <summary>
        /// Number of entries deleted from version <see cref="FromVersion"/> relative to <see cref="Version"/>
        /// </summary>
        [JsonProperty("numDiDelete")]
        public int NumDiDelete { get; set; }

        /// <summary>
        /// Total size of chunks in bytes
        /// </summary>
        [JsonProperty("totalSizeInByte")]
        public long TotalSizeInByte { get; set; }

        /// <summary>
        /// Single chunk size in bytes
        /// </summary>
        [JsonProperty("sizeSingleChunkInByte")]
        public int SingleChunkSize { get; set; }

        /// <summary>
        /// Total chunks count
        /// </summary>
        [JsonProperty("totalChunk")]
        public int TotalChunks { get; set; }

        /// <summary>
        /// Total number of UVCIs in blacklist
        /// </summary>
        [JsonProperty("totalNumberUCVI")]
        public int TotalNumberUCVI { get; set; }
    }
}

