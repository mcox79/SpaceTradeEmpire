namespace SimCore.Programs;

public static class ProgramKind
{
    public const string AutoBuy = "AUTO_BUY";
    public const string AutoSell = "AUTO_SELL";

    // GATE.S4.CONSTR_PROG.001: construction programs v0
    // Program supplies missing stage inputs to drive the minimal construction loop that produces cap_module.
    public const string ConstrCapModuleV0 = "CONSTR_CAP_MODULE_V0";

    // GATE.S3_6.EXPEDITION_PROGRAMS.001: expedition program contract v0
    public const string ExpeditionV0 = "EXPEDITION_V0";

    // GATE.S3_6.EXPLOITATION_PACKAGES.001: exploitation package program kinds v0
    public const string TradeCharterV0 = "TRADE_CHARTER_V0";
    public const string ResourceTapV0 = "RESOURCE_TAP_V0";

    // GATE.S3_6.PLAY_LOOP_PROOF.001: play loop proof report scaffold v0
    public const string PlayLoopProofReportV0 = "PLAY_LOOP_PROOF_REPORT_V0";

    // GATE.S5.ESCORT_PROG.MODEL.001: escort and patrol program kinds v0
    public const string EscortV0 = "ESCORT_V0";
    public const string PatrolV0 = "PATROL_V0";

    // GATE.T41.SURVEY_PROG.MODEL.001: survey program kind v0
    public const string SurveyV0 = "SURVEY_V0";

    // GATE.EXTRACT.FRACTURE_PROGRAM.001: fracture extraction program kind v0
    public const string FractureExtractionV0 = "FRACTURE_EXTRACTION_V0";
}
