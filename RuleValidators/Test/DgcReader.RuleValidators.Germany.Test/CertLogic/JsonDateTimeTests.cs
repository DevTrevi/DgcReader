using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Germany.Test
{
    /// <summary>
    /// Porting of JsonDateTimeTests.kt
    /// <see href="https://github.com/ehn-dcc-development/dgc-business-rules/blob/main/certlogic/certlogic-kotlin/src/test/kotlin/eu/ehn/dcc/certlogic/JsonDateTimeTests.kt"/>
    /// </summary>
    [TestClass]
    public class JsonDateTimeTests
    {
        [TestMethod]
        public void TestDateWithoutTimeInfo()
        {
            Check("2021-05-04", "2021-05-04T00:00:00.000Z");
        }

        [TestMethod]
        public void TestDateTimesFromStringsRFC3339()
        {
            Check("2021-05-04T13:37:42Z", "2021-05-04T13:37:42.000Z");
            Check("2021-05-04T13:37:42+00:00", "2021-05-04T13:37:42.000Z");
            Check("2021-05-04T13:37:42-00:00", "2021-05-04T13:37:42.000Z");
            Check("2021-08-20T12:03:12+02:00", "2021-08-20T12:03:12.000+02:00");     // (keeps timezone offset)
        }

        [TestMethod]
        public void TestDateTimesFromStringsIso8601()
        {
            Check("2021-08-20T12:03:12+02", "2021-08-20T12:03:12.000+02:00");
            Check("2021-05-04T13:37:42+0000", "2021-05-04T13:37:42.000Z");
            Check("2021-05-04T13:37:42-0000", "2021-05-04T13:37:42.000Z");
            Check("2021-08-20T12:03:12+0200", "2021-08-20T12:03:12.000+02:00");
        }

        [TestMethod]
        public void TestDateTimesWithMilliseconds()
        {
            Check("2021-08-01T00:00:00.1Z", "2021-08-01T00:00:00.100Z");       // 100 ms
            Check("2021-08-01T00:00:00.01Z", "2021-08-01T00:00:00.010Z");      //  10 ms
            Check("2021-08-01T00:00:00.001Z", "2021-08-01T00:00:00.001Z");     //   1 ms

            //Check("2021-08-01T00:00:00.0001Z", "2021-08-01T00:00:00.000Z");    // 100 µs
            //Check("2021-08-01T00:00:00.00001Z", "2021-08-01T00:00:00.000Z");   //  10 µs
            //Check("2021-08-01T00:00:00.000001Z", "2021-08-01T00:00:00.000Z");  //   1 µs
        }

        [TestMethod]
        public void TestDateTimesWithoutTimezoneOffset()
        {
            Check("2021-08-01", "2021-08-01T00:00:00.000Z");
            Check("2021-08-01T00:00:00", "2021-08-01T00:00:00.000Z");
        }

        [TestMethod]
        public void TestDateTimesWithShortTimezoneOffset()
        {
            Check("2021-08-01T00:00:00+1:00", "2021-08-01T00:00:00.000+01:00");
        }

        [TestMethod]
        public void TestShouldWorkForSomeSampleFromQA()
        {
            Check("2021-05-20T12:34:56+00:00", "2021-05-20T12:34:56.000Z", "SI");
            Check("2021-06-29T14:02:07Z", "2021-06-29T14:02:07.000Z", "BE");
        }


        private void Check(string dateTimeLike, string expected, string? message = null)
        {
            var exp = DateTimeOffset.Parse(expected);
            var actual = JsonConvert.DeserializeObject<DateTimeOffset>($"\"{dateTimeLike}\"", DgcGermanRulesValidator.JsonSerializerSettings);
            Assert.AreEqual(exp, actual, message);
        }
    }
}
