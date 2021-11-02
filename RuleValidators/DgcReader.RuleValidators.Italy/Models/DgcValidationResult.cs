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
        /// If true, the certificate is considered valid at the moment of validation in the country of verification.
        /// </summary>
        public bool IsActive => Status == DgcResultStatus.Valid || Status == DgcResultStatus.PartiallyValid;

        /// <summary>
        /// The validation status of the DGC
        /// </summary>
        public DgcResultStatus Status { get; internal set; } = DgcResultStatus.NotValid;
    }

    /// <summary>
    /// Detailed status of validation
    /// </summary>
    public enum DgcResultStatus
    {
        /// <summary>
        /// The certificate is not valid
        /// </summary>
        NotValid,
        /// <summary>
        /// The certificate is not valid yet. It will be valid after the <see cref="DgcRulesValidationResult.ActiveFrom"/> date
        /// </summary>
        NotValidYet,

        /// <summary>
        /// The certificate is valid in the country of verification, and should be valid in other countries as well
        /// </summary>
        Valid,

        /// <summary>
        /// The certificate is considered valid in the country of verificstion, but may be considered not valid in other countries
        /// </summary>
        PartiallyValid,

        /// <summary>
        /// The certificate is not a valid EU DCC
        /// </summary>
        NotEuDCC
    }
}
