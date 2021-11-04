using GreenpassReader.Models;
using System;
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
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IRuleValidationResult> GetRulesValidationResult(EuDGC dgc, DateTimeOffset validationInstant, CancellationToken cancellationToken = default);

        /// <summary>
        /// Refresh the validation rules used by the prodvider from server
        /// </summary>
        /// <returns></returns>
        Task RefreshRules(CancellationToken cancellationToken = default);
    }
}
