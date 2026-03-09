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
    // GATE.S18.SHIP_MODULES.FITTING_BUDGET.001: Power draw of installed module.
    public int PowerDraw { get; set; } = 0;
    // GATE.S7.POWER.BUDGET_ENFORCE.001: Module disabled by power budget or sustain shortfall.
    public bool Disabled { get; set; } = false;
    // GATE.S7.POWER.MOUNT_DEGRADE.001: Module condition 0-100. 0 = broken (auto-disabled).
    public int Condition { get; set; } = 100;
}
