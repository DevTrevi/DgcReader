using DgcReader.RuleValidators.Italy.Models;
using System;
using System.IO;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Italy;

/// <summary>
/// Options for the <see cref="DgcItalianRulesValidator"/>
/// </summary>
public class DgcItalianRulesValidatorOptions
{
    /// <summary>
    /// Duration of the stored file before a refresh is requested. Default is 24 hours
    /// </summary>
    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// If specified, prevent that every validation request causes a refresh attempt when the current rules list is expired.
    /// </summary>
    public TimeSpan MinRefreshInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// If true, allows to use the current rules list without waiting for the refresh task to complete.
    /// Otherwise, if the list is expired, every rules valdation request will wait untill the refresh task completes.
    /// </summary>
    public bool UseAvailableValuesWhileRefreshing { get; set; } = false;

    /// <summary>
    /// If true, try to reload values from cache before downloading from the remote server.
    /// This can be useful if values are refreshed by a separate process, i.e. when the same valueset cached file is shared by
    /// multiple instances for reading
    /// </summary>
    public bool TryReloadFromCacheWhenExpired { get; set; } = false;

    /// <summary>
    /// Base path where the rules file will be stored
    /// Default <see cref="Directory.GetCurrentDirectory()"/>
    /// </summary>
    public string BasePath { get; set; } = Directory.GetCurrentDirectory();

    /// <summary>
    /// Maximum duration of the configuration file before is discarded.
    /// If a refresh is not possible when the refresh interval expires, the current file can be used until
    /// it passes the specified period. Default is 15 days
    /// </summary>
    public TimeSpan MaxFileAge { get; set; } = TimeSpan.FromDays(15);

    /// <summary>
    /// If true, validates the rules even if the reference SDK version is obsolete
    /// </summary>
    public bool IgnoreMinimumSdkVersion { get; set; } = false;

    /// <summary>
    /// The verification mode used in order to validate the DGC.
    /// If not specified, defaults to <see cref="ValidationMode.Basic3G"/>
    /// </summary>
    public ValidationMode? ValidationMode { get; set; } = null;
}
