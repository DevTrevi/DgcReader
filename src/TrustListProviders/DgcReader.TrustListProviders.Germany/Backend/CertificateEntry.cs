using Newtonsoft.Json;
using System;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace DgcReader.TrustListProviders.Germany.Backend
{
    public class CertificateEntry
    {
        [JsonProperty("certificateType")]
        public string CertificateType { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("kid")]
        public string Kid { get; set; }

        [JsonProperty("rawData")]
        public byte[] RawData { get; set; }

        [JsonProperty("signature")]
        public byte[] Signature { get; set; }

        [JsonProperty("thumbprint")]
        public string Thumbprint { get; set; }

        [JsonProperty("timestamp")]
        public DateTimeOffset Timestamp { get; set; }
    }
}
