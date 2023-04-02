// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Italy.Const;

/// <summary>
/// Setting names supported by the validator
/// </summary>
public static class SettingNames
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    // Vaccine

    public const string VaccineStartDayComplete = "vaccine_start_day_complete";
    public const string VaccineEndDayComplete = "vaccine_end_day_complete";
    public const string VaccineStartDayNotComplete = "vaccine_start_day_not_complete";
    public const string VaccineEndDayNotComplete = "vaccine_end_day_not_complete";

    public const string VaccineStartDayCompleteIT = "vaccine_start_day_complete_IT";
    public const string VaccineEndDayCompleteIT = "vaccine_end_day_complete_IT";
    public const string VaccineStartDayBoosterIT = "vaccine_start_day_booster_IT";
    public const string VaccineEndDayBoosterIT = "vaccine_end_day_booster_IT";

    public const string VaccineStartDayCompleteNotIT = "vaccine_start_day_complete_NOT_IT";
    public const string VaccineEndDayCompleteNotIT = "vaccine_end_day_complete_NOT_IT";
    public const string VaccineStartDayBoosterNotIT = "vaccine_start_day_booster_NOT_IT";
    public const string VaccineEndDayBoosterNotIT = "vaccine_end_day_booster_NOT_IT";
    public const string VaccineEndDayCompleteUnder18 = "vaccine_end_day_complete_under_18";

    public const string VaccineEndDayCompleteExtendedEMA = "vaccine_end_day_complete_extended_EMA";
    public const string EMAVaccines = "EMA_vaccines";

    public const string VaccineCompleteUnder18Offset = "vaccine_complete_under_18_offset";
    

    // Test

    public const string RapidTestStartHours = "rapid_test_start_hours";
    public const string RapidTestEndHours = "rapid_test_end_hours";
    public const string MolecularTestStartHours = "molecular_test_start_hours";
    public const string MolecularTestEndHours = "molecular_test_end_hours";
    

    // Recovery

    public const string RecoveryCertStartDay = "recovery_cert_start_day";
    public const string RecoveryCertEndDay = "recovery_cert_end_day";
    public const string RecoveryPvCertStartDay = "recovery_pv_cert_start_day";
    public const string RecoveryPvCertEndDay = "recovery_pv_cert_end_day";

    public const string RecoveryCertStartDayIT = "recovery_cert_start_day_IT";
    public const string RecoveryCertEndDayIT = "recovery_cert_end_day_IT";
    public const string RecoveryCertStartDayNotIT = "recovery_cert_start_day_NOT_IT";
    public const string RecoveryCertEndDayNotIT = "recovery_cert_end_day_NOT_IT";

    // Other

    public const string AndroidAppMinVersion = "android";
    public const string SdkMinVersion = "sdk";

    public const string Blacklist = "black_list_uvci";
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
