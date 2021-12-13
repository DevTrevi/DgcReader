using DgcReader.RuleValidators.Italy.Const;
using DgcReader.RuleValidators.Italy.Models;
using System;
using System.Collections.Generic;
using System.Linq;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Italy
{
    internal static class RulesExtensionMethods
    {
        /// <summary>
        /// Search the rule with the specified type.
        /// If no match exists for the specified type, returns null.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="name">The setting name to search</param>
        /// <param name="type">The setting type to search</param>
        /// <returns></returns>
        public static RuleSetting? GetRule(this IEnumerable<RuleSetting> settings, string name, string type)
        {
            return settings.SingleOrDefault(r => r.Name == name && r.Type == type);
        }

        /// <summary>
        /// Convert the value of te specified rule to an Int32 value
        /// </summary>
        /// <param name="rule"></param>
        /// <returns></returns>
        public static int? ToInteger(this RuleSetting rule)
        {
            if (int.TryParse(rule.Value, out int result))
                return result;
            return null;
        }

        public static int GetRuleInteger(this IEnumerable<RuleSetting> settings, string name, string type = SettingTypes.Generic)
        {
            var rule = settings.GetRule(name, type);
            if (rule == null)
                throw new Exception($"No rules found for setting {name} and type {type}");

            var value = rule.ToInteger();
            if (value == null)
                throw new Exception($"Invalid value {rule.Value} in rule {name} type {type}");

            return value.Value;
        }

        public static string[]? GetBlackList(this IEnumerable<RuleSetting> settings)
        {
            var blackList = settings?.GetRule(SettingNames.Blacklist, SettingNames.Blacklist);
            return blackList?.Value?.Split(';').ToArray();
        }
    }

}
