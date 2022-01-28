using GreenpassReader.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DgcReader.Models;
using System.Threading;
using Microsoft.Extensions.Logging;
using DgcReader.Interfaces.RulesValidators;
using DgcReader.Exceptions;
using DgcReader.RuleValidators.Germany.Models;
using Newtonsoft.Json.Linq;
using DgcReader.RuleValidators.Germany.CovpassDgcCertlogic.Data;
using DgcReader.RuleValidators.Germany.CovpassDgcCertlogic;
using DgcReader.RuleValidators.Germany.CovpassDgcCertlogic.Domain.Rules;
using DgcReader.RuleValidators.Germany.Providers;
using Newtonsoft.Json;
using System.Globalization;
using Newtonsoft.Json.Converters;

#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER || NET47_OR_GREATER
using Microsoft.Extensions.Options;
#endif

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Germany
{
    /// <summary>
    /// Unofficial porting of the German rules from https://github.com/Digitaler-Impfnachweis/covpass-android.
    /// </summary>
    public class DgcGermanRulesValidator : IRulesValidator
    {

        private readonly ILogger? Logger;
        private readonly DgcGermanRulesValidatorOptions Options;

        private readonly RuleIdentifiersProvider _ruleIdentifiersProvider;
        private readonly RulesProvider _rulesProvider;
        private readonly ValueSetIdentifiersProvider _valueSetIdentifiersProvider;
        private readonly ValueSetsProvider _valueSetsProvider;

        private readonly DefaultCertLogicEngine _certLogicEngine;
        private readonly CovPassGetRulesUseCase _rulesUseCase;

        /// <summary>
        /// Json serializer settings for the validator
        /// </summary>
        public static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            Converters = {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal, Culture = CultureInfo.InvariantCulture },
            },
        };

#if NET452
        /// <summary>
        /// Constructor
        /// </summary>
        public DgcGermanRulesValidator(HttpClient httpClient,
            DgcGermanRulesValidatorOptions? options = null,
            ILogger<DgcGermanRulesValidator>? logger = null)
        {
            Options = options ?? new DgcGermanRulesValidatorOptions();
            Logger = logger;

            // Valueset providers
            _ruleIdentifiersProvider = new RuleIdentifiersProvider(httpClient, Options, logger);
            _rulesProvider = new RulesProvider(httpClient, Options, _ruleIdentifiersProvider, logger);
            _valueSetIdentifiersProvider = new ValueSetIdentifiersProvider(httpClient, Options, logger);
            _valueSetsProvider = new ValueSetsProvider(httpClient, Options, _valueSetIdentifiersProvider, logger);

            // Validator implemnentations
            var affectedFieldsDataRetriever = new DefaultAffectedFieldsDataRetriever(Logger);
            var jsonLogicValidator = new DefaultJsonLogicValidator();

            _certLogicEngine = new DefaultCertLogicEngine(affectedFieldsDataRetriever, jsonLogicValidator);
            _rulesUseCase = new CovPassGetRulesUseCase();
        }

        /// <summary>
        /// Factory method for creating an instance of <see cref="DgcGermanRulesValidator"/>
        /// whithout using the DI mechanism. Useful for legacy applications
        /// </summary>
        /// <param name="httpClient">The http client instance that will be used for requests to the server</param>
        /// <param name="options">The options for the provider</param>
        /// <param name="logger">Instance of <see cref="ILogger"/> used by the provider (optional).</param>
        /// <returns></returns>
        public static DgcGermanRulesValidator Create(HttpClient httpClient,
            DgcGermanRulesValidatorOptions? options = null,
            ILogger<DgcGermanRulesValidator>? logger = null)
        {
            return new DgcGermanRulesValidator(httpClient, options, logger);
        }

#else
        /// <summary>
        /// Constructor
        /// </summary>
        public DgcGermanRulesValidator(HttpClient httpClient,
            IOptions<DgcGermanRulesValidatorOptions>? options = null,
            ILogger<DgcGermanRulesValidator>? logger = null)
        {
            Options = options?.Value ?? new DgcGermanRulesValidatorOptions();
            Logger = logger;

            // Valueset providers
            _ruleIdentifiersProvider = new RuleIdentifiersProvider(httpClient, Options, logger);
            _rulesProvider = new RulesProvider(httpClient, Options, _ruleIdentifiersProvider, logger);
            _valueSetIdentifiersProvider = new ValueSetIdentifiersProvider(httpClient, Options, logger);
            _valueSetsProvider = new ValueSetsProvider(httpClient, Options, _valueSetIdentifiersProvider, logger);


            // Validator implemnentations
            var affectedFieldsDataRetriever = new DefaultAffectedFieldsDataRetriever(Logger);
            var jsonLogicValidator = new DefaultJsonLogicValidator();

            _certLogicEngine = new DefaultCertLogicEngine(affectedFieldsDataRetriever, jsonLogicValidator);
            _rulesUseCase = new CovPassGetRulesUseCase();
        }

        /// <summary>
        /// Factory method for creating an instance of <see cref="DgcGermanRulesValidator"/>
        /// whithout using the DI mechanism. Useful for legacy applications
        /// </summary>
        /// <param name="httpClient">The http client instance that will be used for requests to the server</param>
        /// <param name="options">The options for the provider</param>
        /// <param name="logger">Instance of <see cref="ILogger"/> used by the provider (optional).</param>
        /// <returns></returns>
        public static DgcGermanRulesValidator Create(HttpClient httpClient,
            DgcGermanRulesValidatorOptions? options = null,
            ILogger<DgcGermanRulesValidator>? logger = null)
        {
            return new DgcGermanRulesValidator(httpClient,
                options == null ? null : Microsoft.Extensions.Options.Options.Create(options),
                logger);
        }
#endif

        #region Implementation of IRulesValidator

        /// <inheritdoc/>
        public async Task<IRulesValidationResult> GetRulesValidationResult(EuDGC dgc,
            string dgcJson,
            DateTimeOffset validationInstant,
            string countryCode = "DE",
            SignatureValidationResult? validationResult = null,
            BlacklistValidationResult? blacklistValidationResult = null,
            CancellationToken cancellationToken = default)
        {
            if (!await SupportsCountry(countryCode))
                throw new DgcException($"Rules validation for country {countryCode} is not supported by this provider");

            var result = new GermanRulesValidationResult()
            {
                RulesVerificationCountry = countryCode,
                Status = DgcResultStatus.NeedRulesVerification,
            };

            if (dgc == null)
            {
                result.Status = DgcResultStatus.NotEuDCC;
                return result;
            }

            try
            {
                var rulesSet = await _rulesProvider.GetValueSet(countryCode, cancellationToken);
                if (rulesSet == null)
                    throw new Exception("Unable to get validation rules");


                var certEntry = dgc.GetCertificateEntry();
                var issuerCountryCode = certEntry.Country;
                var certificateType = dgc.GetCertificateType();

                var rules = _rulesUseCase.Invoke(rulesSet.Rules,
                    validationInstant,
                    countryCode,
                    issuerCountryCode,
                    certificateType);

                var valueSets = await GetValueSetsJson();

                var externalParameters = new ExternalParameter
                {
                    ValidationClock = validationInstant,
                    ValueSets = valueSets,
                    CountryCode = countryCode,
                    Expiration = DateTimeOffset.MaxValue,   // Signature validation is done by another module
                    ValidFrom = DateTimeOffset.MinValue,    // Signature validation is done by another module
                    IssuerCountryCode = dgc.GetCertificateEntry()?.Country ?? string.Empty,
                    Kid = "",                               // Signature validation is done by another module
                    Region = "",
                };


                var testResults = _certLogicEngine.Validate(certificateType,
                    dgc.SchemaVersion,
                    rules,
                    externalParameters,
                    dgcJson).ToArray();

                // Se the validation results to the final result
                result.ValidationResults = testResults;

                var englishCulture = CultureInfo.GetCultureInfo("en");
                if (!testResults.Any())
                {
                    // No rules found for the target acceptance country
                    result.Status = DgcResultStatus.NeedRulesVerification;
                }
                else if (testResults.Any(r=>r.Result == Result.FAIL))
                {
                    result.Status = DgcResultStatus.NotValid;
                    result.StatusMessage = "Rules validation failed: ";
                    result.StatusMessage += string.Join(", ",
                        testResults.Where(r => r.Result == Result.FAIL)
                        .Select(r => r.Rule.Descriptions.GetDescription(englishCulture)));
                }
                else if (testResults.Any(r => r.Result == Result.OPEN))
                {
                    result.Status = DgcResultStatus.OpenResult;
                    result.StatusMessage = "These rules can not be validated: ";
                    result.StatusMessage += string.Join(", ",
                        testResults.Where(r => r.Result == Result.OPEN)
                        .Select(r => r.Rule.Descriptions.GetDescription(englishCulture)));
                }
                else if (testResults.All(r=>r.Result == Result.PASSED))
                {
                    result.Status = DgcResultStatus.Valid;
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
        public async Task<IEnumerable<string>> GetSupportedCountries(CancellationToken cancellationToken = default)
        {
            var rulesIdentifiersValueSet = await _ruleIdentifiersProvider.GetValueSet(cancellationToken);
            if (rulesIdentifiersValueSet == null)
            {
                Logger?.LogWarning("Unable to get the list of supported countries");
                return Enumerable.Empty<string>();
            }
            return rulesIdentifiersValueSet.Identifiers.Select(r => r.Country).Distinct().OrderBy(r => r).ToArray();
        }

        /// <inheritdoc/>
        public async Task RefreshRules(string? countryCode = null, CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrEmpty(countryCode))
            {
                await _rulesProvider.RefreshValueSet(countryCode, cancellationToken);
            }
            else
            {
                // Full refresh: updates all the rules and valuesets from server
                await RefreshAllRules();
                await RefreshValuesets();
            }
        }

        /// <inheritdoc/>
        public async Task<bool> SupportsCountry(string countryCode, CancellationToken cancellationToken = default)
        {
            var supportedCountries = await GetSupportedCountries();
            return supportedCountries.Any(r=>r.Equals(countryCode, StringComparison.InvariantCultureIgnoreCase));
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Refresh all the valuesets (and their identifiers)
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task RefreshValuesets(CancellationToken cancellationToken = default)
        {
            var identifiers = await _valueSetIdentifiersProvider.RefreshValueSet(cancellationToken);
            if (identifiers == null)
                return;

            foreach(var identifier in identifiers.Identifiers)
            {
                await _valueSetsProvider.RefreshValueSet(identifier.Id, cancellationToken);
            }
        }

        /// <summary>
        /// Refresh all the rules (and their identifiers)
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task RefreshAllRules(CancellationToken cancellationToken = default)
        {
            var identifiers = await _ruleIdentifiersProvider.RefreshValueSet(cancellationToken);
            if (identifiers == null)
                return;

            var countries = await GetSupportedCountries();
            foreach (var country in countries)
            {
                await _rulesProvider.RefreshValueSet(country, cancellationToken);
            }
        }
        #endregion

        #region Private

        private async Task<Dictionary<string, JObject>> GetValueSetsJson(CancellationToken cancellationToken = default)
        {
            var valueSetsIdentifiers = await _valueSetIdentifiersProvider.GetValueSet(cancellationToken);

            var temp = new Dictionary<string, JObject>();

            if (valueSetsIdentifiers == null)
                return temp;

            foreach(var identifier in valueSetsIdentifiers.Identifiers)
            {
                var values = await _valueSetsProvider.GetValueSet(identifier.Id, cancellationToken);
                if (values != null)
                    temp.Add(values.Id, JObject.FromObject(values.Values));
            }
            return temp;
        }
        #endregion
    }
}
