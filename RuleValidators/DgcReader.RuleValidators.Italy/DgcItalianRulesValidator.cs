using GreenpassReader.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DgcReader.RuleValidators.Italy.Const;
using DgcReader.RuleValidators.Italy.Models;
using DgcReader.Models;
using System.Threading;
using DgcReader.RuleValidators.Italy.Exceptions;
using Microsoft.Extensions.Logging;
using DgcReader.Interfaces.RulesValidators;
using DgcReader.Interfaces.BlacklistProviders;
using DgcReader.RuleValidators.Abstractions;

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
    public class DgcItalianRulesValidator : ThreadsafeRulesValidatorProvider<RulesList, DgcItalianRulesValidatorOptions>, IRulesValidator, IBlacklistProvider
    {
        // File containing the business logic on the offical SDK repo:
        // https://github.com/ministero-salute/it-dgc-verificac19-sdk-android/blob/develop/sdk/src/main/java/it/ministerodellasalute/verificaC19sdk/model/VerificationViewModel.kt

        private const string ValidationRulesUrl = "https://get.dgc.gov.it/v1/dgc/settings";

        /// <summary>
        /// The version of the sdk used as reference for implementing the rules.
        /// </summary>
        private const string ReferenceSdkMinVersion = "1.0.2";

        /// <summary>
        /// The version of the app used as reference for implementing the rules.
        /// NOTE: this is the version of the android app using the <see cref="ReferenceSdkMinVersion"/> of the SDK. The SDK version is not available in the settings right now.
        /// </summary>
        private const string ReferenceAppMinVersion = "1.1.6";

        private readonly HttpClient _httpClient;

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters = {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.None }
            },
        };


#if NET452
        /// <summary>
        /// Constructor
        /// </summary>
        public DgcItalianRulesValidator(HttpClient httpClient,
            DgcItalianRulesValidatorOptions? options = null,
            ILogger<DgcItalianRulesValidator>? logger = null)
            : base(options, logger)
        {
            _httpClient = httpClient;
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
            : base(options?.Value, logger)
        {
            _httpClient = httpClient;
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
        public override async Task<IRuleValidationResult> GetRulesValidationResult(EuDGC dgc, DateTimeOffset validationInstant, string countryCode = "IT", CancellationToken cancellationToken = default)
        {
            if (!await SupportsCountry(countryCode))
                throw new DgcRulesValidationException($"Rules validation for country {countryCode} is not supported by this provider");

            var result = new DgcRulesValidationResult
            {
                Dgc = dgc,
                ValidationInstant = validationInstant,
            };

            if (result.Dgc == null)
            {
                result.Status = DgcResultStatus.NotEuDCC;
                return result;
            }

            try
            {
                var rulesContainer = await GetRules(countryCode, cancellationToken);
                if (rulesContainer == null)
                    throw new Exception("Unable to get validation rules");

                var rules = rulesContainer.Rules;

                // Checking min version:
                CheckMinSdkVersion(rules);

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
                    result.Status = DgcResultStatus.NotEuDCC;
                }
            }
            catch (Exception e)
            {
                Logger?.LogError(e, $"Validation failed with error {e.Message}");
                result.Status = DgcResultStatus.NotValid;
            }
            return result;
        }

        /// <inheritdoc/>
        public override Task<IEnumerable<string>> GetSupportedCountries(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new[] { "IT" }.AsEnumerable());
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
            var rulesContainer = await GetRules("IT", cancellationToken);
            return rulesContainer?.Rules?.GetBlackList();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<string>?> RefreshBlacklist(CancellationToken cancellationToken = default)
        {
            await RefreshRules(null, cancellationToken);
            var rulesContainer = await GetRules(null, cancellationToken);
            return rulesContainer?.Rules?.GetBlackList();
        }
        #endregion

        #region Implementation of ThreadsafeRulesValidatorProvider

        /// <inheritdoc/>
        protected override async Task<RulesList> GetRulesFromServer(string countryCode, CancellationToken cancellationToken = default)
        {
            Logger?.LogInformation("Refreshing rules from server...");
            var rulesList = new RulesList()
            {
                LastUpdate = DateTime.Now,
            };
            var rules = await FetchSettings(cancellationToken);


            rulesList.Rules = rules.ToArray();

            // Checking min version:
            CheckMinSdkVersion(rules);

            return rulesList;
        }

        /// <summary>
        /// Load the rules list stored in file
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override Task<RulesList?> LoadCache(string countryCode, CancellationToken cancellationToken = default)
        {
            var filePath = GetRulesListFilePath();
            RulesList rulesList = null;
            try
            {
                if (File.Exists(filePath))
                {
                    Logger?.LogInformation($"Loading rules from file");
                    var fileContent = File.ReadAllText(filePath);
                    rulesList = JsonConvert.DeserializeObject<RulesList>(fileContent, JsonSettings);
                }
            }
            catch (Exception e)
            {
                Logger?.LogError(e, $"Error reading trustlist from file: {e.Message}");
            }

            // Check max age and delete file
            if (rulesList != null &&
                rulesList.LastUpdate.Add(Options.MaxFileAge) < DateTime.Now)
            {
                Logger?.LogInformation($"Rules list expired for MaxFileAge, deleting list and file");
                // File has passed the max age, removing file
                try
                {
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                }
                catch (Exception e)
                {
                    Logger?.LogError(e, $"Error deleting rules list file: {e.Message}");
                }
                return Task.FromResult<RulesList?>(null);
            }

            return Task.FromResult<RulesList?>(rulesList);
        }

        /// <inheritdoc/>
        protected override Task UpdateCache(RulesList rules, string countryCode, CancellationToken cancellationToken = default)
        {
            var filePath = GetRulesListFilePath();
            var json = JsonConvert.SerializeObject(rules, JsonSettings);

            File.WriteAllText(filePath, json);
            return Task.FromResult(0);
        }

        /// <inheritdoc/>
        protected override DateTimeOffset GetRulesLastUpdate(RulesList rules) => rules.LastUpdate;

        #endregion

        #region Public methods

        /// <summary>
        /// Validates the specified certificate against the Italian business rules.
        /// Is assumed that the Signed DGC signature was already validated for signature and expiration
        /// </summary>
        /// <param name="signedDgc">Info of the signed DGC</param>
        /// <returns></returns>

        public async Task<DgcRulesValidationResult> ValidateBusinessRules(SignedDgc signedDgc)
        {
            return (DgcRulesValidationResult)await GetRulesValidationResult(signedDgc.Dgc, DateTimeOffset.Now);
        }


        #endregion

        #region Validation methods

        /// <summary>
        /// Computes the status by checking the vaccinations in the DCC
        /// </summary>
        /// <param name="dgc"></param>
        /// <param name="result">The output result compiled by the function</param>
        /// <param name="rules"></param>
        private void CheckVaccinations(EuDGC dgc, DgcRulesValidationResult result, IEnumerable<RuleSetting> rules)
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
                    result.Status = DgcResultStatus.NotValid;
                    return;
                }

                if (result.ValidFrom > result.ValidationInstant)
                    result.Status = DgcResultStatus.NotValidYet;
                else if (result.ValidUntil < result.ValidationInstant)
                    result.Status = DgcResultStatus.NotValid;
                else if (vaccination.DoseNumber < vaccination.TotalDoseSeries)
                    result.Status = DgcResultStatus.PartiallyValid;
                else
                    result.Status = DgcResultStatus.Valid;
            }
        }

        /// <summary>
        /// Computes the status by checking the tests in the DCC
        /// </summary>
        /// <param name="dgc"></param>
        /// <param name="rules"></param>
        private void CheckTests(EuDGC dgc, DgcRulesValidationResult result, IEnumerable<RuleSetting> rules)
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
                        result.Status = DgcResultStatus.NotValid;
                        return;
                }

                result.ValidFrom = test.SampleCollectionDate.AddHours(startHours);
                result.ValidUntil = test.SampleCollectionDate.AddHours(endHours);

                // Calculate the status
                if (result.ValidFrom > result.ValidationInstant)
                    result.Status = DgcResultStatus.NotValidYet;
                else if (result.ValidUntil < result.ValidationInstant)
                    result.Status = DgcResultStatus.NotValid;
                else
                    result.Status = DgcResultStatus.Valid;
            }
            else
            {
                // Positive test or unknown result
                if (test.TestResult != TestResults.Detected)
                    Logger?.LogWarning($"Found test with unkwnown TestResult {test.TestResult}. The certificate is considered invalid");

                result.Status = DgcResultStatus.NotValid;
            }
        }

        /// <summary>
        /// Computes the status by checking the recovery statements in the DCC
        /// </summary>
        /// <param name="dgc"></param>
        /// <param name="rules"></param>
        private void CheckRecoveryStatements(EuDGC dgc, DgcRulesValidationResult result, IEnumerable<RuleSetting> rules)
        {
            var recovery = dgc.Recoveries.Last(r => r.TargetedDiseaseAgent == DiseaseAgents.Covid19);

            int startDay, endDay;

            startDay = rules.GetRuleInteger(SettingNames.RecoveryCertStartDay);
            endDay = rules.GetRuleInteger(SettingNames.RecoveryCertEndDay);

            result.ValidFrom = recovery.ValidFrom.Date.AddDays(startDay);
            result.ValidUntil = recovery.ValidUntil.Date;

            if (result.ValidFrom > result.ValidationInstant)
                result.Status = DgcResultStatus.NotValidYet;
            else if (result.ValidationInstant > result.ValidFrom.Value.AddDays(endDay))
                result.Status = DgcResultStatus.NotValid;
            else if (result.ValidationInstant > result.ValidUntil)
                result.Status = DgcResultStatus.PartiallyValid;
            else
                result.Status = DgcResultStatus.Valid;
        }


        /// <summary>
        /// Check the minimum version of the SDK implementation required.
        /// If <see cref="DgcItalianRulesValidatorOptions.IgnoreMinimumSdkVersion"/> is false, an exception will be thrown if the implementation is obsolete
        /// </summary>
        /// <param name="rules"></param>
        /// <exception cref="DgcRulesValidationException"></exception>
        private void CheckMinSdkVersion(IEnumerable<RuleSetting> rules)
        {
            var obsolete = false;
            string message = string.Empty;


            var sdkMinVersion = rules.GetRule(SettingNames.SdkMinVersion, SettingTypes.AppMinVersion);
            if (sdkMinVersion != null)
            {
                if (sdkMinVersion.Value.CompareTo(ReferenceSdkMinVersion) > 0)
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
                    if (appMinVersion.Value.CompareTo(ReferenceAppMinVersion) > 0)
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
                    throw new DgcRulesValidationException(message);
                }
            }

        }

        #endregion


        #region Private
        private async Task<RuleSetting[]> FetchSettings(CancellationToken cancellationToken = default)
        {
            try
            {
                var start = DateTime.Now;
                Logger?.LogDebug("Fetching rules...");
                var response = await _httpClient.GetAsync(ValidationRulesUrl, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();

                    var results = JsonConvert.DeserializeObject<RuleSetting[]>(content);

                    if (results == null)
                        throw new Exception("Error wile deserializing rules from server");

                    Logger?.LogInformation($"{results.Length} rules read in {DateTime.Now - start}");
                    return results;
                }

                throw new Exception($"The remote server responded with code {response.StatusCode}: {response.ReasonPhrase}");
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, $"Error while getting rules from server: {ex.Message}");
                throw;
            }
        }

        private string GetRulesListFilePath()
        {
            return Path.Combine(Options.BasePath, Options.RulesListFileName);
        }

        #endregion
    }
}
