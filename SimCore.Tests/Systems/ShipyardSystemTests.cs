using NUnit.Framework;
using SimCore;
using SimCore.Gen;
using SimCore.Entities;
using SimCore.Content;
using SimCore.Commands;
using SimCore.Systems;
using SimCore.Tweaks;
using System.Linq;

namespace SimCore.Tests.Systems;

// GATE.T59.SHIP.PURCHASE_CONTRACT.001: Contract tests for shipyard purchase/sell system.
[TestFixture]
[Category("ShipyardContract")]
public sealed class ShipyardSystemTests
{
    private SimState CreateTestState()
    {
        var kernel = new SimKernel(42);
        GalaxyGenerator.Generate(kernel.State, 12, 100f);
        kernel.State.PlayerCredits = 50000;

        // Add a player hero fleet (GalaxyGenerator doesn't create one; WorldLoader does).
        var startNode = kernel.State.Nodes.Keys.First();
        var heroFleet = new Fleet
        {
            Id = "player_hero",
            OwnerId = "player",
            ShipClassId = "corvette",
            CurrentNodeId = startNode,
            State = FleetState.Docked,
            HullHp = 60, HullHpMax = 60,
            ShieldHp = 35, ShieldHpMax = 35,
            FuelCapacity = 500, FuelCurrent = 500,
            ZoneArmorHp = new[] { 25, 20, 20, 15 },
            ZoneArmorHpMax = new[] { 25, 20, 20, 15 },
        };
        kernel.State.Fleets["player_hero"] = heroFleet;

        return kernel.State;
    }

    /// <summary>Find or create a faction-owned station node (shipyard-capable).</summary>
    /// <remarks>
    /// GalaxyGenerator creates Star nodes, not Station nodes.
    /// We promote the first faction-owned node to Station kind for test purposes.
    /// </remarks>
    private string FindOrCreateShipyardNode(SimState state)
    {
        foreach (var kv in state.Nodes)
        {
            if (state.NodeFactionId.TryGetValue(kv.Key, out var fid) &&
                !string.IsNullOrEmpty(fid) &&
                !string.Equals(fid, FactionTweaksV0.PirateId, System.StringComparison.Ordinal))
            {
                kv.Value.Kind = NodeKind.Station;
                return kv.Key;
            }
        }
        return "";
    }

    /// <summary>Get the player hero fleet (non-stored).</summary>
    private Fleet GetHeroFleet(SimState state) =>
        state.Fleets.Values.First(f =>
            string.Equals(f.OwnerId, "player") && !f.IsStored);

    // ── Price lookup ──

    [Test]
    public void GetPurchasePrice_KnownClass_ReturnsPrice()
    {
        Assert.That(ShipyardSystem.GetPurchasePrice("corvette"), Is.EqualTo(ShipyardTweaksV0.PriceCorvette));
        Assert.That(ShipyardSystem.GetPurchasePrice("shuttle"), Is.EqualTo(ShipyardTweaksV0.PriceShuttle));
        Assert.That(ShipyardSystem.GetPurchasePrice("dreadnought"), Is.EqualTo(ShipyardTweaksV0.PriceDreadnought));
    }

    [Test]
    public void GetPurchasePrice_VariantClass_ReturnsVariantPrice()
    {
        Assert.That(ShipyardSystem.GetPurchasePrice("watchman"), Is.EqualTo(ShipyardTweaksV0.PriceWatchman));
        Assert.That(ShipyardSystem.GetPurchasePrice("fang"), Is.EqualTo(ShipyardTweaksV0.PriceFang));
    }

    [Test]
    public void GetPurchasePrice_UnknownClass_ReturnsZero()
    {
        Assert.That(ShipyardSystem.GetPurchasePrice("nonexistent"), Is.EqualTo(0));
        Assert.That(ShipyardSystem.GetPurchasePrice(""), Is.EqualTo(0));
        Assert.That(ShipyardSystem.GetPurchasePrice(null!), Is.EqualTo(0));
    }

    // ── IsShipyardStation ──

    [Test]
    public void IsShipyardStation_FactionStation_ReturnsTrue()
    {
        var state = CreateTestState();
        var nodeId = FindOrCreateShipyardNode(state);
        Assert.That(nodeId, Is.Not.Empty, "Test galaxy should have at least one faction station");
        Assert.That(ShipyardSystem.IsShipyardStation(state, nodeId), Is.True);
    }

    [Test]
    public void IsShipyardStation_NonStation_ReturnsFalse()
    {
        var state = CreateTestState();
        var starNode = state.Nodes.Values.FirstOrDefault(n => n.Kind == NodeKind.Star);
        if (starNode != null)
            Assert.That(ShipyardSystem.IsShipyardStation(state, starNode.Id), Is.False);
    }

    [Test]
    public void IsShipyardStation_NullInputs_ReturnsFalse()
    {
        Assert.That(ShipyardSystem.IsShipyardStation(null!, "any"), Is.False);
        var state = CreateTestState();
        Assert.That(ShipyardSystem.IsShipyardStation(state, ""), Is.False);
        Assert.That(ShipyardSystem.IsShipyardStation(state, "nonexistent_node"), Is.False);
    }

    // ── CreateFleetFromClass ──

    [Test]
    public void CreateFleetFromClass_SetsStatsFromClassDef()
    {
        var state = CreateTestState();
        var classDef = ShipClassContentV0.GetById("frigate")!;
        var fleet = ShipyardSystem.CreateFleetFromClass(classDef, "some_node", state);

        Assert.That(fleet.OwnerId, Is.EqualTo("player"));
        Assert.That(fleet.ShipClassId, Is.EqualTo("frigate"));
        Assert.That(fleet.HullHp, Is.EqualTo(classDef.CoreHull));
        Assert.That(fleet.HullHpMax, Is.EqualTo(classDef.CoreHull));
        Assert.That(fleet.ShieldHp, Is.EqualTo(classDef.BaseShield));
        Assert.That(fleet.ShieldHpMax, Is.EqualTo(classDef.BaseShield));
        Assert.That(fleet.FuelCapacity, Is.EqualTo(classDef.BaseFuelCapacity));
        Assert.That(fleet.FuelCurrent, Is.EqualTo(classDef.BaseFuelCapacity));
        Assert.That(fleet.IsStored, Is.True);
        Assert.That(fleet.Slots.Count, Is.EqualTo(classDef.SlotCount));
        Assert.That(fleet.ZoneArmorHp, Is.EqualTo(classDef.BaseZoneArmor));
    }

    // ── PurchaseShipCommand ──

    [Test]
    public void PurchaseShip_ValidPurchase_DeductsCreditsAndCreatesFleet()
    {
        var state = CreateTestState();
        var nodeId = FindOrCreateShipyardNode(state);
        Assert.That(nodeId, Is.Not.Empty);

        // Dock hero fleet at the shipyard station.
        var hero = GetHeroFleet(state);
        hero.CurrentNodeId = nodeId;
        hero.State = FleetState.Docked;

        long initialCredits = state.PlayerCredits;
        int initialFleetCount = state.Fleets.Count;
        int price = ShipyardSystem.GetPurchasePrice("frigate");

        var cmd = new PurchaseShipCommand("frigate", nodeId);
        cmd.Execute(state);

        Assert.That(state.PlayerCredits, Is.EqualTo(initialCredits - price));
        Assert.That(state.Fleets.Count, Is.EqualTo(initialFleetCount + 1));

        // Find the newly created fleet.
        var newFleet = state.Fleets.Values.FirstOrDefault(f =>
            f.ShipClassId == "frigate" && f.IsStored);
        Assert.That(newFleet, Is.Not.Null);
        Assert.That(newFleet!.CurrentNodeId, Is.EqualTo(nodeId));
    }

    [Test]
    public void PurchaseShip_InsufficientCredits_NoChange()
    {
        var state = CreateTestState();
        state.PlayerCredits = 100L; // Not enough for any ship
        var nodeId = FindOrCreateShipyardNode(state);
        var hero = GetHeroFleet(state);
        hero.CurrentNodeId = nodeId;
        hero.State = FleetState.Docked;

        int fleetCount = state.Fleets.Count;
        var cmd = new PurchaseShipCommand("frigate", nodeId);
        cmd.Execute(state);

        Assert.That(state.PlayerCredits, Is.EqualTo(100));
        Assert.That(state.Fleets.Count, Is.EqualTo(fleetCount));
    }

    [Test]
    public void PurchaseShip_NotDockedAtStation_NoChange()
    {
        var state = CreateTestState();
        var nodeId = FindOrCreateShipyardNode(state);
        var hero = GetHeroFleet(state);
        hero.CurrentNodeId = nodeId;
        hero.State = FleetState.Traveling; // Not docked

        int fleetCount = state.Fleets.Count;
        var cmd = new PurchaseShipCommand("corvette", nodeId);
        cmd.Execute(state);

        Assert.That(state.Fleets.Count, Is.EqualTo(fleetCount));
    }

    [Test]
    public void PurchaseShip_VariantWithoutRep_Blocked()
    {
        var state = CreateTestState();
        var nodeId = FindOrCreateShipyardNode(state);
        var hero = GetHeroFleet(state);
        hero.CurrentNodeId = nodeId;
        hero.State = FleetState.Docked;

        // Watchman is a Concord variant requiring rep 75.
        // Player has no Concord rep by default.
        int fleetCount = state.Fleets.Count;
        var cmd = new PurchaseShipCommand("watchman", nodeId);
        cmd.Execute(state);

        Assert.That(state.Fleets.Count, Is.EqualTo(fleetCount), "Variant purchase without rep should be blocked");
    }

    [Test]
    public void PurchaseShip_VariantWithRep_Succeeds()
    {
        var state = CreateTestState();
        var nodeId = FindOrCreateShipyardNode(state);
        var hero = GetHeroFleet(state);
        hero.CurrentNodeId = nodeId;
        hero.State = FleetState.Docked;

        // Give player enough Concord rep for variant purchase.
        var watchman = ShipClassContentV0.GetById("watchman")!;
        state.FactionReputation[watchman.FactionId] = ShipyardTweaksV0.VariantRepRequired;

        long initialCredits = state.PlayerCredits;
        int fleetCount = state.Fleets.Count;
        var cmd = new PurchaseShipCommand("watchman", nodeId);
        cmd.Execute(state);

        Assert.That(state.Fleets.Count, Is.EqualTo(fleetCount + 1));
        Assert.That(state.PlayerCredits, Is.EqualTo(initialCredits - ShipyardTweaksV0.PriceWatchman));
    }

    [Test]
    public void PurchaseShip_RecordsTransaction()
    {
        var state = CreateTestState();
        var nodeId = FindOrCreateShipyardNode(state);
        var hero = GetHeroFleet(state);
        hero.CurrentNodeId = nodeId;
        hero.State = FleetState.Docked;

        int txCountBefore = state.TransactionLog.Count;
        var cmd = new PurchaseShipCommand("corvette", nodeId);
        cmd.Execute(state);

        Assert.That(state.TransactionLog.Count, Is.EqualTo(txCountBefore + 1));
        var tx = state.TransactionLog.Last();
        Assert.That(tx.Source, Is.EqualTo("ShipPurchase"));
        Assert.That(tx.CashDelta, Is.LessThan(0));
    }

    // ── SellShipCommand ──

    [Test]
    public void SellShip_StoredShipAtShipyard_CreditsPlayerAndRemoves()
    {
        var state = CreateTestState();
        var nodeId = FindOrCreateShipyardNode(state);
        var hero = GetHeroFleet(state);
        hero.CurrentNodeId = nodeId;
        hero.State = FleetState.Docked;

        // Purchase a ship first so we have something to sell.
        var purchaseCmd = new PurchaseShipCommand("corvette", nodeId);
        purchaseCmd.Execute(state);

        var storedFleet = state.Fleets.Values.First(f => f.IsStored && f.ShipClassId == "corvette");
        long creditsBeforeSell = state.PlayerCredits;
        int expectedSellPrice = (int)((long)ShipyardTweaksV0.PriceCorvette * ShipyardTweaksV0.SellBackPctBps / ShipyardTweaksV0.BpsDivisor);

        var sellCmd = new SellShipCommand(storedFleet.Id);
        sellCmd.Execute(state);

        Assert.That(state.PlayerCredits, Is.EqualTo(creditsBeforeSell + expectedSellPrice));
        Assert.That(state.Fleets.ContainsKey(storedFleet.Id), Is.False);
    }

    [Test]
    public void SellShip_HeroShip_Blocked()
    {
        var state = CreateTestState();
        var hero = GetHeroFleet(state);
        var nodeId = FindOrCreateShipyardNode(state);
        hero.CurrentNodeId = nodeId;
        hero.State = FleetState.Docked;

        int fleetCount = state.Fleets.Count;
        var cmd = new SellShipCommand(hero.Id);
        cmd.Execute(state);

        Assert.That(state.Fleets.Count, Is.EqualTo(fleetCount), "Cannot sell hero ship");
        Assert.That(state.Fleets.ContainsKey(hero.Id), Is.True);
    }

    [Test]
    public void SellShip_NotAtShipyard_Blocked()
    {
        var state = CreateTestState();
        var nodeId = FindOrCreateShipyardNode(state);
        var hero = GetHeroFleet(state);
        hero.CurrentNodeId = nodeId;
        hero.State = FleetState.Docked;

        // Purchase a ship, then move it to a non-shipyard node.
        var purchaseCmd = new PurchaseShipCommand("shuttle", nodeId);
        purchaseCmd.Execute(state);

        var storedFleet = state.Fleets.Values.First(f => f.IsStored && f.ShipClassId == "shuttle");
        storedFleet.CurrentNodeId = "nonexistent_fake_node";

        int fleetCount = state.Fleets.Count;
        var cmd = new SellShipCommand(storedFleet.Id);
        cmd.Execute(state);

        Assert.That(state.Fleets.Count, Is.EqualTo(fleetCount), "Cannot sell at non-shipyard");
    }

    [Test]
    public void SellShip_RecordsTransaction()
    {
        var state = CreateTestState();
        var nodeId = FindOrCreateShipyardNode(state);
        var hero = GetHeroFleet(state);
        hero.CurrentNodeId = nodeId;
        hero.State = FleetState.Docked;

        var purchaseCmd = new PurchaseShipCommand("shuttle", nodeId);
        purchaseCmd.Execute(state);
        var storedFleet = state.Fleets.Values.First(f => f.IsStored && f.ShipClassId == "shuttle");

        int txCountBefore = state.TransactionLog.Count;
        var sellCmd = new SellShipCommand(storedFleet.Id);
        sellCmd.Execute(state);

        Assert.That(state.TransactionLog.Count, Is.EqualTo(txCountBefore + 1));
        var tx = state.TransactionLog.Last();
        Assert.That(tx.Source, Is.EqualTo("ShipSale"));
        Assert.That(tx.CashDelta, Is.GreaterThan(0));
    }

    // ── Sell-back ratio ──

    [Test]
    public void SellBackPrice_Is80PercentOfPurchase()
    {
        int price = ShipyardTweaksV0.PriceFrigate;
        int expected = (int)((long)price * ShipyardTweaksV0.SellBackPctBps / ShipyardTweaksV0.BpsDivisor);

        // 80% of 4000 = 3200
        Assert.That(expected, Is.EqualTo(3200));
        Assert.That(ShipyardTweaksV0.SellBackPctBps, Is.EqualTo(8000));
    }

    // ── All base + variant classes have prices ──

    [Test]
    public void AllShipClasses_HavePrices()
    {
        foreach (var cls in ShipClassContentV0.AllClasses)
        {
            // Skip ancient hulls and lattice drones (not purchasable).
            if (cls.ClassId.StartsWith("ancient_") || cls.ClassId.StartsWith("lattice_"))
                continue;

            int price = ShipyardSystem.GetPurchasePrice(cls.ClassId);
            Assert.That(price, Is.GreaterThan(0),
                $"Ship class '{cls.ClassId}' should have a purchase price");
        }
    }
}
