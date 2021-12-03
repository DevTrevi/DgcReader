using Newtonsoft.Json;
using System;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Germany.Backend
{
    public class RuleIdentifier
    {
        [JsonProperty("identifier")]
        public string Identifier { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }


        [JsonProperty("hash")]
        public string Hash { get; set; }

        public override string ToString()
        {
            return $"{Country}: Id {Identifier} (v {Version})";
        }
    }
}
