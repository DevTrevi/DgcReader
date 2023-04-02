#if NET47_OR_GREATER

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.TrustListProviders
{
    /// <summary>
    /// Extension methods for ECCurve
    /// </summary>
    public static class ECCurveExtensions
    {
        private static IEnumerable<ECCurve>? _eccCurves = null;

        /// <summary>
        /// Get the Oid value (dotted representation) for the specified curve if available
        /// </summary>
        /// <param name="curve"></param>
        /// <returns></returns>
        public static string? GetOidValue(this ECCurve curve)
        {
            if (!string.IsNullOrEmpty(curve.Oid.Value))
                return curve.Oid.Value;

            return GetECCurveByFriendlyName(curve.Oid.FriendlyName)?.Oid.Value;
        }

        /// <summary>
        /// Search the specified ECCurve by reflection from <see cref="ECCurve.NamedCurves"/>
        /// </summary>
        /// <param name="curveFriendlyName"></param>
        /// <returns></returns>
        public static ECCurve? GetECCurveByFriendlyName(string? curveFriendlyName)
        {
            if (string.IsNullOrEmpty(curveFriendlyName))
                return null;
            if (_eccCurves == null)
                _eccCurves = GetECCNamedCurves().ToArray();

            return _eccCurves.SingleOrDefault(c => c.Oid.FriendlyName == curveFriendlyName);
        }

        /// <summary>
        /// Returns the list of supported curves from <see cref="ECCurve.NamedCurves"/>
        /// This because .NET framework does not include the full Oid when reading curve parameters from ecdsa.ExportParameters(false);
        /// </summary>
        /// <returns></returns>
        private static IEnumerable<ECCurve> GetECCNamedCurves()
        {
            foreach(var property in typeof(ECCurve.NamedCurves)
                .GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance)
                .Where(p=>p.PropertyType == typeof(ECCurve)))
            {
                yield return (ECCurve)property.GetValue(null, null);
            }
        }
    }
}

#endif
