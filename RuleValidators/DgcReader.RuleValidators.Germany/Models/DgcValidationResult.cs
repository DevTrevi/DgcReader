using DgcReader.Interfaces.RulesValidators;
using DgcReader.Models;
using GreenpassReader.Models;
using System;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Germany.Models
{

    /// <summary>
    /// Represents a validation result for the german rules
    /// </summary>
    public class DgcRulesValidationResult : IRuleValidationResult
    {
        /// <summary>
        /// The validated Dgc
        /// </summary>
        public EuDGC Dgc {  get; internal set; }

        /// <summary>
        /// The instant when the certificate was validated against the business rules.
        /// </summary>
        public DateTimeOffset ValidationInstant { get; internal set; }

        /// <inheritdoc/>
        public DateTimeOffset? ValidFrom { get; internal set; }

        /// <inheritdoc/>
        public DateTimeOffset? ValidUntil { get; internal set; }

        /// <summary>
        /// The validation status of the DGC
        /// </summary>
        public DgcResultStatus Status { get; internal set; } = DgcResultStatus.NotValid;


        /// <inheritdoc/>
        public string RulesVerificationCountry { get; internal set; }

        /// <summary>
        /// If true, the certificate is considered valid at the moment of validation in the country of verification.
        /// </summary>
        public bool IsActive => Status == DgcResultStatus.Valid || Status == DgcResultStatus.PartiallyValid;
    }
}
