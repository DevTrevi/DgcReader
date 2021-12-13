using DgcReader.Interfaces.RulesValidators;
using DgcReader.Models;
using System;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Italy.Models
{
    /// <summary>
    /// Represents a validation result for the italia rules
    /// </summary>
    public class ItalianRulesValidationResult : IRulesValidationResult
    {
        /// <summary>
        /// Validation status according to the official Italian SDK
        /// </summary>
        public DgcItalianResultStatus ItalianStatus { get; internal set; }

        #region Implementation of IRulesValidationResult
        /// <summary>
        /// The validation status of the DGC
        /// </summary>
        public DgcResultStatus Status => ItalianStatus.ToDgcResultStatus();

        /// <summary>
        /// A string message describing the status of the validation result (optional)
        /// </summary>
        public string? StatusMessage { get; internal set; }


        /// <inheritdoc/>
        public string RulesVerificationCountry => "IT";
        #endregion

        #region Additional info
        /// <summary>
        /// The instant when the certificate was validated against the business rules.
        /// </summary>
        public DateTimeOffset ValidationInstant { get; internal set; }

        /// <summary>
        /// If specified, determines the date and time when the certification is considered Valid.
        /// If null, the certification should be considered invalid
        /// Always refer to <see cref="ItalianStatus"/> for the effective validity in Italy
        /// </summary>
        public DateTimeOffset? ValidFrom { get; internal set; }

        /// <summary>
        /// If specified, determines the date and time when the certification is considered expired.
        /// If null, the certification should be considered not valid.
        /// Always refer to <see cref="ItalianStatus"/> for the effective validity in Italy
        /// </summary>
        public DateTimeOffset? ValidUntil { get; internal set; }

        #endregion
    }

    /// <summary>
    /// Detailed validation status according to the official Italian SDK
    /// </summary>
    public enum DgcItalianResultStatus
    {
        /// <summary>
        /// The certificate is not a valid EU DCC
        /// </summary>
        NotEuDCC,

        /// <summary>
        /// The certificate has an invalid signature
        /// </summary>
        InvalidSignature,

        /// <summary>
        /// The certificate is blacklisted
        /// </summary>
        Blacklisted,

        /// <summary>
        /// The certificate has a valid signature, but needs to be verified against the hosting country rules.
        /// </summary>
        NeedRulesVerification,

        /// <summary>
        /// The certificate is not valid
        /// </summary>
        NotValid,

        /// <summary>
        /// The certificate is not valid yet
        /// </summary>
        NotValidYet,

        /// <summary>
        /// The certificate is considered valid in the country of verification, but may be considered not valid in other countries
        /// </summary>
        PartiallyValid,

        /// <summary>
        /// The certificate is valid in the country of verification, and should be valid in other countries as well
        /// </summary>
        Valid,
    }
}
