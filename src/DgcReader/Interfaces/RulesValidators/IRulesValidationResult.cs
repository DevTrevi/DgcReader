using DgcReader.Models;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.Interfaces.RulesValidators;


/// <summary>
/// Rules validation result for the DGC
/// </summary>
public interface IRulesValidationResult
{
    /// <summary>
    /// Validation status of the business rules
    /// </summary>
    DgcResultStatus Status { get; }

    /// <summary>
    /// A string message describing the status of the validation (optional)
    /// </summary>
    public string? StatusMessage { get; }

    /// <summary>
    /// Country for which the rules has been verified (2 letter ISO code)
    /// </summary>
    string RulesVerificationCountry { get;  }
}
