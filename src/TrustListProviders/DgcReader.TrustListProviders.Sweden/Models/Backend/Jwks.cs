using Newtonsoft.Json;
using System.Numerics;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0
// Based on the original work of Myndigheten för digital förvaltning (DIGG)

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace DgcReader.TrustListProviders.Sweden.Models
{
    public partial class Jwks
    {
        [JsonProperty("crv")]
        public string Crv { get; set; }

        [JsonProperty("kid")]
        public string Kid { get; set; }

        [JsonProperty("kty")]
        public string Kty { get; set; }

        [JsonProperty("x")]
        public string X { get; set; }

        [JsonProperty("x5a")]
        public X5A X5A { get; set; }

        [JsonProperty("x5t#S256")]
        public string X5TS256 { get; set; }

        [JsonProperty("y")]
        public string Y { get; set; }

        [JsonProperty("n")]
        public string N { get; set; }

        [JsonProperty("e")]
        public string E { get; set; }
    }

    public partial class X5A
    {
        [JsonProperty("issuer")]
        public string Issuer { get; set; }

        [JsonProperty("serial")]
        public BigInteger Serial { get; set; }

        [JsonProperty("subject")]
        public string Subject { get; set; }
    }

}
