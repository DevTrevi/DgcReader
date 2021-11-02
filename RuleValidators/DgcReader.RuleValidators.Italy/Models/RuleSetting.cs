using Newtonsoft.Json;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Italy.Models
{
    public class RuleSetting
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("value")]
        public string Value { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"Rule {Name} ({Type}): {Value}";
        }
    }
}
