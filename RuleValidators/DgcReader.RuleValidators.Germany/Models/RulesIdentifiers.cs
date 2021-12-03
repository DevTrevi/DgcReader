using DgcReader.RuleValidators.Germany.Backend;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Germany.Models
{

    public class RulesIdentifiers
    {
        /// <summary>
        /// Instant when the identifiers was updated
        /// </summary>
        [JsonProperty("upd",
            DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset LastUpdate { get; set; }

        [JsonProperty("identifiers")]
        public IEnumerable<RuleIdentifier> Identifiers { get; set; }
    }
}
