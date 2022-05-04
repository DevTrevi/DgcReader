// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Italy.Models
{
    /// <summary>
    /// Validation modes supported by the provider, according to the official SDK specifications
    /// </summary>
    public enum ValidationMode
    {
        /// <summary>
        /// Allows validation of vaccinations, recovery certificates and test results
        /// </summary>
        Basic3G,

        /// <summary>
        /// Enables the "Super Greenpass" check, restricting the validation to vaccinations and recovery certificates only.
        /// Test results are not considered valid in this mode.
        /// </summary>
        Strict2G,

        /// <summary>
        /// Enables the booster check, that requires at least 3 vaccination doses to be considered valid (or 2 doses in case of Johnson &amp; Johnson)
        /// </summary>
        Booster,

        /// <summary>
        /// Validates the certificate applying rules needed for entry in Italy
        /// </summary>
        EntryItaly,
    }
}
