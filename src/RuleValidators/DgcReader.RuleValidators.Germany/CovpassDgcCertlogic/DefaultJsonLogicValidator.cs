using JsonLogic.Net;
using Newtonsoft.Json.Linq;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Germany.CovpassDgcCertlogic
{
    /// <summary>
    /// JsonLogic validator interface
    /// </summary>
    public interface IJsonLogicValidator
    {
        /// <summary>
        /// Validates data against the specified JsonLogic rule
        /// </summary>
        /// <param name="rule"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        bool IsDataValid(JObject rule, JObject data);
    }

    /// <summary>
    /// Implementation of IJsonLogicValidator
    /// </summary>
    public class DefaultJsonLogicValidator : IJsonLogicValidator
    {
        private readonly JsonLogicEvaluator evaluator;

        /// <summary>
        /// Constructor
        /// </summary>
        public DefaultJsonLogicValidator()
        {
            this.evaluator = new JsonLogicEvaluator(CertLogicOperators.GetOperators());
        }

        /// <inheritdoc/>
        public bool IsDataValid(JObject rule, JObject data)
        {
            var evaluationResult = evaluator.Apply(rule, data);

            return evaluationResult.IsTruthy();
        }
    }
}
