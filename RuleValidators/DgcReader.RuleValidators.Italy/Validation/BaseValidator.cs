using DgcReader.RuleValidators.Italy.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace DgcReader.RuleValidators.Italy.Validation
{
    /// <summary>
    /// Base class for building ICertificateEntryValidators
    /// </summary>
    public abstract class BaseValidator : ICertificateEntryValidator
    {
        /// <inheritdoc/>
        public BaseValidator(ILogger? logger)
        {
            Logger = logger;
        }

        /// <summary>
        /// The logger instance (optional)
        /// </summary>
        protected ILogger? Logger { get; }

        /// <inheritdoc/>
        public abstract ItalianRulesValidationResult CheckCertificate(ValidationCertificateModel certificateModel, IEnumerable<RuleSetting> rules, ValidationMode validationMode);

        /// <summary>
        /// Instantiate a RecoveryValidator in the initial state, including data that will always be returned
        /// </summary>
        /// <param name="certificateModel"></param>
        /// <param name="validationMode"></param>
        protected virtual ItalianRulesValidationResult InitializeResult(ValidationCertificateModel certificateModel, ValidationMode validationMode)
        {
            return new ItalianRulesValidationResult
            {
                ValidationInstant = certificateModel.ValidationInstant,
                ValidationMode = validationMode,
                ItalianStatus = DgcItalianResultStatus.NeedRulesVerification,
            };
        }
    }
}
