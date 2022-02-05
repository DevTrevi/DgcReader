using GreenpassReader.Models;
using DgcReader.RuleValidators.Italy.Const;

#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER || NET47_OR_GREATER
using Microsoft.Extensions.Options;
#endif

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Italy
{
    /// <summary>
    /// Extension methods for certificate entries
    /// </summary>
    public static class CertificateEntryExtensions
    {
        /// <summary>
        /// Check if the vaccination is considered a BOOSTER (more doses than initially required)
        /// </summary>
        /// <param name="vaccination"></param>
        /// <returns></returns>
        public static bool IsBooster(this VaccinationEntry vaccination)
        {
            if (vaccination.DoseNumber > vaccination.TotalDoseSeries)
                return true;

            if (vaccination.MedicinalProduct == VaccineProducts.JeJVacineCode &&
                vaccination.DoseNumber >= 2)
                return true;

            return vaccination.DoseNumber >= 3;
        }
    }
}