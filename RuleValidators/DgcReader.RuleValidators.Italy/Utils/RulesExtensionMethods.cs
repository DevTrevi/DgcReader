using DgcReader.RuleValidators.Italy.Const;
using DgcReader.RuleValidators.Italy.Models;
using GreenpassReader.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Italy
{
    internal static class RulesExtensionMethods
    {
        const int NoValue = 0;

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

        /// <summary>
        /// Returns the required integer value, or <see cref="NoValue"/> as fallback
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static int GetRuleInteger(this IEnumerable<RuleSetting> settings, string name, string type = SettingTypes.Generic)
        {
            var value = settings.GetRuleNullableInteger(name, type);
            if (value == null)
            {
                Debug.WriteLine($"No rules found for setting {name} and type {type}. Returning default value {NoValue}");
            }

            return value ?? NoValue;
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
        [Obsolete]
        public static int GetRecoveryCertStartDay(this IEnumerable<RuleSetting> settings)
            => settings.GetRuleInteger(SettingNames.RecoveryCertStartDay);

        [Obsolete]
        public static int GetRecoveryCertEndDay(this IEnumerable<RuleSetting> settings)
            => settings.GetRuleInteger(SettingNames.RecoveryCertEndDay);

        public static int GetRecoveryPvCertStartDay(this IEnumerable<RuleSetting> settings)
            => settings.GetRuleInteger(SettingNames.RecoveryPvCertStartDay);

        public static int GetRecoveryPvCertEndDay(this IEnumerable<RuleSetting> settings)
            => settings.GetRuleInteger(SettingNames.RecoveryPvCertEndDay);

        public static int GetRecoveryCertStartDayUnified(this IEnumerable<RuleSetting> settings, string issuerCountryCode)
        {
            return issuerCountryCode.ToUpperInvariantNotNull() == CountryCodes.Italy ?
                settings.GetRuleInteger(SettingNames.RecoveryCertStartDayIT) :
                settings.GetRuleInteger(SettingNames.RecoveryCertStartDayNotIT);
        }

        public static int GetRecoveryCertEndDayUnified(this IEnumerable<RuleSetting> settings, string issuerCountryCode)
        {
            return issuerCountryCode.ToUpperInvariantNotNull() == CountryCodes.Italy ?
                settings.GetRuleInteger(SettingNames.RecoveryCertEndDayIT) :
                settings.GetRuleInteger(SettingNames.RecoveryCertEndDayNotIT);
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

        [Obsolete]
        public static int GetVaccineEndDayComplete(this IEnumerable<RuleSetting> settings, string vaccineType)
            => settings.GetRuleInteger(SettingNames.VaccineEndDayComplete, vaccineType);

        public static int GetVaccineStartDayCompleteUnified(this IEnumerable<RuleSetting> settings, string issuerCountryCode, string vaccineType)
        {
            var daysToAdd = vaccineType == VaccineProducts.JeJVacineCode ? settings.GetVaccineStartDayComplete(VaccineProducts.JeJVacineCode) : NoValue;

            var startDay = issuerCountryCode.ToUpperInvariantNotNull() == CountryCodes.Italy ?
                settings.GetRuleInteger(SettingNames.VaccineStartDayCompleteIT) :
                settings.GetRuleInteger(SettingNames.VaccineStartDayCompleteNotIT);

            return startDay + daysToAdd;
        }

        public static int GetVaccineEndDayCompleteUnified(this IEnumerable<RuleSetting> settings, string issuerCountryCode)
        {
            return issuerCountryCode.ToUpperInvariantNotNull() == CountryCodes.Italy ?
                settings.GetRuleInteger(SettingNames.VaccineEndDayCompleteIT) :
                settings.GetRuleInteger(SettingNames.VaccineEndDayCompleteNotIT);
        }

        public static int GetVaccineStartDayBoosterUnified(this IEnumerable<RuleSetting> settings, string issuerCountryCode)
        {
            return issuerCountryCode.ToUpperInvariantNotNull() == CountryCodes.Italy ?
                settings.GetRuleInteger(SettingNames.VaccineStartDayBoosterIT) :
                settings.GetRuleInteger(SettingNames.VaccineStartDayBoosterNotIT);
        }

        public static int GetVaccineEndDayBoosterUnified(this IEnumerable<RuleSetting> settings, string issuerCountryCode)
        {
            return issuerCountryCode.ToUpperInvariantNotNull() == CountryCodes.Italy ?
                settings.GetRuleInteger(SettingNames.VaccineEndDayBoosterIT) :
                settings.GetRuleInteger(SettingNames.VaccineEndDayBoosterNotIT);
        }

        public static int GetVaccineEndDayCompleteExtendedEMA(this IEnumerable<RuleSetting> settings)
            => settings.GetRuleInteger(SettingNames.VaccineEndDayCompleteExtendedEMA);

        /// <summary>
        /// The EMA approved list of vaccines
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static string[] GetEMAVaccines(this IEnumerable<RuleSetting> settings)
        {
            return settings.GetRule(SettingNames.EMAVaccines, SettingTypes.Generic)?.Value.Split(';') ?? new string[0];
        }

        /// <summary>
        /// Check if the vaccine product is considered valid by EMA
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="medicinalProduct"></param>
        /// <param name="countryOfVaccination"></param>
        /// <returns></returns>
        public static bool IsEMA(this IEnumerable<RuleSetting> settings, string medicinalProduct, string countryOfVaccination)
        {
            // also Sputnik is EMA, but only if from San Marino
            return settings.GetEMAVaccines().Contains(medicinalProduct) ||
                medicinalProduct == VaccineProducts.Sputnik && countryOfVaccination == CountryCodes.SanMarino;
        }

        /// <inheritdoc cref="IsEMA(IEnumerable{RuleSetting}, string, string)"/>
        public static bool IsEMA(this IEnumerable<RuleSetting> settings, VaccinationEntry vaccination)
            => settings.IsEMA(vaccination.MedicinalProduct, vaccination.Country);
        #endregion

        #endregion
    }

}
