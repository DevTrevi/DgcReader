using GreenpassReader.Models;
using System;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Germany.CovpassDgcCertlogic.Data
{
    public enum RuleCertificateType
    {
        GENERAL,
        TEST,
        VACCINATION,
        RECOVERY
    }

    public enum CertificateType
    {
        TEST,
        VACCINATION,
        RECOVERY
    }

    public static class RuleCertificateTypeExtensions
    {
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
}
