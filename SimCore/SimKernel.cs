using System.Diagnostics;
using SimCore.Commands;
using SimCore.Systems;
using SimCore.Programs;
using SimCore.Tweaks;

namespace SimCore;

/// <summary>Per-system timing for a single tick.</summary>
public class TickProfile
{
    public string SystemName { get; set; } = "";
    public long ElapsedMicroseconds { get; set; }
}

/// <summary>Aggregated per-system timing over the ring buffer window.</summary>
public class TickProfileEntry
{
    public string SystemName { get; set; } = "";
    public long AvgMicroseconds { get; set; }
    public long MaxMicroseconds { get; set; }
    public long LastMicroseconds { get; set; }
}

public class SimKernel
{
    // STRUCTURAL: Ring buffer capacity for tick profiling (side-channel only, not determinism-affecting).
    private const int ProfileRingBufferSize = 100;

    /// <summary>Enable per-system Stopwatch instrumentation in Step(). Default false.</summary>
    public static bool ProfilingEnabled;

    private static readonly List<TickProfile>[] _profileRingBuffer = new List<TickProfile>[ProfileRingBufferSize];
    private static int _profileWriteIndex;
    private static int _profileCount;

    private SimState _state;
    private Queue<ICommand> _commandQueue = new();
    private Queue<SimCore.Intents.IIntent> _intentQueue = new();
    public SimState State => _state;

    /// <summary>Returns per-system avg/max/last microsecond timings over the ring buffer.</summary>
    public static List<TickProfileEntry> GetTickProfile()
    {
        if (_profileCount == 0)
            return new List<TickProfileEntry>();

        var totals = new Dictionary<string, (long sum, long max, long last, int count)>();
        int start = _profileCount < ProfileRingBufferSize ? 0 : _profileWriteIndex;
        int count = Math.Min(_profileCount, ProfileRingBufferSize);

        for (int i = 0; i < count; i++)
        {
            int idx = (start + i) % ProfileRingBufferSize;
            var tick = _profileRingBuffer[idx];
            if (tick == null) continue;
            bool isLast = (i == count - 1);
            foreach (var p in tick)
            {
                if (totals.TryGetValue(p.SystemName, out var existing))
                {
                    totals[p.SystemName] = (
                        existing.sum + p.ElapsedMicroseconds,
                        Math.Max(existing.max, p.ElapsedMicroseconds),
                        isLast ? p.ElapsedMicroseconds : existing.last,
                        existing.count + 1
                    );
                }
                else
                {
                    totals[p.SystemName] = (p.ElapsedMicroseconds, p.ElapsedMicroseconds, p.ElapsedMicroseconds, 1);
                }
            }
        }

        var result = new List<TickProfileEntry>(totals.Count);
        foreach (var kvp in totals)
        {
            result.Add(new TickProfileEntry
            {
                SystemName = kvp.Key,
                AvgMicroseconds = kvp.Value.sum / kvp.Value.count,
                MaxMicroseconds = kvp.Value.max,
                LastMicroseconds = kvp.Value.last
            });
        }
        return result;
    }

    public SimKernel(int seed, string? tweakConfigJsonOverride = null,
        DifficultyPreset difficulty = DifficultyPreset.Normal,
        string? captainName = null)
    {
        _state = new SimState(seed);

        // Gate: save_seed_identity
        // SimState does not necessarily expose seed as a readable member. Attach it for persistence.
        SerializationSystem.AttachSeed(_state, seed);

        // GATE.X.TWEAKS.DATA.001
        // Deterministic tweak loading (override wins, else stable defaults).
        _state.LoadTweaksFromJsonOverride(tweakConfigJsonOverride);

        // GATE.S7.MAIN_MENU.NEW_VOYAGE.001: Store chosen difficulty in state for save/load persistence.
        _state.Difficulty = difficulty;

        // GATE.S7.MAIN_MENU.CAPTAIN_NAME.001: Store captain name for narrative display.
        if (!string.IsNullOrWhiteSpace(captainName))
            _state.CaptainName = captainName;
    }

    public void EnqueueCommand(ICommand cmd)
    {
        _commandQueue.Enqueue(cmd);
    }

    public void EnqueueIntent(SimCore.Intents.IIntent intent)
    {
        _intentQueue.Enqueue(intent);
    }

    public void Step()
    {
        while (_commandQueue.TryDequeue(out var cmd))
        {
            cmd.Execute(_state);
        }

        while (_intentQueue.TryDequeue(out var intent))
        {
            // Use SimState's deterministic intent wrapper.
            _state.EnqueueIntent(intent);
        }

        // GATE.S15.FEEL.JUMP_EVENT_SYS.001: Clear transient arrival records from previous tick.
        _state.ArrivalsThisTick.Clear();
        // GATE.S16.NPC_ALIVE.FLEET_DESTROY.001: Clear transient destroyed fleet records.
        _state.NpcFleetsDestroyedThisTick.Clear();

        bool profiling = ProfilingEnabled;
        List<TickProfile>? tickProfiles = profiling ? new List<TickProfile>() : null;
        Stopwatch? sw = profiling ? new Stopwatch() : null;

        // Resolve in-flight inventory arrivals first.
        ProfileCall(sw, tickProfiles, "LaneFlowSystem", () => LaneFlowSystem.Process(_state));

        // Programs emit intents only (no direct ledger mutation).
        ProfileCall(sw, tickProfiles, "ProgramSystem", () => ProgramSystem.Process(_state));

        // Convert intents to commands.
        ProfileCall(sw, tickProfiles, "IntentSystem", () => IntentSystem.Process(_state));

        // Apply fleet movement/state transitions.
        ProfileCall(sw, tickProfiles, "MovementSystem", () => MovementSystem.Process(_state));

        // GATE.T18.NARRATIVE.FRACTURE_WEIGHT.001: Cargo weight shift on arrival at stable space.
        ProfileCall(sw, tickProfiles, "FractureWeightSystem", () => FractureWeightSystem.Process(_state));

        // GATE.S7.SUSTAIN.FUEL_DEDUCT.001: Fleet fuel + module sustain deduction.
        ProfileCall(sw, tickProfiles, "SustainSystem", () => SustainSystem.Process(_state));

        // GATE.X.FLEET_UPKEEP.DRAIN.001: Per-cycle fleet upkeep credit drain.
        ProfileCall(sw, tickProfiles, "FleetUpkeepSystem", () => FleetUpkeepSystem.Process(_state));

        // GATE.T48.TENSION.MAINTENANCE.001: Continuous fleet costs — fuel, wages, hull degradation.
        ProfileCall(sw, tickProfiles, "FleetUpkeepSystem.ContinuousCosts", () => FleetUpkeepSystem.ProcessContinuousCosts(_state));

        // GATE.S16.NPC_ALIVE.FLEET_DESTROY.001: Remove destroyed NPC fleets.
        ProfileCall(sw, tickProfiles, "NpcFleetCombatSystem", () => NpcFleetCombatSystem.Process(_state));

        // GATE.S8.LATTICE_DRONES.SPAWN.001: Spawn/despawn lattice drones per instability phase.
        ProfileCall(sw, tickProfiles, "LatticeDroneSpawnSystem", () => LatticeDroneSpawnSystem.Process(_state));

        // GATE.S8.LATTICE_DRONES.COMBAT.001: Drone auto-engage at player node.
        ProfileCall(sw, tickProfiles, "LatticeDroneCombatSystem", () => LatticeDroneCombatSystem.Process(_state));

        // GATE.S5.LOOT.DROP_SYSTEM.001: Despawn expired loot drops.
        ProfileCall(sw, tickProfiles, "LootTableSystem.ProcessDespawn", () => LootTableSystem.ProcessDespawn(_state));

        // GATE.S15.FEEL.JUMP_EVENT_SYS.001: Random events on lane arrival.
        ProfileCall(sw, tickProfiles, "JumpEventSystem", () => JumpEventSystem.Process(_state));

        // Slice 3 / GATE.S3.RISK_MODEL.001
        // Emit deterministic security incidents on lanes%routes (no time sources, no shared RNG coupling).
        ProfileCall(sw, tickProfiles, "RiskSystem", () => RiskSystem.Process(_state));

        // Compute buffer shortages and assign logistics jobs (produces fleet jobs).
        ProfileCall(sw, tickProfiles, "LogisticsSystem", () => LogisticsSystem.Process(_state));

        // Consume upkeep and produce outputs.
        ProfileCall(sw, tickProfiles, "IndustrySystem", () => IndustrySystem.Process(_state));

        // Station population consumption: food + fuel drain per station per cadence.
        ProfileCall(sw, tickProfiles, "StationConsumptionSystem", () => StationConsumptionSystem.Process(_state));

        // GATE.S8.THREAT.SUPPLY_SHOCK.001: Warfront disrupts production chains.
        ProfileCall(sw, tickProfiles, "SupplyShockSystem", () => SupplyShockSystem.Process(_state));

        // GATE.S9.SYSTEMIC.STATION_CONTEXT.001: Per-station economic context.
        ProfileCall(sw, tickProfiles, "StationContextSystem", () => StationContextSystem.Process(_state));

        // GATE.S1.MISSION.SYSTEM.001: Evaluate mission triggers and advance steps.
        ProfileCall(sw, tickProfiles, "MissionSystem", () => MissionSystem.Process(_state));

        // GATE.T48.TEMPLATE.SCHEMA.001: Mission template step completion.
        ProfileCall(sw, tickProfiles, "MissionTemplateSystem", () => MissionTemplateSystem.Process(_state));

        // GATE.S9.SYSTEMIC.TRIGGER_ENGINE.001: World-state mission trigger detection.
        ProfileCall(sw, tickProfiles, "SystemicMissionSystem", () => SystemicMissionSystem.Process(_state));

        // GATE.S4: Industry pipeline — research, refit, maintenance per tick.
        ProfileCall(sw, tickProfiles, "ResearchSystem", () => ResearchSystem.ProcessResearch(_state));
        ProfileCall(sw, tickProfiles, "RefitSystem", () => RefitSystem.ProcessRefitQueue(_state));
        ProfileCall(sw, tickProfiles, "MaintenanceSystem", () => MaintenanceSystem.ProcessDecay(_state));

        // GATE.S7.POWER.BUDGET_ENFORCE.001: Enforce power budget after refit changes.
        ProfileCall(sw, tickProfiles, "PowerBudgetSystem", () => PowerBudgetSystem.Process(_state));

        // GATE.S8.HAVEN.UPGRADE_SYSTEM.001: Haven tier upgrade progression.
        ProfileCall(sw, tickProfiles, "HavenUpgradeSystem", () => HavenUpgradeSystem.Process(_state));

        // GATE.S8.HAVEN.KEEPER.001: Keeper ambient tier progression.
        ProfileCall(sw, tickProfiles, "HavenUpgradeSystem.ProcessKeeper", () => HavenUpgradeSystem.ProcessKeeper(_state));

        // GATE.S8.HAVEN.RESEARCH_LAB.001: Haven parallel research slot progression.
        ProfileCall(sw, tickProfiles, "HavenResearchLabSystem", () => HavenResearchLabSystem.Process(_state));

        // GATE.S8.HAVEN.FABRICATOR.001: T3 module fabrication tick.
        ProfileCall(sw, tickProfiles, "HavenFabricatorSystem", () => HavenFabricatorSystem.Process(_state));

        // GATE.S8.HAVEN.MARKET_EVOLUTION.001: Haven market periodic restocking.
        ProfileCall(sw, tickProfiles, "HavenMarketSystem", () => HavenMarketSystem.Process(_state));

        // GATE.S8.MEGAPROJECT.SYSTEM.001: Megaproject construction progression.
        ProfileCall(sw, tickProfiles, "MegaprojectSystem", () => MegaprojectSystem.Process(_state));

        // GATE.S4.CONSTR_PROG.SYSTEM.001: Construction step advancement.
        ProfileCall(sw, tickProfiles, "ConstructionSystem", () => ConstructionSystem.ProcessConstruction(_state));

        // GATE.S4.NPC_INDU.DEMAND.001: NPC industry demand + reaction.
        ProfileCall(sw, tickProfiles, "NpcIndustrySystem.ProcessNpcIndustry", () => NpcIndustrySystem.ProcessNpcIndustry(_state));
        ProfileCall(sw, tickProfiles, "NpcIndustrySystem.ProcessNpcReaction", () => NpcIndustrySystem.ProcessNpcReaction(_state));

        // GATE.S7.REVEALS.WARFRONT_REVEAL.001: Node observation tracking for progressive intel.
        ProfileCall(sw, tickProfiles, "IntelSystem.UpdateNodeObservation", () => IntelSystem.UpdateNodeObservation(_state));

        // GATE.S10.TRADE_INTEL.KERNEL.001: Passive scanner + trade route evaluation.
        ProfileCall(sw, tickProfiles, "IntelSystem.ProcessScannerIntel", () => IntelSystem.ProcessScannerIntel(_state));

        // GATE.S11.GAME_FEEL.PRICE_HISTORY.001: Price history snapshot recording.
        ProfileCall(sw, tickProfiles, "IntelSystem.ProcessPriceHistory", () => IntelSystem.ProcessPriceHistory(_state));

        // GATE.T42.PLANET_SCAN.ORBITAL.001: Planet scanning — charge recharge + instability reveals.
        ProfileCall(sw, tickProfiles, "PlanetScanSystem", () => PlanetScanSystem.Process(_state));

        // GATE.S6.OUTCOME.REWARD_MODEL.001: Discovery outcome rewards on Analyzed phase.
        ProfileCall(sw, tickProfiles, "DiscoveryOutcomeSystem", () => DiscoveryOutcomeSystem.Process(_state));

        // GATE.T48.ANOMALY.CHAIN_SYSTEM.001: Anomaly chain lifecycle processing.
        ProfileCall(sw, tickProfiles, "AnomalyChainSystem", () => AnomalyChainSystem.Process(_state));

        // GATE.S7.WARFRONT.DEMAND_SHOCK.001: Wartime demand consumption.
        ProfileCall(sw, tickProfiles, "WarfrontDemandSystem", () => WarfrontDemandSystem.Process(_state));

        // GATE.S7.WARFRONT.EVOLUTION.001: Warfront state transitions (escalation/de-escalation).
        ProfileCall(sw, tickProfiles, "WarfrontEvolutionSystem", () => WarfrontEvolutionSystem.Process(_state));

        // GATE.S7.INSTABILITY.TICK_SYSTEM.001: Per-tick instability evolution near warfronts.
        ProfileCall(sw, tickProfiles, "InstabilitySystem", () => InstabilitySystem.Process(_state));

        // GATE.T45.DEEP_DREAD.PASSIVE_DRAIN.001: Phase-based passive hull drain at Phase 2+.
        // GATE.T52.DREAD.EXPOSURE_SCALING.001: Drain interval scales with exposure level.
        ProfileCall(sw, tickProfiles, "DreadDrainSystem", () => DreadDrainSystem.Process(_state));

        // GATE.T52.DREAD.SECONDARY_STRESS.001: Phase 2+ fuel burn multiplier + cargo decay.
        ProfileCall(sw, tickProfiles, "DreadDrainSecondaryStress", () => DreadDrainSystem.ProcessSecondaryStressors(_state));

        // GATE.T45.DEEP_DREAD.EXPOSURE_TRACK.001: Cumulative deep exposure at Phase 2+ nodes.
        ProfileCall(sw, tickProfiles, "ExposureTrackSystem", () => ExposureTrackSystem.Process(_state));

        // GATE.T45.DEEP_DREAD.SENSOR_GHOSTS.001: Phantom fleet contacts at Phase 2+.
        ProfileCall(sw, tickProfiles, "SensorGhostSystem", () => SensorGhostSystem.Process(_state));

        // GATE.T45.DEEP_DREAD.INFO_FOG.001: Track node visits for market data staleness.
        ProfileCall(sw, tickProfiles, "InformationFogSystem", () => InformationFogSystem.Process(_state));

        // GATE.T45.DEEP_DREAD.LATTICE_FAUNA.001: Lattice Fauna lifecycle (spawn/interfere/depart).
        ProfileCall(sw, tickProfiles, "LatticeFaunaSystem", () => LatticeFaunaSystem.Process(_state));

        // GATE.T18.NARRATIVE.TOPOLOGY_SHIFT.001: Phase 3+ edge mutation on player arrival.
        ProfileCall(sw, tickProfiles, "TopologyShiftSystem", () => TopologyShiftSystem.Process(_state));

        // GATE.S5.NPC_TRADE.SYSTEM.001: NPC trade circulation.
        ProfileCall(sw, tickProfiles, "NpcTradeSystem", () => NpcTradeSystem.ProcessNpcTrade(_state));

        // Dynamic fleet population: replace destroyed NPC fleets using station resources.
        ProfileCall(sw, tickProfiles, "FleetPopulationSystem", () => FleetPopulationSystem.Process(_state));

        // GATE.S5.SEC_LANES.SYSTEM.001: Security lane updates.
        ProfileCall(sw, tickProfiles, "SecurityLaneSystem", () => SecurityLaneSystem.ProcessSecurityLanes(_state));

        // GATE.S5.ESCORT_PROG.MODEL.001: Escort and patrol program advancement.
        ProfileCall(sw, tickProfiles, "EscortSystem", () => EscortSystem.Process(_state));

        // GATE.X.PRESSURE.SYSTEM.001: Pressure state transitions.
        ProfileCall(sw, tickProfiles, "PressureSystem.ProcessPressure", () => PressureSystem.ProcessPressure(_state));

        // GATE.X.PRESSURE.ENFORCE.001: Apply pressure consequences.
        ProfileCall(sw, tickProfiles, "PressureSystem.EnforceConsequences", () => PressureSystem.EnforceConsequences(_state));

        // GATE.S7.REPUTATION.TRADE_DRIFT.001: Natural rep decay toward neutral.
        ProfileCall(sw, tickProfiles, "ReputationSystem", () => ReputationSystem.Process(_state));

        // GATE.S7.FACTION_COMMISSION.ENTITY.001: Commission passive rep + stipend.
        ProfileCall(sw, tickProfiles, "CommissionSystem", () => CommissionSystem.Process(_state));

        // GATE.T18.NARRATIVE.WAR_CONSEQUENCE.001: Resolve mature war consequences.
        ProfileCall(sw, tickProfiles, "WarConsequenceSystem", () => WarConsequenceSystem.Process(_state));

        // GATE.S12.PROGRESSION.MILESTONES.001: Evaluate player milestones.
        ProfileCall(sw, tickProfiles, "MilestoneSystem", () => MilestoneSystem.Process(_state));

        // Tutorial state machine: gate evaluation + phase advancement.
        ProfileCall(sw, tickProfiles, "TutorialSystem", () => TutorialSystem.Process(_state));

        // GATE.T18.NARRATIVE.FO_SYSTEM.001: First Officer tier progression.
        ProfileCall(sw, tickProfiles, "FirstOfficerSystem", () => FirstOfficerSystem.Process(_state));

        // GATE.T18.NARRATIVE.WAR_FACES.001: Narrative NPC lifecycle (Regular vanish, Enemy encounter).
        ProfileCall(sw, tickProfiles, "NarrativeNpcSystem", () => NarrativeNpcSystem.Process(_state));

        // GATE.T18.NARRATIVE.KNOWLEDGE_GRAPH.001: Knowledge graph connection reveals.
        ProfileCall(sw, tickProfiles, "KnowledgeGraphSystem", () => KnowledgeGraphSystem.Process(_state));

        // GATE.S6.FRACTURE_DISCOVERY.MODEL.001: Fracture system gated behind discovery unlock.
        ProfileCall(sw, tickProfiles, "FractureSystem", () => FractureSystem.Process(_state));
        ProfileCall(sw, tickProfiles, "FractureSystem.ApplyFractureGoodsFlowV0", () => FractureSystem.ApplyFractureGoodsFlowV0(_state));

        // GATE.S8.STORY_STATE.TRIGGERS.001: Story state machine — 5 revelation triggers.
        ProfileCall(sw, tickProfiles, "StoryStateMachineSystem", () => StoryStateMachineSystem.Process(_state));

        // GATE.S8.PENTAGON.DETECT.001: Pentagon break detection + economic cascade.
        ProfileCall(sw, tickProfiles, "PentagonBreakSystem", () => PentagonBreakSystem.Process(_state));

        // GATE.S8.HAVEN.ENDGAME_PATHS.001: Endgame path effects + accommodation + Communion rep.
        ProfileCall(sw, tickProfiles, "HavenEndgameSystem", () => HavenEndgameSystem.Process(_state));

        // GATE.S7.DIPLOMACY.FRAMEWORK.001: Diplomacy — treaties, bounties, sanctions.
        ProfileCall(sw, tickProfiles, "DiplomacySystem", () => DiplomacySystem.Process(_state));

        // GATE.S8.WIN.LOSS_DETECT.001: Detect death (hull 0) and bankruptcy (credits below threshold).
        ProfileCall(sw, tickProfiles, "LossDetectionSystem", () => LossDetectionSystem.Process(_state));

        // GATE.S8.WIN.PATH_EVAL.001: Evaluate per-path victory conditions.
        ProfileCall(sw, tickProfiles, "WinConditionSystem", () => WinConditionSystem.Process(_state));

        // GATE.T48.TELEMETRY.SESSION_WRITER.001: Dev telemetry snapshot (END, before AdvanceTick).
        ProfileCall(sw, tickProfiles, "TelemetrySystem", () => TelemetrySystem.Process(_state));

        // Store profiling results in ring buffer (side-channel only).
        if (profiling && tickProfiles != null)
        {
            _profileRingBuffer[_profileWriteIndex] = tickProfiles;
            _profileWriteIndex = (_profileWriteIndex + 1) % ProfileRingBufferSize;
            _profileCount++;
        }

        _state.AdvanceTick();
    }

    private static void ProfileCall(Stopwatch? sw, List<TickProfile>? profiles, string name, Action action)
    {
        if (sw != null && profiles != null)
        {
            sw.Restart();
            action();
            sw.Stop();
            profiles.Add(new TickProfile
            {
                SystemName = name,
                ElapsedMicroseconds = sw.Elapsed.Ticks / (TimeSpan.TicksPerMillisecond / 1000) // STRUCTURAL: microsecond conversion
            });
        }
        else
        {
            action();
        }
    }

    public string SaveToString()
    {
        return SerializationSystem.Serialize(_state);
    }

    public void LoadFromString(string data)
    {
        var loaded = SerializationSystem.Deserialize(data);
        if (loaded != null)
        {
            _state = loaded;

            // Critical for determinism + save/load: ensure derived/transient fields are re-hydrated.
            _state.HydrateAfterLoad();
        }
    }
}
