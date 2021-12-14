using GreenpassReader.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DgcReader.RuleValidators.Italy.Const;
using DgcReader.RuleValidators.Italy.Models;
using DgcReader.Models;
using System.Threading;
using Microsoft.Extensions.Logging;
using DgcReader.Interfaces.RulesValidators;
using DgcReader.Interfaces.BlacklistProviders;
using DgcReader.RuleValidators.Italy.Providers;
using DgcReader.Exceptions;

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
        public Task<IRuleValidationResult> GetRulesValidationResult(EuDGC dgc, DateTimeOffset validationInstant, string countryCode = "IT", CancellationToken cancellationToken = default)
        {


            // Validation mode check
            if (_options.ValidationMode == null)
            {
                // Warning if not set excplicitly
                _logger?.LogWarning($"Validation mode not set. The {ValidationMode.Basic3G} validation mode will be used");
            }

            return this.GetRulesValidationResult(dgc,
                validationInstant,
                _options.ValidationMode ?? ValidationMode.Basic3G,
                cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IRulesValidationResult> GetRulesValidationResult(EuDGC dgc, DateTimeOffset validationInstant, string countryCode = "IT", ValidationMode validationMode, CancellationToken cancellationToken = default)
        {
            var result = new ItalianRulesValidationResult
            {
                ValidationInstant = validationInstant,
            };

            if (!await SupportsCountry(countryCode))
            {
                result.ItalianStatus = DgcItalianResultStatus.NeedRulesVerification;
                result.StatusMessage = $"Rules validation for country {countryCode} is not supported by this provider";
                return result;
            }

            if (dgc == null)
            {
                result.ItalianStatus = DgcItalianResultStatus.NotEuDCC;
                return result;
            }

            // Super Greenpass check
            if (validationMode == ValidationMode.Strict2G)
            {
                // If 2G mode is active, Test entries are considered not valid
                if (dgc.GetCertificateEntry() is TestEntry)
                {
                    Logger.LogWarning($"Test entries are considered not valid when validation mode is {ValidationMode.Strict2G}");
                    result.ItalianStatus = DgcItalianResultStatus.NotValid;
                    return result;
                }
            }

            try
            {
                var rulesContainer = await _rulesProvider.GetValueSet(cancellationToken);
                var rules = rulesContainer?.Rules;
                if (rules == null)
                {
                    result.ItalianStatus = DgcItalianResultStatus.NeedRulesVerification;
                    result.StatusMessage = "Unable to get validation rules";
                    return result;
                }

                // Checking min version:
                CheckMinSdkVersion(rules, validationInstant);

                if (dgc.Recoveries?.Any(r => r.TargetedDiseaseAgent == DiseaseAgents.Covid19) == true)
                {
                    CheckRecoveryStatements(dgc, result, rules);
                }
                else if (dgc.Tests?.Any(r => r.TargetedDiseaseAgent == DiseaseAgents.Covid19) == true)
                {
                    CheckTests(dgc, result, rules);
                }
                else if (dgc.Vaccinations?.Any(r => r.TargetedDiseaseAgent == DiseaseAgents.Covid19) == true)
                {
                    CheckVaccinations(dgc, result, rules);
                }
                else
                {
                    // An EU DCC must have one of the sections above.
                    Logger?.LogWarning($"No vaccinations, tests or recovery statements found in the certificate.");
                    result.ItalianStatus = DgcItalianResultStatus.NotEuDCC;
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
            var blacklist = await GetBlacklist(cancellationToken);
            if (blacklist == null)
            {
                Logger?.LogWarning($"Unable to get the blacklist: considering the certificate valid");
                return true;
            }

            return blacklist.Contains(certificateIdentifier);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<string>?> GetBlacklist(CancellationToken cancellationToken = default)
        {
            var rulesContainer = await _rulesProvider.GetValueSet(cancellationToken);
            return rulesContainer?.Rules?.GetBlackList();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<string>?> RefreshBlacklist(CancellationToken cancellationToken = default)
        {
            var rulesSet = await _rulesProvider.RefreshValueSet(cancellationToken);
            return rulesSet?.Rules?.GetBlackList();
        }
        #endregion


        /// <summary>
        /// Validates the specified certificate against the Italian business rules.
        /// Is assumed that the Signed DGC signature was already validated for signature and expiration
        /// </summary>
        /// <param name="dgc">The DGC to be validated</param>
        /// <returns></returns>

        public async Task<ItalianRulesValidationResult> ValidateBusinessRules(EuDGC dgc)
        {
            return (ItalianRulesValidationResult)await GetRulesValidationResult(dgc, DateTimeOffset.Now);
        }


        #endregion

        #region Validation methods

        /// <summary>
        /// Computes the status by checking the vaccinations in the DCC
        /// </summary>
        /// <param name="dgc"></param>
        /// <param name="result">The output result compiled by the function</param>
        /// <param name="rules"></param>
        private void CheckVaccinations(EuDGC dgc, ItalianRulesValidationResult result, IEnumerable<RuleSetting> rules)
        {
            var vaccination = dgc.Vaccinations.Last(r => r.TargetedDiseaseAgent == DiseaseAgents.Covid19);

            int startDay, endDay;
            if (vaccination.DoseNumber > 0 && vaccination.TotalDoseSeries > 0)
            {

                if (vaccination.DoseNumber < vaccination.TotalDoseSeries)
                {
                    // Vaccination is not completed (partial number of doses)
                    startDay = rules.GetRuleInteger(SettingNames.VaccineStartDayNotComplete,
                        vaccination.MedicinalProduct);
                    endDay = rules.GetRuleInteger(SettingNames.VaccineEndDayNotComplete,
                        vaccination.MedicinalProduct);
                }
                else
                {
                    // Vaccination completed (full number of doses)
                    startDay = rules.GetRuleInteger(SettingNames.VaccineStartDayComplete,
                        vaccination.MedicinalProduct);
                    endDay = rules.GetRuleInteger(SettingNames.VaccineEndDayComplete,
                        vaccination.MedicinalProduct);
                }

                if (vaccination.MedicinalProduct == VaccineProducts.JeJVacineCode &&
                    vaccination.DoseNumber > vaccination.TotalDoseSeries)
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
                else if (vaccination.DoseNumber < vaccination.TotalDoseSeries)
                    result.ItalianStatus = DgcItalianResultStatus.PartiallyValid;
                else
                    result.ItalianStatus = DgcItalianResultStatus.Valid;
            }
        }

        /// <summary>
        /// Computes the status by checking the tests in the DCC
        /// </summary>
        /// <param name="dgc"></param>
        /// <param name="result">The output result compiled by the function</param>
        /// <param name="rules"></param>
        private void CheckTests(EuDGC dgc, ItalianRulesValidationResult result, IEnumerable<RuleSetting> rules)
        {
            var test = dgc.Tests.Last(r => r.TargetedDiseaseAgent == DiseaseAgents.Covid19);

            if (test.TestResult == TestResults.NotDetected)
            {
                // Negative test
                int startHours, endHours;

                switch (test.TestType)
                {
                    case TestTypes.Rapid:
                        startHours = rules.GetRuleInteger(SettingNames.RapidTestStartHours);
                        endHours = rules.GetRuleInteger(SettingNames.RapidTestEndHours);
                        break;
                    case TestTypes.Molecular:
                        startHours = rules.GetRuleInteger(SettingNames.MolecularTestStartHours);
                        endHours = rules.GetRuleInteger(SettingNames.MolecularTestEndHours);
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
        private void CheckRecoveryStatements(EuDGC dgc, ItalianRulesValidationResult result, IEnumerable<RuleSetting> rules)
        {
            var recovery = dgc.Recoveries.Last(r => r.TargetedDiseaseAgent == DiseaseAgents.Covid19);

            int startDay, endDay;

            startDay = rules.GetRuleInteger(SettingNames.RecoveryCertStartDay);
            endDay = rules.GetRuleInteger(SettingNames.RecoveryCertEndDay);

            result.ValidFrom = recovery.ValidFrom.Date.AddDays(startDay);
            result.ValidUntil = recovery.ValidUntil.Date;

            if (result.ValidFrom > result.ValidationInstant)
                result.ItalianStatus = DgcItalianResultStatus.NotValidYet;
            else if (result.ValidationInstant > result.ValidFrom.Value.AddDays(endDay))
                result.ItalianStatus = DgcItalianResultStatus.NotValid;
            else if (result.ValidationInstant > result.ValidUntil)
                result.ItalianStatus = DgcItalianResultStatus.PartiallyValid;
            else
                result.ItalianStatus = DgcItalianResultStatus.Valid;
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
                        ItalianStatus = DgcItalianResultStatus.NeedRulesVerification,
                        StatusMessage = message,
                    };
                    throw new DgcRulesValidationException(message, result);
                }
            }

        }



        #endregion
    }
}
