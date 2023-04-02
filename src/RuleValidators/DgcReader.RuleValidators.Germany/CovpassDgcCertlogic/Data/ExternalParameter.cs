using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace DgcReader.RuleValidators.Germany.CovpassDgcCertlogic.Data
{
    /// <summary>
    /// External parameters for the rules validator
    /// </summary>
    public class ExternalParameter
    {
        /// <summary>
        /// The validation instant
        /// </summary>
        [JsonProperty("validationClock")]
        public DateTimeOffset ValidationClock { get; protected internal set; }

        /// <summary>
        /// Valuesets available during rules validation
        /// </summary>
        [JsonProperty("valueSets")]
        public Dictionary<string, JObject> ValueSets { get; protected internal set; }

        /// <summary>
        /// The acceptance country code
        /// </summary>
        [JsonProperty("countryCode")]
        public string CountryCode { get; protected internal set; }

        /// <summary>
        /// Expiration of the signed object
        /// </summary>
        [JsonProperty("exp")]
        public DateTimeOffset Expiration { get; protected internal set; }

        /// <summary>
        /// Validity start date of the signed object
        /// </summary>
        [JsonProperty("iat")]
        public DateTimeOffset ValidFrom { get; protected internal set; }

        /// <summary>
        /// The issuer country code of the certificate
        /// </summary>
        [JsonProperty("issuerCountryCode ")]
        public string IssuerCountryCode { get; protected internal set; }

        /// <summary>
        /// The key identifier of the signed DGC
        /// </summary>
        [JsonProperty("kid")]
        public string Kid { get; protected internal set; }

        /// <summary>
        /// Region
        /// </summary>
        [JsonProperty("region")]
        public string Region { get; protected internal set; }
    }
}
