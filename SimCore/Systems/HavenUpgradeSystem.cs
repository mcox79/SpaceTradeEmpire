using SimCore.Content;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.S8.HAVEN.UPGRADE_SYSTEM.001: Haven tier progression system.
public static class HavenUpgradeSystem
{
    public static void Process(SimState state)
    {
        var haven = state.Haven;
        if (haven == null) return;
        if (!haven.Discovered) return;
        if (haven.UpgradeTicksRemaining <= 0) return;

        // Advance upgrade progress.
        haven.UpgradeTicksRemaining--;

        if (haven.UpgradeTicksRemaining <= 0)
        {
            // Upgrade complete — advance tier.
            haven.Tier = haven.UpgradeTargetTier;
            haven.UpgradeTargetTier = haven.Tier;

            // Tier 3 unlocks bidirectional thread.
            if (haven.Tier >= HavenTier.Operational)
                haven.BidirectionalThread = true;

            // Refresh Haven market stock for new tier.
            Gen.GalaxyGenerator.RefreshHavenMarketV0(state, haven.Tier);
        }
    }

    // Check if the player can afford an upgrade to the next tier.
    public static bool CanUpgrade(SimState state)
    {
        var haven = state.Haven;
        if (haven == null || !haven.Discovered) return false;
        if (haven.UpgradeTicksRemaining > 0) return false; // Already upgrading
        if ((int)haven.Tier >= (int)HavenTier.Awakened) return false; // Max tier

        var nextTier = (HavenTier)((int)haven.Tier + 1);
        return HasUpgradeResources(state, nextTier);
    }

    public static bool HasUpgradeResources(SimState state, HavenTier targetTier)
    {
        switch (targetTier)
        {
            case HavenTier.Inhabited: // Tier 2
                return state.PlayerCredits >= HavenTweaksV0.UpgradeCreditsTier2
                    && GetPlayerGood(state, WellKnownGoodIds.ExoticMatter) >= HavenTweaksV0.UpgradeExoticMatterTier2
                    && GetPlayerGood(state, WellKnownGoodIds.Composites) >= HavenTweaksV0.UpgradeCompositesTier2
                    && GetPlayerGood(state, WellKnownGoodIds.Electronics) >= HavenTweaksV0.UpgradeElectronicsTier2;

            case HavenTier.Operational: // Tier 3
                return state.PlayerCredits >= HavenTweaksV0.UpgradeCreditsTier3
                    && GetPlayerGood(state, WellKnownGoodIds.ExoticMatter) >= HavenTweaksV0.UpgradeExoticMatterTier3
                    && GetPlayerGood(state, WellKnownGoodIds.RareMetals) >= HavenTweaksV0.UpgradeRareMetalsTier3
                    && GetPlayerGood(state, WellKnownGoodIds.Composites) >= HavenTweaksV0.UpgradeCompositesTier3
                    && (state.Haven?.InstalledFragmentIds?.Count ?? 0) >= HavenTweaksV0.FragmentsRequiredTier3;

            case HavenTier.Expanded: // Tier 4
                return state.PlayerCredits >= HavenTweaksV0.UpgradeCreditsTier4
                    && GetPlayerGood(state, WellKnownGoodIds.ExoticMatter) >= HavenTweaksV0.UpgradeExoticMatterTier4
                    && GetPlayerGood(state, WellKnownGoodIds.RareMetals) >= HavenTweaksV0.UpgradeRareMetalsTier4
                    && GetPlayerGood(state, WellKnownGoodIds.Electronics) >= HavenTweaksV0.UpgradeElectronicsTier4
                    && GetPlayerGood(state, WellKnownGoodIds.ExoticCrystals) >= HavenTweaksV0.UpgradeExoticCrystalsTier4
                    && (state.Haven?.InstalledFragmentIds?.Count ?? 0) >= (HavenTweaksV0.FragmentsRequiredTier3 + HavenTweaksV0.FragmentsRequiredTier4);

            case HavenTier.Awakened: // Tier 5
                return state.PlayerCredits >= HavenTweaksV0.UpgradeCreditsTier5
                    && GetPlayerGood(state, WellKnownGoodIds.ExoticMatter) >= HavenTweaksV0.UpgradeExoticMatterTier5
                    && GetPlayerGood(state, WellKnownGoodIds.RareMetals) >= HavenTweaksV0.UpgradeRareMetalsTier5
                    && GetPlayerGood(state, WellKnownGoodIds.Electronics) >= HavenTweaksV0.UpgradeElectronicsTier5
                    && GetPlayerGood(state, WellKnownGoodIds.ExoticCrystals) >= HavenTweaksV0.UpgradeExoticCrystalsTier5
                    && GetPlayerGood(state, WellKnownGoodIds.SalvagedTech) >= HavenTweaksV0.UpgradeSalvagedTechTier5
                    && (state.Haven?.InstalledFragmentIds?.Count ?? 0) >= (HavenTweaksV0.FragmentsRequiredTier3 + HavenTweaksV0.FragmentsRequiredTier4 + HavenTweaksV0.FragmentsRequiredTier5);

            default:
                return false;
        }
    }

    public static void DeductUpgradeResources(SimState state, HavenTier targetTier)
    {
        switch (targetTier)
        {
            case HavenTier.Inhabited:
                state.PlayerCredits -= HavenTweaksV0.UpgradeCreditsTier2;
                DeductGood(state, WellKnownGoodIds.ExoticMatter, HavenTweaksV0.UpgradeExoticMatterTier2);
                DeductGood(state, WellKnownGoodIds.Composites, HavenTweaksV0.UpgradeCompositesTier2);
                DeductGood(state, WellKnownGoodIds.Electronics, HavenTweaksV0.UpgradeElectronicsTier2);
                break;
            case HavenTier.Operational:
                state.PlayerCredits -= HavenTweaksV0.UpgradeCreditsTier3;
                DeductGood(state, WellKnownGoodIds.ExoticMatter, HavenTweaksV0.UpgradeExoticMatterTier3);
                DeductGood(state, WellKnownGoodIds.RareMetals, HavenTweaksV0.UpgradeRareMetalsTier3);
                DeductGood(state, WellKnownGoodIds.Composites, HavenTweaksV0.UpgradeCompositesTier3);
                break;
            case HavenTier.Expanded:
                state.PlayerCredits -= HavenTweaksV0.UpgradeCreditsTier4;
                DeductGood(state, WellKnownGoodIds.ExoticMatter, HavenTweaksV0.UpgradeExoticMatterTier4);
                DeductGood(state, WellKnownGoodIds.RareMetals, HavenTweaksV0.UpgradeRareMetalsTier4);
                DeductGood(state, WellKnownGoodIds.Electronics, HavenTweaksV0.UpgradeElectronicsTier4);
                DeductGood(state, WellKnownGoodIds.ExoticCrystals, HavenTweaksV0.UpgradeExoticCrystalsTier4);
                break;
            case HavenTier.Awakened:
                state.PlayerCredits -= HavenTweaksV0.UpgradeCreditsTier5;
                DeductGood(state, WellKnownGoodIds.ExoticMatter, HavenTweaksV0.UpgradeExoticMatterTier5);
                DeductGood(state, WellKnownGoodIds.RareMetals, HavenTweaksV0.UpgradeRareMetalsTier5);
                DeductGood(state, WellKnownGoodIds.Electronics, HavenTweaksV0.UpgradeElectronicsTier5);
                DeductGood(state, WellKnownGoodIds.ExoticCrystals, HavenTweaksV0.UpgradeExoticCrystalsTier5);
                DeductGood(state, WellKnownGoodIds.SalvagedTech, HavenTweaksV0.UpgradeSalvagedTechTier5);
                break;
        }
    }

    public static int GetUpgradeDuration(HavenTier targetTier)
    {
        return targetTier switch
        {
            HavenTier.Inhabited => HavenTweaksV0.UpgradeDurationTier2,
            HavenTier.Operational => HavenTweaksV0.UpgradeDurationTier3,
            HavenTier.Expanded => HavenTweaksV0.UpgradeDurationTier4,
            HavenTier.Awakened => HavenTweaksV0.UpgradeDurationTier5,
            _ => 0,
        };
    }

    public static int GetMaxHangarBays(HavenTier tier)
    {
        if (tier >= HavenTier.Awakened) return HavenTweaksV0.HangarBaysTier5;
        if (tier >= HavenTier.Operational) return HavenTweaksV0.HangarBaysTier3;
        return HavenTweaksV0.HangarBaysTier1;
    }

    // GATE.S8.HAVEN.KEEPER.001: Keeper ambient tier progression.
    // Called per tick — checks if Keeper should advance based on cumulative player investment.
    public static void ProcessKeeper(SimState state)
    {
        var haven = state.Haven;
        if (haven == null || !haven.Discovered) return;
        if (haven.KeeperLevel >= KeeperTier.Awakened) return; // Max Keeper tier

        int exo = haven.ExoticMatterDelivered;
        int frags = haven.InstalledFragmentIds?.Count ?? 0;
        int logs = haven.DataLogsDiscovered;

        KeeperTier target = KeeperTier.Dormant;

        if (exo >= HavenTweaksV0.KeeperAwakenedExoticMatter
            && frags >= HavenTweaksV0.KeeperAwakenedFragments
            && logs >= HavenTweaksV0.KeeperAwakenedDataLogs)
            target = KeeperTier.Awakened;
        else if (exo >= HavenTweaksV0.KeeperCommunicatingExoticMatter
            && frags >= HavenTweaksV0.KeeperCommunicatingFragments
            && logs >= HavenTweaksV0.KeeperCommunicatingDataLogs)
            target = KeeperTier.Communicating;
        else if (exo >= HavenTweaksV0.KeeperGuidingExoticMatter
            && frags >= HavenTweaksV0.KeeperGuidingFragments)
            target = KeeperTier.Guiding;
        else if (exo >= HavenTweaksV0.KeeperAwareExoticMatter)
            target = KeeperTier.Aware;

        if (target > haven.KeeperLevel)
            haven.KeeperLevel = target;
    }

    private static int GetPlayerGood(SimState state, string goodId)
    {
        return state.PlayerCargo.TryGetValue(goodId, out var qty) ? qty : 0;
    }

    private static void DeductGood(SimState state, string goodId, int amount)
    {
        if (!state.PlayerCargo.ContainsKey(goodId)) return;
        state.PlayerCargo[goodId] -= amount;
        if (state.PlayerCargo[goodId] <= 0)
            state.PlayerCargo.Remove(goodId);
    }
}
