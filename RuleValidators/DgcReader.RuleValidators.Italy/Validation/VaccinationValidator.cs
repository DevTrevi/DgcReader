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
            var result = InitializeResult(certificateModel, validationMode);

            var vaccination = certificateModel.Dgc.GetCertificateEntry<VaccinationEntry>(DiseaseAgents.Covid19);
            if (vaccination == null)
                return result;

            int startDay, endDay;
            if (vaccination.DoseNumber > 0 && vaccination.TotalDoseSeries > 0)
            {
                // Calculate start/end days
                if (vaccination.DoseNumber < vaccination.TotalDoseSeries)
                {
                    // Vaccination is not completed (partial number of doses)
                    startDay = rules.GetVaccineStartDayNotComplete(vaccination.MedicinalProduct);
                    endDay = rules.GetVaccineEndDayNotComplete(vaccination.MedicinalProduct);
                }
                else
                {
                    // Vaccination completed (full number of doses)

                    // If mode is not basic, use always rules for Italy
                    var countryCode = validationMode == ValidationMode.Basic3G ? vaccination.Country : "IT";


                    // Check rules for "BOOSTER" certificates
                    if (vaccination.IsBooster())
                    {
                        startDay = rules.GetVaccineStartDayBoosterUnified(countryCode);
                        endDay = rules.GetVaccineEndDayBoosterUnified(countryCode);
                    }
                    else
                    {
                        startDay = rules.GetVaccineStartDayCompleteUnified(countryCode, vaccination.MedicinalProduct);
                        endDay = rules.GetVaccineEndDayCompleteUnified(countryCode);
                    }
                }

                // Calculate start/end dates
                if (vaccination.MedicinalProduct == VaccineProducts.JeJVacineCode &&
                        (vaccination.DoseNumber > vaccination.TotalDoseSeries || vaccination.DoseNumber >= 2))
                {
                    // For J&J booster, in case of more vaccinations than expected, the vaccine is immediately valid
                    result.ValidFrom = vaccination.Date.Date;
                    result.ValidUntil = vaccination.Date.Date.AddDays(endDay);
                }
                else
                {
                    result.ValidFrom = vaccination.Date.Date.AddDays(startDay);
                    result.ValidUntil = vaccination.Date.Date.AddDays(endDay);
                }

                // Calculate the status

                // Exception: Checking sputnik not from San Marino
                if (vaccination.MedicinalProduct == VaccineProducts.Sputnik && vaccination.Country != "SM")
                {
                    result.ItalianStatus = DgcItalianResultStatus.NotValid;
                    return result;
                }

                if (result.ValidFrom > result.ValidationInstant.Date)
                    result.ItalianStatus = DgcItalianResultStatus.NotValidYet;
                else if (result.ValidUntil < result.ValidationInstant.Date)
                    result.ItalianStatus = DgcItalianResultStatus.Expired;
                else
                {
                    if (vaccination.DoseNumber < vaccination.TotalDoseSeries)
                    {
                        // Incomplete cycle, invalid for BOOSTER and SCHOOL mode
                        result.ItalianStatus = new[] {
                            ValidationMode.Booster,
                            ValidationMode.School
                        }.Contains(validationMode) ? DgcItalianResultStatus.NotValid : DgcItalianResultStatus.Valid;
                    }
                    else
                    {
                        // Complete cycle
                        if (validationMode == ValidationMode.Booster)
                        {
                            if (vaccination.IsBooster())
                            {
                                // If dose number is higher than total dose series, or minimum booster dose number reached
                                result.ItalianStatus = DgcItalianResultStatus.Valid;
                            }
                            else
                            {
                                // Otherwise, if less thant the minimum "booster" doses, requires a test
                                result.ItalianStatus = DgcItalianResultStatus.TestNeeded;
                            }
                        }
                        else
                        {
                            // Non-booster mode: valid
                            result.ItalianStatus = DgcItalianResultStatus.Valid;
                        }
                    }

                }
            }

            return result;
        }
    }
}
