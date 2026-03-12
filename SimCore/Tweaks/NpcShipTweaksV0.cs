namespace SimCore.Tweaks;

// GATE.S16.NPC_ALIVE.FLEET_RESPAWN.001: NPC ship tuning constants.
public static class NpcShipTweaksV0
{
    // Ticks before a destroyed NPC fleet respawns at its home node.
    public static int RespawnCooldownTicks { get; } = 60;

    // Default fuel tank capacity for NPC fleets.
    public static int DefaultFuelCapacity { get; } = 500;
}
