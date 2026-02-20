using SimCore.Entities;

namespace SimCore.Systems;

public static class MarketSystem
{
    // GATE.MKT.002: publish cadence every 12 game hours.
    // With 1 tick = 1 sim minute, 12 hours = 720 minutes = 720 ticks.
    public const int PublishWindowTicks = 720;

    // GATE.S3.MARKET_ARB.001: transaction fee friction v0.
    // Deterministic integer math. Fee is applied to credit amounts (gross) in basis points.
    // Example: 100 bps = 1.00% fee.
    public const int TransactionFeeBps = 100;

    public static int ComputeTransactionFeeCredits(int grossCredits)
    {
        if (grossCredits <= 0) return 0;

        // Ceil(gross * bps / 10000) using integer math.
        long gross = grossCredits;
        long bps = TransactionFeeBps;
        long fee = (gross * bps + 9999L) / 10000L;

        if (fee <= 0) fee = 1; // ensure a nonzero fee when gross > 0
        if (fee > int.MaxValue) return int.MaxValue;
        return (int)fee;
    }

    public static int ApplyTransactionFee(int grossCredits)
    {
        var fee = ComputeTransactionFeeCredits(grossCredits);
        var net = grossCredits - fee;
        return net < 0 ? 0 : net;
    }

    public static void Process(SimState state)
    {
        // 1. Decay Edge Heat (Cooling)
        foreach (var edge in state.Edges.Values)
        {
            if (edge.Heat > 0)
            {
                edge.Heat -= 0.05f;
                if (edge.Heat < 0) edge.Heat = 0;
            }
        }

        // 2. Publish prices on cadence (deterministic, once per bucket)
        foreach (var market in state.Markets.Values)
        {
            market.PublishPricesIfDue(state.Tick, PublishWindowTicks);
        }
    }

    // Called when a Fleet traverses an Edge with Cargo
    public static void RegisterTraffic(SimState state, string edgeId, int cargoVolume)
    {
        if (state.Edges.TryGetValue(edgeId, out var edge))
        {
            // Heat generated per unit of cargo
            edge.Heat += cargoVolume * 0.01f;
        }
    }
}
