using DgcReader.RuleValidators.Germany.CovpassDgcCertlogic.Data;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0


namespace DgcReader.RuleValidators.Germany.CovpassDgcCertlogic
{

    /// <summary>
    /// Porting of DefaultCertLogicEngine.kt
    /// </summary>
    public class DefaultCertLogicEngine : ICertLogicEngine
    {
        private const string EXTERNAL_KEY = "external";
        private const string PAYLOAD_KEY = "payload";
        private const string CERTLOGIC_KEY = "CERTLOGIC";
        private const string CERTLOGIC_VERSION = "1.0.0";

        private readonly IAffectedFieldsDataRetriever affectedFieldsDataRetriever;
        private readonly IJsonLogicValidator jsonLogicValidator;
        private readonly ILogger? logger;

        public DefaultCertLogicEngine(
            IAffectedFieldsDataRetriever affectedFieldsDataRetriever,
            IJsonLogicValidator jsonLogicValidator,
            ILogger? logger)
        {
            this.affectedFieldsDataRetriever = affectedFieldsDataRetriever;
            this.jsonLogicValidator = jsonLogicValidator;
            this.logger = logger;
        }

        private JObject PrepareData(ExternalParameter externalParameter, string payload)
        {
            var d = new ValidatorData
            {
                ExternalParameter = externalParameter,
                Payload = JObject.Parse(payload),
            };
            var o = JObject.FromObject(d);

            return o;
        }


        public IEnumerable<ValidationResult> Validate(
            CertificateType certificateType,
            string hcertVersionString,
            IEnumerable<RuleEntry> rules,
            ExternalParameter externalParameter,
            string payload)
        {
            if (!rules.Any())
                return Enumerable.Empty<ValidationResult>();

            var dataJsonNode = PrepareData(externalParameter, payload);
            var hcertVersion = ToVersion(hcertVersionString);

            var results = rules.Select(r => CheckRule(r, dataJsonNode, hcertVersion, certificateType)).ToArray();
            return results;
        }


        private ValidationResult CheckRule(RuleEntry rule, JObject dataJsonNode, Version? hcertVersion, CertificateType certificateType)
        {
            var ruleEngineVersion = ToVersion(rule.EngineVersion);
            var schemaVersion = ToVersion(rule.SchemaVersion);

            var validationErrors = new List<Exception>();

            var isCompatibleVersion = rule.Engine == CERTLOGIC_KEY &&
                ruleEngineVersion != null &&
                ToVersion(CERTLOGIC_VERSION) >= ruleEngineVersion &&
                hcertVersion != null &&
                schemaVersion != null &&
                hcertVersion.Major == schemaVersion.Major &&
                hcertVersion >= schemaVersion;

            Result res;
            if (isCompatibleVersion)
            {

                try
                {
                    res = jsonLogicValidator.IsDataValid(rule.Logic, dataJsonNode) ?
                        Result.PASSED : Result.FAIL;
                }
                catch (Exception e)
                {
                    validationErrors.Add(e);
                    res = Result.OPEN;
                }
            }
            else
            {
                res = Result.OPEN;
            }
            var cur = affectedFieldsDataRetriever.GetAffectedFieldsData(rule, dataJsonNode, certificateType);

            return new ValidationResult
            {
                Rule = rule,
                Result = res,
                Current = cur,
                ValidationErrors = validationErrors.Any() ? validationErrors : null,
            };
        }


        private Version? ToVersion(string version)
        {
            if (Version.TryParse(version, out var result))
                return result;
            return null;
        }

        class ValidatorData
        {
            [JsonProperty(EXTERNAL_KEY)]
            public ExternalParameter ExternalParameter { get; set; }

            [JsonProperty(PAYLOAD_KEY)]
            public JObject Payload { get; set; }

        }
    }

    public interface ICertLogicEngine
    {
        IEnumerable<ValidationResult> Validate(
            CertificateType certificateType,
            string hcertVersionString,
            IEnumerable<RuleEntry> rules,
            ExternalParameter externalParameter,
            string payload);
    }

}
