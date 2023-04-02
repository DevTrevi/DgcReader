using DgcReader.TrustListProviders.Abstractions;
using System;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.TrustListProviders.Abstractions.Interfaces;

/// <summary>
/// Options for providers implementing <see cref="ThreadsafeTrustListProvider"/>
/// </summary>
public interface ITrustListProviderBaseOptions
{
    /// <summary>
    /// Duration of the stored file before a refresh is requested. Default is 24 hours
    /// </summary>
    TimeSpan RefreshInterval { get; set; }

    /// <summary>
    /// If specified, prevents that every validation request causes a refresh attempt when the current trustlist is expired.
    /// </summary>
    TimeSpan MinRefreshInterval { get; set; }

    /// <summary>
    /// If true, allows to use the current trustlist whithout waiting for the refresh task to complete.
    /// Otherwise, if the list is expired, every trustlist request will wait untill the refresh task completes.
    /// </summary>
    bool UseAvailableListWhileRefreshing { get; set; }

    /// <summary>
    /// If true, try to reload values from cache before downloading from the remote server.
    /// This can be useful if values are refreshed by a separate process, i.e. when the same valueset cached file is shared by
    /// multiple instances for reading
    /// </summary>
    bool TryReloadFromCacheWhenExpired { get; set; }
}