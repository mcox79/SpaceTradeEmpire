using SimCore.Systems;

namespace SimCore.Commands;

/// <summary>
/// Applies specific hull/shield damage to an NPC fleet from real-time combat
/// (player shooting in 3D space). Shield absorbs first, remainder goes to hull.
/// Also applies delay ticks to slow the fleet.
/// GATE.S16.NPC_ALIVE.DAMAGE_CMD.001
/// </summary>
public sealed class NpcFleetDamageCommand : ICommand
{
    public string FleetId { get; }
    public int Damage { get; }
    public int DelayTicks { get; }

    public NpcFleetDamageCommand(string fleetId, int damage, int delayTicks = 2)
    {
        FleetId = fleetId;
        Damage = damage;
        DelayTicks = delayTicks;
    }

    public void Execute(SimState state)
    {
        if (string.IsNullOrWhiteSpace(FleetId)) return;
        if (Damage <= 0) return;
        if (!state.Fleets.TryGetValue(FleetId, out var fleet)) return;

        // Initialize combat stats if not yet set (HpMax defaults to -1 in Fleet).
        if (fleet.HullHpMax <= 0)
            CombatSystem.InitFleetCombatStats(fleet, isPlayer: false);

        // Shield absorbs first.
        int remaining = Damage;
        if (fleet.ShieldHp > 0)
        {
            int shieldAbsorb = System.Math.Min(remaining, fleet.ShieldHp);
            fleet.ShieldHp -= shieldAbsorb;
            remaining -= shieldAbsorb;
        }

        // Remainder goes to hull.
        if (remaining > 0)
        {
            fleet.HullHp = System.Math.Max(0, fleet.HullHp - remaining);
        }

        // Apply delay.
        if (DelayTicks > 0)
        {
            fleet.DelayTicksRemaining += DelayTicks;
        }

        // Log combat event so combat log panel (L key) shows real-time hits.
        var log = new CombatSystem.CombatLog();
        log.Events.Add(new CombatSystem.CombatEventEntry
        {
            Tick = state.Tick,
            AttackerId = "fleet_trader_1",
            DefenderId = FleetId,
            WeaponId = "realtime_hit",
            DamageDealt = Damage,
            DefenderHullRemaining = fleet.HullHp,
            DefenderShieldRemaining = fleet.ShieldHp,
        });
        log.Outcome = fleet.HullHp <= 0
            ? CombatSystem.CombatOutcome.Win
            : CombatSystem.CombatOutcome.InProgress;
        if (state.CombatLogs.Count >= 10)
            state.CombatLogs.RemoveAt(0);
        state.CombatLogs.Add(log);
    }
}
