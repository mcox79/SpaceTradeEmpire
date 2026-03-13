using System.Text.Json.Serialization;
using SimCore.Entities;

namespace SimCore.Entities;

public enum FleetState
{
    Idle = 0,
    Traveling = 1,
    Docked = 2,
    // SLICE 3: Fracture Travel (Off-Lane)
    FractureTraveling = 3
}

public enum FleetActiveController
{
    None = 0,
    Program = 1,
    LogisticsJob = 2,
    ManualOverride = 3
}

public enum FleetRole
{
    Trader = 0,
    Hauler = 1,
    Patrol = 2
}

// GATE.S7.COMBAT_PHASE2.BATTLE_STATIONS.001: Battle readiness state.
public enum BattleStationsState
{
    StandDown = 0,
    SpinningUp = 1,
    BattleReady = 2
}

// GATE.S18.SHIP_MODULES.ZONE_ARMOR.001: Directional armor zones.
public enum ZoneFacing
{
    Fore = 0,
    Port = 1,
    Starboard = 2,
    Aft = 3
}

public class Fleet
{
    public string Id { get; set; } = "";
    public string OwnerId { get; set; } = "";

    // GATE.S18.SHIP_MODULES.SHIP_CLASS.001: Ship class identifier (e.g. "corvette", "frigate").
    public string ShipClassId { get; set; } = "corvette";

    // SLICE 3: Fleet roles v0 (GATE.S3.FLEET.ROLES.001)
    // Persisted role that influences exactly one deterministic decision surface (route-choice selection).
    public FleetRole Role { get; set; } = FleetRole.Trader;

    // SLICE 3: Fleet roles v0 (GATE.S3.FLEET.ROLES.001)
    // Persisted proof surface for deterministic route-choice selection (survives save%load even though PendingIntents are discarded).
    public string LastRouteChoiceRouteId { get; set; } = "";

    // LOCATION STATE
    public string CurrentNodeId { get; set; } = "";
    public string DestinationNodeId { get; set; } = "";
    public string CurrentEdgeId { get; set; } = "";

    // ROUTE STATE (Slice 3 / GATE.FLEET.ROUTE.001)
    // DestinationNodeId is the next immediate hop while traveling.
    // FinalDestinationNodeId is the requested end node for the whole route.
    public string FinalDestinationNodeId { get; set; } = "";

    // UI Manual Override (Slice 3 / GATE.UI.FLEET.003)
    // Non-empty means UI has asserted a manual destination override.
    // Semantics: while set, routing is controlled by ManualOverrideNodeId until cleared.
    public string ManualOverrideNodeId { get; set; } = "";

    // Planned lane sequence to reach FinalDestinationNodeId.
    // RouteEdgeIndex points to the next edge to traverse.
    public List<string> RouteEdgeIds { get; set; } = new();

    public int RouteEdgeIndex { get; set; } = 0;

    // TRAVEL STATE
    public FleetState State { get; set; } = FleetState.Idle;
    public float TravelProgress { get; set; } = 0f; // 0.0 to 1.0
    public float Speed { get; set; } = 0.5f; // AU per tick

    // LOGIC STATE
    public string CurrentTask { get; set; } = "Idle"; // Explanation for UI

    // Deterministic cooldown: if a job is canceled at tick T, do not auto-assign a new logistics job until T+1.
    public int LastJobCancelTick { get; set; } = -1;

    // Program controller (Slice 3 doctrine). Empty means no program currently owns this fleet.
    // This is a stable identity surface for authority reporting; precedence is enforced by ActiveController.
    public string ProgramId { get; set; } = "";

    public LogisticsJob? CurrentJob { get; set; } // The Active Order

    [JsonIgnore]
    public FleetActiveController ActiveController
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(ManualOverrideNodeId)) return FleetActiveController.ManualOverride;
            if (CurrentJob != null) return FleetActiveController.LogisticsJob;
            if (!string.IsNullOrWhiteSpace(ProgramId)) return FleetActiveController.Program;
            return FleetActiveController.None;
        }
    }

    // CARGO
    // Gate target: dict keyed by GoodId, quantities are non-negative ints.
    // Determinism note: when iterating/serializing, sort keys with Ordinal.
    public Dictionary<string, int> Cargo { get; set; } = new();

    // SLICE 4: Hero ship slot model (GATE.S4.MODULE_MODEL.SLOTS.001)
    // Ordered by SlotId Ordinal asc for deterministic serialization.
    public List<ModuleSlot> Slots { get; set; } = new();


    // GATE.S6.FRACTURE.ACCESS_MODEL.001: Fleet tech level for fracture access gating.
    // TechLevel 0 = baseline; higher values unlock higher-tier fracture nodes.
    public int TechLevel { get; set; } = 0;

    // GATE.S6.FRACTURE.TRAVEL_CMD.001: Target void site for off-lane fracture travel.
    public string FractureTargetSiteId { get; set; } = "";

    // SLICE 5: Combat HP state (GATE.S5.COMBAT_LOCAL.DAMAGE_MODEL.001)
    public int HullHp { get; set; } = -1;
    public int HullHpMax { get; set; } = -1;
    public int ShieldHp { get; set; } = -1;
    public int ShieldHpMax { get; set; } = -1;

    // GATE.S18.SHIP_MODULES.ZONE_ARMOR.001: Directional zone armor (Fore/Port/Stbd/Aft).
    // Index by (int)ZoneFacing. Length is always 4.
    public int[] ZoneArmorHp { get; set; } = new int[4];
    public int[] ZoneArmorHpMax { get; set; } = new int[4];

    // GATE.S7.AUTOMATION_MGMT.DOCTRINE.001: Fleet engagement doctrine (stance, retreat, patrol radius).
    public FleetDoctrine Doctrine { get; set; } = new();

    // GATE.S7.AUTOMATION_MGMT.PROGRAM_METRICS.001: Per-fleet program cycle metrics.
    public ProgramCycleMetrics Metrics { get; set; } = new();

    // GATE.S7.AUTOMATION_MGMT.BUDGET_ENFORCEMENT.001: Per-fleet automation budget caps.
    public AutomationBudget Budget { get; set; } = new();

    // GATE.S7.AUTOMATION_MGMT.PROGRAM_HISTORY.001: Program outcome ring buffer (max 20, newest first).
    public List<ProgramHistoryEntry> History { get; set; } = new();

    // GATE.S5.COMBAT.ESCORT_DOCTRINE.001: Escort doctrine state.
    // When EscortDoctrineActive is true, this fleet is escorting EscortTargetFleetId.
    // The target fleet receives a shield damage reduction bonus in combat.
    public bool EscortDoctrineActive { get; set; } = false;
    public string EscortTargetFleetId { get; set; } = "";

    // GATE.S4.UPGRADE_PIPELINE.TIMED_REFIT.001: Queued module installations awaiting completion.
    [JsonInclude] public List<RefitQueueEntry> RefitQueue { get; set; } = new();

    // GATE.S12.NPC_CIRC.CIRCUIT_ROUTES.001: Multi-hop patrol circuit (3+ node IDs forming a loop).
    // Generated deterministically from fleet ID + galaxy topology. PatrolCircuitIndex wraps around.
    [JsonInclude] public List<string> PatrolCircuit { get; set; } = new();
    [JsonInclude] public int PatrolCircuitIndex { get; set; } = 0;

    // GATE.S3.RISK_SINKS.DELAY_MODEL.001: Remaining delay ticks from risk events.
    [JsonInclude] public int DelayTicksRemaining { get; set; } = 0;

    // GATE.S7.ENFORCEMENT.CONFISCATION.001: Tick of last confiscation (cooldown tracking).
    [JsonInclude] public int LastConfiscationTick { get; set; } = -1;

    // GATE.T18.NARRATIVE.FRACTURE_WEIGHT.001: Tracks instability phase where each cargo good was loaded.
    // Key = goodId, Value = instability phase (0-4) at load origin.
    [JsonInclude] public Dictionary<string, int> CargoOriginPhase { get; set; } = new();

    // GATE.S7.COMBAT_PHASE2.BATTLE_STATIONS.001: Battle readiness.
    public BattleStationsState BattleStations { get; set; } = BattleStationsState.StandDown;
    public int BattleStationsSpinUpTicksRemaining { get; set; } = 0;

    // Dedicated fuel tank — consumed during travel, refilled at stations.
    // FuelCapacity set from ShipClassDef.BaseFuelCapacity + module bonuses.
    public int FuelCurrent { get; set; } = 0;
    public int FuelCapacity { get; set; } = 0;

    [JsonIgnore]
    public bool IsMoving => State == FleetState.Traveling || State == FleetState.FractureTraveling;

    public int GetCargoUnits(string goodId)
    {
        if (string.IsNullOrWhiteSpace(goodId)) return 0;
        return Cargo.TryGetValue(goodId, out var v) ? v : 0;
    }

    // GATE.S5.COMBAT.ESCORT_DOCTRINE.001: Activate escort doctrine for this fleet.
    // targetFleetId must be non-empty; if empty the doctrine is not activated.
    public void SetEscortDoctrine(string targetFleetId)
    {
        if (string.IsNullOrWhiteSpace(targetFleetId))
        {
            EscortDoctrineActive = false;
            EscortTargetFleetId = "";
            return;
        }
        EscortDoctrineActive = true;
        EscortTargetFleetId = targetFleetId;
    }

    // GATE.S5.COMBAT.ESCORT_DOCTRINE.001: Deactivate escort doctrine.
    public void ClearEscortDoctrine()
    {
        EscortDoctrineActive = false;
        EscortTargetFleetId = "";
    }

}

// GATE.S4.UPGRADE_PIPELINE.TIMED_REFIT.001: Entry in the timed refit queue.
public sealed class RefitQueueEntry
{
    [JsonInclude] public string ModuleId { get; set; } = "";
    [JsonInclude] public int SlotIndex { get; set; }
    [JsonInclude] public int TicksRemaining { get; set; }
}
