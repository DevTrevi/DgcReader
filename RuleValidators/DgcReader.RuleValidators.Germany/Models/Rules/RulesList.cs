using DgcReader.RuleValidators.Germany.CovpassDgcCertlogic.Data;
using Newtonsoft.Json;
using System.Collections.Generic;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace DgcReader.RuleValidators.Germany.Models.Rules
{
    /// <summary>
    /// A rules list containing rule entries
    /// </summary>
    public class RulesList : ValueSetBase
    {
        /// <summary>
        /// Validation rules
        /// </summary>
        [JsonProperty("rules")]
        public IEnumerable<RuleEntry> Rules { get; set; }
    }
}
