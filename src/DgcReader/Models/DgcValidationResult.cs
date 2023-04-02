using DgcReader.Interfaces.RulesValidators;
using DgcReader.Interfaces.TrustListProviders;
using GreenpassReader.Models;
using System;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.Models;

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
    /// The validity status of the certificate
    /// </summary>
    public DgcResultStatus Status
    {
        get
        {
            if (Dgc == null)
                return DgcResultStatus.NotEuDCC;

            if (!Signature.HasValidSignature)
                return DgcResultStatus.InvalidSignature;

            if (Blacklist.IsBlacklisted == true)
                return DgcResultStatus.Blacklisted;

            if (RulesValidation == null)
                return DgcResultStatus.NeedRulesVerification;

            return RulesValidation.Status;
        }
    }

    /// <summary>
    /// A string message describing the status of the validation result
    /// </summary>
    public string? StatusMessage { get; internal set; }

    /// <summary>
    /// The date and time for which the certificate was verified
    /// </summary>
    public DateTimeOffset ValidationInstant { get; internal set; }

    /// <summary>
    /// Country for which the rules has been verified (2-letter ISO code)
    /// </summary>
    public string? AcceptanceCountry { get; internal set; }

    /// <summary>
    /// The signature validation result
    /// </summary>
    public SignatureValidationResult Signature { get; internal set; } = new SignatureValidationResult();

    /// <summary>
    /// The blacklist validation result
    /// </summary>
    public BlacklistValidationResult Blacklist { get; internal set; } = new BlacklistValidationResult();

    /// <summary>
    /// The rules validation result. This can have specific implementations for each rules validator, containing additional informations
    /// </summary>
    public IRulesValidationResult? RulesValidation { get; internal set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{nameof(DgcValidationResult)} Status: {Status} - Acceptance country: {AcceptanceCountry}";
    }
}

/// <summary>
/// The signature validation result
/// </summary>
public class SignatureValidationResult
{
    /// <summary>
    /// The certificate key identifier
    /// </summary>
    public string? CertificateKid { get; internal set; }

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
    /// The public key data of the certificate used for checking the signature
    /// </summary>
    public ITrustedCertificateData? PublicKeyData { get; internal set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        var s = "Signature: ";
        if (HasValidSignature)
            return s + $"valid";

        return s + $"invalid (Issued by {(Issuer ?? "(unknown)")} on {(IssuedDate?.ToString("g") ?? "-")} - Expiring on {(SignatureExpiration?.ToString("g") ?? "-")})";
    }
}

/// <summary>
/// The blacklist validation result
/// </summary>
public class BlacklistValidationResult
{
    /// <summary>
    /// True if a blacklist check was performed on the certificate.
    /// This is true also if the certificate is blacklisted
    /// </summary>
    public bool BlacklistVerified { get; internal set; } = false;

    /// <summary>
    /// If true, the certificate must be considered not valid
    /// If null, it means that no blacklist providers was available for validation
    /// </summary>
    public bool? IsBlacklisted { get; internal set; }

    /// <summary>
    /// The certificate identifier used for blacklist validation
    /// </summary>
    public string? CertificateIdentifier { get; internal set; }

    /// <summary>
    /// The type of blacklist provider where a match was found for the certificate identifier
    /// </summary>
    public Type? BlacklistMatchProviderType { get; internal set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        var s = "Blacklist check: ";
        if (BlacklistVerified == false)
            return s + "not verified";

        if (IsBlacklisted == true)
            return s + $"blacklisted";

        return s + "not blacklisted";
    }
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
    /// The certificate rules has not been validated by any rules validator
    /// </summary>
    NeedRulesVerification,

    /// <summary>
    /// The certificate rules has been validated without a definitive result.
    /// A manual check should be performed in order to verifiy if the certificate is valid or not.
    /// </summary>
    OpenResult,

    /// <summary>
    /// The certificate is not valid
    /// </summary>
    NotValid,

    /// <summary>
    /// The certificate is valid in the country of verification, and should be valid in other countries as well
    /// </summary>
    Valid,
}
