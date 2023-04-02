// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Italy.Const;

/// <summary>
/// Setting types supported by the validator
/// </summary>
public static class SettingTypes
{
    /// <summary>
    /// Key specific to public ios/android apps
    /// </summary>
    public const string AppMinVersion = "APP_MIN_VERSION";

    /// <summary>
    /// Key for validation of generic rules
    /// </summary>
    public const string Generic = "GENERIC";
}
