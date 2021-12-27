using DgcReader.Interfaces.TrustListProviders;
using Newtonsoft.Json;
using System.Linq;
using System.Security.Cryptography;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.TrustListProviders.Germany.Models
{
    /// <inheritdoc cref="IECParameters"/>
    public class ECParameters : IECParameters
    {
        /// <inheritdoc/>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public ECParameters()
        {
        }

#if NET452
        /// <inheritdoc/>
        public ECParameters(Org.BouncyCastle.Crypto.Parameters.ECPublicKeyParameters p)
        {
            Curve = p.PublicKeyParamSet.Id;
            X = p.Q.XCoord.ToBigInteger().ToByteArray();
            Y = p.Q.YCoord.ToBigInteger().ToByteArray();
        }
#else
        /// <inheritdoc/>
        public ECParameters(System.Security.Cryptography.ECParameters p)
        {
            Curve = p.Curve.Oid.Value;
            CurveFriendlyName = p.Curve.Oid.FriendlyName;

            X = p.Q.X?.ToArray() ?? new byte[0];
            Y = p.Q.Y?.ToArray() ?? new byte[0];
        }
#endif
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        /// <inheritdoc />
        [JsonProperty("cf", NullValueHandling = NullValueHandling.Ignore)]
        public string? CurveFriendlyName { get; set; }

        /// <inheritdoc />
        [JsonProperty("c", NullValueHandling = NullValueHandling.Ignore)]
        public string? Curve { get; set; }

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


}
