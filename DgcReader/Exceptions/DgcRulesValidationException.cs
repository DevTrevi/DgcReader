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
        public IRuleValidationResult? ValidationResult { get; }

        /// <inheritdoc cref="IRuleValidationResult.ValidFrom"/>
        public DateTimeOffset? ValidFrom => ValidationResult?.ValidFrom;

        /// <inheritdoc cref="IRuleValidationResult.ValidUntil"/>
        public DateTimeOffset? ValidUntil => ValidationResult?.ValidUntil;

        /// <inheritdoc cref="IRuleValidationResult.Status"/>
        public DgcResultStatus Status => ValidationResult?.Status ?? DgcResultStatus.NeedRulesVerification;

        /// <inheritdoc cref="IRuleValidationResult.RulesVerificationCountry"/>
        public string? RulesVerificationCountry => ValidationResult?.RulesVerificationCountry;

        public DgcRulesValidationException(string message,
            IRuleValidationResult? validationResult) :
            base(message)
        {
            ValidationResult = validationResult;
        }

        public DgcRulesValidationException(string message,
            Exception innerException,
            IRuleValidationResult? validationResult) :
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
