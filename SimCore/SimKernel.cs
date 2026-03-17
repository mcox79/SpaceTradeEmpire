using SimCore.Commands;
using SimCore.Systems;
using SimCore.Programs;
using SimCore.Tweaks;

namespace SimCore;

public class SimKernel
{
    private SimState _state;
    private Queue<ICommand> _commandQueue = new();
    private Queue<SimCore.Intents.IIntent> _intentQueue = new();
    public SimState State => _state;

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

        // Resolve in-flight inventory arrivals first.
        LaneFlowSystem.Process(_state);

        // Programs emit intents only (no direct ledger mutation).
        ProgramSystem.Process(_state);

        // Convert intents to commands.
        IntentSystem.Process(_state);

        // Apply fleet movement/state transitions.
        MovementSystem.Process(_state);

        // GATE.T18.NARRATIVE.FRACTURE_WEIGHT.001: Cargo weight shift on arrival at stable space.
        FractureWeightSystem.Process(_state);

        // GATE.S7.SUSTAIN.FUEL_DEDUCT.001: Fleet fuel + module sustain deduction.
        SustainSystem.Process(_state);

        // GATE.X.FLEET_UPKEEP.DRAIN.001: Per-cycle fleet upkeep credit drain.
        FleetUpkeepSystem.Process(_state);

        // GATE.S16.NPC_ALIVE.FLEET_DESTROY.001: Remove destroyed NPC fleets.
        NpcFleetCombatSystem.Process(_state);

        // GATE.S8.LATTICE_DRONES.SPAWN.001: Spawn/despawn lattice drones per instability phase.
        LatticeDroneSpawnSystem.Process(_state);

        // GATE.S8.LATTICE_DRONES.COMBAT.001: Drone auto-engage at player node.
        LatticeDroneCombatSystem.Process(_state);

        // GATE.S5.LOOT.DROP_SYSTEM.001: Despawn expired loot drops.
        LootTableSystem.ProcessDespawn(_state);

        // GATE.S15.FEEL.JUMP_EVENT_SYS.001: Random events on lane arrival.
        JumpEventSystem.Process(_state);

        // Slice 3 / GATE.S3.RISK_MODEL.001
        // Emit deterministic security incidents on lanes%routes (no time sources, no shared RNG coupling).
        RiskSystem.Process(_state);

        // Compute buffer shortages and assign logistics jobs (produces fleet jobs).
        LogisticsSystem.Process(_state);

        // Consume upkeep and produce outputs.
        IndustrySystem.Process(_state);

        // Station population consumption: food + fuel drain per station per cadence.
        StationConsumptionSystem.Process(_state);

        // GATE.S8.THREAT.SUPPLY_SHOCK.001: Warfront disrupts production chains.
        SupplyShockSystem.Process(_state);

        // GATE.S9.SYSTEMIC.STATION_CONTEXT.001: Per-station economic context.
        StationContextSystem.Process(_state);

        // GATE.S1.MISSION.SYSTEM.001: Evaluate mission triggers and advance steps.
        MissionSystem.Process(_state);

        // GATE.S9.SYSTEMIC.TRIGGER_ENGINE.001: World-state mission trigger detection.
        SystemicMissionSystem.Process(_state);

        // GATE.S4: Industry pipeline — research, refit, maintenance per tick.
        ResearchSystem.ProcessResearch(_state);
        RefitSystem.ProcessRefitQueue(_state);
        MaintenanceSystem.ProcessDecay(_state);

        // GATE.S7.POWER.BUDGET_ENFORCE.001: Enforce power budget after refit changes.
        PowerBudgetSystem.Process(_state);

        // GATE.S8.HAVEN.UPGRADE_SYSTEM.001: Haven tier upgrade progression.
        HavenUpgradeSystem.Process(_state);

        // GATE.S8.HAVEN.KEEPER.001: Keeper ambient tier progression.
        HavenUpgradeSystem.ProcessKeeper(_state);

        // GATE.S8.HAVEN.RESEARCH_LAB.001: Haven parallel research slot progression.
        HavenResearchLabSystem.Process(_state);

        // GATE.S8.HAVEN.FABRICATOR.001: T3 module fabrication tick.
        HavenFabricatorSystem.Process(_state);

        // GATE.S8.HAVEN.MARKET_EVOLUTION.001: Haven market periodic restocking.
        HavenMarketSystem.Process(_state);

        // GATE.S8.MEGAPROJECT.SYSTEM.001: Megaproject construction progression.
        MegaprojectSystem.Process(_state);

        // GATE.S4.CONSTR_PROG.SYSTEM.001: Construction step advancement.
        ConstructionSystem.ProcessConstruction(_state);

        // GATE.S4.NPC_INDU.DEMAND.001: NPC industry demand + reaction.
        NpcIndustrySystem.ProcessNpcIndustry(_state);
        NpcIndustrySystem.ProcessNpcReaction(_state);

        // GATE.S7.REVEALS.WARFRONT_REVEAL.001: Node observation tracking for progressive intel.
        IntelSystem.UpdateNodeObservation(_state);

        // GATE.S10.TRADE_INTEL.KERNEL.001: Passive scanner + trade route evaluation.
        IntelSystem.ProcessScannerIntel(_state);

        // GATE.S11.GAME_FEEL.PRICE_HISTORY.001: Price history snapshot recording.
        IntelSystem.ProcessPriceHistory(_state);

        // GATE.S6.OUTCOME.REWARD_MODEL.001: Discovery outcome rewards on Analyzed phase.
        DiscoveryOutcomeSystem.Process(_state);

        // GATE.S7.WARFRONT.DEMAND_SHOCK.001: Wartime demand consumption.
        WarfrontDemandSystem.Process(_state);

        // GATE.S7.WARFRONT.EVOLUTION.001: Warfront state transitions (escalation/de-escalation).
        WarfrontEvolutionSystem.Process(_state);

        // GATE.S7.INSTABILITY.TICK_SYSTEM.001: Per-tick instability evolution near warfronts.
        InstabilitySystem.Process(_state);

        // GATE.T18.NARRATIVE.TOPOLOGY_SHIFT.001: Phase 3+ edge mutation on player arrival.
        TopologyShiftSystem.Process(_state);

        // GATE.S5.NPC_TRADE.SYSTEM.001: NPC trade circulation.
        NpcTradeSystem.ProcessNpcTrade(_state);

        // GATE.S5.SEC_LANES.SYSTEM.001: Security lane updates.
        SecurityLaneSystem.ProcessSecurityLanes(_state);

        // GATE.S5.ESCORT_PROG.MODEL.001: Escort and patrol program advancement.
        EscortSystem.Process(_state);

        // GATE.X.PRESSURE.SYSTEM.001: Pressure state transitions.
        PressureSystem.ProcessPressure(_state);

        // GATE.X.PRESSURE.ENFORCE.001: Apply pressure consequences.
        PressureSystem.EnforceConsequences(_state);

        // GATE.S7.REPUTATION.TRADE_DRIFT.001: Natural rep decay toward neutral.
        ReputationSystem.Process(_state);

        // GATE.S7.FACTION_COMMISSION.ENTITY.001: Commission passive rep + stipend.
        CommissionSystem.Process(_state);

        // GATE.T18.NARRATIVE.WAR_CONSEQUENCE.001: Resolve mature war consequences.
        WarConsequenceSystem.Process(_state);

        // GATE.S12.PROGRESSION.MILESTONES.001: Evaluate player milestones.
        MilestoneSystem.Process(_state);

        // Tutorial state machine: gate evaluation + phase advancement.
        TutorialSystem.Process(_state);

        // GATE.T18.NARRATIVE.FO_SYSTEM.001: First Officer tier progression.
        FirstOfficerSystem.Process(_state);

        // GATE.T18.NARRATIVE.WAR_FACES.001: Narrative NPC lifecycle (Regular vanish, Enemy encounter).
        NarrativeNpcSystem.Process(_state);

        // GATE.T18.NARRATIVE.KNOWLEDGE_GRAPH.001: Knowledge graph connection reveals.
        KnowledgeGraphSystem.Process(_state);

        // GATE.S6.FRACTURE_DISCOVERY.MODEL.001: Fracture system gated behind discovery unlock.
        FractureSystem.Process(_state);
        FractureSystem.ApplyFractureGoodsFlowV0(_state);

        // GATE.S8.STORY_STATE.TRIGGERS.001: Story state machine — 5 revelation triggers.
        StoryStateMachineSystem.Process(_state);

        // GATE.S8.PENTAGON.DETECT.001: Pentagon break detection + economic cascade.
        PentagonBreakSystem.Process(_state);

        // GATE.S8.HAVEN.ENDGAME_PATHS.001: Endgame path effects + accommodation + Communion rep.
        HavenEndgameSystem.Process(_state);

        // GATE.S7.DIPLOMACY.FRAMEWORK.001: Diplomacy — treaties, bounties, sanctions.
        DiplomacySystem.Process(_state);

        // GATE.S8.WIN.LOSS_DETECT.001: Detect death (hull 0) and bankruptcy (credits below threshold).
        LossDetectionSystem.Process(_state);

        // GATE.S8.WIN.PATH_EVAL.001: Evaluate per-path victory conditions.
        WinConditionSystem.Process(_state);

        _state.AdvanceTick();
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
