using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.Interfaces.TrustListProviders
{
    /// <summary>
    /// Represents a provider used to retrieve the certificates used to verify the signature of a Greenpass
    /// </summary>
    public interface ITrustListProvider
    {

        /// <summary>
        /// If true, the provider supports the operations based on the certificate issuer countries
        /// </summary>
        bool SupportsCountryCodes { get; }

        /// <summary>
        /// If true, the provider supports the retrieval of the full certificate of the issuer
        /// </summary>
        bool SupportsCertificates { get; }

        /// <summary>
        /// Get all the valid certificates public keys for signature check
        /// </summary>
        /// <returns></returns>
        /// <exception cref="DgcReaderService">If is not possible to return a trustlist, an exception will be thrown</exception>
        Task<IEnumerable<ITrustedCertificateData>> GetTrustList(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the certificate with the specified key identifier and optionally by the specified country code
        /// </summary>
        /// <param name="kid">The key identifier of the certificate</param>
        /// <param name="country">The country code (optional)</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<ITrustedCertificateData?> GetByKid(string kid, string? country = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Force the refresh of the TrustList from the server
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<ITrustedCertificateData>?> RefreshTrustList(CancellationToken cancellationToken = default);
    }


}
