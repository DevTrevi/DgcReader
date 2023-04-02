using DgcReader.Interfaces.TrustListProviders;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X9;
using System.Linq;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.TrustListProviders.Sweden.Models;

/// <inheritdoc cref="IECParameters"/>
public class ECParameters : IECParameters
{
    /// <inheritdoc/>
    public ECParameters()
    {
    }

    /// <inheritdoc/>
    public ECParameters(
        DerObjectIdentifier oid,
        Org.BouncyCastle.Crypto.Parameters.ECPublicKeyParameters p)
    {
        Curve = oid.Id;
        X = p.Q.XCoord.ToBigInteger().ToByteArray();
        Y = p.Q.YCoord.ToBigInteger().ToByteArray();
    }

    /// <inheritdoc />
    [JsonProperty("cf", NullValueHandling = NullValueHandling.Ignore)]
    public string CurveFriendlyName { get; set; }
    
    /// <inheritdoc />
    [JsonProperty("c")]
    public string Curve { get; set; }

    /// <inheritdoc />
    [JsonProperty("x")]
    public byte[] X { get; set; }

    /// <inheritdoc />
    [JsonProperty("y")]
    public byte[] Y { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        var curve = Curve ?? "";
        if (!string.IsNullOrEmpty(CurveFriendlyName))
            curve += $" ({CurveFriendlyName})";

        return $"ECParameters: Curve {curve} - X: {X} - Y: {Y}";
    }
}
