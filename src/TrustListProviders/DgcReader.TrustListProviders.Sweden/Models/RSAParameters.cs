using DgcReader.Interfaces.TrustListProviders;
using Newtonsoft.Json;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.TrustListProviders.Sweden.Models;

/// <inheritdoc cref="IRSAParameters"/>
public class RSAParameters : IRSAParameters
{
    /// <inheritdoc />
    public RSAParameters()
    {

    }
    /// <inheritdoc />
    public RSAParameters(Org.BouncyCastle.Crypto.Parameters.RsaKeyParameters p)
    {
        Exponent = p.Exponent.ToByteArray();
        Modulus = p.Modulus.ToByteArray();
    }


    /// <inheritdoc />
    [JsonProperty("n")]
    public byte[] Modulus { get; set; }

    /// <inheritdoc />
    [JsonProperty("e")]
    public byte[] Exponent { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"RSAParameters: Mod: {Modulus} - Exp: {Exponent}";
    }
}
