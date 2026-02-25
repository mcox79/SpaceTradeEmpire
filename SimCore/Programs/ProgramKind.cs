namespace SimCore.Programs;

public static class ProgramKind
{
    public const string AutoBuy = "AUTO_BUY";
    public const string AutoSell = "AUTO_SELL";

    // GATE.S4.CONSTR_PROG.001: construction programs v0
    // Program supplies missing stage inputs to drive the minimal construction loop that produces cap_module.
    public const string ConstrCapModuleV0 = "CONSTR_CAP_MODULE_V0";
}
