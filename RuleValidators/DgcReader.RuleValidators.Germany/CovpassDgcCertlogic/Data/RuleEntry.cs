using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Germany.CovpassDgcCertlogic.Data
{
    public class RuleEntry
    {
        [JsonProperty("Identifier")]
        public string Identifier { get; set; }

        [JsonProperty("Type")]
        public RuleType Type { get; set; }

        [JsonProperty("Country")]
        public string CountryCode { get; set; }

        [JsonProperty("Version")]
        public string Version { get; set; }


        [JsonProperty("SchemaVersion")]
        public string SchemaVersion { get; set; }

        [JsonProperty("Engine")]
        public string Engine { get; set; }

        [JsonProperty("EngineVersion")]
        public string EngineVersion { get; set; }

        [JsonProperty("CertificateType")]
        public RuleCertificateType CertificateType { get; set; }

        [JsonProperty("Description")]
        public RuleEntryDescription[] Descriptions { get; set; }

        [JsonProperty("ValidFrom")]
        public DateTimeOffset ValidFrom { get; set; }

        [JsonProperty("ValidTo")]
        public DateTimeOffset ValidTo { get; set; }

        [JsonProperty("AffectedFields")]
        public string[] AffectedString { get; set; }

        [JsonProperty("Logic")]
        public JObject Logic { get; set; }

        [JsonProperty("region", NullValueHandling = NullValueHandling.Ignore)]
        public string? Region { get; set; }


        public override string ToString()
        {
            return $"({Type}) {Descriptions?.FirstOrDefault()?.Descrption} ({Identifier} v{Version})";
        }
    }
}
