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


        private const string ValidationRulesUrl = "https://get.dgc.gov.it/v1/dgc/settings";
        /// <summary>
        /// The version of the sdk used as reference for implementing the rules.
        /// </summary>
        private const string ReferenceAppMinVersion = "1.1.2"; // NOTE: this is the app version. The SDK version is not available in the settings right now.
        private const string ReferenceSdkVersion = "1.0.2";

        private readonly HttpClient _httpClient;
        private readonly ILogger? _logger;
        private readonly DgcItalianRulesValidatorOptions _options;

        private RulesList? _currentRulesList = null;
        private DateTimeOffset _lastRefreshAttempt;

        private SemaphoreSlim _getRulesSemaphore = new SemaphoreSlim(1, 1);

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
        {
            _httpClient = httpClient;
            _options = options ?? new DgcItalianRulesValidatorOptions();
            _logger = logger;
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
            _httpClient = httpClient;
            _options = options?.Value ?? new DgcItalianRulesValidatorOptions();
            _logger = logger;
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
                options == null ? null : Options.Create(options), 
                logger);
        }
#endif

        #region Implementation of IRulesValidator

        /// <inheritdoc/>
        public async Task<IRuleValidationResult> GetRulesValidationResult(EuDGC dgc, DateTimeOffset validationInstant, CancellationToken cancellationToken = default)
        {
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
                var rules = await GetRules(cancellationToken);
                if (rules == null)
                    throw new Exception("Unable to get validation rules");

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
                    _logger?.LogWarning($"No vaccinations, tests or recovery statements found in the certificate.");
                    result.Status = DgcResultStatus.NotEuDCC;
                }
            }
            catch (Exception e)
            {
                _logger?.LogError(e, $"Validation failed with error {e.Message}");
                result.Status = DgcResultStatus.NotValid;
            }
            return result;
        }

        /// <inheritdoc/>
        public async Task RefreshRules(CancellationToken cancellationToken = default)
        {
            await RefreshRulesList(cancellationToken);
        }
        #endregion

        #region Implementation of IBlackListProvider

        /// <inheritdoc/>
        public async Task<bool> IsBlacklisted(string certificateIdentifier, CancellationToken cancellationToken = default)
        {
            var blacklist = await GetBlacklist(cancellationToken);
            if (blacklist == null)
            {
                _logger?.LogWarning($"Unable to get the blacklist: considering the certificate valid");
                return true;
            }

            return blacklist.Contains(certificateIdentifier);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<string>?> GetBlacklist(CancellationToken cancellationToken = default)
        {
            var rules = await GetRules(cancellationToken);
            return rules?.GetBlackList();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<string>?> RefreshBlacklist(CancellationToken cancellationToken = default)
        {
            var rules = await RefreshRulesList(cancellationToken);
            return rules?.GetBlackList();
        }
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


        /// <summary>
        /// Return the validation rules
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<RuleSetting>?> GetRules(CancellationToken cancellationToken = default)
        {

            await _getRulesSemaphore.WaitAsync(cancellationToken);
            try
            {
                var filePath = GetRulesListFilePath();
                // If not loaded, try to load from file
                if (_currentRulesList == null)
                {
                    try
                    {
                        if (File.Exists(filePath))
                        {
                            var fileContent = File.ReadAllText(filePath);
                            //var fileContent = await File.ReadAllTextAsync(filePath);   Only > .net5.0
                            _currentRulesList = JsonConvert.DeserializeObject<RulesList>(fileContent, JsonSettings);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger?.LogError(e, $"Error reading rules list from file: {e.Message}");
                    }
                }

                if (_currentRulesList == null)
                {
                    // If not present, always try to refresh the rules
                    await RefreshRulesList(cancellationToken);
                }
                else if (_currentRulesList.LastUpdate.Add(_options.MaxFileAge) < DateTime.Now)
                {
                    // File has passed the max age and is deleted

                    _currentRulesList = null;
                    try
                    {
                        if (File.Exists(filePath))
                            File.Delete(filePath);
                    }
                    catch (Exception e)
                    {
                        _logger?.LogError(e, $"Error deleting rules list file: {e.Message}");
                    }
                    await RefreshRulesList(cancellationToken);

                }
                else if (_currentRulesList.LastUpdate.Add(_options.RefreshInterval) < DateTime.Now)
                {
                    // If file is expired and the min refresh interval is over, refresh the list
                    if (_lastRefreshAttempt.Add(_options.MinRefreshInterval) < DateTime.Now)
                        await RefreshRulesList(cancellationToken);
                }

                return _currentRulesList?.Rules;

            }
            catch (Exception e)
            {
                _logger?.LogError(e, $"Error getting rules: {e.Message}");
                throw;
            }
            finally
            {
                _getRulesSemaphore.Release();
            }
        }

        public async Task<IEnumerable<RuleSetting>?> RefreshRulesList(CancellationToken cancellationToken = default)
        {
            try
            {
                _lastRefreshAttempt = DateTime.Now;
                var filePath = GetRulesListFilePath();

                _logger?.LogInformation("Refreshing rules settings from server...");
                var rulesList = new RulesList()
                {
                    LastUpdate = DateTime.Now,
                };
                var rules = await FetchSettings(cancellationToken);


                rulesList.Rules = rules.ToArray();

                // Checking min version:
                var minVersion = rules.GetRule("android", SettingTypes.AppMinVersion);
                if (minVersion != null)
                {
                    if (minVersion.Value.CompareTo(ReferenceAppMinVersion) > 0)
                    {
                        var message = $"The minimum version of the SDK implementation is {minVersion.Value}. " +
                            $"Please update the package with the latest implementation in order to get a reliable result";

                        if (_options.IgnoreMinimumSdkVersion)
                        {
                            _logger?.LogWarning(message);
                        }
                        else
                        {
                            throw new DgcRulesValidationException(message);
                        }

                    }
                }

                _currentRulesList = rulesList;

                try
                {
                    var json = JsonConvert.SerializeObject(rulesList, JsonSettings);

                    File.WriteAllText(filePath, json);
                    //await File.WriteAllTextAsync(filePath, json);  Only > .net5.0
                }
                catch (Exception e)
                {
                    _logger?.LogError(e, $"Error saving rules list to file: {e.Message}");
                }

                return rulesList.Rules;
            }
            catch (Exception e)
            {
                _logger?.LogError(e, $"Error refreshing rules list from server: {e.Message}");
                return null;
            }
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
                        _logger?.LogWarning($"Test type {test.TestType} not supported by current rules");
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
                    _logger?.LogWarning($"Found test with unkwnown TestResult {test.TestResult}. The certificate is considered invalid");

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


        #endregion


        #region Private
        private async Task<RuleSetting[]> FetchSettings(CancellationToken cancellationToken = default)
        {
            try
            {
                var start = DateTime.Now;
                _logger?.LogDebug("Fetching rules settings...");
                var response = await _httpClient.GetAsync(ValidationRulesUrl, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();

                    var results = JsonConvert.DeserializeObject<RuleSetting[]>(content);

                    if (results == null)
                        throw new Exception("Error wile deserializing rules from server");

                    _logger?.LogDebug($"{results.Length} rules read in {DateTime.Now - start}");
                    return results;
                }

                throw new Exception($"The remote server responded with code {response.StatusCode}: {response.ReasonPhrase}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error while getting status list from server: {ex.Message}");
                throw;
            }
        }

        private string GetRulesListFilePath()
        {
            return Path.Combine(_options.BasePath, _options.RulesListFileName);
        }

        #endregion

    }
}
