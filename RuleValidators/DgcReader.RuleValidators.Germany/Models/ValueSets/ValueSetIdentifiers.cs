using Newtonsoft.Json;
using System.Collections.Generic;

namespace DgcReader.RuleValidators.Germany.Models.ValueSets
{
    internal class ValueSetIdentifiers : ValueSetBase
    {
        [JsonProperty("identifiers")]
        public IEnumerable<ValueSetIdentifier> Identifiers { get; set; }
    }

    public class ValueSetIdentifier
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("hash")]
        public string Hash { get; set; }
    }

}
