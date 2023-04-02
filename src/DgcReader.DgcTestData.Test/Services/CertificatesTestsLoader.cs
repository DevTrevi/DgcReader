using DgcReader.DgcTestData.Test.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.DgcTestData.Test.Services
{
    /// <summary>
    /// An utility class to load the test data stored in the files of the
    /// <see href="https://github.com/eu-digital-green-certificates/dgc-testdata">dgc-testdata repository</see>
    /// </summary>
    public class CertificatesTestsLoader
    {
        /// <summary>
        /// Name of the folder containing the common tests
        /// </summary>
        public const string CommonTestDirName = "common";

        private readonly string basePath;

        private static JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            //MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind,
            Converters = {
                new DateTimeOffsetConverter(),
                //new DateTimeOffsetConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal },
            },
        };

        /// <summary>
        /// Instantiate the loader class, specifying the path of the
        /// <see href="https://github.com/eu-digital-green-certificates/dgc-testdata">dgc-testdata repository</see> folder
        /// </summary>
        /// <param name="basePath"></param>
        public CertificatesTestsLoader(string basePath)
        {
            this.basePath = basePath;
        }

        /// <summary>
        /// Load all the entries from the base path, scanning all the subfolders
        /// </summary>
        /// <returns></returns>
        public IDictionary<string, IEnumerable<TestEntry>> LoadTestEntries()
        {
            var directories = Directory.GetDirectories(basePath)
                .Select(d => Path.GetFileName(d))
                .Where(d => !d.StartsWith("."))
                .OrderByDescending(d => d == CommonTestDirName)
                .ThenBy(d => d)
                .ToArray();
            Debug.WriteLine($"Found {directories.Length} directories");
            var temp = new Dictionary<string, IEnumerable<TestEntry>>();
            foreach (var dir in directories)
            {
                temp.Add(dir, LoadTestEntries(dir));
            }
            return temp;

        }

        /// <summary>
        /// Load all the entries for the specified folder from the base path.
        /// Usually, each folder represents a country code
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        public IEnumerable<TestEntry> LoadTestEntries(string folder)
        {
            if (!string.IsNullOrEmpty(basePath))
                folder = Path.Combine(basePath, folder);

            var files = Directory.GetFiles(folder, "*.json", SearchOption.AllDirectories);

            Debug.WriteLine($"Found {files.Length} files in directory {Path.GetFileName(folder)}");

            var temp = new List<TestEntry>();
            foreach (var file in files)
            {
                try
                {
                    var json = File.ReadAllText(file);

                    var entry = JsonConvert.DeserializeObject<TestEntry>(json, SerializerSettings);
                    entry.Filename = Path.GetFileName(file);
                    temp.Add(entry);
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Error while loading file {file}: {e.Message}");
                }
            }
            return temp;
        }
    }

    public class DateTimeOffsetConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DateTimeOffset) || objectType == typeof(DateTimeOffset?);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value == null) return null;

            var dateTimeOffset = DateTimeOffset.Parse(reader.Value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

            return dateTimeOffset;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(value);
        }
    }


}
