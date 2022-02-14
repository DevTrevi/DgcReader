using DgcReader.RuleValidators.Italy.Models;
using GreenpassReader.Models;
using System.Linq;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader
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
        /// <param name="targetedDiseaseAgent">Restrict search to the specified disease agent</param>
        /// <returns></returns>
        public static ICertificateEntry? GetCertificateEntry(this ItalianDGC dgc, string? targetedDiseaseAgent = null)
        {
            var empty = Enumerable.Empty<ICertificateEntry>();
            var q = empty
                .Union(dgc.Recoveries ?? empty)
                .Union(dgc.Tests ?? empty)
                .Union(dgc.Vaccinations ?? empty)
                .Union(dgc.Exemptions ?? empty);

            if (!string.IsNullOrEmpty(targetedDiseaseAgent))
                q = q.Where(e => e.TargetedDiseaseAgent == targetedDiseaseAgent);

            return q.LastOrDefault();
        }

        /// <summary>
        /// Return the single certificate entry from the EuDGC (RecoveryEntry, Test, Vaccination or Exemption)
        /// </summary>
        /// <param name="dgc"></param>
        /// <param name="targetedDiseaseAgent">Restrict search to the specified disease agent</param>
        /// <returns></returns>
        public static TCertificate? GetCertificateEntry<TCertificate>(this ItalianDGC dgc, string? targetedDiseaseAgent = null)
            where TCertificate : class, ICertificateEntry
        {
            return dgc.GetCertificateEntry(targetedDiseaseAgent) as TCertificate;
        }
    }
}
