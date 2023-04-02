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
            case ValidationMode.Strict2G:
                return ValidateFor2G(certificateModel, rules, validationMode);
            case ValidationMode.Booster:
                return ValidateForBooster(certificateModel, rules, validationMode);
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

    /// <summary>
    /// Porting of vaccineStrengthenedStrategy
    /// </summary>
    /// <param name="certificateModel"></param>
    /// <param name="rules"></param>
    /// <param name="validationMode"></param>
    /// <returns></returns>
    private ItalianRulesValidationResult ValidateFor2G(
        ValidationCertificateModel certificateModel,
        IEnumerable<RuleSetting> rules,
        ValidationMode validationMode)
    {
        var result = InitializeResult(certificateModel, validationMode);

        var vaccination = certificateModel.Dgc.GetCertificateEntry<VaccinationEntry>(DiseaseAgents.Covid19);
        if (vaccination == null)
            return result;

        if (vaccination.IsFromItaly())
            return ValidateFor3G(certificateModel, rules, validationMode);

        var validationDate = certificateModel.ValidationInstant.Date;
        var vaccinationDate = vaccination.Date.Date;

        DateTime? extendedDate = null;
        if (!vaccination.IsComplete())
        {
            result.ValidFrom = vaccinationDate.AddDays(rules.GetVaccineStartDayNotComplete(vaccination.MedicinalProduct));
            result.ValidUntil = vaccinationDate.AddDays(rules.GetVaccineEndDayNotComplete(vaccination.MedicinalProduct));

            if (!rules.IsEMA(vaccination))
            {
                result.ItalianStatus = DgcItalianResultStatus.NotValid;
                result.StatusMessage = $"Vaccination with {vaccination.MedicinalProduct} from country {vaccination.Country} are not considered valid by EMA";
                return result;
            }
        }
        else
        {
            // Complete
            var startDaysToAdd = vaccination.IsBooster() ?
                rules.GetVaccineStartDayBoosterUnified(CountryCodes.Italy) :
                rules.GetVaccineStartDayCompleteUnified(CountryCodes.Italy, vaccination.MedicinalProduct);

            var endDaysToAdd = vaccination.IsBooster() ?
                rules.GetVaccineEndDayBoosterUnified(CountryCodes.Italy) :
                rules.GetVaccineEndDayCompleteUnified(CountryCodes.Italy);

            var extendedDaysToAdd = rules.GetVaccineEndDayCompleteExtendedEMA();

            result.ValidFrom = vaccinationDate.AddDays(startDaysToAdd);
            result.ValidUntil = vaccinationDate.AddDays(endDaysToAdd);
            extendedDate = vaccinationDate.AddDays(extendedDaysToAdd);
        }

        if (!vaccination.IsComplete())
        {
            if (!rules.IsEMA(vaccination))
            {
                result.ItalianStatus = DgcItalianResultStatus.NotValid;
                result.StatusMessage = $"Vaccination with {vaccination.MedicinalProduct} from country {vaccination.Country} are not considered valid by EMA";
            }
            else if (validationDate < result.ValidFrom)
                result.ItalianStatus = DgcItalianResultStatus.NotValidYet;
            else if (validationDate > result.ValidUntil)
                result.ItalianStatus = DgcItalianResultStatus.Expired;
            else
                result.ItalianStatus = DgcItalianResultStatus.Valid;
        }
        else if (vaccination.IsBooster())
        {
            if (validationDate < result.ValidFrom)
                result.ItalianStatus = DgcItalianResultStatus.NotValidYet;
            else if (validationDate > result.ValidUntil)
                result.ItalianStatus = DgcItalianResultStatus.Expired;
            else if (rules.IsEMA(vaccination))
                result.ItalianStatus = DgcItalianResultStatus.Valid;
            else
            {
                result.ItalianStatus = DgcItalianResultStatus.TestNeeded;
                result.StatusMessage = $"Test is needed for vaccines not certified by EMA";
            }
        }
        else
        {
            if (rules.IsEMA(vaccination))
            {
                if (validationDate < result.ValidFrom)
                    result.ItalianStatus = DgcItalianResultStatus.NotValidYet;
                else if (validationDate <= result.ValidUntil)
                    result.ItalianStatus = DgcItalianResultStatus.Valid;
                else if (validationDate <= extendedDate && extendedDate != null)
                {
                    result.ItalianStatus = DgcItalianResultStatus.TestNeeded;
                    result.StatusMessage = $"Test is needed for expired vaccination within the extended date ({extendedDate:d})";
                }
                else
                    result.ItalianStatus = DgcItalianResultStatus.Expired;
            }
            else
            {
                if (validationDate < result.ValidFrom)
                    result.ItalianStatus = DgcItalianResultStatus.NotValidYet;
                else if (validationDate <= extendedDate && extendedDate != null)
                {
                    result.ItalianStatus = DgcItalianResultStatus.TestNeeded;
                    result.StatusMessage = $"Test is needed for vaccination not certifid by EMA, that are still in the extended date period ({extendedDate:d})";
                }
                else
                    result.ItalianStatus = DgcItalianResultStatus.Expired;
            }
        }


        return result;
    }


    /// <summary>
    /// Porting of vaccineBoosterStrategy
    /// </summary>
    /// <param name="certificateModel"></param>
    /// <param name="rules"></param>
    /// <param name="validationMode"></param>
    /// <returns></returns>
    private ItalianRulesValidationResult ValidateForBooster(
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

        var startDaysToAdd =
            vaccination.IsBooster() ? rules.GetVaccineStartDayBoosterUnified(CountryCodes.Italy) :
            !vaccination.IsComplete() ? rules.GetVaccineStartDayNotComplete(vaccination.MedicinalProduct) :
            rules.GetVaccineStartDayCompleteUnified(CountryCodes.Italy, vaccination.MedicinalProduct);

        var endDaysToAdd =
            vaccination.IsBooster() ? rules.GetVaccineEndDayBoosterUnified(CountryCodes.Italy) :
            !vaccination.IsComplete() ? rules.GetVaccineEndDayNotComplete(vaccination.MedicinalProduct) :
            rules.GetVaccineEndDayCompleteUnified(CountryCodes.Italy);

        result.ValidFrom = vaccinationDate.AddDays(startDaysToAdd);
        result.ValidUntil = vaccinationDate.AddDays(endDaysToAdd);

        if (validationDate < result.ValidFrom)
            result.ItalianStatus = DgcItalianResultStatus.NotValidYet;
        else if (validationDate > result.ValidUntil)
            result.ItalianStatus = DgcItalianResultStatus.Expired;
        else if (vaccination.IsComplete())
        {
            if (vaccination.IsBooster() && rules.IsEMA(vaccination))
                result.ItalianStatus = DgcItalianResultStatus.Valid;
            else
            {
                result.StatusMessage = $"Test is needed for non-booster vaccination and vaccines not certifid by EMA";
                result.ItalianStatus = DgcItalianResultStatus.TestNeeded;
            }
        }
        else
            result.ItalianStatus = DgcItalianResultStatus.NotValid;

        return result;
    }
}
