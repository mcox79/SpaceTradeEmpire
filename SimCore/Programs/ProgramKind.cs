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
}
