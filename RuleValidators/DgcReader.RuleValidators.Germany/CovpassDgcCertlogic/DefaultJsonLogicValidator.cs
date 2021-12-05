using JsonLogic.Net;
using Newtonsoft.Json.Linq;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Germany.CovpassDgcCertlogic
{
    public interface IJsonLogicValidator
    {
        bool IsDataValid(JObject rule, JObject data);
    }


    public class DefaultJsonLogicValidator : IJsonLogicValidator
    {
        private readonly JsonLogicEvaluator evaluator;

        public DefaultJsonLogicValidator()
        {
            this.evaluator = new JsonLogicEvaluator(EvaluateOperators.Default);
        }

        public bool IsDataValid(JObject rule, JObject data)
        {
            var evaluationResult = evaluator.Apply(rule, data);

            return evaluationResult.IsTruthy();
        }
    }
}
