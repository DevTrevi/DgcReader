using DgcReader.Interfaces.RulesValidators;
using DgcReader.Models;
using DgcReader.RuleValidators.Germany;
using DgcReader.RuleValidators.Germany.Models;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader;

/// <summary>
/// Extension methods for the <see cref="GermanRulesValidationResult"/>
/// </summary>
public static class GermanValidationResultsExtensions
{
    /// <summary>
    /// Cast the <see cref="IRulesValidationResult"/> to the specific <see cref="GermanRulesValidationResult"/>
    /// if the result is coming from <see cref="DgcGermanRulesValidator"/>. Otherwise, retun null
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    public static GermanRulesValidationResult? AsGermanValidationResult(this IRulesValidationResult result)
        => result as GermanRulesValidationResult;

    /// <summary>
    /// Return the <see cref="DgcValidationResult.RulesValidation"/> as <see cref="GermanRulesValidationResult"/>
    /// if the result is coming from <see cref="DgcGermanRulesValidator"/>. Otherwise, retun null
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    public static GermanRulesValidationResult? GetGermanValidationResult(this DgcValidationResult result)
        => result.RulesValidation?.AsGermanValidationResult();
}
