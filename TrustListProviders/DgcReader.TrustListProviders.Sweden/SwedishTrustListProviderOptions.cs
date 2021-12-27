using DgcReader.TrustListProviders.Abstractions;
using System;
using System.IO;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.TrustListProviders.Sweden
{
    /// <summary>
    /// Options for the Swedish trustlist provider
    /// </summary>
    public class SwedishTrustListProviderOptions : TrustListProviderBaseOptions
    {
        /// <summary>
        /// Base path where the trust list will be saved
        /// Default <see cref="Directory.GetCurrentDirectory()"/>
        /// </summary>
        public string BasePath { get; set; } = Directory.GetCurrentDirectory();

        /// <summary>
        /// Maximum duration of the configuration file before is discarded.
        /// If a refresh is not possible when the refresh interval expires, the current file can be used until
        /// it passes the specified period. Default is 15 days
        /// </summary>
        public TimeSpan MaxFileAge { get; set; } = TimeSpan.FromDays(15);
    }
}
