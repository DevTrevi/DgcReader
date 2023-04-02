using DgcReader.Exceptions;
using DgcReader.Interfaces.RulesValidators;
using DgcReader.Models;
using DgcReader.RuleValidators.Italy;
using DgcReader.RuleValidators.Italy.Const;
using DgcReader.RuleValidators.Italy.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader;

/// <summary>
/// Extension methods for the <see cref="ItalianRulesValidationResult"/>
/// </summary>
public static class ItalianValidationResultsExtensions
{
    /// <summary>
    /// Cast the <see cref="IRulesValidationResult"/> to the specific <see cref="ItalianRulesValidationResult"/>
    /// if the result is coming from <see cref="DgcItalianRulesValidator"/>. Otherwise, retun null
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    public static ItalianRulesValidationResult? AsItalianValidationResult(this IRulesValidationResult result)
        => result as ItalianRulesValidationResult;

    /// <summary>
    /// Return the <see cref="DgcValidationResult.RulesValidation"/> as <see cref="ItalianRulesValidationResult"/>
    /// if the result is coming from <see cref="DgcItalianRulesValidator"/>. Otherwise, retun null
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    public static ItalianRulesValidationResult? GetItalianValidationResult(this DgcValidationResult result)
        => result.RulesValidation?.AsItalianValidationResult();

    /// <summary>
    /// Return the specific status of validation for the Italian rules, according to the official SDK
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    public static DgcItalianResultStatus? GetItalianResultStatus(this IRulesValidationResult result)
        => result.AsItalianValidationResult()?.ItalianStatus;

    /// <summary>
    /// Return the specific status of validation for the Italian rules, according to the official SDK
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    public static DgcItalianResultStatus? GetItalianResultStatus(this DgcValidationResult result)
        => result.GetItalianValidationResult()?.ItalianStatus;

    /// <summary>
    /// Decodes the DGC data using IT as acceptance country, allowing to specify the <see cref="ValidationMode"/>.
    /// It is mandatory that the <see cref="DgcItalianRulesValidator"/> is registered as RulesValidator in <see cref="DgcReaderService"/>
    /// </summary>
    /// <param name="dgcReaderService"></param>
    /// <param name="qrCodeData">The QRCode data of the DGC</param>
    /// <param name="validationInstant">The validation instant of the DGC</param>
    /// <param name="validationMode">The Italian validation mode</param>
    /// <param name="throwOnError">If true, throw an exception if the validation fails</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<DgcValidationResult> VerifyForItaly(this DgcReaderService dgcReaderService,
        string qrCodeData,
        DateTimeOffset validationInstant,
        ValidationMode validationMode,
        bool throwOnError = true,
        CancellationToken cancellationToken = default)
    {
        return await dgcReaderService.Verify(qrCodeData, CountryCodes.Italy, validationInstant,
            async (dgc, dgcJson, countryCode, validationInstant, signatureValidation, blacklistValidation, cancellationToken) =>
            {
                var italianValidator = dgcReaderService.RulesValidators.OfType<DgcItalianRulesValidator>().FirstOrDefault();
                if (italianValidator == null)
                {
                    throw new DgcException($"{nameof(DgcItalianRulesValidator)} is not registered in {nameof(DgcReaderService)}");
                }

                // Call the overload with validationMode parameter
                return await italianValidator.GetRulesValidationResult(dgc,
                    dgcJson,
                    validationInstant,
                    validationMode,
                    signatureValidation,
                    blacklistValidation,
                    cancellationToken);

            }, throwOnError, cancellationToken);
    }

    /// <summary>
    /// Decodes the DGC data using IT as acceptance country, allowing to specify the <see cref="ValidationMode"/>.
    /// It is mandatory that the <see cref="DgcItalianRulesValidator"/> is registered as RulesValidator in <see cref="DgcReaderService"/>
    /// </summary>
    /// <param name="dgcReaderService"></param>
    /// <param name="qrCodeData">The QRCode data of the DGC</param>
    /// <param name="validationMode">The Italian validation mode</param>
    /// <param name="throwOnError">If true, throw an exception if the validation fails</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static Task<DgcValidationResult> VerifyForItaly(this DgcReaderService dgcReaderService,
        string qrCodeData,
        ValidationMode validationMode,
        bool throwOnError = true,
        CancellationToken cancellationToken = default)
    {
        return dgcReaderService.VerifyForItaly(qrCodeData,
            DateTimeOffset.Now,
            validationMode,
            throwOnError,
            cancellationToken);
    }
}
