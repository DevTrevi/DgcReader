using Newtonsoft.Json;
using System;
using System.Collections.Generic;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Italy.Models
{
    /// <summary>
    /// The italian rules list
    /// </summary>
    public class RulesList
    {
        /// <summary>
        /// Instant when the rules list was updated
        /// </summary>
        [JsonProperty("upd",
            DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset LastUpdate { get; set; }

        /// <summary>
        /// Validation rules
        /// </summary>
        [JsonProperty("rules")]
        public IEnumerable<RuleSetting> Rules { get; set; } = new RuleSetting[0];
    }
}
