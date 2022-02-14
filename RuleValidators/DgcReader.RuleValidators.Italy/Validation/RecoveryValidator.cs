using DgcReader.Models;
using DgcReader.RuleValidators.Italy.Const;
using DgcReader.RuleValidators.Italy.Models;
using GreenpassReader.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace DgcReader.RuleValidators.Italy.Validation
{
    /// <summary>
    /// Validator for Recovery entries
    /// </summary>
    public class RecoveryValidator : BaseValidator
    {
        /// <inheritdoc/>
        public RecoveryValidator(ILogger? logger) : base(logger)
        {
        }

        /// <inheritdoc/>
        public override ItalianRulesValidationResult CheckCertificate(
            ValidationCertificateModel certificateModel,
            IEnumerable<RuleSetting> rules,
            ValidationMode validationMode)
        {
            var result = InitializeResult(certificateModel, validationMode);

            var recovery = certificateModel.Dgc.GetCertificateEntry<RecoveryEntry>(DiseaseAgents.Covid19);
            if (recovery == null)
                return result;

            // If mode is not basic, use always rules for Italy
            var countryCode = validationMode == ValidationMode.Basic3G ? recovery.Country : "IT";

            // Check if is PV (post-vaccination) recovery by checking signer certificate
            var isPvRecovery = IsRecoveryPvSignature(certificateModel.SignatureData);

            var startDaysToAdd = isPvRecovery ? rules.GetRecoveryPvCertStartDay() : rules.GetRecoveryCertStartDayUnified(countryCode);
            var endDaysToAdd =
                validationMode == ValidationMode.School ? rules.GetRecoveryCertEndDaySchool() :
                isPvRecovery ? rules.GetRecoveryPvCertEndDay() :
                rules.GetRecoveryCertEndDayUnified(countryCode);

            result.ValidFrom = recovery.ValidFrom.Date.AddDays(startDaysToAdd);
            if (validationMode == ValidationMode.School)
            {
                // Take the more restrictive from end of "quarantine" after first positive test and the original expiration from the Recovery entry
                result.ValidUntil = recovery.FirstPositiveTestResult.Date.AddDays(endDaysToAdd);
                if (recovery.ValidUntil < result.ValidUntil)
                    result.ValidUntil = recovery.ValidUntil;
            }
            else
            {
                result.ValidUntil = result.ValidFrom.Value.AddDays(endDaysToAdd);
            }

            if (result.ValidFrom > result.ValidationInstant.Date)
                result.ItalianStatus = DgcItalianResultStatus.NotValidYet;
            else if (result.ValidationInstant.Date > result.ValidFrom.Value.AddDays(endDaysToAdd))
                result.ItalianStatus = DgcItalianResultStatus.NotValid;
            else
                result.ItalianStatus = validationMode == ValidationMode.Booster ? DgcItalianResultStatus.TestNeeded : DgcItalianResultStatus.Valid;

            return result;
        }


        /// <summary>
        /// Check if the signer certificate is one of the signer of post-vaccination certificates
        /// </summary>
        /// <param name="signatureValidationResult"></param>
        /// <returns></returns>
        private bool IsRecoveryPvSignature(SignatureValidationResult? signatureValidationResult)
        {
            var extendedKeyUsages = CertificateExtendedKeyUsageUtils.GetExtendedKeyUsages(signatureValidationResult, Logger);

            if (signatureValidationResult == null)
                return false;

            if (signatureValidationResult.Issuer != "IT")
                return false;

            return extendedKeyUsages.Any(usage => CertificateExtendedKeyUsageIdentifiers.RecoveryIssuersIds.Contains(usage));
        }
    }


}
