using JsonLogic.Net;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Germany.CovpassDgcCertlogic
{
    public static class CertLogicOperators
    {
        public static void AddPlusTimeOperator(this IManageOperators manageOperators) => manageOperators.AddOperator("plusTime", EvaluatePlusTime);
        public static void AddAfterOperator(this IManageOperators manageOperators) => manageOperators.AddOperator("after", EvaluateAfter);
        public static void AddBeforeOperator(this IManageOperators manageOperators) => manageOperators.AddOperator("before", EvaluateBefore);
        public static void AddNotAfterOperator(this IManageOperators manageOperators) => manageOperators.AddOperator("not-after", EvaluateNotAfter);
        public static void AddNotBeforeOperator(this IManageOperators manageOperators) => manageOperators.AddOperator("not-before", EvaluateNotBefore);
        public static void AddInOperator(this IManageOperators manageOperators) => manageOperators.AddOperator("in", EvaluateIn);


        public static void AddCertLogicOperators(this IManageOperators manageOperators)
        {
            manageOperators.AddPlusTimeOperator();
            manageOperators.AddAfterOperator();
            manageOperators.AddBeforeOperator();
            manageOperators.AddNotAfterOperator();
            manageOperators.AddNotBeforeOperator();
            manageOperators.AddInOperator();
        }

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
        public static object EvaluateIn(IProcessJsonLogic p, JToken[] args, object data)
        {
            object needle = p.Apply(args[0], data);
            object haystack = p.Apply(args[1], data);
            if (haystack is null) return false;
            if (haystack is String) return (haystack as string).IndexOf(needle.ToString()) >= 0;

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

        public enum TimeUnit
        {
            Year,
            Month,
            Day,
            Hour
        }
    }

}
