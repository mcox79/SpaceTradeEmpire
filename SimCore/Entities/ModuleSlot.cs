namespace SimCore.Entities;

// SLICE 4: Hero ship slot model (GATE.S4.MODULE_MODEL.SLOTS.001)

public enum SlotKind
{
    Cargo = 0,
    Weapon = 1,
    Engine = 2,
    Utility = 3
}

public class ModuleSlot
{
    public string SlotId { get; set; } = "";
    public SlotKind SlotKind { get; set; } = SlotKind.Cargo;
    public string? InstalledModuleId { get; set; } = null;
}
