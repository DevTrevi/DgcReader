// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.TrustListProviders
{
    /// <summary>
    /// Data of a trust certificate entry
    /// </summary>
    public interface ITrustedCertificateData
    {
        /// <summary>
        /// Key identifier
        /// </summary>
        string Kid { get; }

        /// <summary>
        /// Country of the certificate issuer
        /// </summary>
        string Country { get; }

        /// <summary>
        /// Returns the ECDsa public key parameters
        /// </summary>
        /// <returns></returns>
        IECParameters GetECParameters();

        /// <summary>
        /// Returns the RSA public key parameters
        /// </summary>
        /// <returns></returns>
        IRSAParameters GetRSAParameters();

        /// <summary>
        /// Returns the certificate data if available
        /// </summary>
        byte[] Certificate { get; }

    }
}