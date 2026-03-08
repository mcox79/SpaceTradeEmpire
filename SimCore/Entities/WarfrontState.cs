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

public class WarfrontState
{
    public string Id { get; set; } = "";
    public string CombatantA { get; set; } = "";
    public string CombatantB { get; set; } = "";
    public WarfrontIntensity Intensity { get; set; } = WarfrontIntensity.Peace;
    public WarType WarType { get; set; } = WarType.Hot;
    public int TickStarted { get; set; } = 0;
    public List<string> ContestedNodeIds { get; set; } = new();
}
