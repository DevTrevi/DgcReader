// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.TrustListProviders
{
    /// <summary>
    /// Elliptic Curve algorithm parameters
    /// </summary>
    public interface IECParameters
    {
        /// <summary>
        /// The code of the named curve used for the ECDsa key
        /// </summary>
        string Curve { get; }

        /// <summary>
        /// The friendly name of the named curve used for the ECDsa key
        /// </summary>
        string CurveFriendlyName { get; }

        /// <summary>
        /// X parameter of the public key
        /// </summary>
        byte[] X { get; }

        /// <summary>
        /// Y parameter of the public key
        /// </summary>
        byte[] Y { get; }
    }
}