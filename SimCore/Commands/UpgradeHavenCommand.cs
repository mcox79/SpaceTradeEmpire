using SimCore.Entities;
using SimCore.Systems;

namespace SimCore.Commands;

// GATE.S8.HAVEN.UPGRADE_SYSTEM.001: Player initiates Haven tier upgrade.
public class UpgradeHavenCommand : ICommand
{
    public void Execute(SimState state)
    {
        if (!HavenUpgradeSystem.CanUpgrade(state)) return;

        var haven = state.Haven;
        var nextTier = (HavenTier)((int)haven.Tier + 1);

        // Deduct resources.
        HavenUpgradeSystem.DeductUpgradeResources(state, nextTier);

        // Start upgrade timer.
        haven.UpgradeTargetTier = nextTier;
        haven.UpgradeTicksRemaining = HavenUpgradeSystem.GetUpgradeDuration(nextTier);
    }
}
