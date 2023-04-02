using DgcReader.Interfaces.TrustListProviders;
using DgcReader.TrustListProviders.Abstractions.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.TrustListProviders.Sweden.Models;

/// <inheritdoc cref="ITrustList"/>
public class TrustList : ITrustList
{
    /// <inheritdoc/>
    [JsonProperty("upd")]
    public DateTimeOffset LastUpdate { get; set; }


    /// <inheritdoc/>
    [JsonProperty("exp")]
    public DateTimeOffset? Expiration { get; set; }

    /// <summary>
    /// Issued at time
    /// </summary>
    [JsonProperty("iat")]
    public DateTimeOffset IssuedAt { get; set; }

    /// <summary>
    /// Identifier of DSC-TL
    /// </summary>
    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
    public string Id { get; set; }

    /// <summary>
    /// Identifier of the issuer of the DSC-TL
    /// </summary>
    [JsonProperty("iss")]
    public string Issuer { get; set; }


    /// <inheritdoc/>
    [JsonProperty("certificates")]
    public IEnumerable<CertificateData> Certificates { get; set; }

    IEnumerable<ITrustedCertificateData> ITrustList.Certificates => Certificates;
}
