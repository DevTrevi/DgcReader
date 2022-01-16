using DgcReader.Interfaces.TrustListProviders;
using Newtonsoft.Json;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace DgcReader.TrustListProviders.Italy.Models
{
    /// <inheritdoc cref="IECParameters"/>
    public class ECParameters : IECParameters
    {
        /// <inheritdoc />
        [JsonProperty("cf", NullValueHandling = NullValueHandling.Ignore)]
        public string? CurveFriendlyName { get; set; }

        /// <inheritdoc />
        [JsonProperty("c")]
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
