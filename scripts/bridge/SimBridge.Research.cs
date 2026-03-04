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

    // GATE.S4.TECH_INDUSTRIALIZE.BRIDGE_DEPTH.001
    public int GetTechTierV0()
    {
        int level = 0;
        TryExecuteSafeRead(state => { level = state.Tech.TechLevel; });
        return level;
    }

    public Godot.Collections.Array<Godot.Collections.Dictionary> GetTechRequirementsV0(string techId)
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        var def = TechContentV0.GetById(techId);
        if (def == null) return result;
        TryExecuteSafeRead(state =>
        {
            foreach (var prereq in def.Prerequisites)
            {
                var row = new Godot.Collections.Dictionary();
                row["tech_id"] = prereq;
                row["met"] = state.Tech.UnlockedTechIds.Contains(prereq);
                var prereqDef = TechContentV0.GetById(prereq);
                row["display_name"] = prereqDef?.DisplayName ?? prereq;
                result.Add(row);
            }
        });
        return result;
    }

    // GATE.S4.UI_INDU.WHY_BLOCKED.001
    public string GetResearchBlockReasonV0(string techId)
    {
        string reason = "";
        var def = TechContentV0.GetById(techId);
        if (def == null) return "unknown_tech";
        TryExecuteSafeRead(state =>
        {
            if (state.Tech.UnlockedTechIds.Contains(techId)) { reason = "already_unlocked"; return; }
            if (state.Tech.IsResearching) { reason = "already_researching"; return; }
            if (def.Tier > state.Tech.TechLevel + 1) { reason = "tier_locked:" + def.Tier; return; }
            if (!TechContentV0.PrerequisitesMet(techId, state.Tech.UnlockedTechIds))
            {
                foreach (var prereq in def.Prerequisites)
                {
                    if (!state.Tech.UnlockedTechIds.Contains(prereq))
                    {
                        var prereqDef = TechContentV0.GetById(prereq);
                        reason = "missing_prereq:" + (prereqDef?.DisplayName ?? prereq);
                        return;
                    }
                }
            }
        });
        return reason;
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
