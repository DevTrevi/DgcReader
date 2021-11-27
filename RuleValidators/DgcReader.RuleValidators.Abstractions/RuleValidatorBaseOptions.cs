using DgcReader.RuleValidators.Abstractions.Interfaces;
using System;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Abstractions
{
    /// <inheritdoc cref="IRuleValidatorBaseOptions"/>
    public class RuleValidatorBaseOptions : IRuleValidatorBaseOptions
    {
        /// <inheritdoc />
        public bool UseAvailableRulesWhileRefreshing { get; set; } = true;

        /// <inheritdoc />
        public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromHours(24);

        /// <inheritdoc />
        public TimeSpan MinRefreshInterval { get; set; } = TimeSpan.FromHours(1);
    }

}
