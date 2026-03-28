using NUnit.Framework;
using SimCore;
using SimCore.Gen;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Commands;
using SimCore.Tweaks;
using SimCore.Content;
using System.Collections.Generic;

namespace SimCore.Tests.Systems;

// GATE.S8.HAVEN.CONTRACT.001: Contract tests for Haven systems.
[TestFixture]
public sealed class HavenTests
{
    private SimKernel CreateKernel(int seed = 42)
    {
        var kernel = new SimKernel(seed);
        GalaxyGenerator.Generate(kernel.State, 12, 100f);
        return kernel;
    }

    // --- HavenUpgradeSystem ---

    [Test]
    public void Haven_StartsUndiscovered()
    {
        var kernel = CreateKernel();
        Assert.That(kernel.State.Haven, Is.Not.Null);
        Assert.That(kernel.State.Haven.Discovered, Is.False);
        Assert.That(kernel.State.Haven.Tier, Is.EqualTo(HavenTier.Undiscovered));
        Assert.That(kernel.State.Haven.NodeId, Is.Not.Empty);
    }

    [Test]
    public void Haven_NodeId_IsNotAtPlayerStart()
    {
        var kernel = CreateKernel();
        var state = kernel.State;

        // Haven must exist and not be at the player's start node.
        Assert.That(state.Haven, Is.Not.Null);
        Assert.That(state.Haven.NodeId, Is.Not.Empty);
        Assert.That(state.Haven.NodeId, Is.Not.EqualTo(state.PlayerLocationNodeId),
            "Haven should not be placed at the player start node");

        // Haven node must exist in the graph.
        Assert.That(state.Nodes.ContainsKey(state.Haven.NodeId), Is.True,
            "Haven node ID must reference an existing node");
    }

    [Test]
    public void CanUpgrade_ReturnsFalse_WhenUndiscovered()
    {
        var kernel = CreateKernel();
        Assert.That(HavenUpgradeSystem.CanUpgrade(kernel.State), Is.False);
    }

    [Test]
    public void CanUpgrade_ReturnsFalse_WhenNoResources()
    {
        var kernel = CreateKernel();
        kernel.State.Haven.Discovered = true;
        kernel.State.Haven.Tier = HavenTier.Powered;
        kernel.State.PlayerCredits = 0;
        Assert.That(HavenUpgradeSystem.CanUpgrade(kernel.State), Is.False);
    }

    [Test]
    public void CanUpgrade_ReturnsTrue_WhenResourcesAvailable_Tier2()
    {
        var kernel = CreateKernel();
        kernel.State.Haven.Discovered = true;
        kernel.State.Haven.Tier = HavenTier.Powered;

        kernel.State.PlayerCredits = HavenTweaksV0.UpgradeCreditsTier2;
        kernel.State.PlayerCargo[WellKnownGoodIds.ExoticMatter] = HavenTweaksV0.UpgradeExoticMatterTier2;
        kernel.State.PlayerCargo[WellKnownGoodIds.Composites] = HavenTweaksV0.UpgradeCompositesTier2;
        kernel.State.PlayerCargo[WellKnownGoodIds.Electronics] = HavenTweaksV0.UpgradeElectronicsTier2;

        Assert.That(HavenUpgradeSystem.CanUpgrade(kernel.State), Is.True);
    }

    [Test]
    public void UpgradeCommand_DeductsResources_AndStartsTimer()
    {
        var kernel = CreateKernel();
        kernel.State.Haven.Discovered = true;
        kernel.State.Haven.Tier = HavenTier.Powered;

        kernel.State.PlayerCredits = HavenTweaksV0.UpgradeCreditsTier2 + 100;
        kernel.State.PlayerCargo[WellKnownGoodIds.ExoticMatter] = HavenTweaksV0.UpgradeExoticMatterTier2 + 5;
        kernel.State.PlayerCargo[WellKnownGoodIds.Composites] = HavenTweaksV0.UpgradeCompositesTier2;
        kernel.State.PlayerCargo[WellKnownGoodIds.Electronics] = HavenTweaksV0.UpgradeElectronicsTier2;

        kernel.EnqueueCommand(new UpgradeHavenCommand());
        kernel.Step();

        Assert.That(kernel.State.PlayerCredits, Is.EqualTo(100));
        Assert.That(kernel.State.Haven.UpgradeTicksRemaining, Is.GreaterThan(0));
        Assert.That(kernel.State.Haven.UpgradeTargetTier, Is.EqualTo(HavenTier.Inhabited));
        // Exotic matter should have been deducted
        Assert.That(kernel.State.PlayerCargo.GetValueOrDefault(WellKnownGoodIds.ExoticMatter), Is.EqualTo(5));
    }

    [Test]
    public void UpgradeProcess_AdvancesTier_WhenTimerExpires()
    {
        var kernel = CreateKernel();
        kernel.State.Haven.Discovered = true;
        kernel.State.Haven.Tier = HavenTier.Powered;
        kernel.State.Haven.UpgradeTargetTier = HavenTier.Inhabited;
        kernel.State.Haven.UpgradeTicksRemaining = 2;

        kernel.Step(); // tick 1: remaining -> 1
        Assert.That(kernel.State.Haven.Tier, Is.EqualTo(HavenTier.Powered));

        kernel.Step(); // tick 2: remaining -> 0, tier advances
        Assert.That(kernel.State.Haven.Tier, Is.EqualTo(HavenTier.Inhabited));
        Assert.That(kernel.State.Haven.UpgradeTicksRemaining, Is.EqualTo(0));
    }

    [Test]
    public void UpgradeToTier3_UnlocksBidirectionalThread()
    {
        var kernel = CreateKernel();
        kernel.State.Haven.Discovered = true;
        kernel.State.Haven.Tier = HavenTier.Inhabited;
        kernel.State.Haven.UpgradeTargetTier = HavenTier.Operational;
        kernel.State.Haven.UpgradeTicksRemaining = 1;

        Assert.That(kernel.State.Haven.BidirectionalThread, Is.False);

        kernel.Step();
        Assert.That(kernel.State.Haven.Tier, Is.EqualTo(HavenTier.Operational));
        Assert.That(kernel.State.Haven.BidirectionalThread, Is.True);
    }

    [Test]
    public void CanUpgrade_ReturnsFalse_WhenAlreadyAtMaxTier()
    {
        var kernel = CreateKernel();
        kernel.State.Haven.Discovered = true;
        kernel.State.Haven.Tier = HavenTier.Awakened;
        Assert.That(HavenUpgradeSystem.CanUpgrade(kernel.State), Is.False);
    }

    [Test]
    public void CanUpgrade_ReturnsFalse_WhenAlreadyUpgrading()
    {
        var kernel = CreateKernel();
        kernel.State.Haven.Discovered = true;
        kernel.State.Haven.Tier = HavenTier.Powered;
        kernel.State.Haven.UpgradeTicksRemaining = 10;
        Assert.That(HavenUpgradeSystem.CanUpgrade(kernel.State), Is.False);
    }

    // --- HavenHangarSystem ---

    [Test]
    public void CanStore_ReturnsFalse_WhenUndiscovered()
    {
        var kernel = CreateKernel();
        var fleetId = kernel.State.Fleets.Keys.GetEnumerator();
        fleetId.MoveNext();
        Assert.That(HavenHangarSystem.CanStore(kernel.State, fleetId.Current), Is.False);
    }

    [Test]
    public void CanStore_ReturnsFalse_AtTier1_NoBaysAvailable()
    {
        var kernel = CreateKernel();
        kernel.State.Haven.Discovered = true;
        kernel.State.Haven.Tier = HavenTier.Powered;

        // Tier 1 has maxBays=1, maxStored = maxBays-1 = 0 (no storage slots)
        var fleetId = GetFirstFleetId(kernel.State);
        Assert.That(HavenHangarSystem.CanStore(kernel.State, fleetId), Is.False);
    }

    [Test]
    public void CanStore_ReturnsTrue_AtTier3()
    {
        var kernel = CreateKernel();
        kernel.State.Haven.Discovered = true;
        kernel.State.Haven.Tier = HavenTier.Operational;

        var fleetId = GetFirstFleetId(kernel.State);
        Assert.That(HavenHangarSystem.CanStore(kernel.State, fleetId), Is.True);
    }

    [Test]
    public void StoreFleet_MarksFleetAsStored()
    {
        var kernel = CreateKernel();
        kernel.State.Haven.Discovered = true;
        kernel.State.Haven.Tier = HavenTier.Operational;

        var fleetId = GetFirstFleetId(kernel.State);
        var result = HavenHangarSystem.StoreFleet(kernel.State, fleetId);

        Assert.That(result, Is.True);
        Assert.That(kernel.State.Fleets[fleetId].IsStored, Is.True);
        Assert.That(kernel.State.Haven.StoredShipIds, Does.Contain(fleetId));
    }

    [Test]
    public void StoreFleet_RejectsDuplicate()
    {
        var kernel = CreateKernel();
        kernel.State.Haven.Discovered = true;
        kernel.State.Haven.Tier = HavenTier.Operational;

        var fleetId = GetFirstFleetId(kernel.State);
        HavenHangarSystem.StoreFleet(kernel.State, fleetId);
        var result2 = HavenHangarSystem.StoreFleet(kernel.State, fleetId);

        Assert.That(result2, Is.False);
    }

    [Test]
    public void SwapShip_SwapsActiveAndStored()
    {
        var kernel = CreateKernel();
        kernel.State.Haven.Discovered = true;
        kernel.State.Haven.Tier = HavenTier.Operational;

        // Need 2 fleets. Create a second one.
        var activeId = GetFirstFleetId(kernel.State);
        var storedId = "test_stored_fleet";
        kernel.State.Fleets[storedId] = new Fleet
        {
            Id = storedId,
            CurrentNodeId = kernel.State.Haven.NodeId,
            IsStored = true,
            State = FleetState.Idle
        };
        kernel.State.Haven.StoredShipIds.Add(storedId);

        // Put active fleet at Haven node
        kernel.State.Fleets[activeId].CurrentNodeId = kernel.State.Haven.NodeId;

        var result = HavenHangarSystem.SwapShip(kernel.State, activeId, storedId);

        Assert.That(result, Is.True);
        Assert.That(kernel.State.Fleets[activeId].IsStored, Is.True);
        Assert.That(kernel.State.Fleets[storedId].IsStored, Is.False);
        Assert.That(kernel.State.Fleets[storedId].State, Is.EqualTo(FleetState.Docked));
        Assert.That(kernel.State.Haven.StoredShipIds, Does.Contain(activeId));
        Assert.That(kernel.State.Haven.StoredShipIds, Does.Not.Contain(storedId));
    }

    [Test]
    public void SwapShip_Fails_WhenActiveNotAtHaven()
    {
        var kernel = CreateKernel();
        kernel.State.Haven.Discovered = true;
        kernel.State.Haven.Tier = HavenTier.Operational;

        var activeId = GetFirstFleetId(kernel.State);
        // Active fleet NOT at Haven node (default is player start)
        var storedId = "test_stored_fleet";
        kernel.State.Fleets[storedId] = new Fleet
        {
            Id = storedId,
            CurrentNodeId = kernel.State.Haven.NodeId,
            IsStored = true,
            State = FleetState.Idle
        };
        kernel.State.Haven.StoredShipIds.Add(storedId);

        var result = HavenHangarSystem.SwapShip(kernel.State, activeId, storedId);
        Assert.That(result, Is.False);
    }

    // --- Haven Market ---

    [Test]
    public void HavenMarket_NotInStateMarkets_BeforeDiscovery()
    {
        var kernel = CreateKernel();
        Assert.That(kernel.State.Markets.ContainsKey("haven_market"), Is.False);
    }

    [Test]
    public void RefreshHavenMarket_CreatesMarket_WithExoticCrystals()
    {
        var kernel = CreateKernel();
        GalaxyGenerator.RefreshHavenMarketV0(kernel.State, HavenTier.Powered);

        Assert.That(kernel.State.Markets.ContainsKey("haven_market"), Is.True);
        var mkt = kernel.State.Markets["haven_market"];
        Assert.That(mkt.Inventory.ContainsKey(WellKnownGoodIds.ExoticCrystals), Is.True);
        Assert.That(mkt.Inventory[WellKnownGoodIds.ExoticCrystals], Is.GreaterThanOrEqualTo(HavenTweaksV0.MarketStockTier1));
    }

    [Test]
    public void RefreshHavenMarket_Tier2_AddsFuelMetalOrganics()
    {
        var kernel = CreateKernel();
        GalaxyGenerator.RefreshHavenMarketV0(kernel.State, HavenTier.Inhabited);

        var mkt = kernel.State.Markets["haven_market"];
        Assert.That(mkt.Inventory.ContainsKey(WellKnownGoodIds.Fuel), Is.True);
        Assert.That(mkt.Inventory.ContainsKey(WellKnownGoodIds.Metal), Is.True);
        Assert.That(mkt.Inventory.ContainsKey(WellKnownGoodIds.Organics), Is.True);
        Assert.That(mkt.Inventory[WellKnownGoodIds.Fuel], Is.GreaterThanOrEqualTo(HavenTweaksV0.MarketStockTier2));
    }

    [Test]
    public void RefreshHavenMarket_Tier3_AddsExoticMatterSalvagedTech()
    {
        var kernel = CreateKernel();
        GalaxyGenerator.RefreshHavenMarketV0(kernel.State, HavenTier.Operational);

        var mkt = kernel.State.Markets["haven_market"];
        Assert.That(mkt.Inventory.ContainsKey(WellKnownGoodIds.ExoticMatter), Is.True);
        Assert.That(mkt.Inventory.ContainsKey(WellKnownGoodIds.SalvagedTech), Is.True);
    }

    [Test]
    public void UpgradeCompletion_RefreshesMarket()
    {
        var kernel = CreateKernel();
        kernel.State.Haven.Discovered = true;
        kernel.State.Haven.Tier = HavenTier.Powered;
        kernel.State.Haven.UpgradeTargetTier = HavenTier.Inhabited;
        kernel.State.Haven.UpgradeTicksRemaining = 1;

        kernel.Step(); // Upgrade completes, should refresh market

        Assert.That(kernel.State.Markets.ContainsKey("haven_market"), Is.True);
        var mkt = kernel.State.Markets["haven_market"];
        Assert.That(mkt.Inventory.ContainsKey(WellKnownGoodIds.Fuel), Is.True);
    }

    // --- Tractor Range ---

    [Test]
    public void TractorRange_NoModule_ReturnsFallback()
    {
        var kernel = CreateKernel();
        var fleetId = GetFirstFleetId(kernel.State);
        var fleet = kernel.State.Fleets[fleetId];
        var range = CollectLootCommand.GetTractorRange(fleet);
        Assert.That(range, Is.EqualTo(HavenTweaksV0.TractorFallbackRange));
    }

    // --- Hangar Bay Counts ---

    [Test]
    public void GetMaxHangarBays_ByTier()
    {
        Assert.That(HavenUpgradeSystem.GetMaxHangarBays(HavenTier.Powered), Is.EqualTo(HavenTweaksV0.HangarBaysTier1));
        Assert.That(HavenUpgradeSystem.GetMaxHangarBays(HavenTier.Inhabited), Is.EqualTo(HavenTweaksV0.HangarBaysTier1));
        Assert.That(HavenUpgradeSystem.GetMaxHangarBays(HavenTier.Operational), Is.EqualTo(HavenTweaksV0.HangarBaysTier3));
        Assert.That(HavenUpgradeSystem.GetMaxHangarBays(HavenTier.Expanded), Is.EqualTo(HavenTweaksV0.HangarBaysTier3));
        Assert.That(HavenUpgradeSystem.GetMaxHangarBays(HavenTier.Awakened), Is.EqualTo(HavenTweaksV0.HangarBaysTier5));
    }

    // --- Program Templates ---

    [Test]
    public void ProgramTemplates_Has5Entries()
    {
        Assert.That(ProgramTemplateContentV0.AllTemplates.Count, Is.EqualTo(5));
    }

    [Test]
    public void ProgramTemplates_AllHaveValidFields()
    {
        foreach (var tpl in ProgramTemplateContentV0.AllTemplates)
        {
            Assert.That(tpl.TemplateId, Is.Not.Empty, "TemplateId must be set");
            Assert.That(tpl.DisplayName, Is.Not.Empty, $"DisplayName missing for {tpl.TemplateId}");
            Assert.That(tpl.Description, Is.Not.Empty, $"Description missing for {tpl.TemplateId}");
            Assert.That(tpl.ProgramKind, Is.Not.Empty, $"ProgramKind missing for {tpl.TemplateId}");
            Assert.That(tpl.DefaultCadenceTicks, Is.GreaterThan(0), $"DefaultCadenceTicks must be > 0 for {tpl.TemplateId}");
        }
    }

    [Test]
    public void ProgramTemplates_GetById_ReturnsCorrectTemplate()
    {
        var tpl = ProgramTemplateContentV0.GetById("template_buy_low_sell_high");
        Assert.That(tpl, Is.Not.Null);
        Assert.That(tpl!.DisplayName, Is.Not.Empty);
    }

    [Test]
    public void ProgramTemplates_GetById_ReturnsNull_ForUnknown()
    {
        var tpl = ProgramTemplateContentV0.GetById("nonexistent");
        Assert.That(tpl, Is.Null);
    }

    // --- GATE.S8.HAVEN.KEEPER.001: Keeper progression tests ---

    [Test]
    public void Keeper_StartsDormant()
    {
        var kernel = CreateKernel();
        kernel.State.Haven.Discovered = true;
        Assert.That(kernel.State.Haven.KeeperLevel, Is.EqualTo(KeeperTier.Dormant));
    }

    [Test]
    public void Keeper_AdvancesToAware_OnExoticMatterDelivery()
    {
        var kernel = CreateKernel();
        kernel.State.Haven.Discovered = true;
        kernel.State.Haven.ExoticMatterDelivered = HavenTweaksV0.KeeperAwareExoticMatter;
        kernel.Step();
        Assert.That(kernel.State.Haven.KeeperLevel, Is.EqualTo(KeeperTier.Aware));
    }

    [Test]
    public void Keeper_AdvancesToGuiding_WhenFragmentsAndExotic()
    {
        var kernel = CreateKernel();
        kernel.State.Haven.Discovered = true;
        kernel.State.Haven.ExoticMatterDelivered = HavenTweaksV0.KeeperGuidingExoticMatter;
        kernel.State.Haven.InstalledFragmentIds.Add("frag_1");
        kernel.State.Haven.InstalledFragmentIds.Add("frag_2");
        kernel.Step();
        Assert.That(kernel.State.Haven.KeeperLevel, Is.EqualTo(KeeperTier.Guiding));
    }

    [Test]
    public void Keeper_AdvancesToCommunicating_WhenAllThresholdsMet()
    {
        var kernel = CreateKernel();
        kernel.State.Haven.Discovered = true;
        kernel.State.Haven.ExoticMatterDelivered = HavenTweaksV0.KeeperCommunicatingExoticMatter;
        for (int i = 0; i < HavenTweaksV0.KeeperCommunicatingFragments; i++)
            kernel.State.Haven.InstalledFragmentIds.Add($"frag_{i}");
        kernel.State.Haven.DataLogsDiscovered = HavenTweaksV0.KeeperCommunicatingDataLogs;
        kernel.Step();
        Assert.That(kernel.State.Haven.KeeperLevel, Is.EqualTo(KeeperTier.Communicating));
    }

    [Test]
    public void Keeper_AdvancesToAwakened_WhenFullyInvested()
    {
        var kernel = CreateKernel();
        kernel.State.Haven.Discovered = true;
        kernel.State.Haven.ExoticMatterDelivered = HavenTweaksV0.KeeperAwakenedExoticMatter;
        for (int i = 0; i < HavenTweaksV0.KeeperAwakenedFragments; i++)
            kernel.State.Haven.InstalledFragmentIds.Add($"frag_{i}");
        kernel.State.Haven.DataLogsDiscovered = HavenTweaksV0.KeeperAwakenedDataLogs;
        kernel.Step();
        Assert.That(kernel.State.Haven.KeeperLevel, Is.EqualTo(KeeperTier.Awakened));
    }

    [Test]
    public void Keeper_DoesNotRegress()
    {
        var kernel = CreateKernel();
        kernel.State.Haven.Discovered = true;
        kernel.State.Haven.ExoticMatterDelivered = HavenTweaksV0.KeeperAwareExoticMatter;
        kernel.Step();
        Assert.That(kernel.State.Haven.KeeperLevel, Is.EqualTo(KeeperTier.Aware));

        // Remove exotic matter — Keeper should NOT regress.
        kernel.State.Haven.ExoticMatterDelivered = 0;
        kernel.Step();
        Assert.That(kernel.State.Haven.KeeperLevel, Is.EqualTo(KeeperTier.Aware));
    }

    // --- GATE.S8.HAVEN.RESONANCE.001: Resonance Chamber tests ---

    private void SetupResonancePairCollected(SimState state, string pairId)
    {
        var pair = AdaptationFragmentContentV0.GetPairById(pairId)!;
        state.AdaptationFragments[pair.FragmentA] = new AdaptationFragment
        {
            FragmentId = pair.FragmentA, CollectedTick = 1, ResonancePairId = pairId
        };
        state.AdaptationFragments[pair.FragmentB] = new AdaptationFragment
        {
            FragmentId = pair.FragmentB, CollectedTick = 2, ResonancePairId = pairId
        };
    }

    [Test]
    public void Resonance_CombineSuccess_WhenTierAndFragmentsMet()
    {
        var kernel = CreateKernel();
        var state = kernel.State;
        state.Haven.Discovered = true;
        state.Haven.Tier = HavenTier.Expanded; // Tier 4
        SetupResonancePairCollected(state, "pair_01");

        var result = AdaptationFragmentSystem.CombineResonancePair(state, "pair_01");
        Assert.That(result.Success, Is.True);
        Assert.That(result.PairId, Is.EqualTo("pair_01"));
        Assert.That(result.BonusDescription, Is.Not.Empty);
        Assert.That(state.Haven.ActivatedResonancePairs, Does.Contain("pair_01"));
    }

    [Test]
    public void Resonance_RejectsTierTooLow()
    {
        var kernel = CreateKernel();
        var state = kernel.State;
        state.Haven.Discovered = true;
        state.Haven.Tier = HavenTier.Operational; // Tier 3, needs 4
        SetupResonancePairCollected(state, "pair_01");

        var result = AdaptationFragmentSystem.CombineResonancePair(state, "pair_01");
        Assert.That(result.Success, Is.False);
        Assert.That(result.Reason, Is.EqualTo("tier_too_low"));
    }

    [Test]
    public void Resonance_RejectsCooldown()
    {
        var kernel = CreateKernel();
        var state = kernel.State;
        state.Haven.Discovered = true;
        state.Haven.Tier = HavenTier.Expanded;
        state.Haven.ResonanceCooldownUntilTick = state.Tick + 100;
        SetupResonancePairCollected(state, "pair_01");

        var result = AdaptationFragmentSystem.CombineResonancePair(state, "pair_01");
        Assert.That(result.Success, Is.False);
        Assert.That(result.Reason, Is.EqualTo("cooldown_active"));
    }

    [Test]
    public void Resonance_RejectsAlreadyActivated()
    {
        var kernel = CreateKernel();
        var state = kernel.State;
        state.Haven.Discovered = true;
        state.Haven.Tier = HavenTier.Expanded;
        SetupResonancePairCollected(state, "pair_01");

        // First combine succeeds.
        AdaptationFragmentSystem.CombineResonancePair(state, "pair_01");
        // Reset cooldown for second attempt.
        state.Haven.ResonanceCooldownUntilTick = 0;

        var result = AdaptationFragmentSystem.CombineResonancePair(state, "pair_01");
        Assert.That(result.Success, Is.False);
        Assert.That(result.Reason, Is.EqualTo("already_activated"));
    }

    [Test]
    public void Resonance_RejectsFragmentsNotCollected()
    {
        var kernel = CreateKernel();
        var state = kernel.State;
        state.Haven.Discovered = true;
        state.Haven.Tier = HavenTier.Expanded;
        // Don't set up fragments — they're not collected.

        var result = AdaptationFragmentSystem.CombineResonancePair(state, "pair_01");
        Assert.That(result.Success, Is.False);
        Assert.That(result.Reason, Is.EqualTo("fragments_not_collected"));
    }

    [Test]
    public void Resonance_GetAvailablePairs_ExcludesActivated()
    {
        var kernel = CreateKernel();
        var state = kernel.State;
        state.Haven.Discovered = true;
        state.Haven.Tier = HavenTier.Expanded;
        SetupResonancePairCollected(state, "pair_01");
        SetupResonancePairCollected(state, "pair_02");

        var available = AdaptationFragmentSystem.GetAvailableResonancePairs(state);
        Assert.That(available, Does.Contain("pair_01"));
        Assert.That(available, Does.Contain("pair_02"));

        // Activate pair_01.
        AdaptationFragmentSystem.CombineResonancePair(state, "pair_01");

        available = AdaptationFragmentSystem.GetAvailableResonancePairs(state);
        Assert.That(available, Does.Not.Contain("pair_01"));
        Assert.That(available, Does.Contain("pair_02"));
    }

    // --- GATE.S8.HAVEN.FABRICATOR.001: Fabricator tests ---

    [Test]
    public void Fabricator_StartSuccess_WhenTierAndCredits()
    {
        var kernel = CreateKernel();
        var state = kernel.State;
        state.Haven.Discovered = true;
        state.Haven.Tier = HavenTier.Expanded;
        state.PlayerCredits = HavenTweaksV0.FabricateExoticMatterCost + 100;

        var result = HavenFabricatorSystem.StartFabrication(state, "mod_test_t3");
        Assert.That(result.Success, Is.True);
        Assert.That(state.Haven.FabricatingModuleId, Is.EqualTo("mod_test_t3"));
        Assert.That(state.Haven.FabricationTicksRemaining, Is.EqualTo(HavenTweaksV0.FabricateDurationTicks));
        Assert.That(state.PlayerCredits, Is.EqualTo(100));
    }

    [Test]
    public void Fabricator_RejectsTierTooLow()
    {
        var kernel = CreateKernel();
        var state = kernel.State;
        state.Haven.Discovered = true;
        state.Haven.Tier = HavenTier.Operational;
        state.PlayerCredits = 10000;

        var result = HavenFabricatorSystem.StartFabrication(state, "mod_test_t3");
        Assert.That(result.Success, Is.False);
        Assert.That(result.Reason, Is.EqualTo("tier_too_low"));
    }

    [Test]
    public void Fabricator_RejectsInsufficientCredits()
    {
        var kernel = CreateKernel();
        var state = kernel.State;
        state.Haven.Discovered = true;
        state.Haven.Tier = HavenTier.Expanded;
        state.PlayerCredits = HavenTweaksV0.FabricateExoticMatterCost - 1;

        var result = HavenFabricatorSystem.StartFabrication(state, "mod_test_t3");
        Assert.That(result.Success, Is.False);
        Assert.That(result.Reason, Is.EqualTo("insufficient_exotic_matter"));
    }

    [Test]
    public void Fabricator_RejectsAlreadyInProgress()
    {
        var kernel = CreateKernel();
        var state = kernel.State;
        state.Haven.Discovered = true;
        state.Haven.Tier = HavenTier.Expanded;
        state.PlayerCredits = 10000;

        HavenFabricatorSystem.StartFabrication(state, "mod_a");
        var result = HavenFabricatorSystem.StartFabrication(state, "mod_b");
        Assert.That(result.Success, Is.False);
        Assert.That(result.Reason, Is.EqualTo("fabrication_in_progress"));
    }

    [Test]
    public void Fabricator_CompletesAfterDurationTicks()
    {
        var kernel = CreateKernel();
        var state = kernel.State;
        state.Haven.Discovered = true;
        state.Haven.Tier = HavenTier.Expanded;
        state.PlayerCredits = 10000;

        HavenFabricatorSystem.StartFabrication(state, "mod_test_t3");

        // Step through fabrication duration.
        for (int i = 0; i < HavenTweaksV0.FabricateDurationTicks; i++)
            kernel.Step();

        Assert.That(state.Haven.FabricatingModuleId, Is.Null);
        Assert.That(state.Haven.FabricationTicksRemaining, Is.EqualTo(0));
        Assert.That(state.Haven.CompletedFabricationIds, Does.Contain("mod_test_t3"));
    }

    // --- GATE.S8.HAVEN.MARKET_EVOLUTION.001: Market evolution tests ---

    [Test]
    public void HavenMarket_RestocksOnInterval()
    {
        var kernel = CreateKernel();
        var state = kernel.State;
        state.Haven.Discovered = true;
        state.Haven.Tier = HavenTier.Inhabited; // Tier 2

        // Manually create Haven market.
        SimCore.Gen.GalaxyGenerator.RefreshHavenMarketV0(state, state.Haven.Tier);
        Assert.That(state.Markets.ContainsKey(state.Haven.MarketId), Is.True);

        var mkt = state.Markets[state.Haven.MarketId];
        // Drain fuel stock (stocked at Tier 2).
        mkt.Inventory["fuel"] = 0;

        // Step to next restock interval.
        for (int i = 0; i < HavenTweaksV0.MarketRestockIntervalTicks; i++)
            kernel.Step();

        // Market should have restocked fuel.
        Assert.That(mkt.Inventory["fuel"], Is.GreaterThan(0));
    }

    [Test]
    public void HavenMarket_DoesNotRestockBeforeInterval()
    {
        var kernel = CreateKernel();
        var state = kernel.State;
        state.Haven.Discovered = true;
        state.Haven.Tier = HavenTier.Inhabited;

        SimCore.Gen.GalaxyGenerator.RefreshHavenMarketV0(state, state.Haven.Tier);
        var mkt = state.Markets[state.Haven.MarketId];
        mkt.Inventory["fuel"] = 0;

        // First step at tick=0 restocks (0%50==0). Advance past that.
        kernel.Step(); // tick 0→1, restocks fuel
        mkt.Inventory["fuel"] = 0; // drain again after tick-0 restock

        // Step fewer ticks than interval — next restock at tick 50, we're at tick 1.
        for (int i = 0; i < HavenTweaksV0.MarketRestockIntervalTicks - 2; i++)
            kernel.Step();

        // Still haven't reached tick 50, so no restock.
        Assert.That(mkt.Inventory["fuel"], Is.EqualTo(0));
    }

    // --- GATE.S8.HAVEN.ENDGAME_PATHS.001: Endgame path tests ---

    [Test]
    public void ChooseEndgamePath_Success_AtTier4()
    {
        var kernel = CreateKernel();
        kernel.State.Haven.Discovered = true;
        kernel.State.Haven.Tier = HavenTier.Expanded;

        var result = HavenEndgameSystem.ChooseEndgamePath(kernel.State, EndgamePath.Reinforce);
        Assert.That(result, Is.True);
        Assert.That(kernel.State.Haven.ChosenEndgamePath, Is.EqualTo(EndgamePath.Reinforce));
    }

    [Test]
    public void ChooseEndgamePath_Fails_BelowTier4()
    {
        var kernel = CreateKernel();
        kernel.State.Haven.Discovered = true;
        kernel.State.Haven.Tier = HavenTier.Operational;

        var result = HavenEndgameSystem.ChooseEndgamePath(kernel.State, EndgamePath.Naturalize);
        Assert.That(result, Is.False);
        Assert.That(kernel.State.Haven.ChosenEndgamePath, Is.EqualTo(EndgamePath.None));
    }

    [Test]
    public void ChooseEndgamePath_Fails_WhenAlreadyChosen()
    {
        var kernel = CreateKernel();
        kernel.State.Haven.Discovered = true;
        kernel.State.Haven.Tier = HavenTier.Expanded;

        HavenEndgameSystem.ChooseEndgamePath(kernel.State, EndgamePath.Reinforce);
        var result = HavenEndgameSystem.ChooseEndgamePath(kernel.State, EndgamePath.Naturalize);
        Assert.That(result, Is.False);
        Assert.That(kernel.State.Haven.ChosenEndgamePath, Is.EqualTo(EndgamePath.Reinforce));
    }

    [Test]
    public void EndgamePath_Reinforce_DriftsConcordRep()
    {
        var kernel = CreateKernel();
        kernel.State.Haven.Discovered = true;
        kernel.State.Haven.Tier = HavenTier.Expanded;
        kernel.State.Haven.ChosenEndgamePath = EndgamePath.Reinforce;
        kernel.State.FactionReputation[Tweaks.FactionTweaksV0.ConcordId] = 0;

        // Step to a drift interval tick.
        for (int i = 0; i < Tweaks.EndgameTweaksV0.PathDriftIntervalTicks; i++)
            kernel.Step();

        Assert.That(kernel.State.FactionReputation[Tweaks.FactionTweaksV0.ConcordId],
            Is.GreaterThan(0));
    }

    // --- GATE.S8.HAVEN.ACCOMMODATION.001: Accommodation thread tests ---

    [Test]
    public void Accommodation_InitializesThreads_AtTier3()
    {
        var kernel = CreateKernel();
        kernel.State.Haven.Discovered = true;
        kernel.State.Haven.Tier = HavenTier.Operational;
        kernel.Step();

        foreach (var threadId in AccommodationThreadIds.All)
            Assert.That(kernel.State.Haven.AccommodationProgress.ContainsKey(threadId), Is.True);
    }

    [Test]
    public void Accommodation_DiscoveryThread_AdvancesWithFragments()
    {
        var kernel = CreateKernel();
        kernel.State.Haven.Discovered = true;
        kernel.State.Haven.Tier = HavenTier.Operational;
        kernel.State.StoryState.CollectedFragmentCount = 5;
        kernel.Step();

        Assert.That(kernel.State.Haven.AccommodationProgress[AccommodationThreadIds.Discovery],
            Is.GreaterThan(0));
    }

    [Test]
    public void Accommodation_NotInitialized_BelowTier3()
    {
        var kernel = CreateKernel();
        kernel.State.Haven.Discovered = true;
        kernel.State.Haven.Tier = HavenTier.Inhabited;
        kernel.Step();

        Assert.That(kernel.State.Haven.AccommodationProgress.Count, Is.EqualTo(0));
    }

    // --- GATE.S8.HAVEN.COMMUNION_REP.001: Communion Representative tests ---

    [Test]
    public void CommunionRep_Present_WhenTier3AndPositiveRep()
    {
        var kernel = CreateKernel();
        kernel.State.Haven.Discovered = true;
        kernel.State.Haven.Tier = HavenTier.Operational;
        kernel.State.FactionReputation[Tweaks.FactionTweaksV0.CommunionId] = 10;
        kernel.Step();

        Assert.That(kernel.State.Haven.CommunionRep.Present, Is.True);
    }

    [Test]
    public void CommunionRep_NotPresent_WhenTier2()
    {
        var kernel = CreateKernel();
        kernel.State.Haven.Discovered = true;
        kernel.State.Haven.Tier = HavenTier.Inhabited;
        kernel.State.FactionReputation[Tweaks.FactionTweaksV0.CommunionId] = 50;
        kernel.Step();

        Assert.That(kernel.State.Haven.CommunionRep.Present, Is.False);
    }

    [Test]
    public void CommunionRep_NotPresent_WhenNegativeRep()
    {
        var kernel = CreateKernel();
        kernel.State.Haven.Discovered = true;
        kernel.State.Haven.Tier = HavenTier.Operational;
        kernel.State.FactionReputation[Tweaks.FactionTweaksV0.CommunionId] = -10;
        kernel.Step();

        Assert.That(kernel.State.Haven.CommunionRep.Present, Is.False);
    }

    // GATE.T44.NARRATIVE.COMMUNION_DIALOGUE.001: Verify all 3 tiers return non-empty dialogue.
    [Test]
    public void CommunionDialogue_HasContent_ForAllTiers()
    {
        for (int tier = 0; tier < 3; tier++)
        {
            var lines = CommunionRepDialogueContentV0.GetDialogue(tier);
            Assert.That(lines, Is.Not.Null, $"Tier {tier} dialogue is null");
            Assert.That(lines.Count, Is.GreaterThan(0), $"Tier {tier} dialogue is empty");
            foreach (var line in lines)
                Assert.That(string.IsNullOrWhiteSpace(line), Is.False, $"Tier {tier} has blank line");
        }
    }

    // GATE.T44.NARRATIVE.KEEPER_EXPAND.001: Verify all 5 tiers return non-empty dialogue.
    [Test]
    public void KeeperDialogue_HasContent_ForAllTiers()
    {
        for (int tier = 0; tier <= 4; tier++)
        {
            var lines = KeeperDialogueContentV0.GetDialogue(tier);
            Assert.That(lines, Is.Not.Null, $"KeeperTier {tier} dialogue is null");
            Assert.That(lines.Count, Is.GreaterThan(0), $"KeeperTier {tier} dialogue is empty");
            foreach (var line in lines)
                Assert.That(string.IsNullOrWhiteSpace(line), Is.False, $"KeeperTier {tier} has blank line");
        }
    }

    private static string GetFirstFleetId(SimState state)
    {
        var e = state.Fleets.Keys.GetEnumerator();
        e.MoveNext();
        return e.Current;
    }
}
