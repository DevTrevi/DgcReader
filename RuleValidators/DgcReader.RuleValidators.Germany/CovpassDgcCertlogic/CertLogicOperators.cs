using JsonLogic.Net;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Germany.CovpassDgcCertlogic
{
    /// <summary>
    /// Provides implementations of CertLogic operators for the JsonLogic library
    /// <see href="https://github.com/ehn-dcc-development/dgc-business-rules/tree/main/certlogic"/>
    /// </summary>
    public static class CertLogicOperators
    {
        /// <summary>
        /// Add the plusTime operator to the collection
        /// <see href="https://github.com/ehn-dcc-development/dgc-business-rules/blob/main/certlogic/specification/README.md#offset-date-time-plustime"/>
        /// </summary>
        /// <param name="manageOperators"></param>
        public static void AddPlusTimeOperator(this IManageOperators manageOperators) => manageOperators.AddOperator("plusTime", EvaluatePlusTime);

        /// <summary>
        /// Add the "after" (equivalent of > ) DateTime operator to the collection
        /// <see href="https://github.com/ehn-dcc-development/dgc-business-rules/blob/main/certlogic/specification/README.md#operations-with-infix-operators"/>
        /// </summary>
        /// <param name="manageOperators"></param>
        public static void AddAfterOperator(this IManageOperators manageOperators) => manageOperators.AddOperator("after", EvaluateAfter);

        /// <summary>
        /// Add the "before" (equivalent of &lt; ) DateTime operator to the collection
        /// <see href="https://github.com/ehn-dcc-development/dgc-business-rules/blob/main/certlogic/specification/README.md#operations-with-infix-operators"/>
        /// </summary>
        /// <param name="manageOperators"></param>
        public static void AddBeforeOperator(this IManageOperators manageOperators) => manageOperators.AddOperator("before", EvaluateBefore);

        /// <summary>
        /// Add the "not-after" (equivalent of &lt;= ) DateTime operator to the collection
        /// <see href="https://github.com/ehn-dcc-development/dgc-business-rules/blob/main/certlogic/specification/README.md#operations-with-infix-operators"/>
        /// </summary>
        /// <param name="manageOperators"></param>
        public static void AddNotAfterOperator(this IManageOperators manageOperators) => manageOperators.AddOperator("not-after", EvaluateNotAfter);

        /// <summary>
        /// Add the "not-before" (equivalent of >= ) DateTime operator to the collection
        /// <see href="https://github.com/ehn-dcc-development/dgc-business-rules/blob/main/certlogic/specification/README.md#operations-with-infix-operators"/>
        /// </summary>
        /// <param name="manageOperators"></param>
        public static void AddNotBeforeOperator(this IManageOperators manageOperators) => manageOperators.AddOperator("not-before", EvaluateNotBefore);


        /// <summary>
        /// Add a modified version of the "in" operator
        /// </summary>
        /// <param name="manageOperators"></param>
        public static void AddInOperator(this IManageOperators manageOperators) => manageOperators.AddOperator("in", EvaluateIn);

        /// <summary>
        /// Add all the CertLogic operators to the collection
        /// </summary>
        /// <param name="manageOperators"></param>
        public static void AddCertLogicOperators(this IManageOperators manageOperators)
        {
            manageOperators.AddPlusTimeOperator();
            manageOperators.AddAfterOperator();
            manageOperators.AddBeforeOperator();
            manageOperators.AddNotAfterOperator();
            manageOperators.AddNotBeforeOperator();
            manageOperators.AddInOperator();
        }

        /// <summary>
        /// Get the basic JsonLogic operators with the added CertLogic operators
        /// </summary>
        /// <returns></returns>
        public static IManageOperators GetOperators()
        {
            var operators = EvaluateOperators.Default;
            operators.AddCertLogicOperators();
            return operators;
        }

        #region Implementations

        /// <summary>
        /// Implementation of the plusTime specification
        /// <see href="https://github.com/ehn-dcc-development/dgc-business-rules/blob/main/certlogic/specification/README.md#offset-date-time-plustime"/>
        /// </summary>
        /// <param name="p"></param>
        /// <param name="args"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static object EvaluatePlusTime(IProcessJsonLogic p, JToken[] args, object data)
        {
            if (args.Length != 3)
                throw new ArgumentException($"Expected 3 arguments for plusTime operator: datetime, integer and time unit");

            var value = p.Apply(args[0], data);
            var offset = Convert.ToInt32(p.Apply(args[1], data));
            var unit = Enum.Parse(typeof(TimeUnit), p.Apply(args[2], data)?.ToString(), ignoreCase: true);

            DateTimeOffset dateTimeOff;
            if (value is DateTimeOffset dto)
            {
                dateTimeOff = dto;
            }
            else if (value is DateTime dt)
            {
                dateTimeOff = new DateTimeOffset(dt);
            }
            else
            {
                if (!DateTimeOffset.TryParse(value.ToString(), out dateTimeOff))
                    throw new ArgumentException($"Value {value} is not a DateTime or DateTimeOffset");
            }

            switch (unit)
            {
                case TimeUnit.Year:
                    return dateTimeOff.AddYears(offset);
                case TimeUnit.Month:
                    return dateTimeOff.AddMonths(offset);
                case TimeUnit.Day:
                    return dateTimeOff.AddDays(offset);
                case TimeUnit.Hour:
                    return dateTimeOff.AddHours(offset);
                default:
                    throw new ArgumentException($"Unsupported time unit {unit}");
            }
        }

        /// <summary>
        /// Evaluates the "after" DateTime operator
        /// </summary>
        /// <param name="p"></param>
        /// <param name="args"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static object EvaluateAfter(IProcessJsonLogic p, JToken[] args, object data)
        {
            if (args.Length < 2 || args.Length > 3)
                throw new ArgumentException($"Expected 2 or 3 arguments, got {args.Length}");

            var ops = args.Select(a => p.Apply(a, data)).Cast<DateTimeOffset>().ToArray();


            for (int i = 0; i < ops.Length -1; i++)
            {
                if (!(ops[i] > ops[i + 1]))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Evaluates the "before" DateTime operator
        /// </summary>
        /// <param name="p"></param>
        /// <param name="args"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static object EvaluateBefore(IProcessJsonLogic p, JToken[] args, object data)
        {
            if (args.Length < 2 || args.Length > 3)
                throw new ArgumentException($"Expected 2 or 3 arguments, got {args.Length}");

            var ops = args.Select(a => p.Apply(a, data)).Cast<DateTimeOffset>().ToArray();


            for (int i = 0; i < ops.Length - 1; i++)
            {
                if (!(ops[i] < ops[i + 1]))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Evaluates the "not-after" DateTime operator
        /// </summary>
        /// <param name="p"></param>
        /// <param name="args"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static object EvaluateNotAfter(IProcessJsonLogic p, JToken[] args, object data)
        {
            if (args.Length < 2 || args.Length > 3)
                throw new ArgumentException($"Expected 2 or 3 arguments, got {args.Length}");

            var ops = args.Select(a => p.Apply(a, data)).Cast<DateTimeOffset>().ToArray();


            for (int i = 0; i < ops.Length - 1; i++)
            {
                if (!(ops[i] <= ops[i + 1]))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Evaluates the "not-before" DateTime operator
        /// </summary>
        /// <param name="p"></param>
        /// <param name="args"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static object EvaluateNotBefore(IProcessJsonLogic p, JToken[] args, object data)
        {
            if (args.Length < 2 || args.Length > 3)
                throw new ArgumentException($"Expected 2 or 3 arguments, got {args.Length}");

            var ops = args.Select(a => p.Apply(a, data)).Cast<DateTimeOffset>().ToArray();


            for (int i = 0; i < ops.Length - 1; i++)
            {
                if (!(ops[i] >= ops[i + 1]))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Evaluates the modified "in" operator
        /// </summary>
        /// <param name="p"></param>
        /// <param name="args"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static object EvaluateIn(IProcessJsonLogic p, JToken[] args, object data)
        {
            object needle = p.Apply(args[0], data);
            object haystack = p.Apply(args[1], data);
            if (haystack is null) return false;
            if (haystack is string s)
            {
                return s.IndexOf(needle.ToString()) >= 0;
            }

            // Edit: Search key in Dictionary
            // This considers a valid match the presence of a property with the specified name
            // regardless of the type
            if (haystack is JObject jobj && !string.IsNullOrEmpty(needle as string))
            {
                if (jobj.Children().OfType<JProperty>().Any(t => t.Name == needle.ToString()))
                    return true;
            }

            return haystack.MakeEnumerable().Any(item => item.EqualTo(needle));
        }

        #endregion

        /// <summary>
        /// Represent a time unit used by the CertLogic rules
        /// </summary>
        public enum TimeUnit
        {
            /// <summary>
            /// Year
            /// </summary>
            Year,

            /// <summary>
            /// Month
            /// </summary>
            Month,

            /// <summary>
            /// Day
            /// </summary>
            Day,

            /// <summary>
            /// Hour
            /// </summary>
            Hour
        }
    }

}
