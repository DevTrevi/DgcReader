using DgcReader.TrustListProviders.Abstractions.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DgcReader.Interfaces.TrustListProviders;
using DgcReader.Exceptions;
using DgcReader.Providers.Abstractions;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.TrustListProviders.Abstractions;

/// <summary>
/// Base class for implementing a trustlist provider optimized for being used by multiple threads.
/// When the trustlist is expired, the refresh is managed by a single task.
/// </summary>
public abstract class ThreadsafeTrustListProvider : ThreadsafeValueSetProvider<ITrustList>, ITrustListProvider, IDisposable
{
    /// <summary>
    /// Options for the provider
    /// </summary>
    protected readonly ITrustListProviderBaseOptions Options;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="options"></param>
    /// <param name="logger"></param>
    protected ThreadsafeTrustListProvider(
        ITrustListProviderBaseOptions options,
        ILogger? logger)
        : base(logger)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        Options = options;
    }

    // Implementation of ITrustListProvider

    /// <inheritdoc/>
    public abstract bool SupportsCountryCodes { get; }

    /// <inheritdoc/>
    public abstract bool SupportsCertificates { get; }

    /// <inheritdoc/>
    public async Task<IEnumerable<ITrustedCertificateData>> GetTrustList(CancellationToken cancellationToken = default)
    {

        var trustList = await GetValueSet(cancellationToken);

        if (trustList?.Certificates == null)
            throw new DgcException($"Can not get a valid TrustList. Make sure that the application can connect to the remote server and try again.");

        return trustList.Certificates;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ITrustedCertificateData>?> RefreshTrustList(CancellationToken cancellationToken = default)
    {

        var trustList = await RefreshValueSet(cancellationToken);
        return trustList?.Certificates;
    }

    /// <inheritdoc/>
    public virtual async Task<ITrustedCertificateData?> GetByKid(string kid, string? country, CancellationToken cancellationToken = default)
    {
        var trustList = await GetTrustList(cancellationToken);

        var q = trustList.Where(x => x.Kid == kid);
        if (!string.IsNullOrEmpty(country))
            q = q.Where(x => x.Country == country);
        return q.SingleOrDefault();
    }

    

    // Implementation of ThreadsafeValueSetProvider

    /// <inheritdoc/>
    public override TimeSpan MinRefreshInterval => Options.MinRefreshInterval;

    /// <inheritdoc/>
    public override TimeSpan RefreshInterval => Options.RefreshInterval;

    /// <inheritdoc/>
    public override bool UseAvailableValuesWhileRefreshing => Options.UseAvailableListWhileRefreshing;

    /// <inheritdoc/>
    public override bool TryReloadFromCacheWhenExpired => Options.TryReloadFromCacheWhenExpired;

    /// <inheritdoc/>
    protected override string GetValuesetName() => "TrustList";

    /// <inheritdoc/>
    protected override DateTimeOffset GetLastUpdate(ITrustList valueSet) => valueSet.LastUpdate;

}

/// <inheritdoc cref="ThreadsafeTrustListProvider"/>
public abstract class ThreadsafeTrustListProvider<TOptions> : ThreadsafeTrustListProvider
    where TOptions : class, ITrustListProviderBaseOptions, new()
{
    /// <inheritdoc cref="ThreadsafeTrustListProvider.Options"/>
    public new TOptions Options => (TOptions)base.Options;

    /// <inheritdoc />
    protected ThreadsafeTrustListProvider(TOptions? options, ILogger? logger) : base(options ?? new TOptions(), logger)
    {

    }
}
