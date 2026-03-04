#nullable enable

using Godot;
using SimCore;
using SimCore.Content;
using SimCore.Gen;
using SimCore.Commands;
using SimCore.Intents;
using SimCore.Systems;
using SimCore.Programs;
using SimCore.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

namespace SpaceTradeEmpire.Bridge;

// GATE.S4.TECH.BRIDGE.001: SimBridge.Research partial — tech tree queries + start research intent.
public partial class SimBridge
{
    private Godot.Collections.Array _cachedTechTreeV0 = new Godot.Collections.Array();
    private Godot.Collections.Dictionary _cachedResearchStatusV0 = new Godot.Collections.Dictionary();

    /// <summary>
    /// Returns all techs with unlock status and prerequisites.
    /// [{tech_id, display_name, description, research_ticks, credit_cost, unlocked, prerequisites_met, prerequisites}]
    /// </summary>
    public Godot.Collections.Array GetTechTreeV0()
    {
        TryExecuteSafeRead(state =>
        {
            var arr = new Godot.Collections.Array();
            foreach (var tech in TechContentV0.AllTechs)
            {
                var d = new Godot.Collections.Dictionary
                {
                    ["tech_id"] = tech.TechId,
                    ["display_name"] = tech.DisplayName,
                    ["description"] = tech.Description,
                    ["research_ticks"] = tech.ResearchTicks,
                    ["credit_cost"] = tech.CreditCost,
                    ["unlocked"] = state.Tech.UnlockedTechIds.Contains(tech.TechId),
                    ["prerequisites_met"] = TechContentV0.PrerequisitesMet(tech.TechId, state.Tech.UnlockedTechIds),
                };
                var prereqArr = new Godot.Collections.Array();
                foreach (var p in tech.Prerequisites)
                    prereqArr.Add(p);
                d["prerequisites"] = prereqArr;
                arr.Add(d);
            }
            lock (_snapshotLock) { _cachedTechTreeV0 = arr; }
        }, 0);

        lock (_snapshotLock) { return _cachedTechTreeV0; }
    }

    /// <summary>
    /// Returns current research status: {researching, tech_id, progress_ticks, total_ticks, progress_pct}
    /// </summary>
    public Godot.Collections.Dictionary GetResearchStatusV0()
    {
        TryExecuteSafeRead(state =>
        {
            var d = new Godot.Collections.Dictionary
            {
                ["researching"] = state.Tech.IsResearching,
                ["tech_id"] = state.Tech.CurrentResearchTechId,
                ["progress_ticks"] = state.Tech.ResearchProgressTicks,
                ["total_ticks"] = state.Tech.ResearchTotalTicks,
                ["progress_pct"] = state.Tech.ResearchTotalTicks > 0
                    ? (state.Tech.ResearchProgressTicks * 100) / state.Tech.ResearchTotalTicks
                    : 0,
                ["credits_spent"] = state.Tech.ResearchCreditsSpent,
            };
            lock (_snapshotLock) { _cachedResearchStatusV0 = d; }
        }, 0);

        lock (_snapshotLock) { return _cachedResearchStatusV0; }
    }

    /// <summary>
    /// Starts research on a tech. Returns {success, reason}.
    /// </summary>
    public Godot.Collections.Dictionary StartResearchV0(string techId)
    {
        var result = new Godot.Collections.Dictionary { ["success"] = false, ["reason"] = "" };
        _stateLock.EnterWriteLock();
        try
        {
            var r = ResearchSystem.StartResearch(_kernel.State, techId);
            result["success"] = r.Success;
            result["reason"] = r.Reason;
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }
        return result;
    }
}
