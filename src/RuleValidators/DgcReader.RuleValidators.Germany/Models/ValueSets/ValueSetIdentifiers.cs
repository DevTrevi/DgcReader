using Newtonsoft.Json;
using System.Collections.Generic;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace DgcReader.RuleValidators.Germany.Models.ValueSets;

/// <summary>
/// Identifiers of the available valuesets
/// </summary>
public class ValueSetIdentifiers : ValueSetBase
{
    /// <summary>
    /// Identifiers for getting valuesets
    /// </summary>
    [JsonProperty("identifiers")]
    public IEnumerable<ValueSetIdentifier> Identifiers { get; set; }
}

/// <summary>
/// A valueset identifier
/// </summary>
public class ValueSetIdentifier
{
    /// <summary>
    /// Id of the valueset
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; }


    /// <summary>
    /// Hash of the valueset
    /// </summary>
    [JsonProperty("hash")]
    public string Hash { get; set; }
}
