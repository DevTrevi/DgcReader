using DgcReader.TrustListProviders.Abstractions;
using System;
using System.IO;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.TrustListProviders.Italy
{
    /// <summary>
    /// Options for the Italian trustlist provider
    /// </summary>
    public class ItalianTrustListProviderOptions : TrustListProviderBaseOptions
    {
        /// <summary>
        /// Base path where the trust list will be saved
        /// Default <see cref="Directory.GetCurrentDirectory()"/>
        /// </summary>
        public string BasePath { get; set; } = Directory.GetCurrentDirectory();

        /// <summary>
        /// The file name used for the trustlist file name. Default is dgc-trustlist-it.json
        /// </summary>
        public string TrustListFileName { get; set; } = "dgc-trustlist-it.json";

        /// <summary>
        /// Maximum duration of the configuration file before is discarded.
        /// If a refresh is not possible when the refresh interval expires, the current file can be used until
        /// it passes the specified period. Default is 15 days
        /// </summary>
        public TimeSpan MaxFileAge { get; set; } = TimeSpan.FromDays(15);

        /// <summary>
        /// If true, the full certificate is stored in the list after parsing. Default is false
        /// </summary>
        public bool SaveCertificate { get; set; } = false;
    }
}
