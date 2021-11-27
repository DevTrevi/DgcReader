using GreenpassReader.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.Interfaces.RulesValidators
{
    /// <summary>
    /// Country specific rules validator service for the DGC
    /// </summary>
    public interface IRulesValidator
    {
        /// <summary>
        /// Returns the result
        /// </summary>
        /// <param name="dgc"></param>
        /// <param name="validationInstant">The validation instant when the DGC is validated</param>
        /// <param name="countryCode">The 2-letter ISO country code for which to request rules validation</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IRuleValidationResult> GetRulesValidationResult(EuDGC dgc, DateTimeOffset validationInstant, string countryCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// Refresh the validation rules used by the prodvider from server
        /// </summary>
        /// <returns></returns>
        Task RefreshRules(CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a list of 2 letter ISO county codes supported by the provider for rule validations
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<string>> GetSupportedCountries(CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if the specified country is supported by the provider
        /// </summary>
        /// <param name="countryCode">2-letter ISO code of the country</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> SupportsCountry(string countryCode, CancellationToken cancellationToken = default);
    }
}
