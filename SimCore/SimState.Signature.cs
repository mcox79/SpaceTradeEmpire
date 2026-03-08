using System.Text;
using System.Security.Cryptography;
using System.Linq;
using System.Globalization;

namespace SimCore;

public partial class SimState
{
    public string GetSignature()
    {
        var sb = new StringBuilder();
        sb.Append($"Tick:{Tick}|Cred:{PlayerCredits}|Loc:{PlayerLocationNodeId}|");

        sb.Append($"Nodes:{Nodes.Count}|Edges:{Edges.Count}|Markets:{Markets.Count}|Fleets:{Fleets.Count}|Sites:{IndustrySites.Count}|");

        foreach (var f in Fleets.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            sb.Append($"Flt:{f.Key}_N:{f.Value.CurrentNodeId}_S:{f.Value.State}_D:{f.Value.DestinationNodeId}|");

            // Include cargo deterministically (keys sorted, stable formatting).
            if (f.Value.Cargo is not null && f.Value.Cargo.Count > 0)
            {
                sb.Append("Cargo:");
                foreach (var kv in f.Value.Cargo.OrderBy(kv => kv.Key, StringComparer.Ordinal))
                {
                    sb.Append($"{kv.Key}:{kv.Value},");
                }
                sb.Append("|");
            }
        }

        foreach (var m in Markets.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            sb.Append($"Mkt:{m.Key}|");
            foreach (var kv in m.Value.Inventory.OrderBy(i => i.Key, StringComparer.Ordinal))
            {
                sb.Append($"{kv.Key}:{kv.Value},");
            }
            sb.Append("|");
        }

        if (LogisticsReservations is not null && LogisticsReservations.Count > 0)
        {
            foreach (var kv in LogisticsReservations.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                var r = kv.Value;
                sb.Append($"Res:{kv.Key}|M:{r.MarketId}|G:{r.GoodId}|F:{r.FleetId}|Rem:{r.Remaining}|");
            }
        }

        if (Programs is not null && Programs.Instances is not null && Programs.Instances.Count > 0)
        {
            foreach (var kv in Programs.Instances.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                var p = kv.Value;
                sb.Append($"Prog:{p.Id}|K:{p.Kind}|S:{p.Status}|Cad:{p.CadenceTicks}|Nx:{p.NextRunTick}|Ls:{p.LastRunTick}|Site:{p.SiteId}|M:{p.MarketId}|G:{p.GoodId}|Q:{p.Quantity}|");
            }
        }

        if (Intel is not null && Intel.Observations is not null && Intel.Observations.Count > 0)
        {
            foreach (var kv in Intel.Observations.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                var obs = kv.Value;
                sb.Append($"Intel:{kv.Key}@{obs.ObservedTick}={obs.ObservedInventoryQty}|");
            }
        }

        foreach (var s in IndustrySites.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            // Include tech sustainment state so determinism drift cannot hide.
            sb.Append($"Site:{s.Key}|Eff:{s.Value.Efficiency.ToString("F4", CultureInfo.InvariantCulture)}|Health:{s.Value.HealthBps}|BufD:{s.Value.BufferDays}|Rem:{s.Value.DegradeRemainder}|");
        }

        // GATE.S4.INDU.MIN_LOOP.001
        // Include persisted construction state in signature so save%load%replay drift cannot hide.
        if (IndustryBuilds is not null && IndustryBuilds.Count > 0)
        {
            foreach (var kv in IndustryBuilds.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                var b = kv.Value;
                if (b is null) continue;
                sb.Append($"IB:{kv.Key}|A:{(b.Active ? 1 : 0)}|R:{b.RecipeId}|Si:{b.StageIndex}|Sn:{b.StageName}|Rem:{b.StageTicksRemaining}|Blk:{b.BlockerReason}|Act:{b.SuggestedAction}|");
            }
        }

        // GATE.S1.MISSION.MODEL.001: Mission state in signature for determinism.
        if (Missions is not null)
        {
            if (!string.IsNullOrEmpty(Missions.ActiveMissionId))
            {
                sb.Append($"Mission:{Missions.ActiveMissionId}|Step:{Missions.CurrentStepIndex}|");
                foreach (var step in Missions.ActiveSteps)
                {
                    sb.Append($"MS:{step.StepIndex}:{(step.Completed ? 1 : 0)}|");
                }
            }
            if (Missions.CompletedMissionIds is not null && Missions.CompletedMissionIds.Count > 0)
            {
                foreach (var mId in Missions.CompletedMissionIds.OrderBy(x => x, StringComparer.Ordinal))
                {
                    sb.Append($"MComp:{mId}|");
                }
            }
        }

        // GATE.S7.FACTION.REPUTATION_SYS.001: Include faction reputation in signature.
        if (FactionReputation is not null && FactionReputation.Count > 0)
        {
            foreach (var kv in FactionReputation.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                sb.Append($"FRep:{kv.Key}:{kv.Value}|");
            }
        }

        foreach (var n in Nodes.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            if (n.Value.Trace > 0.001f) sb.Append($"N_Tr:{n.Key}:{n.Value.Trace.ToString("F2", CultureInfo.InvariantCulture)}|");
            // GATE.S7.INSTABILITY.PHASE_MODEL.001: Include instability in signature.
            if (n.Value.InstabilityLevel > 0) sb.Append($"N_Inst:{n.Key}:{n.Value.InstabilityLevel}|");
        }

        // GATE.S7.WARFRONT.STATE_MODEL.001: Warfront state in signature for determinism.
        if (Warfronts is not null && Warfronts.Count > 0)
        {
            foreach (var kv in Warfronts.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                var w = kv.Value;
                sb.Append($"WF:{kv.Key}|A:{w.CombatantA}|B:{w.CombatantB}|I:{(int)w.Intensity}|T:{(int)w.WarType}|Ts:{w.TickStarted}|");
            }
        }

        // GATE.S7.SUPPLY.DELIVERY_LEDGER.001: Supply ledger in signature.
        if (WarSupplyLedger is not null && WarSupplyLedger.Count > 0)
        {
            foreach (var wfKv in WarSupplyLedger.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                foreach (var gKv in wfKv.Value.OrderBy(k => k.Key, StringComparer.Ordinal))
                {
                    sb.Append($"WSL:{wfKv.Key}:{gKv.Key}:{gKv.Value}|");
                }
            }
        }

        // GATE.S7.TERRITORY.EMBARGO_MODEL.001: Embargo state in signature.
        if (Embargoes is not null && Embargoes.Count > 0)
        {
            foreach (var e in Embargoes.OrderBy(e => e.Id, StringComparer.Ordinal))
            {
                sb.Append($"EMB:{e.Id}|F:{e.EnforcingFactionId}|G:{e.GoodId}|");
            }
        }

        foreach (var e in Edges.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            if (e.Value.Heat > 0.001f) sb.Append($"E_Ht:{e.Key}:{e.Value.Heat.ToString("F2", CultureInfo.InvariantCulture)}|");
        }

        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(bytes);
    }
}
