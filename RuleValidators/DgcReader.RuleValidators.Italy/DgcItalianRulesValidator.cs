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
using DgcReader.RuleValidators.Italy.Validation;
using DgcReader.Interfaces.Deserializers;
using DgcReader.Deserializers.Italy;

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
    public class DgcItalianRulesValidator : IRulesValidator, IBlacklistProvider, ICustomDeserializerDependentService
    {
        private readonly ILogger? Logger;
        private readonly DgcItalianRulesValidatorOptions Options;
        private readonly RulesProvider _rulesProvider;

        #region Implementation of IRulesValidator

        /// <inheritdoc/>
        public async Task<IRulesValidationResult> GetRulesValidationResult(EuDGC? dgc,
            string dgcJson,
            DateTimeOffset validationInstant,
            string countryCode = CountryCodes.Italy,
            SignatureValidationResult? signatureValidationResult = null,
            BlacklistValidationResult? blacklistValidationResult = null,
            CancellationToken cancellationToken = default)
        {

            // Validation mode check
            if (Options.ValidationMode == null)
            {
                // Warning if not set excplicitly
                Logger?.LogWarning($"Validation mode not set. The {ValidationMode.Basic3G} validation mode will be used");
            }

            var validationMode = Options.ValidationMode ?? ValidationMode.Basic3G;

            if (!await SupportsCountry(countryCode))
            {
                var result = new ItalianRulesValidationResult
                {
                    ItalianStatus = DgcItalianResultStatus.NeedRulesVerification,
                    StatusMessage = $"Rules validation for country {countryCode} is not supported by this provider",
                    ValidationMode = validationMode,
                };
                return result;
            }

            return await this.GetRulesValidationResult(dgc,
                dgcJson,
                validationInstant,
                validationMode,
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
            return Task.FromResult(new[] { CountryCodes.Italy }.AsEnumerable());
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

        #region Implementation of ICustomDeserializerDependentService
        /// <inheritdoc/>
        public IDgcDeserializer GetCustomDeserializer() => new ItalianDgcDeserializer();
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
        public async Task<IRulesValidationResult> GetRulesValidationResult(EuDGC? dgc,
            string dgcJson,
            DateTimeOffset validationInstant,
            ValidationMode validationMode,
            SignatureValidationResult? signatureValidationResult = null,
            BlacklistValidationResult? blacklistValidationResult = null,
            CancellationToken cancellationToken = default)
        {
            // Check preconditions
            var result = new ItalianRulesValidationResult
            {
                ValidationInstant = validationInstant,
                ValidationMode = validationMode,
                ItalianStatus = DgcItalianResultStatus.NeedRulesVerification,
            };

            if (dgc == null || string.IsNullOrEmpty(dgcJson))
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
                    result.ItalianStatus = DgcItalianResultStatus.NeedRulesVerification;
                    result.StatusMessage = "Unable to get validation rules";
                    return result;
                }

                // Checking min version:
                CheckMinSdkVersion(rules, validationInstant, validationMode);

                // Preparing model for validators
                var certificateModel = new ValidationCertificateModel
                {
                    Dgc = dgc,
                    SignatureData = signatureValidationResult,
                    ValidationInstant = validationInstant,
                };

                var validator = GetValidator(certificateModel);
                if (validator == null)
                {
                    // An EU DCC must have one of the sections above.
                    Logger?.LogWarning($"No vaccinations, tests, recovery or exemptions statements found in the certificate.");
                    result.ItalianStatus = DgcItalianResultStatus.NotEuDCC;
                    return result;
                }

                return validator.CheckCertificate(certificateModel, rules, validationMode);
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

        private ICertificateEntryValidator? GetValidator(ValidationCertificateModel certificateModel)
        {
            if (certificateModel.Dgc.HasVaccinations())
                return new VaccinationValidator(Logger);
            if(certificateModel.Dgc.HasRecoveries())
                return new RecoveryValidator(Logger);
            if (certificateModel.Dgc.HasTests())
                return new TestValidator(Logger);
            if (certificateModel.Dgc.HasExemptions())
                return new ExemptionValidator(Logger);
            return null;
        }

        /// <summary>
        /// Check the minimum version of the SDK implementation required.
        /// If <see cref="DgcItalianRulesValidatorOptions.IgnoreMinimumSdkVersion"/> is false, an exception will be thrown if the implementation is obsolete
        /// </summary>
        /// <param name="rules"></param>
        /// <param name="validationInstant"></param>
        /// <param name="validationMode"></param>
        /// <exception cref="DgcRulesValidationException"></exception>
        private void CheckMinSdkVersion(IEnumerable<RuleSetting> rules, DateTimeOffset validationInstant, ValidationMode validationMode)
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
                        ValidationMode = validationMode,
                        ItalianStatus = DgcItalianResultStatus.NeedRulesVerification,
                        StatusMessage = message,
                    };
                    throw new DgcRulesValidationException(message, result);
                }
            }

        }

        #endregion

        #region Constructor and factory methods

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

        #endregion
    }
}