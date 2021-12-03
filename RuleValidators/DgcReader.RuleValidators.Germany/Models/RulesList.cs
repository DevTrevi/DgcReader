using DgcReader.RuleValidators.Germany.Backend;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Germany.Models
{
    /// <summary>
    /// A rules list containing rule entries
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
        public IEnumerable<RuleEntry> Rules { get; set; }
    }
}
