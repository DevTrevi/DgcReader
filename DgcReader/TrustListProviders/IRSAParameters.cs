// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.TrustListProviders
{
    /// <summary>
    /// RSA algorithm parameters
    /// </summary>
    public interface IRSAParameters
    {
        /// <summary>
        /// The Exponent component of the RSA public key
        /// </summary>
        byte[] Exponent { get; }

        /// <summary>
        /// The Modulus component of the RSA public key
        /// </summary>
        byte[] Modulus { get; }
    }
}