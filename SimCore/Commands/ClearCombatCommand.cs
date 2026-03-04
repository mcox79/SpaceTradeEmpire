namespace SimCore.Commands;

/// <summary>
/// Clears combat state flags (InCombat, CombatOpponentId) after an encounter is acknowledged.
/// GATE.S5.COMBAT_LOCAL.SCENE_PROOF.001
/// </summary>
public sealed class ClearCombatCommand : ICommand
{
	public void Execute(SimState state)
	{
		state.InCombat = false;
		state.CombatOpponentId = null;
	}
}
