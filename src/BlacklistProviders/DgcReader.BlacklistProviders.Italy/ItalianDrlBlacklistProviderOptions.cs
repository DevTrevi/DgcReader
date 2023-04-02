using Microsoft.EntityFrameworkCore;
using System;
using System.IO;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.BlacklistProviders.Italy
{
    /// <summary>
    /// Options for the <see cref="ItalianDrlBlacklistProvider"/>
    /// </summary>
    public class ItalianDrlBlacklistProviderOptions
    {
        /// <summary>
        /// Duration of the stored file before a refresh is requested. Default is 24 hours
        /// </summary>
        public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromHours(24);

        /// <summary>
        /// If specified, prevent that every validation request causes a refresh attempt when the current values needs to be updated.
        /// </summary>
        public TimeSpan MinRefreshInterval { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Maximum duration of the configuration file before is discarded.
        /// If a refresh is not possible when the refresh interval expires, the current file can be used until
        /// it passes the specified period. Default is 15 days
        /// </summary>
        public TimeSpan MaxFileAge { get; set; } = TimeSpan.FromDays(15);

        /// <summary>
        /// If true, allows to use the current values without waiting for the refresh task to complete.
        /// Otherwise, if the list is expired, every rules valdation request will wait untill the refresh task completes.
        /// </summary>
        public bool UseAvailableValuesWhileRefreshing { get; set; } = false;

        /// <summary>
        /// Base path where the rules file will be stored
        /// Default <see cref="Directory.GetCurrentDirectory()"/>
        /// </summary>
        public string BasePath { get; set; } = Directory.GetCurrentDirectory();

        /// <summary>
        /// Options for configuring the DbContext
        /// </summary>
        public DbContextOptionsBuilder DbContext { get; private set; } = new DbContextOptionsBuilder();
    }
}
