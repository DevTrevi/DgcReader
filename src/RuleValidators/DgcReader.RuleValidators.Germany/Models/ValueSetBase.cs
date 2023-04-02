// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

using Newtonsoft.Json;
using System;

namespace DgcReader.RuleValidators.Germany.Models;

/// <summary>
/// Base class for implementing valueset containers
/// </summary>
public abstract class ValueSetBase
{
    /// <summary>
    /// Datetime of the last update for the valueset
    /// </summary>
    [JsonProperty("upd",
        DefaultValueHandling = DefaultValueHandling.Ignore,
        NullValueHandling = NullValueHandling.Ignore)]
    public DateTimeOffset LastUpdate { get; set; }
}
