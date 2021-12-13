using DgcReader.Interfaces.TrustListProviders;
using Newtonsoft.Json;
using System.Linq;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.TrustListProviders.Italy.Models
{
    /// <inheritdoc cref="IRSAParameters"/>
    public class RSAParameters : IRSAParameters
    {

#if NET452

        /// <inheritdoc />
        public RSAParameters(Org.BouncyCastle.Crypto.Parameters.RsaKeyParameters p)
        {
            Exponent = p.Exponent.ToByteArray();
            Modulus = p.Modulus.ToByteArray();
        }
#else

        /// <inheritdoc />
        public RSAParameters(System.Security.Cryptography.RSAParameters p)
        {
            Modulus = p.Modulus.ToArray();
            Exponent = p.Exponent.ToArray();
        }
#endif

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
