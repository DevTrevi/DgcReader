using Newtonsoft.Json;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace DgcReader.TrustListProviders.Germany.Backend
{
    internal class GermanyTrustList
    {
        [JsonProperty("certificates")]
        public CertificateEntry[] Certificates { get; set; }
    }
}
