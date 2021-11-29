using DgcReader.Interfaces.TrustListProviders;
using DgcReader.TrustListProviders.Abstractions.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.TrustListProviders.Germany.Models
{
    /// <inheritdoc cref="ITrustList"/>
    public class TrustList : ITrustList
    {
        /// <inheritdoc/>
        [JsonProperty("upd")]
        public DateTimeOffset LastUpdate { get; set; }

        /// <inheritdoc/>
        [JsonProperty("certificates")]
        public IEnumerable<CertificateData> Certificates { get; set; }

        /// <inheritdoc/>
        public DateTimeOffset? Expiration => null;

        IEnumerable<ITrustedCertificateData> ITrustList.Certificates => Certificates;
    }
}
