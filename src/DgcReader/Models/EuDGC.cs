using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0
// Based on the original work of Myndigheten för digital förvaltning (DIGG)

namespace GreenpassReader.Models
{
    /// <summary>
    /// EU Digital Green Certificate
    /// Schema version: 1.3.0 - 2021-06-09
    /// </summary>
    public class EuDGC
    {
        #region Properties
        /// <summary>
        /// Date of Birth of the person addressed in the DGC. ISO 8601 date format restricted to
        /// range 1900-2099
        /// </summary>
        [JsonProperty("dob")]
        public string DateOfBirth { get; internal set; }

        /// <summary>
        /// Surname(s), given name(s) - in that order
        /// </summary>
        [JsonProperty("nam")]
        public Name Name { get; internal set; }

        /// <summary>
        /// Recovery Group
        /// </summary>
        [JsonProperty("r", NullValueHandling = NullValueHandling.Ignore)]
        public RecoveryEntry[]? Recoveries { get; internal set; }

        /// <summary>
        /// Test Group
        /// </summary>
        [JsonProperty("t", NullValueHandling = NullValueHandling.Ignore)]
        public TestEntry[]? Tests { get; internal set; }

        /// <summary>
        /// Vaccination Group
        /// </summary>
        [JsonProperty("v", NullValueHandling = NullValueHandling.Ignore)]
        public VaccinationEntry[]? Vaccinations { get; internal set; }

        /// <summary>
        /// Version of the schema, according to Semantic versioning (ISO, https://semver.org/ version 2.0.0 or newer)
        /// </summary>
        [JsonProperty("ver")]
        public string SchemaVersion { get; internal set; }
        #endregion

        #region Methods

        /// <summary>
        /// Return all the <see cref="ICertificateEntry"/> in the certificate
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<ICertificateEntry> GetCertificateEntries()
        {
            var empty = Enumerable.Empty<ICertificateEntry>();

            return empty
                .Union(Recoveries ?? empty)
                .Union(Tests ?? empty)
                .Union(Vaccinations ?? empty);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{this.GetType().Name}: {string.Join(", ", GetCertificateEntries())}";
        }

        #endregion
    }

    /// <summary>
    /// Surname(s), given name(s) - in that order
    /// Person name: Surname(s), given name(s) - in that order
    /// </summary>
    public class Name
    {
        /// <summary>
        /// The family or primary name(s) of the person addressed in the certificate
        /// </summary>
        [JsonProperty("fn", NullValueHandling = NullValueHandling.Ignore)]
        public string FamilyName { get; internal set; }

        /// <summary>
        /// The family name(s) of the person transliterated
        /// </summary>
        [JsonProperty("fnt")]
        public string FamilyNameTransliterated { get; internal set; }

        /// <summary>
        /// The given name(s) of the person addressed in the certificate
        /// </summary>
        [JsonProperty("gn", NullValueHandling = NullValueHandling.Ignore)]
        public string GivenName { get; internal set; }

        /// <summary>
        /// The given name(s) of the person transliterated
        /// </summary>
        [JsonProperty("gnt", NullValueHandling = NullValueHandling.Ignore)]
        public string GivenNameTransliterated { get; internal set; }


    }

    /// <summary>
    /// Recovery Entry
    /// </summary>
    public class RecoveryEntry : ICertificateEntry
    {
        /// <summary>
        /// Unique Certificate Identifier, UVCI
        /// </summary>
        [JsonProperty("ci")]
        public string CertificateIdentifier { get; internal set; }

        /// <summary>
        /// Country of Test
        /// </summary>
        [JsonProperty("co")]
        public string Country { get; internal set; }

        /// <summary>
        /// Certificate Issuer
        /// </summary>
        [JsonProperty("is")]
        public string Issuer { get; internal set; }


        /// <summary>
        /// A coded value from the value set disease-agent-targeted.json
        /// </summary>
        [JsonProperty("tg")]
        public string TargetedDiseaseAgent { get; internal set; }

        /// <summary>
        /// ISO 8601 Date: Certificate Valid From
        /// </summary>
        [JsonProperty("df")]
        public DateTimeOffset ValidFrom { get; internal set; }

        /// <summary>
        /// Certificate Valid Until
        /// </summary>
        [JsonProperty("du")]
        public DateTimeOffset ValidUntil { get; internal set; }

        /// <summary>
        /// ISO 8601 Date of First Positive Test Result
        /// </summary>
        [JsonProperty("fr")]
        public DateTimeOffset FirstPositiveTestResult { get; internal set; }
    }

    /// <summary>
    /// Test Entry
    /// Test group, if present, MUST contain exactly 1 (one) entry describing exactly one test result.
    /// </summary>
    public class TestEntry : ICertificateEntry
    {
        /// <summary>
        /// Unique Certificate Identifier, UVCI
        /// </summary>
        [JsonProperty("ci")]
        public string CertificateIdentifier { get; internal set; }

        /// <summary>
        /// Country of Test
        /// 2-letter ISO3166 code (RECOMMENDED) or a reference to an international
        /// organisation responsible for carrying out the test which the test
        /// was carried out (such as UNHCR or WHO)
        /// </summary>
        [JsonProperty("co")]
        public string Country { get; internal set; }

        /// <summary>
        /// Certificate Issuer
        /// </summary>
        [JsonProperty("is")]
        public string Issuer { get; internal set; }

        /// <summary>
        /// Coded value of the targeted disease agent
        /// </summary>
        [JsonProperty("tg")]
        public string TargetedDiseaseAgent { get; internal set; }

        /// <summary>
        /// RAT Test name and manufacturer (rapid antigen tests only)
        /// Rapid antigen test (RAT) device identifier from the JRC database.
        /// Value set (HSC common list):
        /// <see href="https://covid-19-diagnostics.jrc.ec.europa.eu/devices/hsc-common-recognition-rat">(machine-readable, values of the field id_device included on the list form the value set)</see>
        /// </summary>
        [JsonProperty("ma", NullValueHandling = NullValueHandling.Ignore)]
        public string? Ma { get; internal set; }

        /// <summary>
        /// NAA Test Name
        /// The name of the nucleic acid amplification test (NAAT) used.
        /// The name should include the name of the test manufacturer and
        /// the commercial name of the test, separated by a comma.
        /// For NAAT: the field is optional.
        /// For RAT: the field SHOULD NOT be used, as the name of the test is supplied
        /// indirectly through the test device identifier (t/ma)
        /// </summary>
        [JsonProperty("nm", NullValueHandling = NullValueHandling.Ignore)]
        public string NaatName { get; internal set; }

        /// <summary>
        /// Date/Time of Sample Collection
        /// The date and time when the test sample was collected. The time MUST
        /// include information on the time zone.The value MUST NOT denote the time
        /// when the test result was produced.
        /// </summary>
        [JsonProperty("sc")]
        public DateTimeOffset SampleCollectionDate { get; internal set; }

        /// <summary>
        /// Testing Centre
        /// Name of the actor that conducted the test
        /// </summary>
        [JsonProperty("tc")]
        public string TestingCentre { get; internal set; }

        /// <summary>
        /// Test Result
        /// </summary>
        [JsonProperty("tr")]
        public string TestResult { get; internal set; }

        /// <summary>
        /// Type of Test
        /// </summary>
        [JsonProperty("tt")]
        public string TestType { get; internal set; }
    }

    /// <summary>
    /// Vaccination Entry
    /// </summary>
    public class VaccinationEntry : ICertificateEntry
    {
        /// <summary>
        /// Unique Certificate Identifier: UVCI
        /// </summary>
        [JsonProperty("ci")]
        public string CertificateIdentifier { get; internal set; }

        /// <summary>
        /// Country of Vaccination
        /// 2-letter ISO3166 code (RECOMMENDED) or a reference to an international
        /// organisation responsible for carrying out the test which the test
        /// was carried out (such as UNHCR or WHO)
        /// </summary>
        [JsonProperty("co")]
        public string Country { get; internal set; }

        /// <summary>
        /// Certificate Issuer
        /// </summary>
        [JsonProperty("is")]
        public string Issuer { get; internal set; }

        /// <summary>
        /// Coded value of the targeted disease agent
        /// </summary>
        [JsonProperty("tg")]
        public string TargetedDiseaseAgent { get; internal set; }

        /// <summary>
        /// Dose Number
        /// Sequence number (positive integer) of the dose given during this vaccination event. 1 for the first dose, 2 for the second dose etc.
        /// </summary>
        [JsonProperty("dn")]
        public int DoseNumber { get; internal set; }

        /// <summary>
        /// Date of Vaccination
        /// </summary>
        [JsonProperty("dt")]
        public DateTimeOffset Date { get; internal set; }

        /// <summary>
        /// Marketing Authorization Holder - if no MAH present, then manufacturer
        /// </summary>
        [JsonProperty("ma")]
        public string MarketingAuthorizationHolder { get; internal set; }

        /// <summary>
        /// Vaccine medicinal product
        /// Medicinal product used for this specific dose of vaccination.
        /// </summary>
        [JsonProperty("mp")]
        public string MedicinalProduct { get; internal set; }

        /// <summary>
        /// Total Series of Doses
        /// Total number of doses (positive integer) in a complete vaccination series
        /// according to the used vaccination protocol.The protocol is not in all cases
        /// directly defined by the vaccine product, as in some countries only one dose of
        /// normally two-dose vaccines is delivered to people recovered from COVID-19
        /// </summary>
        [JsonProperty("sd")]
        public int TotalDoseSeries { get; internal set; }

        /// <summary>
        /// Vaccine or prophylaxis
        /// Type of the vaccine or prophylaxis used.
        /// </summary>
        [JsonProperty("vp")]
        public string VaccineOrProphylaxis { get; internal set; }
    }

    /// <summary>
    /// A certificate entry supported by the EuDGC
    /// </summary>
    public interface ICertificateEntry
    {
        /// <summary>
        /// Unique Certificate Identifier: UVCI
        /// </summary>
        string CertificateIdentifier { get; }

        /// <summary>
        /// Country
        /// 2-letter ISO3166 code (RECOMMENDED) or a reference to an international organisation
        /// </summary>
        string Country { get; }

        /// <summary>
        /// Certificate Issuer
        /// </summary>
        string Issuer { get; }

        /// <summary>
        /// Disease or agent targeted
        /// </summary>
        string TargetedDiseaseAgent { get; }
    }
}
