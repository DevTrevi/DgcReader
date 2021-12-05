using Newtonsoft.Json;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Germany.CovpassDgcCertlogic.Data
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
