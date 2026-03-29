using SimCore.Systems;
using SimCore.Tweaks;

namespace SimCore.Commands;

/// <summary>
/// Starts and resolves a combat encounter between the player fleet and an opponent.
/// Sets InCombat=true, runs the encounter, stores the log. InCombat stays true
/// until explicitly cleared so bridge queries can observe the result.
/// GATE.S5.COMBAT_LOCAL.SCENE_PROOF.001
/// </summary>
public sealed class StartCombatCommand : ICommand
{
	public string PlayerFleetId { get; }
	public string OpponentFleetId { get; }

	public StartCombatCommand(string playerFleetId, string opponentFleetId)
	{
		PlayerFleetId = playerFleetId;
		OpponentFleetId = opponentFleetId;
	}

	public void Execute(SimState state)
	{
		if (!state.Fleets.TryGetValue(PlayerFleetId, out var player)) return;
		if (!state.Fleets.TryGetValue(OpponentFleetId, out var opponent)) return;

		// Initialize combat stats if not yet set (HpMax defaults to -1 in Fleet).
		if (player.HullHpMax <= 0)
			CombatSystem.InitFleetCombatStats(player, isPlayer: true);
		if (opponent.HullHpMax <= 0)
			CombatSystem.InitFleetCombatStats(opponent, isPlayer: false);

		state.InCombat = true;
		state.CombatOpponentId = OpponentFleetId;

		// weaponBaseDamage=null → CalcDamage uses CombatTweaksV0.DefaultWeaponBaseDamage for all weapons.
		var log = CombatSystem.RunEncounter(player, opponent, weaponBaseDamage: null);

		// Cap stored logs at 10.
		if (state.CombatLogs.Count >= 10)
			state.CombatLogs.RemoveAt(0);
		state.CombatLogs.Add(log);

		// fh_14: Track player decisions for FO silence fallback.
		if (state.FirstOfficer != null) state.FirstOfficer.DecisionsSinceLastLine++;
	}
}
