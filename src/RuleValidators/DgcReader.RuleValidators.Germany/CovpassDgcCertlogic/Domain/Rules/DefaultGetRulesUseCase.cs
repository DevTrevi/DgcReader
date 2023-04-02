using DgcReader.RuleValidators.Germany.CovpassDgcCertlogic.Data;
using System;
using System.Collections.Generic;
using System.Linq;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0


namespace DgcReader.RuleValidators.Germany.CovpassDgcCertlogic.Domain.Rules;

/// <inheritdoc/>
public class CovPassGetRulesUseCase : DefaultGetRulesUseCase
{
    // Note: the implementation is the same of DefaultGetRulesUseCase
}

/// <inheritdoc/>
public class DefaultGetRulesUseCase : IGetRulesUseCase
{
    /// <summary>
    /// Constructor
    /// </summary>
    public DefaultGetRulesUseCase()
    {

    }

    /// <inheritdoc/>
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

/// <summary>
/// Service for filtering rules for a specific use case
/// </summary>
public interface IGetRulesUseCase
{
    /// <summary>
    /// Filter the rules for the specified use case
    /// </summary>
    /// <param name="rules"></param>
    /// <param name="validationClock"></param>
    /// <param name="acceptanceCountryIsoCode"></param>
    /// <param name="issuanceCountryIsoCode"></param>
    /// <param name="certificateType"></param>
    /// <param name="region"></param>
    /// <returns></returns>
    IEnumerable<RuleEntry> Invoke(
        IEnumerable<RuleEntry> rules,
        DateTimeOffset validationClock,
        string acceptanceCountryIsoCode,
        string issuanceCountryIsoCode,
        CertificateType certificateType,
        string? region = null);
}
