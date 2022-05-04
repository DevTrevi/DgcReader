using DgcReader.Deserializers.Italy.Models;
using DgcReader.RuleValidators.Italy.Const;
using DgcReader.RuleValidators.Italy.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace DgcReader.RuleValidators.Italy.Validation
{
    /// <summary>
    /// Validator for Italian Exemptions
    /// </summary>
    public class ExemptionValidator : BaseValidator
    {
        /// <inheritdoc/>
        public ExemptionValidator(ILogger? logger) : base(logger)
        {
        }

        /// <inheritdoc/>
        public override ItalianRulesValidationResult CheckCertificate(
            ValidationCertificateModel certificateModel,
            IEnumerable<RuleSetting> rules,
            ValidationMode validationMode,
            bool doubleScanMode)
        {
            var result = InitializeResult(certificateModel, validationMode);

            var exemption = certificateModel.Dgc.AsItalianDgc()?.GetCertificateEntry<ExemptionEntry>(DiseaseAgents.Covid19);
            if (exemption == null)
                return result;

            result.ValidFrom = exemption.ValidFrom.Date;
            result.ValidUntil = exemption.ValidUntil?.Date;

            if (exemption.ValidFrom.Date > result.ValidationInstant.Date)
                result.ItalianStatus = DgcItalianResultStatus.NotValidYet;
            else if (exemption.ValidUntil != null && result.ValidationInstant.Date > exemption.ValidUntil?.Date)
                result.ItalianStatus = DgcItalianResultStatus.Expired;
            else
            {
                switch (validationMode)
                {
                    case ValidationMode.EntryItaly:
                        result.ItalianStatus = DgcItalianResultStatus.NotValid;
                        result.StatusMessage = $"Exemptions are not valid for entry in Italy";
                        break;
                    case ValidationMode.Booster:
                        result.ItalianStatus = DgcItalianResultStatus.TestNeeded;
                        result.StatusMessage = $"Certificate is valid, but mode {validationMode} requires also a valid test";
                        break;
                    default:
                        result.ItalianStatus = DgcItalianResultStatus.Valid;
                        break;
                }
            }

            return result;
        }
    }
}
