using GreenpassReader.Models;
using System.Linq;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader;

/// <summary>
/// Extension methods for <see cref="EuDGC"/>
/// </summary>
public static class EuDGCExtensionMethods
{
    /// <summary>
    /// Return the single certificate entry from the EuDGC (RecoveryEntry, Test or Vaccination)
    /// </summary>
    /// <param name="dgc"></param>
    /// <param name="targetedDiseaseAgent">Restrict search to the specified disease agent</param>
    /// <returns></returns>
    public static ICertificateEntry? GetCertificateEntry(this EuDGC dgc, string? targetedDiseaseAgent = null)
    {
        var q = dgc.GetCertificateEntries();

        if (!string.IsNullOrEmpty(targetedDiseaseAgent))
            q = q.Where(e => e.TargetedDiseaseAgent == targetedDiseaseAgent);

        return q.LastOrDefault();
    }

    /// <summary>
    /// Return the single certificate entry from the EuDGC (RecoveryEntry, Test or Vaccination)
    /// </summary>
    /// <param name="dgc"></param>
    /// <param name="targetedDiseaseAgent">Restrict search to the specified disease agent</param>
    /// <returns></returns>
    public static TCertificate? GetCertificateEntry<TCertificate>(this EuDGC dgc, string? targetedDiseaseAgent = null)
        where TCertificate : class, ICertificateEntry
    {
        return dgc.GetCertificateEntry(targetedDiseaseAgent) as TCertificate;
    }
}
