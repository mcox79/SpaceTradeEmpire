namespace SimCore;

// GATE.X.LEDGER.TX_MODEL.001: Economic transaction record for audit trail.
public sealed class TransactionRecord
{
    public int Tick { get; set; }
    public int CashDelta { get; set; }        // Positive = income, negative = expense
    public string GoodId { get; set; } = "";
    public int Quantity { get; set; }
    public string Source { get; set; } = "";   // "Buy", "Sell", "MissionReward", "Sustain"
    public string NodeId { get; set; } = "";   // Market/node where transaction occurred
    // GATE.X.LEDGER.COST_BASIS.001: Realized profit on sell (sell revenue - cost basis). 0 on buys.
    public int ProfitDelta { get; set; }
}
