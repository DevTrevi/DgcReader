using GreenpassReader.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.Deserializers.Italy.Models
{
    /// <summary>
    /// Italian customization of the EU Digital Green Certificate, including exemptions
    /// </summary>
    public class ItalianDGC : EuDGC
    {
        /// <summary>
        /// Exemptions Group
        /// </summary>
        [JsonProperty("e", NullValueHandling = NullValueHandling.Ignore)]
        public ExemptionEntry[]? Exemptions { get; internal set; }

        /// <inheritdoc/>
        public override IEnumerable<ICertificateEntry> GetCertificateEntries()
        {
            return base.GetCertificateEntries()
                .Union(Exemptions ?? Enumerable.Empty<ICertificateEntry>());
        }
    }

    /// <summary>
    /// Italian Exemption entry
    /// </summary>
    public class ExemptionEntry : ICertificateEntry
    {
        /// <inheritdoc/>
        [JsonProperty("tg")]
        public string TargetedDiseaseAgent { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("co")]
        public string Country { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("is")]
        public string Issuer { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("ci")]
        public string CertificateIdentifier { get; internal set; }


        /// <summary>
        /// Certificate Valid From
        /// </summary>
        [JsonProperty("df")]
        public DateTimeOffset ValidFrom { get; internal set; }

        /// <summary>
        /// Certificate Valid Until
        /// </summary>
        [JsonProperty("du")]
        public DateTimeOffset? ValidUntil { get; internal set; }
    }
}
