using System.Collections.Generic;

namespace SimCore.Entities;

// GATE.S7.WARFRONT.STATE_MODEL.001: Warfront entity with intensity 0-4.
public enum WarfrontIntensity
{
    Peace = 0,
    Tension = 1,
    Skirmish = 2,
    OpenWar = 3,
    TotalWar = 4
}

public enum WarType
{
    Hot = 0,   // territorial, kinetic conflict
    Cold = 1   // informational, espionage, economic pressure
}

// GATE.S7.WARFRONT.OBJECTIVES.001: Strategic objective types.
public enum ObjectiveType
{
    SupplyDepot = 0,
    CommRelay = 1,
    Factory = 2
}

// GATE.S7.WARFRONT.OBJECTIVES.001: A capturable strategic objective at a warfront node.
public class WarfrontObjective
{
    public string NodeId { get; set; } = "";
    public ObjectiveType Type { get; set; } = ObjectiveType.SupplyDepot;
    public string ControllingFactionId { get; set; } = "";
    public int DominanceTicks { get; set; } = 0;
    public string DominantFactionId { get; set; } = "";
}

public class WarfrontState
{
    public string Id { get; set; } = "";
    public string CombatantA { get; set; } = "";
    public string CombatantB { get; set; } = "";
    public WarfrontIntensity Intensity { get; set; } = WarfrontIntensity.Peace;
    public WarType WarType { get; set; } = WarType.Hot;
    public int TickStarted { get; set; } = 0;
    public List<string> ContestedNodeIds { get; set; } = new();

    // GATE.S7.WARFRONT.ATTRITION.001: Fleet strength per combatant (0-100).
    // Depleted by attrition at Skirmish+ intensity, restored by supply deliveries.
    public int FleetStrengthA { get; set; } = 100;
    public int FleetStrengthB { get; set; } = 100;

    // GATE.S7.WARFRONT.OBJECTIVES.001: Strategic objectives at contested nodes.
    public List<WarfrontObjective> Objectives { get; set; } = new();
}
