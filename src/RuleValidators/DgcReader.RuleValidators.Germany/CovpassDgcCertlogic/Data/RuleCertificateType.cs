using GreenpassReader.Models;
using System;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Germany.CovpassDgcCertlogic.Data;

/// <summary>
/// Rule types
/// </summary>
public enum RuleCertificateType
{
    /// <summary>
    /// General rule
    /// </summary>
    GENERAL,

    /// <summary>
    /// Rule applied to test certificates
    /// </summary>
    TEST,

    /// <summary>
    /// Rule applied to vaccination certificates
    /// </summary>
    VACCINATION,

    /// <summary>
    /// Rule applied to recovery certificates
    /// </summary>
    RECOVERY
}

/// <summary>
/// Certificate types
/// </summary>
public enum CertificateType
{
    /// <summary>
    /// Test certificate
    /// </summary>
    TEST,

    /// <summary>
    /// Vaccination certificate
    /// </summary>
    VACCINATION,

    /// <summary>
    /// Recvery certificate
    /// </summary>
    RECOVERY
}

/// <summary>
/// Extension methods for RuleCertificateType and CertificateType
/// </summary>
public static class RuleCertificateTypeExtensions
{
    /// <summary>
    /// Converta <see cref="CertificateType"/> to <see cref="RuleCertificateType"/>
    /// </summary>
    /// <param name="certificateType"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static RuleCertificateType ToRuleCertificateType(this CertificateType certificateType)
    {
        switch (certificateType)
        {
            case CertificateType.TEST:
                return RuleCertificateType.TEST;
            case CertificateType.VACCINATION:
                return RuleCertificateType.VACCINATION;
            case CertificateType.RECOVERY:
                return RuleCertificateType.RECOVERY;
            default:
                throw new ArgumentOutOfRangeException(certificateType.ToString());
        }
    }

    /// <summary>
    /// Get the <see cref="CertificateType"/> of an <see cref="EuDGC"/>
    /// </summary>
    /// <param name="dgc"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static CertificateType GetCertificateType(this EuDGC dgc)
    {
        var certificateEntry = dgc.GetCertificateEntry();

        if (certificateEntry is TestEntry)
            return CertificateType.TEST;
        if (certificateEntry is VaccinationEntry)
            return CertificateType.VACCINATION;
        if (certificateEntry is RecoveryEntry)
            return CertificateType.RECOVERY;
        throw new ArgumentNullException();
    }
}
