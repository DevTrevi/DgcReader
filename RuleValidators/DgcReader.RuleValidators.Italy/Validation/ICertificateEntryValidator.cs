using DgcReader.Models;
using DgcReader.RuleValidators.Italy.Const;
using DgcReader.RuleValidators.Italy.Models;
using GreenpassReader.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DgcReader.RuleValidators.Italy.Validation
{

    public interface ICertificateEntryValidator
    {
        ItalianRulesValidationResult CheckCertificate(
            ValidationCertificateModel certificateModel,
            IEnumerable<RuleSetting> rules,
            ValidationMode validationMode);
    }

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
        protected virtual ItalianRulesValidationResult InitializeResult(ValidationCertificateModel certificateModel, ValidationMode validationMode)
        {
            return new ItalianRulesValidationResult
            {
                ValidationInstant = certificateModel.ValidationInstant,
                ValidationMode = validationMode,
                ItalianStatus = DgcItalianResultStatus.NotValidated,
            };
        }
    }
}
