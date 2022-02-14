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

            // If mode is not 3G, use always rules for Italy
            var countryCode = validationMode == ValidationMode.Basic3G ? recovery.Country : CountryCodes.Italy;

            // Check if is PV (post-vaccination) recovery by checking signer certificate
            var isPvRecovery = IsRecoveryPvSignature(certificateModel.SignatureData);

            var startDaysToAdd = isPvRecovery ? rules.GetRecoveryPvCertStartDay() : rules.GetRecoveryCertStartDayUnified(countryCode);
            var endDaysToAdd =
                validationMode == ValidationMode.School ? rules.GetRecoveryCertEndDaySchool() :
                isPvRecovery ? rules.GetRecoveryPvCertEndDay() :
                rules.GetRecoveryCertEndDayUnified(countryCode);

            var startDate =
                validationMode == ValidationMode.School ? recovery.FirstPositiveTestResult.Date :
                recovery.ValidFrom.Date;
            var endDate = startDate.AddDays(endDaysToAdd);


            result.ValidFrom = startDate.AddDays(startDaysToAdd);
            result.ValidUntil = endDate;

            if (result.ValidationInstant.Date < result.ValidFrom)
                result.ItalianStatus = DgcItalianResultStatus.NotValidYet;
            else if (result.ValidationInstant.Date > result.ValidUntil)
                result.ItalianStatus = DgcItalianResultStatus.Expired;
            else
            {
                if (validationMode == ValidationMode.Booster && !isPvRecovery)
                {
                    result.ItalianStatus = DgcItalianResultStatus.TestNeeded;
                    result.StatusMessage = "Certificate is valid, but a test is also needed if mode is booster and the recovery certificate is not issued after a vaccination";
                }
                else
                    result.ItalianStatus = DgcItalianResultStatus.Valid;
            }

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

            if (signatureValidationResult.Issuer != CountryCodes.Italy)
                return false;

            return extendedKeyUsages.Any(usage => CertificateExtendedKeyUsageIdentifiers.RecoveryIssuersIds.Contains(usage));
        }
    }


}
