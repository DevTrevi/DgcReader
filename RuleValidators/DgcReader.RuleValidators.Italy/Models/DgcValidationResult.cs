using DgcReader.Models;
using System;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Italy.Models
{
    /// <summary>
    /// Represents a validation result for the italia rules
    /// </summary>
    public class DgcRulesValidationResult
    {
        /// <summary>
        /// The validated Dgc
        /// </summary>
        public DgcResult Dgc {  get; set; }

        /// <summary>
        /// The instant when the certificate was validated against the business rules.
        /// </summary>
        public DateTimeOffset ValidationInstant { get; internal set; }

        /// <summary>
        /// If specified, determines the date and time when the certification is considered active.
        /// If null, the certification is to be considered invalid
        /// </summary>
        public DateTimeOffset? ActiveFrom { get; internal set; }

        /// <summary>
        /// If specified, determines the date and time when the certification is considered expired.
        /// If null, the certification is to be considered invalid
        /// </summary>
        public DateTimeOffset? ActiveUntil { get; internal set; }

        /// <summary>
        /// If true, the certificate is considered valid at the moment of validation
        /// </summary>
        public bool IsActive { get; internal set; }
    }
}
