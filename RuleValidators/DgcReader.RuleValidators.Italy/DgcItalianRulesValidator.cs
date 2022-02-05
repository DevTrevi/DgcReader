using GreenpassReader.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DgcReader.RuleValidators.Italy.Const;
using DgcReader.RuleValidators.Italy.Models;
using System.Threading;
using Microsoft.Extensions.Logging;
using DgcReader.Interfaces.RulesValidators;
using DgcReader.Interfaces.BlacklistProviders;
using DgcReader.RuleValidators.Italy.Providers;
using DgcReader.Exceptions;
using DgcReader.Models;

#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER || NET47_OR_GREATER
using Microsoft.Extensions.Options;
#endif

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Italy
{

    /// <summary>
    /// Unofficial porting of the Italian rules from https://github.com/ministero-salute/it-dgc-verificac19-sdk-android.
    /// This service is also an implementation of <see cref="IBlacklistProvider"/>
    /// </summary>
    public class DgcItalianRulesValidator : IRulesValidator, IBlacklistProvider
    {
        // File containing the business logic on the offical SDK repo:
        // https://github.com/ministero-salute/it-dgc-verificac19-sdk-android/blob/develop/sdk/src/main/java/it/ministerodellasalute/verificaC19sdk/model/VerificationViewModel.kt

        private readonly ILogger? Logger;
        private readonly DgcItalianRulesValidatorOptions Options;

        private readonly RulesProvider _rulesProvider;

#if NET452
        /// <summary>
        /// Constructor
        /// </summary>
        public DgcItalianRulesValidator(HttpClient httpClient,
            DgcItalianRulesValidatorOptions? options = null,
            ILogger<DgcItalianRulesValidator>? logger = null)
        {
            Options = options ?? new DgcItalianRulesValidatorOptions();
            Logger = logger;

            _rulesProvider = new RulesProvider(httpClient, Options, logger);
        }

        /// <summary>
        /// Factory method for creating an instance of <see cref="DgcItalianRulesValidator"/>
        /// whithout using the DI mechanism. Useful for legacy applications
        /// </summary>
        /// <param name="httpClient">The http client instance that will be used for requests to the server</param>
        /// <param name="options">The options for the provider</param>
        /// <param name="logger">Instance of <see cref="ILogger"/> used by the provider (optional).</param>
        /// <returns></returns>
        public static DgcItalianRulesValidator Create(HttpClient httpClient,
            DgcItalianRulesValidatorOptions? options = null,
            ILogger<DgcItalianRulesValidator>? logger = null)
        {
            return new DgcItalianRulesValidator(httpClient, options, logger);
        }

#else
        /// <summary>
        /// Constructor
        /// </summary>
        public DgcItalianRulesValidator(HttpClient httpClient,
            IOptions<DgcItalianRulesValidatorOptions>? options = null,
            ILogger<DgcItalianRulesValidator>? logger = null)
        {
            Options = options?.Value ?? new DgcItalianRulesValidatorOptions();
            Logger = logger;

            _rulesProvider = new RulesProvider(httpClient, Options, logger);
        }

        /// <summary>
        /// Factory method for creating an instance of <see cref="DgcItalianRulesValidator"/>
        /// whithout using the DI mechanism. Useful for legacy applications
        /// </summary>
        /// <param name="httpClient">The http client instance that will be used for requests to the server</param>
        /// <param name="options">The options for the provider</param>
        /// <param name="logger">Instance of <see cref="ILogger"/> used by the provider (optional).</param>
        /// <returns></returns>
        public static DgcItalianRulesValidator Create(HttpClient httpClient,
            DgcItalianRulesValidatorOptions? options = null,
            ILogger<DgcItalianRulesValidator>? logger = null)
        {
            return new DgcItalianRulesValidator(httpClient,
                options == null ? null : Microsoft.Extensions.Options.Options.Create(options),
                logger);
        }
#endif

        #region Implementation of IRulesValidator

        /// <inheritdoc/>
        public async Task<IRulesValidationResult> GetRulesValidationResult(EuDGC dgc,
            string dgcJson,
            DateTimeOffset validationInstant,
            string countryCode = "IT",
            SignatureValidationResult? signatureValidationResult = null,
            BlacklistValidationResult? blacklistValidationResult = null,
            CancellationToken cancellationToken = default)
        {
            if (!await SupportsCountry(countryCode))
            {
                var result = new ItalianRulesValidationResult
                {
                    ItalianStatus = DgcItalianResultStatus.NotValidated,
                    StatusMessage = $"Rules validation for country {countryCode} is not supported by this provider",
                };
                return result;
            }

            // Validation mode check
            if (Options.ValidationMode == null)
            {
                // Warning if not set excplicitly
                Logger?.LogWarning($"Validation mode not set. The {ValidationMode.Basic3G} validation mode will be used");
            }

            return await this.GetRulesValidationResult(dgc,
                dgcJson,
                validationInstant,
                Options.ValidationMode ?? ValidationMode.Basic3G,
                signatureValidationResult,
                blacklistValidationResult,
                cancellationToken);
        }

        /// <inheritdoc/>
        public Task RefreshRules(string? countryCode = null, CancellationToken cancellationToken = default)
        {
            return _rulesProvider.RefreshValueSet(cancellationToken);
        }

        /// <inheritdoc/>
        public Task<IEnumerable<string>> GetSupportedCountries(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new[] { "IT" }.AsEnumerable());
        }

        /// <inheritdoc/>
        public async Task<bool> SupportsCountry(string countryCode, CancellationToken cancellationToken = default)
        {
            var supportedCountries = await GetSupportedCountries();
            return supportedCountries.Any(r => r.Equals(countryCode, StringComparison.InvariantCultureIgnoreCase));
        }
        #endregion

        #region Implementation of IBlackListProvider

        /// <inheritdoc/>
        public async Task<bool> IsBlacklisted(string certificateIdentifier, CancellationToken cancellationToken = default)
        {
            var rulesContainer = await _rulesProvider.GetValueSet(cancellationToken);
            var blacklist = rulesContainer?.Rules?.GetBlackList();

            if (blacklist == null)
            {
                Logger?.LogWarning($"Unable to get the blacklist: considering the certificate valid");
                return true;
            }

            return blacklist.Contains(certificateIdentifier);
        }

        /// <inheritdoc/>
        public async Task RefreshBlacklist(CancellationToken cancellationToken = default)
        {
            await _rulesProvider.RefreshValueSet(cancellationToken);
        }
        #endregion

        #region Public methods

        /// <inheritdoc/>
        public async Task<IEnumerable<string>?> GetBlacklist(CancellationToken cancellationToken = default)
        {
            var rulesContainer = await _rulesProvider.GetValueSet(cancellationToken);
            return rulesContainer?.Rules?.GetBlackList();
        }

        /// <summary>
        /// Returns the validation result
        /// </summary>
        /// <param name="dgc"></param>
        /// <param name="dgcJson">The RAW json of the DGC</param>
        /// <param name="validationInstant"></param>
        /// <param name="validationMode">The Italian validation mode to be used</param>
        /// <param name="signatureValidationResult">The result from the signature validation step</param>
        /// <param name="blacklistValidationResult">The result from the blacklist validation step</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IRulesValidationResult> GetRulesValidationResult(EuDGC dgc,
            string dgcJson,
            DateTimeOffset validationInstant,
            ValidationMode validationMode,
            SignatureValidationResult? signatureValidationResult = null,
            BlacklistValidationResult? blacklistValidationResult = null,
            CancellationToken cancellationToken = default)
        {
            var result = new ItalianRulesValidationResult
            {
                ValidationInstant = validationInstant,
            };

            if (dgc == null)
            {
                result.ItalianStatus = DgcItalianResultStatus.NotEuDCC;
                return result;
            }

            if (signatureValidationResult?.HasValidSignature != true)
            {
                result.ItalianStatus = DgcItalianResultStatus.InvalidSignature;
                return result;
            }

            if (blacklistValidationResult?.IsBlacklisted == true)
            {
                // Note: returns revoked for both Blacklist and revocation list.
                // if needed, you can differentiate the result by checking the provider that returned the blacklist result
                // blacklistValidationResult.BlacklistMatchProviderType
                result.ItalianStatus = DgcItalianResultStatus.Revoked;
                return result;
            }

            try
            {
                var rulesContainer = await _rulesProvider.GetValueSet(cancellationToken);
                var rules = rulesContainer?.Rules;
                if (rules == null)
                {
                    result.ItalianStatus = DgcItalianResultStatus.NotValidated;
                    result.StatusMessage = "Unable to get validation rules";
                    return result;
                }

                // Checking min version:
                CheckMinSdkVersion(rules, validationInstant);

                if (dgc.Recoveries?.Any(r => r.TargetedDiseaseAgent == DiseaseAgents.Covid19) == true)
                {
                    CheckRecoveryStatements(dgc, result, rules, signatureValidationResult, validationMode);
                }
                else if (dgc.Tests?.Any(r => r.TargetedDiseaseAgent == DiseaseAgents.Covid19) == true)
                {
                    CheckTests(dgc, result, rules, signatureValidationResult, validationMode);
                }
                else if (dgc.Vaccinations?.Any(r => r.TargetedDiseaseAgent == DiseaseAgents.Covid19) == true)
                {
                    CheckVaccinations(dgc, result, rules, signatureValidationResult, validationMode);
                }
                else
                {
                    // Try to check for exemptions (custom for Italy)
                    var italianDgc = ItalianDGC.FromJson(dgcJson);
                    if(italianDgc?.Exemptions?.Any(r => r.TargetedDiseaseAgent == DiseaseAgents.Covid19) == true)
                    {
                        CheckExemptionStatements(italianDgc, result, rules, signatureValidationResult, validationMode);
                    }
                    else
                    {
                        // An EU DCC must have one of the sections above.
                        Logger?.LogWarning($"No vaccinations, tests, recovery or exemptions statements found in the certificate.");
                        result.ItalianStatus = DgcItalianResultStatus.NotEuDCC;
                    }
                }
            }
            catch (DgcRulesValidationException e)
            {
                if (e.ValidationResult != null)
                    return e.ValidationResult;

            }
            catch (Exception e)
            {
                Logger?.LogError(e, $"Validation failed with error {e.Message}");
                result.ItalianStatus = DgcItalianResultStatus.NotValid;
            }
            return result;
        }

        #endregion

        #region Validation methods

        /// <summary>
        /// Computes the status by checking the vaccinations in the DCC
        /// </summary>
        /// <param name="dgc"></param>
        /// <param name="result">The output result compiled by the function</param>
        /// <param name="rules"></param>
        /// <param name="signatureValidation">The result from the signature validation step</param>
        /// <param name="validationMode"></param>
        private void CheckVaccinations(EuDGC dgc, ItalianRulesValidationResult result, IEnumerable<RuleSetting> rules,
            SignatureValidationResult? signatureValidation, ValidationMode validationMode)
        {
            var vaccination = dgc.Vaccinations.Last(r => r.TargetedDiseaseAgent == DiseaseAgents.Covid19);
            if (vaccination == null) return;

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
                    startDay = rules.GetVaccineStartDayComplete(vaccination.MedicinalProduct);
                    endDay = rules.GetVaccineEndDayComplete(vaccination.MedicinalProduct);

                }

                // Calculate start/end dates
                if (vaccination.MedicinalProduct == VaccineProducts.JeJVacineCode &&
                        (vaccination.DoseNumber > vaccination.TotalDoseSeries || vaccination.DoseNumber >= 2))
                {
                    // For J&J booster, in case of more vaccinations than expected, the vaccine is immediately valid
                    result.ValidFrom = vaccination.Date;
                    result.ValidUntil = vaccination.Date.AddDays(endDay);
                }
                else
                {
                    result.ValidFrom = vaccination.Date.AddDays(startDay);
                    result.ValidUntil = vaccination.Date.AddDays(endDay);
                }

                // Calculate the status

                // Exception: Checking sputnik not from San Marino
                if (vaccination.MedicinalProduct == VaccineProducts.Sputnik && vaccination.Country != "SM")
                {
                    result.ItalianStatus = DgcItalianResultStatus.NotValid;
                    return;
                }

                if (result.ValidFrom > result.ValidationInstant)
                    result.ItalianStatus = DgcItalianResultStatus.NotValidYet;
                else if (result.ValidUntil < result.ValidationInstant)
                    result.ItalianStatus = DgcItalianResultStatus.NotValid;
                else
                {
                    if(vaccination.DoseNumber < vaccination.TotalDoseSeries)
                    {
                        // Incomplete cycle, invalid for BOOSTER mode
                        result.ItalianStatus = validationMode == ValidationMode.Booster ? DgcItalianResultStatus.NotValid : DgcItalianResultStatus.Valid;
                    }
                    else
                    {
                        // Complete cycle
                        if (validationMode == ValidationMode.Booster)
                        {
                            // Min num of doses to be considered "booster":
                            // J&J: required full cycle with at least 2 doses, other vaccines 3 doses
                            var boostedDoseNumber = vaccination.MedicinalProduct == VaccineProducts.JeJVacineCode ? 2 : 3;


                            if (vaccination.DoseNumber > vaccination.TotalDoseSeries ||
                                vaccination.DoseNumber >= boostedDoseNumber)
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
        }

        /// <summary>
        /// Computes the status by checking the tests in the DCC
        /// </summary>
        /// <param name="dgc"></param>
        /// <param name="result">The output result compiled by the function</param>
        /// <param name="rules"></param>
        /// <param name="signatureValidation">The result from the signature validation step</param>
        /// <param name="validationMode"></param>
        private void CheckTests(EuDGC dgc, ItalianRulesValidationResult result, IEnumerable<RuleSetting> rules,
            SignatureValidationResult? signatureValidation, ValidationMode validationMode)
        {
            var test = dgc.Tests.Last(r => r.TargetedDiseaseAgent == DiseaseAgents.Covid19);
            if (test == null) return;

            // Super Greenpass check
            if (validationMode == ValidationMode.Strict2G || validationMode == ValidationMode.Booster)
            {
                // If BOOSTER or 2G mode is active, Test entries are considered not valid
                if (dgc.GetCertificateEntry() is TestEntry)
                {
                    Logger.LogWarning($"Test entries are considered not valid when validation mode is {validationMode}");
                    result.ItalianStatus = DgcItalianResultStatus.NotValid;
                    return;
                }
            }


            if (test.TestResult == TestResults.NotDetected)
            {
                // Negative test
                int startHours, endHours;

                switch (test.TestType)
                {
                    case TestTypes.Rapid:
                        startHours = rules.GetRapidTestStartHour();
                        endHours = rules.GetRapidTestEndHour();
                        break;
                    case TestTypes.Molecular:
                        startHours = rules.GetMolecularTestStartHour();
                        endHours = rules.GetMolecularTestEndHour();
                        break;
                    default:
                        Logger?.LogWarning($"Test type {test.TestType} not supported by current rules");
                        result.ItalianStatus = DgcItalianResultStatus.NotValid;
                        return;
                }

                result.ValidFrom = test.SampleCollectionDate.AddHours(startHours);
                result.ValidUntil = test.SampleCollectionDate.AddHours(endHours);

                // Calculate the status
                if (result.ValidFrom > result.ValidationInstant)
                    result.ItalianStatus = DgcItalianResultStatus.NotValidYet;
                else if (result.ValidUntil < result.ValidationInstant)
                    result.ItalianStatus = DgcItalianResultStatus.NotValid;
                else
                    result.ItalianStatus = DgcItalianResultStatus.Valid;
            }
            else
            {
                // Positive test or unknown result
                if (test.TestResult != TestResults.Detected)
                    Logger?.LogWarning($"Found test with unkwnown TestResult {test.TestResult}. The certificate is considered invalid");

                result.ItalianStatus = DgcItalianResultStatus.NotValid;
            }
        }

        /// <summary>
        /// Computes the status by checking the recovery statements in the DCC
        /// </summary>
        /// <param name="dgc"></param>
        /// <param name="result">The output result compiled by the function</param>
        /// <param name="rules"></param>
        /// <param name="signatureValidation">The result from the signature validation step</param>
        /// <param name="validationMode"></param>
        private void CheckRecoveryStatements(EuDGC dgc, ItalianRulesValidationResult result, IEnumerable<RuleSetting> rules,
            SignatureValidationResult? signatureValidation, ValidationMode validationMode)
        {
            var recovery = dgc.Recoveries.Last(r => r.TargetedDiseaseAgent == DiseaseAgents.Covid19);

            // Check if is PV (post-vaccination) recovery by checking signer certificate
            var isPvRecovery = IsRecoveryPvSignature(signatureValidation);

            var recoveryCertStartDay = isPvRecovery ? rules.GetRecoveryPvCertStartDay() : rules.GetRecoveryCertStartDay();
            var recoveryCertEndDay = isPvRecovery ? rules.GetRecoveryPvCertEndDay() : rules.GetRecoveryCertEndDay();

            result.ValidFrom = recovery.ValidFrom.Date.AddDays(recoveryCertStartDay);
            result.ValidUntil = recovery.ValidUntil.Date;

            if (result.ValidFrom > result.ValidationInstant)
                result.ItalianStatus = DgcItalianResultStatus.NotValidYet;
            else if (result.ValidationInstant > result.ValidFrom.Value.AddDays(recoveryCertEndDay))
                result.ItalianStatus = DgcItalianResultStatus.NotValid;
            else
                result.ItalianStatus = validationMode == ValidationMode.Booster ? DgcItalianResultStatus.TestNeeded : DgcItalianResultStatus.Valid;
        }

        /// <summary>
        /// Computes the status by checking the exemption statements in the Italian DCC
        /// </summary>
        /// <param name="italianDgc"></param>
        /// <param name="result">The output result compiled by the function</param>
        /// <param name="rules"></param>
        /// <param name="signatureValidation">The result from the signature validation step</param>
        /// <param name="validationMode"></param>
        private void CheckExemptionStatements(ItalianDGC italianDgc, ItalianRulesValidationResult result, IEnumerable<RuleSetting> rules,
            SignatureValidationResult? signatureValidation, ValidationMode validationMode)
        {
            var exemption = italianDgc.Exemptions.Last(r => r.TargetedDiseaseAgent == DiseaseAgents.Covid19);

            Logger?.LogDebug($"Exemption from {exemption.ValidFrom} to {exemption.ValidUntil}");

            if (exemption.ValidFrom > result.ValidationInstant)
                result.ItalianStatus = DgcItalianResultStatus.NotValidYet;
            else if (exemption.ValidUntil != null && result.ValidationInstant > exemption.ValidUntil)
                result.ItalianStatus = DgcItalianResultStatus.NotValid;
            else
            {
                if (validationMode == ValidationMode.Booster)
                    result.ItalianStatus = DgcItalianResultStatus.TestNeeded;
                else
                    result.ItalianStatus = DgcItalianResultStatus.Valid;
            }
        }


        /// <summary>
        /// Check the minimum version of the SDK implementation required.
        /// If <see cref="DgcItalianRulesValidatorOptions.IgnoreMinimumSdkVersion"/> is false, an exception will be thrown if the implementation is obsolete
        /// </summary>
        /// <param name="rules"></param>
        /// <param name="validationInstant"></param>
        /// <exception cref="DgcRulesValidationException"></exception>
        private void CheckMinSdkVersion(IEnumerable<RuleSetting> rules, DateTimeOffset validationInstant)
        {
            var obsolete = false;
            string message = string.Empty;


            var sdkMinVersion = rules.GetRule(SettingNames.SdkMinVersion, SettingTypes.AppMinVersion);
            if (sdkMinVersion != null)
            {
                if (sdkMinVersion.Value.CompareTo(SdkConstants.ReferenceSdkMinVersion) > 0)
                {
                    obsolete = true;
                    message = $"The minimum version of the SDK implementation is {sdkMinVersion.Value}. " +
                        $"Please update the package with the latest implementation in order to get a reliable result";
                }
            }
            else
            {
                // Fallback to android app version
                var appMinVersion = rules.GetRule(SettingNames.AndroidAppMinVersion, SettingTypes.AppMinVersion);
                if (appMinVersion != null)
                {
                    if (appMinVersion.Value.CompareTo(SdkConstants.ReferenceAppMinVersion) > 0)
                    {
                        obsolete = true;
                        message = $"The minimum version of the App implementation is {appMinVersion.Value}. " +
                            $"Please update the package with the latest implementation in order to get a reliable result";
                    }
                }
            }

            if (obsolete)
            {

                if (Options.IgnoreMinimumSdkVersion)
                {
                    Logger?.LogWarning(message);
                }
                else
                {
                    var result = new ItalianRulesValidationResult
                    {
                        ValidationInstant = validationInstant,
                        ItalianStatus = DgcItalianResultStatus.NotValidated,
                        StatusMessage = message,
                    };
                    throw new DgcRulesValidationException(message, result);
                }
            }

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

#endregion
    }
}