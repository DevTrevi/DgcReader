using DgcReader.RuleValidators.Germany.CovpassDgcCertlogic.Data;
using System;
using System.Collections.Generic;
using System.Linq;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0


namespace DgcReader.RuleValidators.Germany.CovpassDgcCertlogic.Domain.Rules
{

    public class CovPassGetRulesUseCase : DefaultGetRulesUseCase
    {
        // Note: the implementation is the same of DefaultGetRulesUseCase
    }

    public class DefaultGetRulesUseCase : IGetRulesUseCase
    {
        public DefaultGetRulesUseCase()
        {

        }

        public IEnumerable<RuleEntry> Invoke(IEnumerable<RuleEntry> rules, DateTimeOffset validationClock, string acceptanceCountryIsoCode, string issuanceCountryIsoCode, CertificateType certificateType, string? region = null)
        {
            var filteredAcceptanceRules = new Dictionary<string, RuleEntry>();
            var selectedRegion = region ?? string.Empty;

            var filteredRules = GeRulesBy(rules,
                acceptanceCountryIsoCode,
                validationClock,
                RuleType.ACCEPTANCE,
                certificateType.ToRuleCertificateType());

            if (!string.IsNullOrEmpty(issuanceCountryIsoCode))
            {
                filteredRules = filteredRules.Union(GeRulesBy(rules,
                    issuanceCountryIsoCode,
                    validationClock,
                    RuleType.INVALIDATION,
                    certificateType.ToRuleCertificateType()));
            }

            // If multiple rules are available for the same identifier, type and region,
            // returns only the higher version
            filteredRules = filteredRules
                .GroupBy(r => new
                {
                    r.Identifier,
                    r.Type,
                    Region = r.Region?.Trim() ?? string.Empty,
                })
                .Select(g => g.OrderByDescending(r => ToVersion(r.Version)).First());

            return filteredRules;
        }


        private IEnumerable<RuleEntry> GeRulesBy(
            IEnumerable<RuleEntry> rules,
            string acceptanceCountryIsoCode,
            DateTimeOffset validationClock,
            RuleType ruleType,
            RuleCertificateType ruleCertificateType)
        {
            return rules.Where(r => r.CountryCode.Equals(acceptanceCountryIsoCode, StringComparison.InvariantCultureIgnoreCase) &&
                validationClock >= r.ValidFrom && validationClock < r.ValidTo &&
                r.Type == ruleType &&
                (r.CertificateType == ruleCertificateType || r.CertificateType == RuleCertificateType.GENERAL));
        }

        private Version? ToVersion(string version)
        {
            if (Version.TryParse(version, out var result))
                return result;
            return null;
        }
    }

    public interface IGetRulesUseCase
    {
        IEnumerable<RuleEntry> Invoke(
            IEnumerable<RuleEntry> rules,
            DateTimeOffset validationClock,
            string acceptanceCountryIsoCode,
            string issuanceCountryIsoCode,
            CertificateType certificateType,
            string? region = null);
    }
}
