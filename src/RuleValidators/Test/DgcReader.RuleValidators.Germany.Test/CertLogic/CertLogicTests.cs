using DgcReader.RuleValidators.Germany.CovpassDgcCertlogic;
using JsonLogic.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace DgcReader.RuleValidators.Germany.Test
{
    /// <summary>
    /// Porting of certlogicTest.kt
    /// <see href="https://github.com/ehn-dcc-development/dgc-business-rules/blob/main/certlogic/certlogic-kotlin/src/test/kotlin/eu/ehn/dcc/certlogic/certlogicTests.kt"/>
    /// </summary>
    [TestClass]
    public class CertLogicTests
    {
        private IProcessJsonLogic Processor;
        private IEnumerable<TestSuite> TestSuites;

        public CertLogicTests()
        {
            Processor = new JsonLogicEvaluator(CertLogicOperators.GetOperators());
            TestSuites = GetAllTestSuites();
        }

        [TestMethod]
        public void RunTestSuites()
        {
            foreach(var testSuite in TestSuites)
            {
                RunTestSuite(testSuite);
            }
        }


        private void RunTestSuite(TestSuite testSuite)
        {
            if (testSuite.Directive == "skip")
            {
                Debug.WriteLine($"Skipping test suite {testSuite.Name}.");
                return;
            }

            Debug.WriteLine($"Running test suite {testSuite.Name}:");
            foreach (var testCase in testSuite.Cases)
            {
                if (testCase.Directive == "skip")
                {
                    Debug.WriteLine($"\t(Skipping test case {testCase.Name}.)");
                    continue;
                }

                Debug.WriteLine($"\tRunning test case {testCase.Name}:");
                for (int i = 0; i < testCase.Assertions.Count(); i++)
                {
                    var assertion = testCase.Assertions.ElementAt(i);
                    var assertionText = assertion.Message ?? $"#{i + 1}";
                    if (assertion.CertLogicExpression == null && testCase.CertLogicExpression == null)
                    {
                        Debug.WriteLine($"\t\t!! no CertLogic expression defined on assertion {assertionText}, and neither on encompassing test case {testCase.Name}");
                    }

                    switch (assertion.Directive)
                    {
                        case "skip":
                            Debug.WriteLine($"\t\t! skipped assertion {assertionText}");
                            break;
                        case "only":
                            Debug.WriteLine($"(test directive 'only' not supported on assertions - ignoring)");
                            break;
                        default:
                            Debug.WriteLine($"\t\tRunning assertion {i + 1}...");

                            var expr = assertion.CertLogicExpression ?? testCase.CertLogicExpression;
                            var message = assertion.Message ?? assertion.Data.ToString();

                            try
                            {
                                var result = Processor.Apply(expr, assertion.Data);


                                var equals = AreSameValue(assertion.Expected, result);

                                Assert.IsTrue(equals, message);
                            }
                            catch (System.Exception e)
                            {
                                Debug.WriteLine($"\t\tError: {e.Message}");
                                Assert.Fail(e.Message);
                            }
                            break;
                    }
                }
            }
        }


        private IEnumerable<TestSuite> GetAllTestSuites()
        {


            var path = Path.Combine(Directory.GetCurrentDirectory(), @"CertLogic\TestSuite");
            var temp = new List<TestSuite>();
            foreach (var file in Directory.GetFiles(path, "*.json"))
            {
                var testData = JsonConvert.DeserializeObject<TestSuite>(File.ReadAllText(file), DgcGermanRulesValidator.JsonSerializerSettings);
                if (testData != null)
                    temp.Add(testData);
            }
            return temp;
        }



        private static bool AreSameValue(object expected, object actual)
        {
            if (IsNullOrEmpty(expected) || IsNullOrEmpty(actual))
                return IsNullOrEmpty(expected) && IsNullOrEmpty(actual);

            var jExp = ToJToken(expected);
            var jAct = ToJToken(actual);
            if (JToken.DeepEquals(jExp, jAct))
                return true;

            if (IsNumeric(jExp) && IsNumeric(jAct))
            {
                return (decimal)jExp == (decimal)jAct;
            }
            return false;
        }
        private static bool IsNullOrEmpty(object value)
        {
            if (value == null)
                return true;

            var token = value as JToken;
            if (token == null)
                token = ToJToken(value);

            return (token == null) ||
                    (token.Type == JTokenType.Array && !token.HasValues) ||
                    (token.Type == JTokenType.Object && !token.HasValues) ||
                    (token.Type == JTokenType.String && token.ToString() == string.Empty) ||
                    (token.Type == JTokenType.Null);
        }

        private static JToken ToJToken(object value)
        {
            if (value is JToken token)
                return token;
            return JToken.FromObject(value);
        }

        private static bool IsNumeric(JToken token)
        {
            return new[] { JTokenType.Integer, JTokenType.Float }.Contains(token.Type);
        }

        public class Assertion
        {
            [JsonProperty("directive")]
            public string? Directive { get; set; }

            [JsonProperty("message")]
            public string? Message { get; set; }

            [JsonProperty("certLogicExpression")]
            public JToken CertLogicExpression { get; set; }

            [JsonProperty("data")]
            public JToken Data { get; set; }

            [JsonProperty("expected")]
            public JToken Expected { get; set; }

            public override string ToString()
            {
                var s = $"Expr: {CertLogicExpression} -  Data: {Data} - Expected: {Expected} ";


                if (!string.IsNullOrEmpty(Directive))
                    s += $"Directive {Directive} ";

                if (!string.IsNullOrEmpty(Directive))
                    s += $"Message {Directive} ";

                return s.Trim();
            }
        }

        public class TestCase
        {
            [JsonProperty("name")]
            public string? Name { get; set; }

            [JsonProperty("directive")]
            public string? Directive { get; set; }

            [JsonProperty("certLogicExpression")]
            public JToken CertLogicExpression { get; set; }

            [JsonProperty("assertions")]
            public IEnumerable<Assertion> Assertions { get; set; }

            public override string ToString()
            {
                return $"{Name} - Expr: {CertLogicExpression} - Assertions: {Assertions.Count()} - Directive: {Directive}";
            }
        }

        public class TestSuite
        {
            [JsonProperty("name")]
            public string? Name { get; set; }

            [JsonProperty("directive")]
            public string? Directive { get; set; }

            [JsonProperty("cases")]
            public IEnumerable<TestCase> Cases { get; set; }

            public override string ToString()
            {
                return $"{Name} - Cases: {Cases.Count()} - Directive: {Directive}";
            }
        }
    }
}
