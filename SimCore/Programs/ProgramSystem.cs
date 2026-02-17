using System;
using System.Linq;
using SimCore.Intents;
using SimCore.Events;

namespace SimCore.Programs;

public static class ProgramSystem
{
    // Hook for Program vs ManualOverride interaction. Must remain deterministic.
    private static void ApplyManualOverrideInteractions(SimState state, long tick)
    {
        if (state is null) return;
        if (state.LogisticsEventLog is null) return;
        if (state.LogisticsEventLog.Count == 0) return;

        // Deterministic: collect fleets that had ManualOverrideSet at this tick.
        // We avoid string parsing in Note and rely on the schema-bound event type.
        var affectedFleets = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);

        foreach (var e in state.LogisticsEventLog)
        {
            if (e is null) continue;
            if (e.Tick != tick) continue;
            if (e.Type != LogisticsEvents.LogisticsEventType.ManualOverrideSet) continue;

            var fid = e.FleetId ?? "";
            if (!string.IsNullOrWhiteSpace(fid)) affectedFleets.Add(fid);
        }

        if (affectedFleets.Count == 0) return;

        if (state.Programs is null) return;
        if (state.Programs.Instances is null) return;

        // Policy: ManualOverride takes authority for the affected fleet only.
        // Pause only programs explicitly bound to that fleet.
        foreach (var kv in state.Programs.Instances.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            var p = kv.Value;
            if (p is null) continue;

            var pfid = p.FleetId ?? "";
            if (string.IsNullOrWhiteSpace(pfid)) continue;

            if (p.Status == ProgramStatus.Running && affectedFleets.Contains(pfid))
            {
                p.Status = ProgramStatus.Paused;
            }
        }
    }
    public static void Process(SimState state)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));
        if (state.Programs is null) return;
        if (state.Programs.Instances.Count == 0) return;

        var tick = state.Tick;

        ApplyManualOverrideInteractions(state, tick);

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
