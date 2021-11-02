using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.TrustListProviders.Abstractions.Interfaces
{
    /// <summary>
    /// Trustlist informations
    /// </summary>
    public interface ITrustList
    {
        /// <summary>
        /// Instant when the trust list was updated
        /// </summary>
        DateTimeOffset LastUpdate { get; }

        /// <summary>
        /// Expiration date of the trustlist (if supported by the provider)
        /// </summary>
        DateTimeOffset? Expiration { get; }

        /// <summary>
        /// The public key data of the trusted certificates
        /// </summary>
        IEnumerable<ITrustedCertificateData> Certificates { get; }
    }


}
