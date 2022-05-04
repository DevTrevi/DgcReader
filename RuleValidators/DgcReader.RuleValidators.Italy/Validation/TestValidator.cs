using DgcReader.RuleValidators.Italy.Const;
using DgcReader.RuleValidators.Italy.Models;
using GreenpassReader.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace DgcReader.RuleValidators.Italy.Validation
{
    /// <summary>
    /// Validator for Test entries
    /// </summary>
    public class TestValidator : BaseValidator
    {
        /// <inheritdoc/>
        public TestValidator(ILogger? logger) : base(logger)
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

            var test = certificateModel.Dgc.GetCertificateEntry<TestEntry>(DiseaseAgents.Covid19);
            if (test == null)
                return result;

            // Check if the validation mode selected is Booster, then check if double scan flow is selected
            var isADoubleScanBoosterTest = validationMode == ValidationMode.Booster;

            // Super Greenpass check
            if (new[] {
                ValidationMode.Strict2G,
                ValidationMode.Booster,
            }.Contains(validationMode))
            {
                result.StatusMessage = $"Test entries are considered not valid when validation mode is {validationMode}";
                result.ItalianStatus = DgcItalianResultStatus.NotValid;
                Logger?.LogWarning(result.StatusMessage);
                return result;
            }

            if (test.TestResult == TestResults.NotDetected)
            {
                // Negative test
                switch (test.TestType)
                {
                    case TestTypes.Rapid:
                        result.ValidFrom = test.SampleCollectionDate.AddHours(rules.GetRapidTestStartHour());
                        result.ValidUntil = test.SampleCollectionDate.AddHours(rules.GetRapidTestEndHour());
                        break;
                    case TestTypes.Molecular:
                        result.ValidFrom = test.SampleCollectionDate.AddHours(rules.GetMolecularTestStartHour());

                        // See https://github.com/ministero-salute/it-dgc-verificac19-sdk-android/compare/1.1.5...release/1.1.6
                        if (doubleScanMode && isADoubleScanBoosterTest)
                            result.ValidUntil = test.SampleCollectionDate.AddHours(rules.GetRapidTestEndHour());
                        else
                            result.ValidUntil = test.SampleCollectionDate.AddHours(rules.GetMolecularTestEndHour());
                        break;
                    default:
                        result.StatusMessage = $"Test type {test.TestType} not supported by current rules";
                        result.ItalianStatus = DgcItalianResultStatus.NotValid;
                        Logger?.LogWarning(result.StatusMessage);
                        return result;
                }

                // Calculate the status
                if (result.ValidFrom > result.ValidationInstant)
                    result.ItalianStatus = DgcItalianResultStatus.NotValidYet;
                else if (result.ValidUntil < result.ValidationInstant)
                    result.ItalianStatus = DgcItalianResultStatus.Expired;
                else
                    result.ItalianStatus = DgcItalianResultStatus.Valid;
            }
            else
            {
                // Positive test or unknown result
                if (test.TestResult != TestResults.Detected)
                {
                    result.StatusMessage = $"Found test with unkwnown TestResult {test.TestResult}. The certificate is considered not valid";
                    Logger?.LogWarning(result.StatusMessage);

                }
                else
                {
                    result.StatusMessage = "Test result is positive, certificate is not valid";
                }

                result.ItalianStatus = DgcItalianResultStatus.NotValid;
            }

            return result;
        }
    }
}
