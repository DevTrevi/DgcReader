using DgcReader.RuleValidators.Germany.CovpassDgcCertlogic.Data;
using System;
using System.Collections.Generic;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace DgcReader.RuleValidators.Germany.CovpassDgcCertlogic;

/// <summary>
/// Validation of one rule against the certificate data
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// The rule that has been tested
    /// </summary>
    public RuleEntry Rule { get; internal set; }

    /// <summary>
    /// The validation result for the rule
    /// </summary>
    public Result Result { get; internal set; }

    /// <summary>
    /// Current value tested if available
    /// </summary>
    public string? Current { get; internal set; }

    /// <summary>
    /// Errors during validation, leading to an Open result
    /// </summary>
    public IEnumerable<Exception>? ValidationErrors { get; internal set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{Result} {Rule} - Current: {Current}";
    }
}

/// <summary>
/// Result status of a rule validation
/// </summary>
public enum Result
{
    /// <summary>
    /// Rule check passed
    /// </summary>
    PASSED,

    /// <summary>
    /// Rule check failed
    /// </summary>
    FAIL,

    /// <summary>
    /// Rule check can not be performed, so the result could not be determined
    /// </summary>
    OPEN
}
