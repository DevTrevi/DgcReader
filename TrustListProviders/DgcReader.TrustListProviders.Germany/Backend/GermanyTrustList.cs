using Newtonsoft.Json;
using System;

namespace DgcReader.TrustListProviders.Germany.Backend
{
    internal class GermanyTrustList
    {
        [JsonProperty("certificates")]
        public CertificateEntry[] Certificates { get; set; }
    }
}
