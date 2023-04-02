using Newtonsoft.Json;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace DgcReader.RuleValidators.Germany.CovpassDgcCertlogic.Data;

/// <summary>
/// Identifier info for an available rule
/// </summary>
public class RuleIdentifier
{

    /// <summary>
    /// Rule identifier
    /// </summary>
    [JsonProperty("identifier")]
    public string Identifier { get; set; }

    /// <summary>
    /// Rule version
    /// </summary>
    [JsonProperty("version")]
    public string Version { get; set; }

    /// <summary>
    /// Acceptance country code
    /// </summary>
    [JsonProperty("country")]
    public string Country { get; set; }

    /// <summary>
    /// Rule hash
    /// </summary>
    [JsonProperty("hash")]
    public string Hash { get; set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{Country}: Id {Identifier} (v {Version})";
    }
}
