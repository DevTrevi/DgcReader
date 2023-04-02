// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Italy.Const
{
    /// <summary>
    /// Specific certificate key identifiers used to identify exceptional rules
    /// See <see href="https://ec.europa.eu/health/sites/default/files/ehealth/docs/digital-green-certificates_v5_en.pdf"/> for more details
    /// </summary>
    public static class CertificateExtendedKeyUsageIdentifiers
    {
        /// <summary>
        /// Extended key usage identifiers used by recovery certificates issuers
        /// </summary>
        public static readonly string[] TestIssuersIds = new[]
        {
            "1.3.6.1.4.1.1847.2021.1.1",
        };

        /// <summary>
        /// Extended key usage identifiers used by recovery certificates issuers
        /// </summary>
        public static readonly string[] VaccinationIssuersIds = new[]
        {
            "1.3.6.1.4.1.1847.2021.1.2",
        };

        /// <summary>
        /// Extended key usage identifiers used by recovery certificates issuers
        /// </summary>
        public static readonly string[] RecoveryIssuersIds = new[]
        {
            "1.3.6.1.4.1.1847.2021.1.3",
            "1.3.6.1.4.1.0.1847.2021.1.3",
        };
    }
}
