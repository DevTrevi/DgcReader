using GreenpassReader.Models;
using System;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.Models
{
    /// <summary>
    /// Digital green certificate validation result
    /// </summary>
    public class DgcValidationResult
    {
        /// <summary>
        /// The Digital Green Certificate data
        /// </summary>
        public EuDGC? Dgc { get; internal set; }

        /// <summary>
        /// The issuer of the signed COSE object
        /// </summary>
        public string? Issuer { get; internal set; }

        /// <summary>
        /// Expiration date of the signed object.
        /// NOTE: this is NOT the effective expiration date of the certificate for a given country.
        /// Validity of the certificate must be checked against the business rules of the country where the certificate is verified.
        /// </summary>
        public DateTime? SignatureExpiration { get; internal set; }

        /// <summary>
        /// The issue date of the signed object.
        /// </summary>
        public DateTime? IssuedDate { get; internal set; }

        /// <summary>
        /// The result of the signature verification
        /// </summary>
        public bool HasValidSignature { get; internal set; } = false;

        /// <summary>
        /// True if a blacklist check was performed on the certificate.
        /// This is true also if the certificate is blacklisted
        /// </summary>
        public bool BlacklistVerified { get; internal set; } = false;

        /// <summary>
        /// The validity status of the certificate.
        /// Only <see cref="DgcResultStatus.Valid"/> and <see cref="DgcResultStatus.PartiallyValid"/> values should be considered successful
        /// </summary>
        public DgcResultStatus Status { get; internal set; } = DgcResultStatus.NotEuDCC;

        /// <summary>
        /// If specified, determines the date and time when the certification is considered Valid.
        /// If null, the certification should be considered invalid
        /// Always refer to <see cref="Status"/> for the effective validity for the verifying country
        /// </summary>
        public DateTimeOffset? ValidFrom { get; internal set; }

        /// <summary>
        /// If specified, determines the date and time when the certification is considered expired.
        /// If null, the certification should be considered not valid.
        /// Always refer to <see cref="Status"/> for the effective validity for the verifying country
        /// </summary>
        public DateTimeOffset? ValidUntil { get; internal set; }

        /// <summary>
        /// The date and time for which the certificate was verified
        /// </summary>
        public DateTimeOffset ValidationInstant { get; internal set; }

        /// <summary>
        /// Country for which the rules has been verified (2-letter ISO code)
        /// </summary>
        public string? RulesVerificationCountry { get; internal set; }

        /// <summary>
        /// A string message describing the status of the validation result
        /// </summary>
        public string StatusMessage { get; internal set; }

    }

    /// <summary>
    /// Detailed status of validation
    /// </summary>
    public enum DgcResultStatus
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
