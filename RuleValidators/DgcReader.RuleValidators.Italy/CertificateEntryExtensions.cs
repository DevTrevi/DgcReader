using GreenpassReader.Models;
using DgcReader.RuleValidators.Italy.Const;
using System;
using System.Globalization;
using System.Linq;

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

        /// <summary>
        /// Parse the DateOfBirth of the certificate
        /// Accepts: yyyy-MM-dd, yyyy-MM-ddTHH:mm:ss, yyy-MM, yyyy
        /// </summary>
        /// <param name="dgc"></param>
        /// <returns></returns>
        public static DateTime GetBirthDate(this EuDGC dgc)
        {
            return ParseDgcDateOfBirth(dgc.DateOfBirth);
        }

        /// <summary>
        /// Parse a date in pseudo-iso format
        /// Accepts: yyyy-MM-dd, yyyy-MM-ddTHH:mm:ss, yyy-MM, yyyy
        /// </summary>
        /// <param name="dateString"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static DateTime ParseDgcDateOfBirth(string dateString)
        {
            try
            {
                // Try ISO (yyyy-MM-dd or yyyy-MM-ddTHH:mm:ss)
                if (DateTime.TryParseExact(dateString, "s", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out DateTime date))
                {
                    return date.Date;
                }

                // Split by allowed separators
                var dateStrComponents = dateString.Trim()
                    .Split('-')
                    .Select(s => int.Parse(s.Trim()))
                    .ToArray();

                var year = dateStrComponents[0];
                var month = Math.Max(1, dateStrComponents.Length > 1 ? dateStrComponents[1] : 1);
                var day = Math.Max(1, dateStrComponents.Length > 2 ? dateStrComponents[2] : 1);
                return new DateTime(year, month, day);
            }
            catch (Exception e)
            {
                throw new Exception($"Value {dateString} is not a valid Date: {e}", e);
            }
        }

        /// <summary>
        /// Return the age between 2 dates in completed years
        /// </summary>
        /// <param name="birthDate"></param>
        /// <param name="currentDate"></param>
        /// <returns></returns>
        public static int GetAge(this DateTime birthDate, DateTime currentDate)
        {
            currentDate = currentDate.Date;
            var age = currentDate.Year - birthDate.Year;
            if (birthDate.Date > currentDate.AddYears(-age))
                return age - 1;
            return age;
        }

    }
}