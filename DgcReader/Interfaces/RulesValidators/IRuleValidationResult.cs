using DgcReader.Models;
using System;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.Interfaces.RulesValidators
{

    /// <summary>
    /// Rules validation result for the DGC
    /// </summary>
    public interface IRuleValidationResult
    {
        /// <summary>
        /// If specified, determines the date and time when the certification is considered active.
        /// If null, the certification should be considered invalid
        /// Always refer to <see cref="Status"/> for the effective validity for the verifying country
        /// </summary>
        public DateTimeOffset? ValidFrom { get; }

        /// <summary>
        /// If specified, determines the date and time when the certification is considered expired.
        /// If null, the certification should be considered not valid. 
        /// Always refer to <see cref="Status"/> for the effective validity for the verifying country
        /// </summary>
        public DateTimeOffset? ValidUntil { get; }

        /// <summary>
        /// Validation status of the business rules
        /// </summary>
        DgcResultStatus Status { get; }

        /// <summary>
        /// Country for which the rules has been verified (2 letter ISO code)
        /// </summary>
        string RulesVerificationCountry { get;  }
    }
}
