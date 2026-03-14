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
        sb.Append($"Tick:{Tick}|Cred:{PlayerCredits}|Loc:{PlayerLocationNodeId}|GR:{(int)GameResultValue}|");

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

        // GATE.S7.TERRITORY.HYSTERESIS.001: Committed regime in signature for determinism.
        if (NodeRegimeCommitted is not null && NodeRegimeCommitted.Count > 0)
        {
            foreach (var kv in NodeRegimeCommitted.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                sb.Append($"NRC:{kv.Key}:{kv.Value}|");
            }
        }

        // GATE.S7.WARFRONT.STATE_MODEL.001: Warfront state in signature for determinism.
        if (Warfronts is not null && Warfronts.Count > 0)
        {
            foreach (var kv in Warfronts.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                var w = kv.Value;
                sb.Append($"WF:{kv.Key}|A:{w.CombatantA}|B:{w.CombatantB}|I:{(int)w.Intensity}|T:{(int)w.WarType}|Ts:{w.TickStarted}|FSA:{w.FleetStrengthA}|FSB:{w.FleetStrengthB}|");
                // GATE.S7.WARFRONT.OBJECTIVES.001: Objective state in signature.
                if (w.Objectives is not null && w.Objectives.Count > 0)
                {
                    foreach (var obj in w.Objectives)
                    {
                        sb.Append($"OBJ:{obj.NodeId}|OT:{(int)obj.Type}|OC:{obj.ControllingFactionId}|OD:{obj.DominanceTicks}|OF:{obj.DominantFactionId}|");
                    }
                }
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

        // GATE.S7.DIPLOMACY.FRAMEWORK.001: Diplomatic acts in signature.
        if (DiplomaticActs is not null && DiplomaticActs.Count > 0)
        {
            foreach (var kv in DiplomaticActs.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                sb.Append($"DA:{kv.Key}|T:{(int)kv.Value.ActType}|S:{(int)kv.Value.Status}|F:{kv.Value.FactionId}|");
            }
        }

        // GATE.S9.SYSTEMIC.TRIGGER_ENGINE.001: Systemic offers in signature.
        if (SystemicOffers is not null && SystemicOffers.Count > 0)
        {
            foreach (var o in SystemicOffers.OrderBy(o => o.OfferId, StringComparer.Ordinal))
            {
                sb.Append($"SYS:{o.OfferId}|{(int)o.TriggerType}|{o.NodeId}|{o.GoodId}|{o.ExpiryTick}|");
            }
        }

        foreach (var e in Edges.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            if (e.Value.Heat > 0.001f) sb.Append($"E_Ht:{e.Key}:{e.Value.Heat.ToString("F2", CultureInfo.InvariantCulture)}|");
        }

        // GATE.T18.NARRATIVE.ENTITIES.001: Narrative state in signature.
        if (StationMemory is not null && StationMemory.Count > 0)
        {
            foreach (var kv in StationMemory.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                sb.Append($"SM:{kv.Key}:{kv.Value.TotalDeliveries}:{kv.Value.TotalQuantity}|");
            }
        }
        if (WarConsequences is not null && WarConsequences.Count > 0)
        {
            foreach (var kv in WarConsequences.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                sb.Append($"WC:{kv.Key}:{(kv.Value.IsResolved ? 1 : 0)}|");
            }
        }
        if (FirstOfficer is not null)
        {
            sb.Append($"FO:{(FirstOfficer.IsPromoted ? 1 : 0)}:{(int)FirstOfficer.CandidateType}:{(int)FirstOfficer.Tier}|");
        }
        if (NarrativeNpcs is not null && NarrativeNpcs.Count > 0)
        {
            foreach (var kv in NarrativeNpcs.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                sb.Append($"NPC:{kv.Key}:{(kv.Value.IsAlive ? 1 : 0)}|");
            }
        }
        sb.Append($"FEJ:{FractureExposureJumps}|");

        // GATE.S8.HAVEN.ENTITY.001: Haven state in signature.
        if (Haven is not null && Haven.Discovered)
        {
            sb.Append($"HVN:{(int)Haven.Tier}|UT:{Haven.UpgradeTicksRemaining}|KT:{(int)Haven.KeeperLevel}|EMD:{Haven.ExoticMatterDelivered}|DLD:{Haven.DataLogsDiscovered}|");
            if (Haven.StoredShipIds is not null && Haven.StoredShipIds.Count > 0)
            {
                sb.Append("HS:");
                foreach (var sid in Haven.StoredShipIds)
                    sb.Append($"{sid},");
                sb.Append("|");
            }
            // GATE.S8.HAVEN.TROPHY_WALL.001: Trophy wall in signature.
            if (Haven.TrophyWall is not null && Haven.TrophyWall.Count > 0)
            {
                sb.Append("TW:");
                foreach (var kv in Haven.TrophyWall.OrderBy(k => k.Key, StringComparer.Ordinal))
                    sb.Append($"{kv.Key}={kv.Value},");
                sb.Append("|");
            }
            // GATE.S8.HAVEN.FABRICATOR.001: Fabrication state in signature.
            if (!string.IsNullOrEmpty(Haven.FabricatingModuleId))
                sb.Append($"FAB:{Haven.FabricatingModuleId}|FTR:{Haven.FabricationTicksRemaining}|");
            if (Haven.CompletedFabricationIds is not null && Haven.CompletedFabricationIds.Count > 0)
            {
                sb.Append("CFAB:");
                foreach (var mid in Haven.CompletedFabricationIds)
                    sb.Append($"{mid},");
                sb.Append("|");
            }
            // GATE.S8.HAVEN.RESEARCH_LAB.001: Research lab slots in signature.
            if (Haven.ResearchLabSlots is not null && Haven.ResearchLabSlots.Count > 0)
            {
                foreach (var slot in Haven.ResearchLabSlots)
                {
                    if (slot.IsActive)
                        sb.Append($"HRL:{slot.SlotIndex}|T:{slot.TechId}|P:{slot.ProgressTicks}/{slot.TotalTicks}|");
                }
            }
        }

        // GATE.S8.MEGAPROJECT.ENTITY.001: Megaproject state in signature.
        if (Megaprojects is not null && Megaprojects.Count > 0)
        {
            foreach (var kv in Megaprojects.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                var mp = kv.Value;
                sb.Append($"MP:{kv.Key}|T:{mp.TypeId}|N:{mp.NodeId}|S:{mp.Stage}/{mp.MaxStages}|P:{mp.ProgressTicks}|C:{mp.CompletedTick}|M:{(mp.MutationApplied ? 1 : 0)}|");
            }
        }

        // GATE.S8.MEGAPROJECT.MAP_RULES.001: Sensor pylon nodes in signature.
        if (SensorPylonNodes is not null && SensorPylonNodes.Count > 0)
        {
            sb.Append("SPN:");
            foreach (var nid in SensorPylonNodes.OrderBy(x => x, StringComparer.Ordinal))
                sb.Append($"{nid},");
            sb.Append("|");
        }

        // GATE.S8.ADAPTATION.COLLECTION.001: Adaptation fragment collection in signature.
        if (AdaptationFragments is not null && AdaptationFragments.Count > 0)
        {
            sb.Append("AF:");
            foreach (var kv in AdaptationFragments.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                if (kv.Value.IsCollected)
                    sb.Append($"{kv.Key}:{kv.Value.CollectedTick},");
            }
            sb.Append("|");
        }

        // GATE.S8.STORY_STATE.ENTITY.001: Story state in signature.
        if (StoryState is not null)
        {
            sb.Append($"SS:{(int)StoryState.RevealedFlags}|Act:{(int)StoryState.CurrentAct}|PTF:{StoryState.PentagonTradeFlags}|FXC:{StoryState.FractureExposureCount}|LVC:{StoryState.LatticeVisitCount}|FC:{StoryState.CollectedFragmentCount}|");
            // GATE.S8.PENTAGON.DETECT.001: Pentagon cascade state in signature.
            if (StoryState.PentagonCascadeActive)
                sb.Append($"PCA:{(StoryState.PentagonCascadeActive ? 1 : 0)}|PCT:{StoryState.PentagonCascadeTick}|");
        }

        // GATE.S8.HAVEN.ENDGAME_PATHS.001: Endgame path + accommodation in signature.
        if (Haven is not null && Haven.Discovered)
        {
            if (Haven.ChosenEndgamePath != Entities.EndgamePath.None)
                sb.Append($"EP:{(int)Haven.ChosenEndgamePath}|EPT:{Haven.EndgamePathChosenTick}|");
            if (Haven.AccommodationProgress is not null && Haven.AccommodationProgress.Count > 0)
            {
                sb.Append("ACC:");
                foreach (var kv in Haven.AccommodationProgress.OrderBy(k => k.Key, StringComparer.Ordinal))
                    sb.Append($"{kv.Key}={kv.Value},");
                sb.Append("|");
            }
            // GATE.S8.HAVEN.COMMUNION_REP.001: Communion rep state in signature.
            if (Haven.CommunionRep is not null && Haven.CommunionRep.Present)
                sb.Append($"CR:{(Haven.CommunionRep.Present ? 1 : 0)}|CRD:{Haven.CommunionRep.DialogueTier}|");
        }

        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(bytes);
    }
}
