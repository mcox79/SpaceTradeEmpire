using System;
using System.Linq;
using SimCore.Entities;

namespace SimCore.Commands;

// GATE.S8.HAVEN.DRYDOCK_TRANSFER.001: Transfer a module between ships at Haven drydock (Tier 3+).
public sealed class TransferModuleCommand : ICommand
{
    public string SourceFleetId { get; }
    public string SourceSlotId { get; }
    public string TargetFleetId { get; }
    public string TargetSlotId { get; }

    public TransferModuleCommand(string sourceFleetId, string sourceSlotId,
        string targetFleetId, string targetSlotId)
    {
        SourceFleetId = sourceFleetId ?? "";
        SourceSlotId = sourceSlotId ?? "";
        TargetFleetId = targetFleetId ?? "";
        TargetSlotId = targetSlotId ?? "";
    }

    public void Execute(SimState state)
    {
        var haven = state.Haven;
        if (haven == null || !haven.Discovered) return;

        // Drydock requires Tier 3+ (Operational).
        if (haven.Tier < HavenTier.Operational) return;

        // Both fleets must exist.
        if (!state.Fleets.TryGetValue(SourceFleetId, out var sourceFleet)) return;
        if (!state.Fleets.TryGetValue(TargetFleetId, out var targetFleet)) return;

        // Both must be at Haven (active at Haven node or stored in hangar).
        bool sourceAtHaven = IsFleetAtHaven(sourceFleet, haven);
        bool targetAtHaven = IsFleetAtHaven(targetFleet, haven);
        if (!sourceAtHaven || !targetAtHaven) return;

        // Find source slot with a module installed.
        var sourceSlot = sourceFleet.Slots.FirstOrDefault(s =>
            string.Equals(s.SlotId, SourceSlotId, StringComparison.Ordinal));
        if (sourceSlot == null) return;
        if (string.IsNullOrEmpty(sourceSlot.InstalledModuleId)) return;

        // Find target slot that is empty.
        var targetSlot = targetFleet.Slots.FirstOrDefault(s =>
            string.Equals(s.SlotId, TargetSlotId, StringComparison.Ordinal));
        if (targetSlot == null) return;
        if (!string.IsNullOrEmpty(targetSlot.InstalledModuleId)) return;

        // Slot kind must match.
        if (sourceSlot.SlotKind != targetSlot.SlotKind) return;

        // Transfer the module.
        targetSlot.InstalledModuleId = sourceSlot.InstalledModuleId;
        targetSlot.PowerDraw = sourceSlot.PowerDraw;
        targetSlot.Condition = sourceSlot.Condition;

        sourceSlot.InstalledModuleId = null;
        sourceSlot.PowerDraw = 0;
        sourceSlot.Disabled = false;
    }

    private static bool IsFleetAtHaven(Fleet fleet, HavenStarbase haven)
    {
        if (fleet.IsStored && haven.StoredShipIds.Contains(fleet.Id))
            return true;
        if (string.Equals(fleet.CurrentNodeId, haven.NodeId, StringComparison.Ordinal))
            return true;
        return false;
    }
}
