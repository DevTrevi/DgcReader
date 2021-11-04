using System;
using System.IO;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Italy
{
    /// <summary>
    /// Options for the <see cref="DgcItalianRulesValidator"/>
    /// </summary>
    public class DgcItalianRulesValidatorOptions
    {
        /// <summary>
        /// Base path where the rules file will be stored
        /// Default <see cref="Directory.GetCurrentDirectory()"/>
        /// </summary>
        public string BasePath { get; set; } = Directory.GetCurrentDirectory();

        /// <summary>
        /// The file name used for the rules file name. Default is dgc-rules-it.json
        /// </summary>
        public string RulesListFileName { get; set; } = "dgc-rules-it.json";

        /// <summary>
        /// Duration of the stored file before a refresh is requested. Default is 24 hours
        /// </summary>
        public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromHours(24);

        /// <summary>
        /// Maximum duration of the configuration file before is discarded.
        /// If a refresh is not possible when the refresh interval expires, the current file can be used until
        /// it passes the specified period. Default is 30 days
        /// </summary>
        public TimeSpan MaxFileAge { get; set; } = TimeSpan.FromDays(30);

        /// <summary>
        /// If specified, prevent that every validation request causes a refresh attempt when the current rules list is expired.
        /// </summary>
        public TimeSpan MinRefreshInterval { get; set; } = TimeSpan.FromHours(1);

        /// <summary>
        /// If true, validates the rules even if the reference SDK version is obsolete
        /// </summary>
        public bool IgnoreMinimumSdkVersion { get; set; } = false;        
    }
}
