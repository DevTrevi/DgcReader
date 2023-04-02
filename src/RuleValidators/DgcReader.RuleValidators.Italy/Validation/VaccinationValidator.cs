using DgcReader.RuleValidators.Italy.Const;
using DgcReader.RuleValidators.Italy.Models;
using GreenpassReader.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace DgcReader.RuleValidators.Italy.Validation;

/// <summary>
/// Validator for Vaccination entries
/// </summary>
public class VaccinationValidator : BaseValidator
{
    /// <inheritdoc/>
    public VaccinationValidator(ILogger? logger) : base(logger)
    {
    }

    /// <inheritdoc/>
    public override ItalianRulesValidationResult CheckCertificate(
        ValidationCertificateModel certificateModel,
        IEnumerable<RuleSetting> rules,
        ValidationMode validationMode)
    {
        switch (validationMode)
        {
            case ValidationMode.Basic3G:
                return ValidateFor3G(certificateModel, rules, validationMode);
            default:
                var result = InitializeResult(certificateModel, validationMode);

                result.ItalianStatus = DgcItalianResultStatus.NeedRulesVerification;
                result.StatusMessage = $"Validation for mode {validationMode} is not implemented";
                return result;
        }
    }

    /// <summary>
    /// Porting of vaccineStandardStrategy
    /// </summary>
    /// <param name="certificateModel"></param>
    /// <param name="rules"></param>
    /// <param name="validationMode"></param>
    /// <returns></returns>
    private ItalianRulesValidationResult ValidateFor3G(
        ValidationCertificateModel certificateModel,
        IEnumerable<RuleSetting> rules,
        ValidationMode validationMode)
    {
        var result = InitializeResult(certificateModel, validationMode);

        var vaccination = certificateModel.Dgc.GetCertificateEntry<VaccinationEntry>(DiseaseAgents.Covid19);
        if (vaccination == null)
            return result;

        var validationDate = certificateModel.ValidationInstant.Date;
        var vaccinationDate = vaccination.Date.Date;

        // startDate
        if (vaccination.IsComplete())
        {
            result.ValidFrom = vaccinationDate.AddDays(
                vaccination.IsBooster() ? rules.GetVaccineStartDayBoosterUnified(CountryCodes.Italy) :
                rules.GetVaccineStartDayCompleteUnified(CountryCodes.Italy, vaccination.MedicinalProduct));
        }
        else
        {
            result.ValidFrom = vaccinationDate.AddDays(rules.GetVaccineStartDayNotComplete(vaccination.MedicinalProduct));
        }

        // endDate
        if (vaccination.IsComplete())
        {
            result.ValidUntil = vaccinationDate.AddDays(
                vaccination.IsBooster() ? rules.GetVaccineEndDayBoosterUnified(CountryCodes.Italy) :
                rules.GetVaccineEndDayCompleteUnified(CountryCodes.Italy));
        }
        else
        {
            result.ValidUntil = vaccinationDate.AddDays(rules.GetVaccineEndDayNotComplete(vaccination.MedicinalProduct));
        }

        if (validationDate < result.ValidFrom)
            result.ItalianStatus = DgcItalianResultStatus.NotValidYet;
        else if (validationDate > result.ValidUntil)
            result.ItalianStatus = DgcItalianResultStatus.Expired;
        else if (!rules.IsEMA(vaccination))
        {
            result.StatusMessage = $"Vaccination with {vaccination.MedicinalProduct} from country {vaccination.Country} are not considered valid by EMA";
            result.ItalianStatus = DgcItalianResultStatus.NotValid;
        }
        else
        {
            result.ItalianStatus = DgcItalianResultStatus.Valid;
        }

        return result;
    }
}
