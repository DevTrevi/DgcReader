using DgcReader.RuleValidators.Abstractions;
using System;
using System.IO;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Germany
{
    /// <summary>
    /// Options for the <see cref="DgcGermanRulesValidator"/>
    /// </summary>
    public class DgcGermanRulesValidatorOptions : RuleValidatorBaseOptions
    {
        /// <summary>
        /// Base path where the rules file will be stored
        /// Default <see cref="Directory.GetCurrentDirectory()"/>
        /// </summary>
        public string BasePath { get; set; } = Directory.GetCurrentDirectory();

        /// <summary>
        /// The folder name where rules will be stored, relative to <see cref="BasePath"/>
        /// </summary>
        public string FolderName { get; set; } = "Dgc-Rules-DE";

        /// <summary>
        /// The file name used for the rules file name. Default is rules-identifiers.json
        /// </summary>
        public string RulesIdentifiersFileName { get; set; } = "rules-identifiers.json";

        /// <summary>
        /// Maximum duration of the configuration file before is discarded.
        /// If a refresh is not possible when the refresh interval expires, the current file can be used until
        /// it passes the specified period. Default is 30 days
        /// </summary>
        public TimeSpan MaxFileAge { get; set; } = TimeSpan.FromDays(30);
    }
}
