#nullable enable

using Godot;
using System;

namespace SpaceTradeEmpire.Bridge;

/// <summary>
/// Debug-only bridge methods for bot/test scripts.
/// These mutate state directly — NOT for production gameplay.
/// </summary>
public partial class SimBridge
{
    /// <summary>
    /// Set player hull to a specific value. For screenshot bots to test
    /// hull critical visual states.
    /// </summary>
    public void DebugSetPlayerHullV0(int hullHp)
    {
        try
        {
            _stateLock.EnterWriteLock();
            if (_kernel?.State?.Fleets != null &&
                _kernel.State.Fleets.TryGetValue("fleet_trader_1", out var fleet))
            {
                // Ensure combat stats are initialized (HullHpMax defaults to -1).
                if (fleet.HullHpMax <= 0)
                    SimCore.Systems.CombatSystem.InitFleetCombatStats(fleet, isPlayer: true);
                fleet.HullHp = Math.Clamp(hullHp, 0, fleet.HullHpMax);
            }
        }
        finally { _stateLock.ExitWriteLock(); }
    }

    /// <summary>
    /// Restore player hull to max. Reverses DebugSetPlayerHullV0 damage.
    /// </summary>
    public void DebugRestorePlayerHullV0()
    {
        try
        {
            _stateLock.EnterWriteLock();
            if (_kernel?.State?.Fleets != null &&
                _kernel.State.Fleets.TryGetValue("fleet_trader_1", out var fleet))
            {
                if (fleet.HullHpMax <= 0)
                    SimCore.Systems.CombatSystem.InitFleetCombatStats(fleet, isPlayer: true);
                fleet.HullHp = fleet.HullHpMax;
            }
        }
        finally { _stateLock.ExitWriteLock(); }
    }

    /// <summary>
    /// Set player credits. For screenshot bots to set up trade scenarios.
    /// </summary>
    public void DebugSetCreditsV0(int credits)
    {
        try
        {
            _stateLock.EnterWriteLock();
            if (_kernel?.State != null)
            {
                _kernel.State.PlayerCredits = Math.Max(0, credits);
            }
        }
        finally { _stateLock.ExitWriteLock(); }
    }

    /// <summary>
    /// Ensure player fleet combat stats (hull/shield) are initialized.
    /// Safe to call multiple times — no-op if already initialized.
    /// </summary>
    public void DebugInitPlayerCombatV0()
    {
        try
        {
            _stateLock.EnterWriteLock();
            if (_kernel?.State?.Fleets != null &&
                _kernel.State.Fleets.TryGetValue("fleet_trader_1", out var fleet))
            {
                if (fleet.HullHpMax <= 0)
                    SimCore.Systems.CombatSystem.InitFleetCombatStats(fleet, isPlayer: true);
            }
        }
        finally { _stateLock.ExitWriteLock(); }
    }

    /// <summary>
    /// Set player fleet battle stations to BattleReady (100% damage).
    /// The SpinningUp→BattleReady tick transition is not yet implemented,
    /// so bots use this to bypass the spin-up delay.
    /// </summary>
    public void ForceBattleReadyV0()
    {
        try
        {
            _stateLock.EnterWriteLock();
            if (_kernel?.State?.Fleets != null &&
                _kernel.State.Fleets.TryGetValue("fleet_trader_1", out var fleet))
            {
                fleet.BattleStations = SimCore.Entities.BattleStationsState.BattleReady;
                fleet.BattleStationsSpinUpTicksRemaining = 0;
            }
        }
        finally { _stateLock.ExitWriteLock(); }
    }

    /// <summary>
    /// Advance sim by N ticks. For research near-completion screenshot.
    /// </summary>
    public void DebugAdvanceTicksV0(int ticks)
    {
        if (ticks <= 0 || ticks > 2000) return;
        try
        {
            _stateLock.EnterWriteLock();
            for (int i = 0; i < ticks; i++)
            {
                _kernel?.Step();
            }
        }
        finally { _stateLock.ExitWriteLock(); }
    }
}
