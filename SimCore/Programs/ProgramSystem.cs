using System;
using System.Linq;
using SimCore.Intents;

namespace SimCore.Programs;

public static class ProgramSystem
{
    public static void Process(SimState state)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));
        if (state.Programs is null) return;
        if (state.Programs.Instances.Count == 0) return;

        var tick = state.Tick;

        // Deterministic ordering by program id
        foreach (var kv in state.Programs.Instances.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            var p = kv.Value;
            if (p is null) continue;

            if (!p.IsRunnableAt(tick)) continue;

            // Execute: emit intents only, never mutate ledgers directly.
            var qty = p.Quantity;

            if (qty > 0 && !string.IsNullOrWhiteSpace(p.MarketId) && !string.IsNullOrWhiteSpace(p.GoodId))
            {
                if (string.Equals(p.Kind, ProgramKind.AutoBuy, StringComparison.Ordinal))
                {
                    state.EnqueueIntent(new BuyIntent(p.MarketId, p.GoodId, qty));
                }
                else if (string.Equals(p.Kind, ProgramKind.AutoSell, StringComparison.Ordinal))
                {
                    state.EnqueueIntent(new SellIntent(p.MarketId, p.GoodId, qty));
                }
            }

            p.LastRunTick = tick;

            // Prevent runaway loops if cadence is invalid
            var cadence = p.CadenceTicks <= 0 ? 1 : p.CadenceTicks;
            p.NextRunTick = checked(tick + cadence);
        }
    }
}
