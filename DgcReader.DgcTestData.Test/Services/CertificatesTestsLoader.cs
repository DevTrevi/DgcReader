using DgcReader.DgcTestData.Test.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public async Task<IDictionary<string, IEnumerable<TestEntry>>> LoadTestEntries()
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
                temp.Add(dir, await LoadTestEntries(dir));
            }
            return temp;

        }

        /// <summary>
        /// Load all the entries for the specified folder from the base path.
        /// Usually, each folder represents a country code
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        public async Task<IEnumerable<TestEntry>> LoadTestEntries(string folder)
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
#if NET5_0_OR_GREATER
                    var json = await File.ReadAllTextAsync(file);
#else
                    var json = File.ReadAllText(file);
#endif
                    var entry = JsonConvert.DeserializeObject<TestEntry>(json);
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


}
