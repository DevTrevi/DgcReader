using DgcReader.RuleValidators.Italy.Models;
using System.Collections.Generic;

namespace DgcReader.RuleValidators.Italy.Validation
{

    /// <summary>
    /// Validator forItalian Rules
    /// </summary>
    public interface ICertificateEntryValidator
    {
        /// <summary>
        /// Return the validation result for rules implemented by this type of validator
        /// </summary>
        /// <param name="certificateModel"></param>
        /// <param name="rules"></param>
        /// <param name="validationMode"></param>
        /// <returns></returns>
        ItalianRulesValidationResult CheckCertificate(
            ValidationCertificateModel certificateModel,
            IEnumerable<RuleSetting> rules,
            ValidationMode validationMode);
    }
}
