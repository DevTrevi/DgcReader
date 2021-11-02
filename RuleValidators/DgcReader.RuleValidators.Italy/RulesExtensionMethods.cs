using DgcReader.RuleValidators.Italy.Const;
using DgcReader.RuleValidators.Italy.Exceptions;
using DgcReader.RuleValidators.Italy.Models;
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
        /// Search rules with the specified name, trying to match the type.
        /// If no match exists for the specified type, the <see cref="SettingTypes.Generic"/> type is used.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="name">The setting name to search</param>
        /// <param name="type">The setting type to search</param>
        /// <returns></returns>
        public static RuleSetting? GetBestMatch(this IEnumerable<RuleSetting> settings, string name, string type)
        {
            return settings.SingleOrDefault(r => r.Name == name && r.Type == type) ??
                settings.SingleOrDefault(r => r.Name == name && r.Type == SettingTypes.Generic);
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

        public static int GetBestMatchInteger(this IEnumerable<RuleSetting> settings, string name, string type = SettingTypes.Generic)
        {
            var rule = settings.GetBestMatch(name, type);
            if (rule == null)
                throw new DgcRulesValidationException($"No rules found for setting {name} and type {type}");

            var value = rule.ToInteger();
            if (value == null)
                throw new DgcRulesValidationException($"Invalid value {rule.Value} in rule {name} type {type}");

            return value.Value;
        }

        public static int GetRuleInteger(this IEnumerable<RuleSetting> settings, string name, string type = SettingTypes.Generic)
        {
            var rule = settings.GetRule(name, type);
            if (rule == null)
                throw new DgcRulesValidationException($"No rules found for setting {name} and type {type}");

            var value = rule.ToInteger();
            if (value == null)
                throw new DgcRulesValidationException($"Invalid value {rule.Value} in rule {name} type {type}");

            return value.Value;
        }
    }



}
