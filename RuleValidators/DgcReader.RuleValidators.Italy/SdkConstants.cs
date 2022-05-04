#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER || NET47_OR_GREATER
using Microsoft.Extensions.Options;
#endif

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Italy
{
    internal class SdkConstants
    {
        /// <summary>
        /// The endpoint url for downloading rules
        /// </summary>
        public const string ValidationRulesUrl = "https://get.dgc.gov.it/v1/dgc/settings";

        /// <summary>
        /// The version of the sdk used as reference for implementing the rules.
        /// </summary>
        public const string ReferenceSdkMinVersion = "1.1.5";

        /// <summary>
        /// The version of the app used as reference for implementing the rules.
        /// NOTE: this is the version of the android app using the <see cref="ReferenceSdkMinVersion"/> of the SDK. The SDK version is not available in the settings right now.
        /// </summary>
        public const string ReferenceAppMinVersion = "1.2.0";

        /// <summary>
        /// Underage limit
        /// </summary>
        public const int VaccineUnderageAge = 18;
    }
}
