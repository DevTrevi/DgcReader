using DgcReader.Models;
using DgcReader.RuleValidators.Italy.Models;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader
{
    /// <summary>
    /// Extension methods for ItalianRulesValidation types
    /// </summary>
    public static class ItalianRulesValidationResultExtensions
    {
        /// <summary>
        /// Converts the <see cref="DgcItalianResultStatus"/> to the standard <see cref="DgcResultStatus"/>
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public static DgcResultStatus ToDgcResultStatus(this DgcItalianResultStatus status)
        {
            switch (status)
            {
                case DgcItalianResultStatus.NotEuDCC:
                    return DgcResultStatus.NotEuDCC;

                case DgcItalianResultStatus.InvalidSignature:
                    return DgcResultStatus.InvalidSignature;

                case DgcItalianResultStatus.Blacklisted:
                    return DgcResultStatus.Blacklisted;

                case DgcItalianResultStatus.NeedRulesVerification:
                    return DgcResultStatus.NeedRulesVerification;

                case DgcItalianResultStatus.NotValid:
                case DgcItalianResultStatus.NotValidYet:
                    return DgcResultStatus.NotValid;

                case DgcItalianResultStatus.Valid:
                    return DgcResultStatus.Valid;

                default:
                    return DgcResultStatus.OpenResult;
            }
        }
    }
}
