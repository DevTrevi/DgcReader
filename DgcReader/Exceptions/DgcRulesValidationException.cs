using DgcReader.Interfaces.RulesValidators;
using DgcReader.Models;
using System;
using System.Runtime.Serialization;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace DgcReader.Exceptions
{
    [Serializable]
    public class DgcRulesValidationException : DgcException
    {
        /// <summary>
        /// The rules validation result
        /// </summary>
        public IRulesValidationResult? ValidationResult { get; }

        /// <inheritdoc cref="IRulesValidationResult.Status"/>
        public DgcResultStatus Status => ValidationResult?.Status ?? DgcResultStatus.NeedRulesVerification;

        /// <inheritdoc cref="IRulesValidationResult.RulesVerificationCountry"/>
        public string? RulesVerificationCountry => ValidationResult?.RulesVerificationCountry;

        public DgcRulesValidationException(string message,
            IRulesValidationResult? validationResult) :
            base(message)
        {
            ValidationResult = validationResult;
        }

        public DgcRulesValidationException(string message,
            Exception innerException,
            IRulesValidationResult? validationResult) :
            base(message, innerException)
        {
            ValidationResult = validationResult;
        }

        public DgcRulesValidationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
