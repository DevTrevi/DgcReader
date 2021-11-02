using DgcReader.TrustListProviders.Abstractions;
using System;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.TrustListProviders.Abstractions.Interfaces
{
    /// <summary>
    /// Options for providers implementing <see cref="ThreadsafeTrustListProvider"/>
    /// </summary>
    public interface ITrustListProviderBaseOptions
    {
        /// <summary>
        /// If true, allows to use the current trustlist whithout waiting for the refresh task to complete.
        /// Otherwise, if the list is expired, every trustlist request will wait untill the refresh task completes.
        /// </summary>
        TimeSpan MinRefreshInterval { get; set; }

        /// <summary>
        /// Duration of the stored file before a refresh is requested. Default is 24 hours
        /// </summary>
        TimeSpan RefreshInterval { get; set; }

        /// <summary>
        /// If specified, prevent that every validation request causes a refresh attempt when the current trustlist is expired.
        /// </summary>
        bool UseAvailableListWhileRefreshing { get; set; }
    }
}