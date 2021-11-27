using System;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Abstractions.Interfaces
{
    /// <summary>
    /// Options for providers implementing <see cref="ThreadsafeRulesValidatorProvider"/>
    /// </summary>
    public interface IRuleValidatorBaseOptions
    {
        /// <summary>
        /// If true, allows to use the current rules list whithout waiting for the refresh task to complete.
        /// Otherwise, if the list is expired, every rules valdation request will wait untill the refresh task completes.
        /// </summary>
        TimeSpan MinRefreshInterval { get; set; }

        /// <summary>
        /// Duration of the stored file before a refresh is requested. Default is 24 hours
        /// </summary>
        TimeSpan RefreshInterval { get; set; }

        /// <summary>
        /// If specified, prevent that every validation request causes a refresh attempt when the current rules list is expired.
        /// </summary>
        bool UseAvailableRulesWhileRefreshing { get; set; }
    }
}