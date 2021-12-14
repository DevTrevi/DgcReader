using DgcReader.Interfaces.RulesValidators;
using DgcReader.Models;
using DgcReader.RuleValidators.Italy;
using DgcReader.RuleValidators.Italy.Models;
using System;
using System.Threading.Tasks;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader
{
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
        /// <param name="qrCodeData"></param>
        /// <param name="validationInstant"></param>
        /// <param name="validationMode">The Italian validation mode</param>
        /// <param name="throwOnError"></param>
        /// <returns></returns>
        public static async Task<DgcValidationResult> VerifyForItaly(this DgcReaderService dgcReaderService,
            string qrCodeData,
            DateTimeOffset validationInstant,
            ValidationMode validationMode,
            bool throwOnError = true)
        {
            return await dgcReaderService.Verify(qrCodeData, validationInstant,
                (dgc, validationInstant, cancellationToken) =>
                {
                    var italianValidator = dgcReaderService.RulesValidator as DgcItalianRulesValidator;
                    if (italianValidator == null)
                    {
                        throw new DgcException($"The registered RulesValidator provider is not a {nameof(DgcItalianRulesValidator)}");
                    }

                    // Call the overload with validationMode parameter
                    return italianValidator.GetRulesValidationResult(dgc, validationInstant, validationMode, cancellationToken);
                }, throwOnError);
        }

        /// <summary>
        /// Decodes the DGC data using IT as acceptance country, allowing to specify the <see cref="ValidationMode"/>.
        /// It is mandatory that the <see cref="DgcItalianRulesValidator"/> is registered as RulesValidator in <see cref="DgcReaderService"/>
        /// </summary>
        /// <param name="dgcReaderService"></param>
        /// <param name="qrCodeData"></param>
        /// <param name="validationMode">The Italian validation mode</param>
        /// <param name="throwOnError"></param>
        /// <returns></returns>
        public static Task<DgcValidationResult> VerifyForItaly(this DgcReaderService dgcReaderService,
            string qrCodeData,
            ValidationMode validationMode,
            bool throwOnError = true)
        {
            return dgcReaderService.VerifyForItaly(qrCodeData, DateTimeOffset.Now, validationMode, throwOnError);
        }
    }

}
