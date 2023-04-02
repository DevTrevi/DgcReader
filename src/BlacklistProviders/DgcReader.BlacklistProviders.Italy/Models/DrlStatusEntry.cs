using Newtonsoft.Json;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace DgcReader.BlacklistProviders.Italy.Models;

/// <summary>
/// Status informations of the current Drl version compared to a previous one
/// </summary>
public class DrlStatusEntry : IDrlVersionInfo
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
    /// Total number of UCVIs in blacklist
    /// </summary>
    [JsonProperty("totalNumberUCVI")]
    public int TotalNumberUCVI { get; set; }
}

