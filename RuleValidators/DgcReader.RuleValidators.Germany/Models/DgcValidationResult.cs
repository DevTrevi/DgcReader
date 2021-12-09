using DgcReader.Interfaces.RulesValidators;
using DgcReader.Models;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Germany.Models
{

    /// <summary>
    /// Represents a validation result for the german rules
    /// </summary>
    public class DgcRulesValidationResult : IRulesValidationResult
    {
        /// <summary>
        /// The validation status of the DGC
        /// </summary>
        public DgcResultStatus Status { get; internal set; } = DgcResultStatus.NotValid;

        /// <inheritdoc/>
        public string StatusMessage { get; internal set; }

        /// <inheritdoc/>
        public string RulesVerificationCountry { get; internal set; }

    }
}
