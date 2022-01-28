using GreenpassReader.Models;
using System.Linq;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Italy.Models
{
    /// <summary>
    /// Extension methods for <see cref="ItalianDGC"/>
    /// </summary>
    public static class ItalianDGCExtensionMethods
    {
        /// <summary>
        /// Return the single certificate entry from the EuDGC (RecoveryEntry, Test, Vaccination or Exemption)
        /// </summary>
        /// <param name="dgc"></param>
        /// <returns></returns>
        public static ICertificateEntry GetCertificateEntry(this ItalianDGC dgc)
        {
            var empty = Enumerable.Empty<ICertificateEntry>();
            return empty
                .Union(dgc.Recoveries ?? empty)
                .Union(dgc.Tests ?? empty)
                .Union(dgc.Vaccinations ?? empty)
                .Union(dgc.Exemptions ?? empty)
                .Last();
        }

        /// <summary>
        /// Return the single certificate entry from the EuDGC (RecoveryEntry, Test, Vaccination or Exemption)
        /// </summary>
        /// <param name="dgc"></param>
        /// <returns></returns>
        public static TCertificate? GetCertificateEntry<TCertificate>(this EuDGC dgc)
            where TCertificate : class, ICertificateEntry
        {
            return dgc.GetCertificateEntry() as TCertificate;
        }
    }
}
