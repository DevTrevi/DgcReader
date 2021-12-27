using Newtonsoft.Json;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace DgcReader.RuleValidators.Germany.CovpassDgcCertlogic.Data
{
    /// <summary>
    /// Description of a rule in a specific language
    /// </summary>
    public class RuleEntryDescription
    {
        /// <summary>
        /// Language code of the description
        /// </summary>
        [JsonProperty("lang")]
        public string LanguageCode { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        [JsonProperty("desc")]
        public string Descrption { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{LanguageCode}: {Descrption}";
        }
    }


    /// <summary>
    /// Extension methods for <see cref="RuleEntryDescription"/>
    /// </summary>
    public static class RuleEntryDescriptionExtensionMethods
    {
        /// <summary>
        /// Return the CultureInfo representing the languagecode of the RuleEntryDescription
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public static CultureInfo GetLanguage(this RuleEntryDescription d)
        {
            var ci = CultureInfo.GetCultureInfo(d.LanguageCode);
            return ci;
        }

        /// <summary>
        /// Get the description in the more appropriate language possible
        /// </summary>
        /// <param name="descriptions"></param>
        /// <param name="requestedLanguage"></param>
        /// <returns></returns>
        public static string? GetDescription(this IEnumerable<RuleEntryDescription> descriptions, CultureInfo requestedLanguage)
        {
            return descriptions.Select(r => new
            {
                Entry = r,
                Language = r.GetLanguage(),
            })
            .OrderByDescending(r => r.Language.Equals(requestedLanguage))
            .ThenByDescending(r => r.Language.Equals(requestedLanguage.Parent))
            .ThenByDescending(r => r.Language.Name == "en" || r.Language.Parent.Name == "en")
            .Select(r => r.Entry.Descrption)
            .FirstOrDefault();
        }
    }
}
