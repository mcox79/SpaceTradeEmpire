namespace SimCore.Tweaks;

// GATE.T41.SURVEY_PROG.TWEAKS.001: SurveyProgram automation parameters.
public static class SurveyProgramTweaksV0
{
    // How often the survey program ticks (in sim ticks).
    public const int SurveyCadenceTicks = 10;

    // BFS range from home node for discovery scanning.
    public const int SurveyRangeHops = 3;

    // Number of manual scans of a discovery family before FO suggests deploying a survey program.
    public const int ManualScanGateCount = 3;
}
