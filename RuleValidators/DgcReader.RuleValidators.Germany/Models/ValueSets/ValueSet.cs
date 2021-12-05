using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;

namespace DgcReader.RuleValidators.Germany.Models.ValueSets
{
    public class ValueSet : ValueSetBase
    {
        [JsonProperty("valueSetId")]
        public string Id { get; set; }

        [JsonProperty("valueSetDate")]
        public DateTime Date { get; set; }

        [JsonProperty("valueSetValues")]
        public ReadOnlyDictionary<string, ValueSetEntry> Values { get; set; }
    }

    public class ValueSetEntry
    {
        [JsonProperty("display")]
        public string DisplayName { get; set; }

        [JsonProperty("lang")]
        public string LanguageCode { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("system")]
        public string System { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        public override string ToString()
        {
            return $"{DisplayName} (language: {LanguageCode} - active: {Active}";
        }
    }

}
