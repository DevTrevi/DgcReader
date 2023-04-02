using DgcReader.RuleValidators.Germany.CovpassDgcCertlogic.Data;
using Newtonsoft.Json;
using System.Collections.Generic;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace DgcReader.RuleValidators.Germany.Models.Rules;

/// <summary>
/// Identifiers of the available rules
/// </summary>
public class RulesIdentifiers : ValueSetBase
{
    /// <summary>
    /// Identifiers for getting rules
    /// </summary>
    [JsonProperty("identifiers")]
    public IEnumerable<RuleIdentifier> Identifiers { get; set; }
}
