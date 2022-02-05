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

        public static int? GetRuleNullableInteger(this IEnumerable<RuleSetting> settings, string name, string type = SettingTypes.Generic)
        {
            var rule = settings.GetRule(name, type);
            return rule?.ToInteger();
        }

        public static string[]? GetBlackList(this IEnumerable<RuleSetting> settings)
        {
            var blackList = settings?.GetRule(SettingNames.Blacklist, SettingNames.Blacklist);
            return blackList?.Value?.Split(';').ToArray();
        }

        public static string ToUpperInvariantNotNull(this string s)
            => s?.ToUpperInvariant() ?? string.Empty;

        #region Start/end days

        #region Recovery
        public static int GetRecoveryCertStartDay(this IEnumerable<RuleSetting> settings)
            => settings.GetRuleInteger(SettingNames.RecoveryCertStartDay);

        public static int GetRecoveryCertEndDay(this IEnumerable<RuleSetting> settings)
            => settings.GetRuleInteger(SettingNames.RecoveryCertEndDay);

        public static int GetRecoveryPvCertStartDay(this IEnumerable<RuleSetting> settings)
            => settings.GetRuleInteger(SettingNames.RecoveryPvCertStartDay);

        public static int GetRecoveryPvCertEndDay(this IEnumerable<RuleSetting> settings)
            => settings.GetRuleInteger(SettingNames.RecoveryPvCertEndDay);

        public static int GetRecoveryCertStartDayUnified(this IEnumerable<RuleSetting> settings, string issuerCountryCode)
        {
            return issuerCountryCode.ToUpperInvariantNotNull() == "IT" ?
                settings.GetRuleNullableInteger(SettingNames.RecoveryCertStartDayIT) ?? 0 :
                settings.GetRuleNullableInteger(SettingNames.RecoveryCertStartDayNotIT) ?? 0;
        }

        public static int GetRecoveryCertEndDayUnified(this IEnumerable<RuleSetting> settings, string issuerCountryCode)
        {
            issuerCountryCode = issuerCountryCode?.ToUpperInvariant() ?? string.Empty;

            return issuerCountryCode.ToUpperInvariantNotNull() == "IT" ?
                settings.GetRuleNullableInteger(SettingNames.RecoveryCertEndDayIT) ?? 180 :
                settings.GetRuleNullableInteger(SettingNames.RecoveryCertEndDayNotIT) ?? 270;
        }
        #endregion

        #region Test
        public static int GetMolecularTestStartHour(this IEnumerable<RuleSetting> settings)
            => settings.GetRuleInteger(SettingNames.MolecularTestStartHours);
        public static int GetMolecularTestEndHour(this IEnumerable<RuleSetting> settings)
            => settings.GetRuleInteger(SettingNames.MolecularTestEndHours);

        public static int GetRapidTestStartHour(this IEnumerable<RuleSetting> settings)
            => settings.GetRuleInteger(SettingNames.RapidTestStartHours);
        public static int GetRapidTestEndHour(this IEnumerable<RuleSetting> settings)
            => settings.GetRuleInteger(SettingNames.RapidTestEndHours);
        #endregion

        #region Vaccination
        public static int GetVaccineStartDayNotComplete(this IEnumerable<RuleSetting> settings, string vaccineType)
            => settings.GetRuleInteger(SettingNames.VaccineStartDayNotComplete, vaccineType);
        public static int GetVaccineEndDayNotComplete(this IEnumerable<RuleSetting> settings, string vaccineType)
            => settings.GetRuleInteger(SettingNames.VaccineEndDayNotComplete, vaccineType);

        public static int GetVaccineStartDayComplete(this IEnumerable<RuleSetting> settings, string vaccineType)
            => settings.GetRuleInteger(SettingNames.VaccineStartDayComplete, vaccineType);
        public static int GetVaccineEndDayComplete(this IEnumerable<RuleSetting> settings, string vaccineType)
            => settings.GetRuleInteger(SettingNames.VaccineEndDayComplete, vaccineType);

        public static int GetVaccineStartDayCompleteUnified(this IEnumerable<RuleSetting> settings, string issuerCountryCode, string vaccineType)
        {
            var daysToAdd = vaccineType == VaccineProducts.JeJVacineCode ? 15 : 0;

            var startDay = issuerCountryCode.ToUpperInvariantNotNull() == "IT" ?
                settings.GetRuleNullableInteger(SettingNames.VaccineStartDayCompleteIT) ?? 0 :
                settings.GetRuleNullableInteger(SettingNames.VaccineStartDayCompleteNotIT) ?? 0;

            return startDay + daysToAdd;
        }

        public static int GetVaccineEndDayCompleteUnified(this IEnumerable<RuleSetting> settings, string issuerCountryCode)
        {
            return issuerCountryCode.ToUpperInvariantNotNull() == "IT" ?
                settings.GetRuleNullableInteger(SettingNames.VaccineEndDayCompleteIT) ?? 180 :
                settings.GetRuleNullableInteger(SettingNames.VaccineEndDayCompleteNotIT) ?? 270;
        }

        public static int GetVaccineStartDayBoosterUnified(this IEnumerable<RuleSetting> settings, string issuerCountryCode)
        {
            return issuerCountryCode.ToUpperInvariantNotNull() == "IT" ?
                settings.GetRuleNullableInteger(SettingNames.VaccineStartDayBoosterIT) ?? 0 :
                settings.GetRuleNullableInteger(SettingNames.VaccineStartDayBoosterNotIT) ?? 0;
        }

        public static int GetVaccineEndDayBoosterUnified(this IEnumerable<RuleSetting> settings, string issuerCountryCode)
        {
            return issuerCountryCode.ToUpperInvariantNotNull() == "IT" ?
                settings.GetRuleNullableInteger(SettingNames.VaccineEndDayBoosterIT) ?? 180 :
                settings.GetRuleNullableInteger(SettingNames.VaccineEndDayBoosterNotIT) ?? 270;
        }



        #endregion


        #endregion
    }

}
