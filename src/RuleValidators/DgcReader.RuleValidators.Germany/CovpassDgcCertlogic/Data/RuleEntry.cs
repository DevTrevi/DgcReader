using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace DgcReader.RuleValidators.Germany.CovpassDgcCertlogic.Data
{
    /// <summary>
    /// A validation rule entry
    /// </summary>
    public class RuleEntry
    {
        /// <summary>
        /// Rule indentifier
        /// </summary>
        [JsonProperty("Identifier")]
        public string Identifier { get; protected internal set; }

        /// <summary>
        /// Rule type
        /// </summary>
        [JsonProperty("Type")]
        public RuleType Type { get; protected internal set; }

        /// <summary>
        /// Country code
        /// </summary>
        [JsonProperty("Country")]
        public string CountryCode { get; protected internal set; }

        /// <summary>
        /// Rule version
        /// </summary>
        [JsonProperty("Version")]
        public string Version { get; protected internal set; }

        /// <summary>
        /// Schema version
        /// </summary>
        [JsonProperty("SchemaVersion")]
        public string SchemaVersion { get; protected internal set; }

        /// <summary>
        /// The engine required to validate the rule
        /// </summary>
        [JsonProperty("Engine")]
        public string Engine { get; protected internal set; }

        /// <summary>
        /// The engine version required to validate the rule
        /// </summary>
        [JsonProperty("EngineVersion")]
        public string EngineVersion { get; protected internal set; }

        /// <summary>
        /// Certificate type for which the rule can be applied
        /// </summary>
        [JsonProperty("CertificateType")]
        public RuleCertificateType CertificateType { get; protected internal set; }

        /// <summary>
        /// Descriptions of the rule in multiple languages
        /// </summary>
        [JsonProperty("Description")]
        public RuleEntryDescription[] Descriptions { get; protected internal set; }

        /// <summary>
        /// Validity start of the rule
        /// </summary>
        [JsonProperty("ValidFrom")]
        public DateTimeOffset ValidFrom { get; protected internal set; }

        /// <summary>
        /// Validiti end of the rule
        /// </summary>
        [JsonProperty("ValidTo")]
        public DateTimeOffset ValidTo { get; protected internal set; }

        /// <summary>
        /// Fields affected by the rule
        /// </summary>
        [JsonProperty("AffectedFields")]
        public string[] AffectedString { get; protected internal set; }

        /// <summary>
        /// The logic of the rule
        /// </summary>
        [JsonProperty("Logic")]


        public JObject Logic { get; protected internal set; }

        /// <summary>
        /// Specific region of the rule
        /// </summary>
        [JsonProperty("region", NullValueHandling = NullValueHandling.Ignore)]
        public string? Region { get; protected internal set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"({Type}) {Descriptions?.GetDescription(Thread.CurrentThread.CurrentUICulture)} ({Identifier} v{Version})";
        }
    }
}
