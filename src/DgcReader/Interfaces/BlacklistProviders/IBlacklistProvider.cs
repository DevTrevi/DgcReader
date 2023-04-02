using System.Threading;
using System.Threading.Tasks;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.Interfaces.BlacklistProviders;

/// <summary>
/// Blacklist provider for checking blacklisted DGC identifiers
/// </summary>
public interface IBlacklistProvider
{
    /// <summary>
    /// Check if the supplied certificate identifier is blacklisted
    /// </summary>
    /// <param name="certificateIdentifier"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>True if the supplied identifier is blacklisted and shoul be considered not valid</returns>
    Task<bool> IsBlacklisted(string certificateIdentifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Force the refresh of the Blacklist from the server
    /// </summary>
    /// <returns>The updated blacklist</returns>
    Task RefreshBlacklist(CancellationToken cancellationToken = default);
}
