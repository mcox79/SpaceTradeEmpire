namespace SimCore.Commands;

// GATE.S4.MODULE_MODEL.EQUIP.001
// Sets InstalledModuleId on the named slot of the given fleet.
// Pass moduleId="" to unequip.
public sealed class EquipModuleCommand : ICommand
{
    public string FleetId  { get; }
    public string SlotId   { get; }
    public string ModuleId { get; }

    public EquipModuleCommand(string fleetId, string slotId, string moduleId)
    {
        FleetId  = fleetId  ?? "";
        SlotId   = slotId   ?? "";
        ModuleId = moduleId ?? "";
    }

    public void Execute(SimCore.SimState state)
    {
        if (!state.Fleets.TryGetValue(FleetId, out var fleet)) return;
        var slot = fleet.Slots.FirstOrDefault(s => s.SlotId == SlotId);
        if (slot == null) return;
        slot.InstalledModuleId = ModuleId.Length > 0 ? ModuleId : null;
        // fh_14: Track player decisions for FO silence fallback.
        if (state.FirstOfficer != null) state.FirstOfficer.DecisionsSinceLastLine++;
    }
}
