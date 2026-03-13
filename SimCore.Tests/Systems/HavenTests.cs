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
    public void Haven_NodeId_IsFarthestFromStart()
    {
        var kernel = CreateKernel();
        var state = kernel.State;
        var startNode = state.Nodes[state.PlayerLocationNodeId];
        var havenNode = state.Nodes[state.Haven.NodeId];

        // Haven should be one of the farthest nodes
        float havenDist = (havenNode.Position.X - startNode.Position.X) * (havenNode.Position.X - startNode.Position.X)
                        + (havenNode.Position.Z - startNode.Position.Z) * (havenNode.Position.Z - startNode.Position.Z);

        // Verify no other node is farther
        foreach (var kv in state.Nodes)
        {
            if (kv.Key == state.PlayerLocationNodeId) continue;
            var dx = kv.Value.Position.X - startNode.Position.X;
            var dz = kv.Value.Position.Z - startNode.Position.Z;
            var dist = dx * dx + dz * dz;
            Assert.That(havenDist, Is.GreaterThanOrEqualTo(dist),
                $"Node {kv.Key} is farther than Haven node {state.Haven.NodeId}");
        }
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

    private static string GetFirstFleetId(SimState state)
    {
        var e = state.Fleets.Keys.GetEnumerator();
        e.MoveNext();
        return e.Current;
    }
}
