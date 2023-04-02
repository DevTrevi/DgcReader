using Newtonsoft.Json;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Italy.Models
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    /// <summary>
    /// A rule setting entry
    /// </summary>
    public class RuleSetting
    {
        /// <summary>
        /// Rule name identifier
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Rule type
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Rule value
        /// </summary>
        [JsonProperty("value")]

        public string Value { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"Rule {Name} ({Type}): {Value}";
        }
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
}
