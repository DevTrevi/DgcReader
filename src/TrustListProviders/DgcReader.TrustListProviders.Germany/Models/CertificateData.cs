using DgcReader.Interfaces.TrustListProviders;
using Newtonsoft.Json;
using System;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace DgcReader.TrustListProviders.Germany.Models
{
    /// <inheritdoc cref="ITrustedCertificateData"/>
    public class CertificateData : ITrustedCertificateData
    {
        /// <inheritdoc/>
        [JsonProperty("kid")]
        public string Kid { get; set; }

        /// <inheritdoc/>
        [JsonProperty("c")]
        public string Country { get; set; }

        /// <inheritdoc/>
        [JsonProperty("ka")]
        public string? KeyAlgorithm { get; set; }

        /// <summary>
        /// Signature algorithm name
        /// </summary>
        [JsonProperty("sa")]
        public string? SignatureAlgo { get; set; }

        /// <summary>
        /// ECDsa public key parameters
        /// </summary>
        [JsonProperty("ec",
            DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore)]
        public ECParameters EC { get; set; }

        /// <summary>
        /// RSA public key parameters
        /// </summary>
        [JsonProperty("rsa",
            DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore)]
        public RSAParameters RSA { get; set; }

        /// <inheritdoc/>
        [JsonProperty("cer", NullValueHandling = NullValueHandling.Ignore)]
        public byte[] Certificate { get; set; }

        /// <inheritdoc/>
        public IECParameters GetECParameters() => EC;

        /// <inheritdoc/>
        public IRSAParameters GetRSAParameters() => RSA;

        /// <summary>
        /// Signature
        /// </summary>
        [JsonProperty("sign", NullValueHandling = NullValueHandling.Ignore)]
        public byte[] Signature { get; set; }

        /// <summary>
        /// Thumbprint
        /// </summary>
        [JsonProperty("thumbprint", NullValueHandling = NullValueHandling.Ignore)]
        public string Thumbprint { get; set; }

        /// <summary>
        /// Timestamp of the entry
        /// </summary>
        [JsonProperty("t")]
        public DateTimeOffset Timestamp { get; set; }


        /// <inheritdoc/>
        public override string ToString()
        {
            return $"Country {Country} Kid {Kid} KeyAlgo {KeyAlgorithm}";
        }
    }


}
