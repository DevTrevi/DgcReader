using GreenpassReader.Models;
using System.Linq;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader
{
    /// <summary>
    /// Extension methods for <see cref="EuDGC"/>
    /// </summary>
    public static class EuDGCExtensionMethods
    {
        /// <summary>
        /// Return the single certificate entry from the EuDGC (RecoveryEntry, Test or Vaccination)
        /// </summary>
        /// <param name="dgc"></param>
        /// <returns></returns>
        public static ICertificateEntry GetCertificateEntry(this EuDGC dgc)
        {
            var empty = Enumerable.Empty<ICertificateEntry>();
            return empty
                .Union(dgc.Recoveries ?? empty)
                .Union(dgc.Tests ?? empty)
                .Union(dgc.Vaccinations ?? empty)
                .Last();
        }

        /// <summary>
        /// Return the single certificate entry from the EuDGC (RecoveryEntry, Test or Vaccination)
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
