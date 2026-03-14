#nullable enable

using Godot;
using SimCore;
using SimCore.Entities;

namespace SpaceTradeEmpire.Bridge;

public partial class SimBridge
{
    // GATE.S8.STORY_STATE.BRIDGE.001: Story state queries.

    // GetRevelationStateV0: Returns revelation flags, current act, revelation count,
    // per-revelation boolean presence flags, and tick-of-firing for each revelation.
    // Nonblocking: returns last cached dict if read lock is unavailable.
    private Godot.Collections.Dictionary _cachedRevelationStateV0 = new();

    public Godot.Collections.Dictionary GetRevelationStateV0()
    {
        TryExecuteSafeRead(state =>
        {
            var story = state.StoryState;

            var d = new Godot.Collections.Dictionary
            {
                ["revealed_flags"]    = (int)story.RevealedFlags,
                ["current_act"]       = (int)story.CurrentAct,
                ["revelation_count"]  = story.RevelationCount,

                ["has_r1"] = story.HasRevelation(RevelationFlags.R1_Module),
                ["has_r2"] = story.HasRevelation(RevelationFlags.R2_Concord),
                ["has_r3"] = story.HasRevelation(RevelationFlags.R3_Pentagon),
                ["has_r4"] = story.HasRevelation(RevelationFlags.R4_Communion),
                ["has_r5"] = story.HasRevelation(RevelationFlags.R5_Instability),

                ["r1_tick"] = story.R1Tick,
                ["r2_tick"] = story.R2Tick,
                ["r3_tick"] = story.R3Tick,
                ["r4_tick"] = story.R4Tick,
                ["r5_tick"] = story.R5Tick,
            };

            // GATE.S8.PENTAGON.DELIVERY.001: Determine latest fired revelation for toast delivery.
            string latestId = "";
            int latestTick = -1;
            if (story.HasRevelation(RevelationFlags.R1_Module) && story.R1Tick > latestTick) { latestId = "R1"; latestTick = story.R1Tick; }
            if (story.HasRevelation(RevelationFlags.R2_Concord) && story.R2Tick > latestTick) { latestId = "R2"; latestTick = story.R2Tick; }
            if (story.HasRevelation(RevelationFlags.R3_Pentagon) && story.R3Tick > latestTick) { latestId = "R3"; latestTick = story.R3Tick; }
            if (story.HasRevelation(RevelationFlags.R4_Communion) && story.R4Tick > latestTick) { latestId = "R4"; latestTick = story.R4Tick; }
            if (story.HasRevelation(RevelationFlags.R5_Instability) && story.R5Tick > latestTick) { latestId = "R5"; }
            d["latest_revelation_id"] = latestId;

            lock (_snapshotLock)
            {
                _cachedRevelationStateV0 = d;
            }
        }, 0);

        lock (_snapshotLock) { return _cachedRevelationStateV0; }
    }

    // GATE.S8.PENTAGON.DELIVERY.001: Revelation display text for gold toast delivery.
    private static readonly System.Collections.Generic.Dictionary<string, (string Title, string Body)> RevelationTexts = new()
    {
        { "R1", ("MODULE ANALYSIS COMPLETE", "Your ship modules contain technology of unknown origin.") },
        { "R2", ("CONCORD DIPLOMATIC CHANNEL", "The Concord reveals classified information about the lane network.") },
        { "R3", ("TRADE PATTERN DETECTED", "The pentagon dependency has been mapped. Economic consequences are cascading.") },
        { "R4", ("COMMUNION REVELATION", "The Communion shares the truth about the Lattice.") },
        { "R5", ("INSTABILITY THRESHOLD", "The galaxy's structural integrity is failing. The endgame approaches.") },
    };

    public Godot.Collections.Dictionary GetRevelationTextV0(string revelationId)
    {
        var result = new Godot.Collections.Dictionary
        {
            ["gold_toast_title"] = "REVELATION",
            ["gold_toast_body"] = "",
        };

        if (!string.IsNullOrEmpty(revelationId) && RevelationTexts.TryGetValue(revelationId, out var text))
        {
            result["gold_toast_title"] = text.Title;
            result["gold_toast_body"] = text.Body;
        }

        return result;
    }

    // GetStoryProgressV0: Returns raw progress counters used to evaluate revelation
    // conditions — pentagon trade flags, fracture exposure, lattice visits, fragment count,
    // and communion log read flag.
    // Nonblocking: returns last cached dict if read lock is unavailable.
    private Godot.Collections.Dictionary _cachedStoryProgressV0 = new();

    public Godot.Collections.Dictionary GetStoryProgressV0()
    {
        TryExecuteSafeRead(state =>
        {
            var story = state.StoryState;

            var d = new Godot.Collections.Dictionary
            {
                ["pentagon_trade_flags"]      = story.PentagonTradeFlags,
                ["all_pentagon_traded"]        = story.AllPentagonFactionsTraded,
                ["fracture_exposure_count"]    = story.FractureExposureCount,
                ["lattice_visit_count"]        = story.LatticeVisitCount,
                ["collected_fragment_count"]   = story.CollectedFragmentCount,
                ["has_read_communion_log"]     = story.HasReadCommunionLog,
            };

            lock (_snapshotLock)
            {
                _cachedStoryProgressV0 = d;
            }
        }, 0);

        lock (_snapshotLock) { return _cachedStoryProgressV0; }
    }

    // GATE.S8.PENTAGON.BRIDGE.001: Pentagon break state snapshot.
    private Godot.Collections.Dictionary _cachedPentagonStateV0 = new();

    public Godot.Collections.Dictionary GetPentagonStateV0()
    {
        TryExecuteSafeRead(state =>
        {
            var story = state.StoryState;
            int flags = story.PentagonTradeFlags;

            var d = new Godot.Collections.Dictionary
            {
                ["pentagon_trade_flags"] = flags,
                ["concord_traded"] = (flags & 0x01) != 0,
                ["chitin_traded"] = (flags & 0x02) != 0,
                ["weavers_traded"] = (flags & 0x04) != 0,
                ["valorin_traded"] = (flags & 0x08) != 0,
                ["communion_traded"] = (flags & 0x10) != 0,
                ["all_traded"] = story.AllPentagonFactionsTraded,
                ["cascade_active"] = story.PentagonCascadeActive,
                ["cascade_tick"] = story.PentagonCascadeTick,
                ["has_r3"] = story.HasRevelation(RevelationFlags.R3_Pentagon),
            };

            lock (_snapshotLock) { _cachedPentagonStateV0 = d; }
        }, 0);

        lock (_snapshotLock) { return _cachedPentagonStateV0; }
    }

    // GATE.S8.PENTAGON.BRIDGE.001: Cascade economic effects snapshot.
    private Godot.Collections.Dictionary _cachedCascadeEffectsV0 = new();

    public Godot.Collections.Dictionary GetCascadeEffectsV0()
    {
        TryExecuteSafeRead(state =>
        {
            var story = state.StoryState;
            int gdpImpactBps = story.PentagonCascadeActive
                ? SimCore.Tweaks.PentagonBreakTweaksV0.CascadeGdpImpactBps
                : 0;

            // Count Communion nodes with food injection.
            int communionNodesAffected = 0;
            if (story.PentagonCascadeActive)
            {
                foreach (var kv in state.NodeFactionId)
                {
                    if (string.Equals(kv.Value, SimCore.Tweaks.FactionTweaksV0.CommunionId, System.StringComparison.Ordinal))
                        communionNodesAffected++;
                }
            }

            var d = new Godot.Collections.Dictionary
            {
                ["cascade_active"] = story.PentagonCascadeActive,
                ["gdp_impact_bps"] = gdpImpactBps,
                ["communion_nodes_affected"] = communionNodesAffected,
                ["food_injection_qty"] = story.PentagonCascadeActive
                    ? SimCore.Tweaks.PentagonBreakTweaksV0.CommunionFoodSelfProductionQty
                    : 0,
                ["food_injection_interval"] = SimCore.Tweaks.PentagonBreakTweaksV0.CascadeFoodIntervalTicks,
            };

            lock (_snapshotLock) { _cachedCascadeEffectsV0 = d; }
        }, 0);

        lock (_snapshotLock) { return _cachedCascadeEffectsV0; }
    }

    // GetPendingRevelationV0: Returns the next revelation likely to fire and rough
    // progress toward it (0-100). Returns "" and 0 when all 5 revelations have fired.
    // Nonblocking: returns last cached dict if read lock is unavailable.
    private Godot.Collections.Dictionary _cachedPendingRevelationV0 = new();

    public Godot.Collections.Dictionary GetPendingRevelationV0()
    {
        TryExecuteSafeRead(state =>
        {
            var story = state.StoryState;
            var tweaks = typeof(SimCore.Tweaks.StoryStateTweaksV0);

            string nextId = "";
            int progressPct = 0;

            if (!story.HasRevelation(RevelationFlags.R1_Module))
            {
                // R1: progress based on fracture exposure (primary gate) and lattice visits.
                nextId = "R1";
                int expThresh = SimCore.Tweaks.StoryStateTweaksV0.R1FractureExposureThreshold;
                int latThresh = SimCore.Tweaks.StoryStateTweaksV0.R1LatticeVisitMinimum;
                int expPct = expThresh > 0
                    ? System.Math.Clamp(story.FractureExposureCount * 100 / expThresh, 0, 100)
                    : 100;
                int latPct = latThresh > 0
                    ? System.Math.Clamp(story.LatticeVisitCount * 100 / latThresh, 0, 100)
                    : 100;
                // Both conditions must be satisfied; report the lower (bottleneck) progress.
                progressPct = System.Math.Min(expPct, latPct);
            }
            else if (!story.HasRevelation(RevelationFlags.R2_Concord))
            {
                // R2: progress based on Concord faction reputation.
                nextId = "R2";
                int repThresh = SimCore.Tweaks.StoryStateTweaksV0.R2ConcordRepThreshold;
                int concordRep = 0;
                // Concord faction id — check reputation map.
                if (state.FactionReputation.TryGetValue("concord", out var rep))
                    concordRep = rep;
                progressPct = repThresh > 0
                    ? System.Math.Clamp(concordRep * 100 / repThresh, 0, 100)
                    : 100;
            }
            else if (!story.HasRevelation(RevelationFlags.R3_Pentagon))
            {
                // R3: progress based on how many of the 5 pentagon factions have been traded.
                nextId = "R3";
                int bitsSet = 0;
                int flags = story.PentagonTradeFlags;
                while (flags != 0) { bitsSet += flags & 1; flags >>= 1; }
                progressPct = System.Math.Clamp(bitsSet * 100 / 5, 0, 100);
            }
            else if (!story.HasRevelation(RevelationFlags.R4_Communion))
            {
                // R4: progress based on fracture exposure and communion log read.
                nextId = "R4";
                int expThresh = SimCore.Tweaks.StoryStateTweaksV0.R4FractureExposureThreshold;
                int expPct = expThresh > 0
                    ? System.Math.Clamp(story.FractureExposureCount * 100 / expThresh, 0, 100)
                    : 100;
                int logPct = story.HasReadCommunionLog ? 100 : 0;
                // Both conditions required; report bottleneck.
                progressPct = System.Math.Min(expPct, logPct);
            }
            else if (!story.HasRevelation(RevelationFlags.R5_Instability))
            {
                // R5: progress based on tick count and collected fragment count.
                nextId = "R5";
                int minTick = SimCore.Tweaks.StoryStateTweaksV0.R5MinimumTick;
                int minFrags = SimCore.Tweaks.StoryStateTweaksV0.R5MinimumFragments;
                int tickPct = minTick > 0
                    ? System.Math.Clamp(state.Tick * 100 / minTick, 0, 100)
                    : 100;
                int fragPct = minFrags > 0
                    ? System.Math.Clamp(story.CollectedFragmentCount * 100 / minFrags, 0, 100)
                    : 100;
                progressPct = System.Math.Min(tickPct, fragPct);
            }
            // else: all 5 fired — nextId stays "" and progressPct stays 0.

            var d = new Godot.Collections.Dictionary
            {
                ["next_revelation"] = nextId,
                ["progress_pct"]    = progressPct,
            };

            lock (_snapshotLock)
            {
                _cachedPendingRevelationV0 = d;
            }
        }, 0);

        lock (_snapshotLock) { return _cachedPendingRevelationV0; }
    }
}
