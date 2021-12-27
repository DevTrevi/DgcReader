using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace DgcReader.RuleValidators.Germany.Models.ValueSets
{
    /// <summary>
    /// Valueset
    /// </summary>
    public class ValueSet : ValueSetBase
    {
        /// <summary>
        /// The valueset identifier
        /// </summary>
        [JsonProperty("valueSetId")]
        public string Id { get; set; }

        /// <summary>
        /// Date of the valueset, from the remote server
        /// </summary>
        [JsonProperty("valueSetDate")]
        public DateTime Date { get; set; }

        /// <summary>
        /// Values of the valueset
        /// </summary>
        [JsonProperty("valueSetValues")]
        public ReadOnlyDictionary<string, ValueSetEntry> Values { get; set; }
    }

    /// <summary>
    /// Valueset entry
    /// </summary>
    public class ValueSetEntry
    {
        /// <summary>
        /// Display name for the value
        /// </summary>
        [JsonProperty("display")]
        public string DisplayName { get; set; }

        /// <summary>
        /// 2-letter ISO language code
        /// </summary>
        [JsonProperty("lang")]
        public string LanguageCode { get; set; }

        /// <summary>
        /// Enabling flag for the valueset value
        /// </summary>
        [JsonProperty("active")]
        public bool Active { get; set; }

        /// <summary>
        /// System
        /// </summary>
        [JsonProperty("system")]
        public string System { get; set; }

        /// <summary>
        /// Version
        /// </summary>
        [JsonProperty("version")]
        public string Version { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{DisplayName} (language: {LanguageCode} - active: {Active}";
        }
    }

}
