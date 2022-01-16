using DgcReader.Interfaces.TrustListProviders;
using Newtonsoft.Json;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.TrustListProviders.Italy.Models
{
    /// <inheritdoc cref="IRSAParameters"/>
    public class RSAParameters : IRSAParameters
    {
        /// <inheritdoc/>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public RSAParameters()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {

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


}
