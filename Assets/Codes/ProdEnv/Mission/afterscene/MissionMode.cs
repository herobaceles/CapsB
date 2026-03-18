/// <summary>
/// Identifies which AR recovery sub-mode to activate during the After phase.
/// Matched against AfterMissionManager ARModeBinding entries by task ID.
/// </summary>
public enum MissionMode
{
    /// <summary>Collecting and cleaning recovery gear after a disaster.</summary>
    CleanupGear,

    /// <summary>Scanning the environment for hidden post-flood hazards (e.g. submerged debris).</summary>
    HazardScan,

    /// <summary>Assessing structural damage — delegates to MissionData.startQuiz.</summary>
    DamageAssessment,

    /// <summary>Disinfecting the house by removing mud piles and contamination.</summary>
    DisinfectHouse,
}
