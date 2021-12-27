using DgcReader.Interfaces.RulesValidators;
using DgcReader.Models;
using DgcReader.RuleValidators.Germany.CovpassDgcCertlogic;
using System.Collections.Generic;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace DgcReader.RuleValidators.Germany.Models
{

    /// <summary>
    /// Represents a validation result for the german rules
    /// </summary>
    public class GermanRulesValidationResult : IRulesValidationResult
    {
        /// <summary>
        /// The validation status of the DGC
        /// </summary>
        public DgcResultStatus Status { get; internal set; } = DgcResultStatus.NotValid;

        /// <inheritdoc/>
        public string? StatusMessage { get; internal set; }

        /// <inheritdoc/>
        public string RulesVerificationCountry { get; internal set; }

        /// <summary>
        /// Validation results of the rules
        /// </summary>
        public IEnumerable<ValidationResult> ValidationResults { get; internal set; } = new ValidationResult[0];

    }
}
