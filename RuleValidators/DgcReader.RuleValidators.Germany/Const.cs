// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

using System.IO;

namespace DgcReader.RuleValidators.Germany
{
    internal class Const
    {
        /// <summary>
        /// Url used for getting rules and rules identifiers
        /// </summary>
        public const string BaseUrl = "https://distribution.dcc-rules.de";

        /// <summary>
        /// The path relative to <see cref="DgcGermanRulesValidatorOptions.BasePath"/> where downloaded files will be save
        /// </summary>
        internal static readonly string ProviderDataFolder = Path.Combine("DgcReaderData", "RuleValidators", "Germany");
    }
}
