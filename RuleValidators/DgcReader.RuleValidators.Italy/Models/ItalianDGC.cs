using GreenpassReader.Models;
using Newtonsoft.Json;
using System;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Italy.Models
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


        /// <summary>
        /// Deserialize an <see cref="ItalianDGC"/> from json
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static new ItalianDGC? FromJson(string json) => JsonConvert.DeserializeObject<ItalianDGC>(json, Converter.Settings);
    }

    /// <summary>
    /// Italian Exemption entry
    /// </summary>
    public class ExemptionEntry : ICertificateEntry
    {
        /// <inheritdoc/>
        [JsonProperty("tg")]
        public string TargetedDiseaseAgent => throw new NotImplementedException();

        /// <inheritdoc/>
        [JsonProperty("co")]
        public string Country => throw new NotImplementedException();

        /// <inheritdoc/>
        [JsonProperty("is")]
        public string Issuer => throw new NotImplementedException();

        /// <inheritdoc/>
        [JsonProperty("ci")]
        public string CertificateIdentifier => throw new NotImplementedException();


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
