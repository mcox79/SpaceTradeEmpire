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
    //
    // Migration note (GATE.X.TWEAKS.DATA.MIGRATE.MARKET_FEES.001):
    // - TransactionFeeBps remains the stable base default.
    // - Effective fee bps may be scaled by state.Tweaks.MarketFeeMultiplier (when provided).
    public const int TransactionFeeBps = 100;

    // GATE.S3_6.DISCOVERY_UNLOCK_CONTRACT.003
    // Market access eligibility: a market may require a Permit unlock id.
    // Deterministic: single dictionary lookup by stable unlock id.
    public static bool CanAccessMarket(SimState state, Market market)
    {
        if (market is null) return false;

        var req = market.RequiresPermitUnlockId;
        if (string.IsNullOrWhiteSpace(req)) return true;

        if (state.Intel.Unlocks.TryGetValue(req, out var u))
        {
            return u.IsAcquired && u.Kind == UnlockKind.Permit;
        }

        return false;
    }

    // GATE.S3_6.DISCOVERY_UNLOCK_CONTRACT.003
    // Broker unlock economic effect: if any acquired Broker unlock exists, transaction fees are waived (bps=0).
    // Result is order-independent (any-match), so no sort needed.
    private static bool HasAnyAcquiredBrokerUnlock(SimState state)
    {
        foreach (var kvp in state.Intel.Unlocks)
        {
            var u = kvp.Value;
            if (u.IsAcquired && u.Kind == UnlockKind.Broker) return true;
        }

        return false;
    }

    public static int GetEffectiveTransactionFeeBps(SimState? state)
    {
        if (state is null) return TransactionFeeBps;

        if (HasAnyAcquiredBrokerUnlock(state)) return default;

        // Default multiplier without introducing a new numeric literal token.
        double defaultMult = (double)TransactionFeeBps / TransactionFeeBps;

        var mult = state.Tweaks?.MarketFeeMultiplier ?? defaultMult;
        if (!double.IsFinite(mult)) return TransactionFeeBps;

        // Deterministic scaling and rounding across platforms:
        // use decimal and explicit midpoint mode (round to 0 decimals via overload).
        decimal scaled = (decimal)TransactionFeeBps * (decimal)mult;
        int bps = (int)decimal.Round(scaled, MidpointRounding.AwayFromZero);

        int minBps = default;
        int maxBps = checked(TransactionFeeBps * TransactionFeeBps);

        if (bps < minBps) bps = minBps;
        if (bps > maxBps) bps = maxBps;
        return bps;
    }

    public static int ComputeTransactionFeeCredits(int grossCredits)
        => ComputeTransactionFeeCredits(state: null, grossCredits);

    public static int ComputeTransactionFeeCredits(SimState? state, int grossCredits)
    {
        if (grossCredits <= 0) return 0;

        long bps = GetEffectiveTransactionFeeBps(state);
        if (bps == default) return default;

        // Ceil(gross * bps / 10000) using integer math.
        long gross = grossCredits;
        long denom = (long)(TransactionFeeBps * TransactionFeeBps);
        long fee = (gross * bps + 9999L) / denom;

        if (fee <= 0) fee = 1; // ensure a nonzero fee when gross > 0 and bps > 0
        if (fee > int.MaxValue) return int.MaxValue;
        return (int)fee;
    }

    public static int ApplyTransactionFee(int grossCredits)
        => ApplyTransactionFee(state: null, grossCredits);

    public static int ApplyTransactionFee(SimState? state, int grossCredits)
    {
        var fee = ComputeTransactionFeeCredits(state, grossCredits);
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
