using Newtonsoft.Json;
using System;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Germany.Backend
{
    public class RuleEntry
    {
        [JsonProperty("Identifier")]
        public string Identifier { get; set; }

        [JsonProperty("Type")]
        public string Type { get; set; }

        [JsonProperty("Country")]
        public string Country { get; set; }

        [JsonProperty("Version")]
        public string Version { get; set; }


        [JsonProperty("SchemaVersion")]
        public string SchemaVersion { get; set; }

        [JsonProperty("Engine")]
        public string Engine { get; set; }

        [JsonProperty("EngineVersion")]
        public string EngineVersion { get; set; }

        [JsonProperty("CertificateType")]
        public string CertificateType { get; set; }

        [JsonProperty("Description")]
        public RuleEntryDescription[] Description { get; set; }

        [JsonProperty("ValidFrom")]
        public DateTimeOffset ValidFrom { get; set; }

        [JsonProperty("ValidTo")]
        public DateTimeOffset ValidTo { get; set; }

        [JsonProperty("AffectedFields")]
        public string[] AffectedFields { get; set; }

        [JsonProperty("Logic")]
        public object Logic { get; set; }
    }
}
