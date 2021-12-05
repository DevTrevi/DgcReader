// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

using Newtonsoft.Json;
using System;

namespace DgcReader.RuleValidators.Germany.Models
{
    public abstract class ValueSetBase
    {
        [JsonProperty("upd",
            DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset LastUpdate { get; set; }
    }
}
