using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Germany.CovpassDgcCertlogic.Data
{
    public class ExternalParameter
    {

        [JsonProperty("validationClock")]
        public DateTimeOffset ValidationClock { get; set; }

        [JsonProperty("valueSets")]
        public Dictionary<string, JObject> ValueSets { get; set; }

        [JsonProperty("countryCode")]
        public string CountryCode { get; set; }

        [JsonProperty("exp")]
        public DateTimeOffset Expiration { get; set; }

        [JsonProperty("iat")]
        public DateTimeOffset ValidFrom { get; set; }

        [JsonProperty("issuerCountryCode ")]
        public string IssuerCountryCode { get; set; }

        [JsonProperty("kid")]
        public string Kid { get; set; }

        [JsonProperty("region")]
        public string Region { get; set; }
    }
}
