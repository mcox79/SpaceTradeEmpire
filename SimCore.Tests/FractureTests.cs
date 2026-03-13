using NUnit.Framework;
using SimCore;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Gen;
using SimCore.Systems;
using System;
using System.Linq;
using System.Numerics;

namespace SimCore.Tests;

public class FractureTests
{
    [Test]
    public void Fracture_Movement_AccumulatesTrace_OnArrival()
    {
        var state = new SimState(456);
        state.FractureUnlocked = true; // GATE.S6.FRACTURE_DISCOVERY.MODEL.001

        // Setup Nodes (10 units apart)
        var n1 = new Node { Id = "n1", Position = new Vector3(0, 0, 0), Trace = 0f };
        var n2 = new Node { Id = "n2", Position = new Vector3(10, 0, 0), Trace = 0f };
        state.Nodes.Add("n1", n1);
        state.Nodes.Add("n2", n2);

        // Setup Fleet (Speed 5, should take 2 ticks)
        var fleet = new Fleet
        {
            Id = "f1",
            CurrentNodeId = "n1",
            DestinationNodeId = "n2",
            State = FleetState.FractureTraveling,
            Speed = 5.0f
        };
        state.Fleets.Add("f1", fleet);

        // Tick 1: Progress = 0.5
        FractureSystem.Process(state);
        Assert.That(fleet.State, Is.EqualTo(FleetState.FractureTraveling));
        Assert.That(fleet.TravelProgress, Is.EqualTo(0.5f).Within(0.01f));
        Assert.That(state.Nodes["n2"].Trace, Is.EqualTo(0f));

        // Tick 2: Arrival
        FractureSystem.Process(state);
        Assert.That(fleet.State, Is.EqualTo(FleetState.Idle));
        Assert.That(fleet.CurrentNodeId, Is.EqualTo("n2"));

        // Assert Trace Generation
        Assert.That(state.Nodes["n2"].Trace, Is.GreaterThan(0f));
    }

    [Test]
    public void Fracture_FleetProcessingOrder_IsDeterministic_AndPinpointsFirstMismatch()
    {
        // Setup minimal state with multiple Fracture-traveling fleets inserted in non-sorted order.
        var state = new SimState(456);
        state.FractureUnlocked = true; // GATE.S6.FRACTURE_DISCOVERY.MODEL.001

        // Nodes required for validity, but this test targets ordering only.
        state.Nodes.Add("n1", new Node { Id = "n1", Position = new Vector3(0, 0, 0), Trace = 0f });
        state.Nodes.Add("n2", new Node { Id = "n2", Position = new Vector3(1, 0, 0), Trace = 0f });

        state.Fleets.Add("f10", new Fleet { Id = "f10", CurrentNodeId = "n1", DestinationNodeId = "n2", State = FleetState.FractureTraveling, Speed = 1.0f });
        state.Fleets.Add("f2", new Fleet { Id = "f2", CurrentNodeId = "n1", DestinationNodeId = "n2", State = FleetState.FractureTraveling, Speed = 1.0f });
        state.Fleets.Add("f1", new Fleet { Id = "f1", CurrentNodeId = "n1", DestinationNodeId = "n2", State = FleetState.FractureTraveling, Speed = 1.0f });

        var actual = FractureSystem.GetFractureFleetProcessOrder(state);
        var expected = new[] { "f1", "f10", "f2" }; // Ordinal string sort

        // Deterministic first-mismatch report (no timestamps, stable formatting).
        var min = Math.Min(expected.Length, actual.Length);
        for (int i = 0; i < min; i++)
        {
            if (!string.Equals(expected[i], actual[i], StringComparison.Ordinal))
            {
                Assert.Fail($"fracture_fleet_order_mismatch mismatch_index={i} expected={expected[i]} actual={actual[i]}");
            }
        }

        if (expected.Length != actual.Length)
        {
            Assert.Fail($"fracture_fleet_order_mismatch length_expected={expected.Length} length_actual={actual.Length}");
        }

        Assert.Pass();
    }

    // GATE.S6.FRACTURE.ACCESS_MODEL.001 contract tests.

    [Test]
    public void FractureAccessCheck_QualifiedFleet_IsAllowed()
    {
        // Fleet with hull_hp_max >= 120 and TechLevel >= node FractureTier => allowed.
        var state = new SimState(100);
        state.Nodes.Add("fn1", new Node
        {
            Id = "fn1",
            Position = Vector3.Zero,
            IsFractureNode = true,
            FractureTier = 1
        });
        state.Fleets.Add("fleet_q", new Fleet
        {
            Id = "fleet_q",
            HullHpMax = 150,
            TechLevel = 1
        });

        var result = FractureSystem.FractureAccessCheck(state, "fleet_q", "fn1");

        Assert.That(result.Allowed, Is.True, "Qualified fleet must be allowed.");
        Assert.That(result.Reason, Is.EqualTo(""));
    }

    [Test]
    public void FractureAccessCheck_UnderclassHull_IsDenied_WithReason()
    {
        // Fleet with hull_hp_max < 120 => denied with hull reason.
        var state = new SimState(101);
        state.Nodes.Add("fn2", new Node
        {
            Id = "fn2",
            Position = Vector3.Zero,
            IsFractureNode = true,
            FractureTier = 0
        });
        state.Fleets.Add("fleet_weak", new Fleet
        {
            Id = "fleet_weak",
            HullHpMax = 80,
            TechLevel = 5
        });

        var result = FractureSystem.FractureAccessCheck(state, "fleet_weak", "fn2");

        Assert.That(result.Allowed, Is.False, "Underclass hull must be denied.");
        Assert.That(result.Reason, Does.Contain("hull_hp_max"), "Reason must mention hull_hp_max.");
        Assert.That(result.Reason, Does.Contain("80"), "Reason must include actual value.");
        Assert.That(result.Reason, Does.Contain("120"), "Reason must include minimum threshold.");
    }

    [Test]
    public void FractureAccessCheck_MissingTechLevel_IsDenied_WithReason()
    {
        // Fleet with hull_hp_max >= 120 but TechLevel < node FractureTier => denied with tech reason.
        var state = new SimState(102);
        state.Nodes.Add("fn3", new Node
        {
            Id = "fn3",
            Position = Vector3.Zero,
            IsFractureNode = true,
            FractureTier = 3
        });
        state.Fleets.Add("fleet_lotech", new Fleet
        {
            Id = "fleet_lotech",
            HullHpMax = 200,
            TechLevel = 2
        });

        var result = FractureSystem.FractureAccessCheck(state, "fleet_lotech", "fn3");

        Assert.That(result.Allowed, Is.False, "Insufficient tech level must be denied.");
        Assert.That(result.Reason, Does.Contain("tech_level"), "Reason must mention tech_level.");
        Assert.That(result.Reason, Does.Contain("2"), "Reason must include fleet tech level.");
        Assert.That(result.Reason, Does.Contain("3"), "Reason must include required fracture tier.");
    }

    [Test]
    public void FractureAccessCheck_UnknownNode_IsDenied_WithNodeNotFoundReason()
    {
        // Unknown node => Allowed=false, Reason="node not found".
        var state = new SimState(103);
        state.Fleets.Add("fleet_x", new Fleet { Id = "fleet_x", HullHpMax = 200, TechLevel = 5 });

        var result = FractureSystem.FractureAccessCheck(state, "fleet_x", "node_does_not_exist");

        Assert.That(result.Allowed, Is.False);
        Assert.That(result.Reason, Is.EqualTo("node not found"));
    }

    [Test]
    public void FractureAccessCheck_ZeroTierNode_OnlyRequiresDurabilityThreshold()
    {
        // Node with FractureTier=0 means any tech level is acceptable — only hull check matters.
        var state = new SimState(104);
        state.Nodes.Add("fn_tier0", new Node
        {
            Id = "fn_tier0",
            Position = Vector3.Zero,
            IsFractureNode = true,
            FractureTier = 0
        });
        state.Fleets.Add("fleet_t0", new Fleet
        {
            Id = "fleet_t0",
            HullHpMax = 120,
            TechLevel = 0
        });

        var result = FractureSystem.FractureAccessCheck(state, "fleet_t0", "fn_tier0");

        Assert.That(result.Allowed, Is.True, "TechLevel=0 fleet at FractureTier=0 node must be allowed if hull passes.");
    }

    // GATE.S6.FRACTURE.MARKET_MODEL.001 contract tests.

    [Test]
    public void FracturePricing_SameStock_HasHigherMarginThanLane()
    {
        // At same stock level, fracture buy-sell spread must be wider than lane spread.
        const int stock = 30; // below IdealStock=50 to ensure both have positive deviation.

        // Lane baseline: using Market methods directly.
        var laneMarket = new Market();
        laneMarket.Inventory["exotic_crystals"] = stock;
        int laneBuy = laneMarket.GetBuyPrice("exotic_crystals");
        int laneSell = laneMarket.GetSellPrice("exotic_crystals");
        int laneSpread = laneBuy - laneSell;

        // Fracture pricing.
        var fracResult = FractureSystem.FracturePricingV0(stock);
        int fractureSpread = fracResult.Buy - fracResult.Sell;

        Assert.That(fractureSpread, Is.GreaterThan(laneSpread),
            "Fracture spread must exceed lane spread for same stock level.");
        Assert.That(fracResult.Buy, Is.GreaterThan(laneBuy),
            "Fracture buy price must exceed lane buy price (volatility effect).");
    }

    [Test]
    public void FracturePricing_VolumeCap_IsHalfOfLaneIdealStock()
    {
        // Volume cap = 50% of IdealStock = 25 (IdealStock=50).
        var result = FractureSystem.FracturePricingV0(stock: 50);

        Assert.That(result.VolumeCap, Is.EqualTo(25),
            "Fracture volume cap must be 50% of IdealStock.");
    }

    [Test]
    public void FracturePricing_BuyAlwaysExceedsSell()
    {
        // Deterministic property: buy > sell for any stock level.
        foreach (int stock in new[] { 0, 10, 25, 50, 75, 100, 200 })
        {
            var result = FractureSystem.FracturePricingV0(stock);
            Assert.That(result.Buy, Is.GreaterThan(result.Sell),
                $"Fracture buy must exceed sell at stock={stock}.");
        }
    }

    [Test]
    public void FracturePricing_IsDeterministic_SameInputSameOutput()
    {
        // Same inputs must always produce identical outputs (no RNG, no time).
        var r1 = FractureSystem.FracturePricingV0(stock: 20);
        var r2 = FractureSystem.FracturePricingV0(stock: 20);

        Assert.That(r2.Mid, Is.EqualTo(r1.Mid));
        Assert.That(r2.Buy, Is.EqualTo(r1.Buy));
        Assert.That(r2.Sell, Is.EqualTo(r1.Sell));
        Assert.That(r2.VolumeCap, Is.EqualTo(r1.VolumeCap));
    }

    // GATE.S7.FRACTURE.OFFLANE_ROUTES.001: Offlane route tests.

    [Test]
    public void OfflaneRoute_ValidJump_ReturnsValidWithCosts()
    {
        var state = new SimState(500);
        state.FractureUnlocked = true;
        state.Nodes.Add("n1", new Node { Id = "n1", Position = new Vector3(0, 0, 0) });
        state.Nodes.Add("n2", new Node { Id = "n2", Position = new Vector3(10, 0, 0) });
        state.Fleets.Add("f1", new Fleet { Id = "f1", CurrentNodeId = "n1", TechLevel = 2, FuelCurrent = 200 });

        var result = FractureSystem.ComputeOfflaneRoute(state, "f1", "n1", "n2");

        Assert.That(result.Valid, Is.True);
        Assert.That(result.Distance, Is.GreaterThan(0f));
        // Distance=10, FuelCost=10*5=50, HullStress=10*2=20.
        Assert.That(result.FuelCost, Is.EqualTo(50));
        Assert.That(result.HullStress, Is.EqualTo(20));
    }

    [Test]
    public void OfflaneRoute_InsufficientFuel_ReturnsInvalid()
    {
        var state = new SimState(501);
        state.FractureUnlocked = true;
        state.Nodes.Add("n1", new Node { Id = "n1", Position = new Vector3(0, 0, 0) });
        state.Nodes.Add("n2", new Node { Id = "n2", Position = new Vector3(10, 0, 0) });
        state.Fleets.Add("f1", new Fleet { Id = "f1", CurrentNodeId = "n1", TechLevel = 2, FuelCurrent = 10 });

        var result = FractureSystem.ComputeOfflaneRoute(state, "f1", "n1", "n2");

        Assert.That(result.Valid, Is.False);
        Assert.That(result.Reason, Does.Contain("insufficient fuel"));
        // Still reports the cost.
        Assert.That(result.FuelCost, Is.GreaterThan(0));
    }

    [Test]
    public void OfflaneRoute_LowTechLevel_ReturnsInvalid()
    {
        var state = new SimState(502);
        state.FractureUnlocked = true;
        state.Nodes.Add("n1", new Node { Id = "n1", Position = new Vector3(0, 0, 0) });
        state.Nodes.Add("n2", new Node { Id = "n2", Position = new Vector3(5, 0, 0) });
        state.Fleets.Add("f1", new Fleet { Id = "f1", CurrentNodeId = "n1", TechLevel = 1, FuelCurrent = 200 });

        var result = FractureSystem.ComputeOfflaneRoute(state, "f1", "n1", "n2");

        Assert.That(result.Valid, Is.False);
        Assert.That(result.Reason, Does.Contain("tech_level"));
    }

    [Test]
    public void OfflaneRoute_FractureNotUnlocked_ReturnsInvalid()
    {
        var state = new SimState(503);
        state.FractureUnlocked = false;
        state.Nodes.Add("n1", new Node { Id = "n1", Position = new Vector3(0, 0, 0) });
        state.Nodes.Add("n2", new Node { Id = "n2", Position = new Vector3(5, 0, 0) });
        state.Fleets.Add("f1", new Fleet { Id = "f1", CurrentNodeId = "n1", TechLevel = 3, FuelCurrent = 200 });

        var result = FractureSystem.ComputeOfflaneRoute(state, "f1", "n1", "n2");

        Assert.That(result.Valid, Is.False);
        Assert.That(result.Reason, Does.Contain("fracture not unlocked"));
    }

    [Test]
    public void OfflaneRoute_CostScalesWithDistance()
    {
        var state = new SimState(504);
        state.FractureUnlocked = true;
        state.Nodes.Add("n1", new Node { Id = "n1", Position = new Vector3(0, 0, 0) });
        state.Nodes.Add("near", new Node { Id = "near", Position = new Vector3(5, 0, 0) });
        state.Nodes.Add("far", new Node { Id = "far", Position = new Vector3(20, 0, 0) });
        state.Fleets.Add("f1", new Fleet { Id = "f1", CurrentNodeId = "n1", TechLevel = 2, FuelCurrent = 500 });

        var nearRoute = FractureSystem.ComputeOfflaneRoute(state, "f1", "n1", "near");
        var farRoute = FractureSystem.ComputeOfflaneRoute(state, "f1", "n1", "far");

        Assert.That(nearRoute.Valid, Is.True);
        Assert.That(farRoute.Valid, Is.True);
        Assert.That(farRoute.FuelCost, Is.GreaterThan(nearRoute.FuelCost));
        Assert.That(farRoute.HullStress, Is.GreaterThan(nearRoute.HullStress));
    }

    [Test]
    public void OfflaneJumpCommand_InitiatesFractureTravel()
    {
        var state = new SimState(505);
        state.FractureUnlocked = true;
        state.Nodes.Add("n1", new Node { Id = "n1", Position = new Vector3(0, 0, 0) });
        state.Nodes.Add("n2", new Node { Id = "n2", Position = new Vector3(10, 0, 0) });
        state.Fleets.Add("f1", new Fleet
        {
            Id = "f1", CurrentNodeId = "n1", TechLevel = 2, FuelCurrent = 200,
            State = FleetState.Idle, Speed = 1f
        });

        var cmd = new SimCore.Commands.OfflaneJumpCommand("f1", "n2");
        cmd.Execute(state);

        var fleet = state.Fleets["f1"];
        Assert.That(fleet.State, Is.EqualTo(FleetState.FractureTraveling));
        Assert.That(fleet.DestinationNodeId, Is.EqualTo("n2"));
        Assert.That(fleet.FuelCurrent, Is.LessThan(200), "Fuel must be deducted on departure.");
    }

    // GATE.S6.FRACTURE.CONTENT.001 contract tests.

    [Test]
    public void FractureContent_WellKnownGoodIds_FractureGoodsAreDefined()
    {
        // The 3 fracture-exclusive good IDs must be non-empty stable constants.
        Assert.That(WellKnownGoodIds.ExoticMatter, Is.EqualTo("exotic_matter"));
        Assert.That(WellKnownGoodIds.ExoticCrystals, Is.EqualTo("exotic_crystals"));
        Assert.That(WellKnownGoodIds.SalvagedTech, Is.EqualTo("salvaged_tech"));
    }

    [Test]
    public void FractureContent_DefaultRegistry_ContainsAllFractureGoods()
    {
        // The embedded default registry must include all 3 fracture-exclusive goods.
        var reg = ContentRegistryLoader.LoadFromJsonOrThrow(ContentRegistryLoader.DefaultRegistryJsonV0);
        var goodIds = new System.Collections.Generic.HashSet<string>(
            reg.Goods.Select(g => g.Id), StringComparer.Ordinal);

        Assert.That(goodIds.Contains(WellKnownGoodIds.ExoticMatter), Is.True,
            "Registry must contain exotic_matter.");
        Assert.That(goodIds.Contains(WellKnownGoodIds.ExoticCrystals), Is.True,
            "Registry must contain exotic_crystals.");
        Assert.That(goodIds.Contains(WellKnownGoodIds.SalvagedTech), Is.True,
            "Registry must contain salvaged_tech.");
    }

    [Test]
    public void FractureContent_FractureOutpostWorldClassId_IsDefinedInGalaxyGenerator()
    {
        // FRACTURE_OUTPOST world class constant must be non-empty and distinct from lane classes.
        Assert.That(GalaxyGenerator.FractureOutpostWorldClassId, Is.EqualTo("FRACTURE_OUTPOST"));
        Assert.That(GalaxyGenerator.FractureOutpostFeeMultiplier, Is.GreaterThan(1.0f),
            "Fracture outpost fee multiplier must exceed 1.0 (premium access).");

        // Must not collide with any existing WorldClassesV0 entry.
        var laneClassIds = GalaxyGenerator.WorldClassesV0
            .Select(c => c.WorldClassId)
            .ToArray();
        Assert.That(laneClassIds, Does.Not.Contain(GalaxyGenerator.FractureOutpostWorldClassId),
            "FRACTURE_OUTPOST must not overlap with lane world class IDs.");
    }

    [Test]
    public void FractureContent_DefaultRegistry_PassesValidation()
    {
        // The default registry including fracture goods must pass structural validation.
        var result = ContentRegistryLoader.ValidatePackJsonV0(ContentRegistryLoader.DefaultRegistryJsonV0);
        Assert.That(result.IsValid, Is.True,
            "Default registry with fracture goods must pass validation. Failures: "
            + string.Join(", ", result.Failures));
    }

    [Test]
    public void FractureContent_DefaultRegistry_GoodsOrderedOrdinal()
    {
        // Registry must be sorted Ordinal after normalization.
        var reg = ContentRegistryLoader.LoadFromJsonOrThrow(ContentRegistryLoader.DefaultRegistryJsonV0);
        for (int i = 1; i < reg.Goods.Count; i++)
        {
            Assert.That(
                StringComparer.Ordinal.Compare(reg.Goods[i - 1].Id, reg.Goods[i].Id),
                Is.LessThanOrEqualTo(0),
                $"Goods must be ordered Ordinal: {reg.Goods[i - 1].Id} vs {reg.Goods[i].Id}");
        }
    }

    // GATE.S6.FRACTURE.TRAVEL.001 contract tests.

    [Test]
    public void FractureTravel_DirectJump_CostsTripleFuel()
    {
        // A fracture jump over the same distance as a lane edge must cost 3x the lane travel ticks.
        // Lane edge: 10 AU at speed 1.0 => 10 ticks.
        // Fracture jump: same 10 AU at speed 1.0 => 10 * 300 / 100 = 30 ticks.
        var state = new SimState(200);
        state.Nodes.Add("hub", new Node
        {
            Id = "hub",
            Position = new Vector3(0, 0, 0),
            IsFractureNode = false
        });
        state.Nodes.Add("frac", new Node
        {
            Id = "frac",
            Position = new Vector3(10, 0, 0),
            IsFractureNode = true
        });

        float speed = 1.0f;
        bool ok = SimCore.Systems.RoutePlanner.TryPlanFractureRoute(state, "hub", "frac", speed, out var plan);

        Assert.That(ok, Is.True, "TryPlanFractureRoute must succeed when one node is fracture.");

        // Lane base ticks for 10 AU at speed 1.0 = ceil(10/1) = 10.
        // Fracture ticks = 10 * 300 / 100 = 30.
        Assert.That(plan.TotalTravelTicks, Is.EqualTo(30),
            "Fracture jump travel ticks must be 3x lane ticks for same distance.");
        Assert.That(plan.FuelCost, Is.EqualTo(plan.TotalTravelTicks),
            "FuelCost must equal TotalTravelTicks.");
        Assert.That(plan.IsFracture, Is.True);
    }

    [Test]
    public void FractureTravel_RequiresFractureNode()
    {
        // TryPlanFractureRoute must return false if neither node is a fracture node.
        var state = new SimState(201);
        state.Nodes.Add("lane_a", new Node { Id = "lane_a", Position = new Vector3(0, 0, 0), IsFractureNode = false });
        state.Nodes.Add("lane_b", new Node { Id = "lane_b", Position = new Vector3(5, 0, 0), IsFractureNode = false });

        bool ok = SimCore.Systems.RoutePlanner.TryPlanFractureRoute(state, "lane_a", "lane_b", 1.0f, out _);

        Assert.That(ok, Is.False, "Fracture route must fail when neither node is a fracture node.");
    }

    [Test]
    public void FractureTravel_RiskIsHigher()
    {
        // Fracture risk score must exceed normal edge risk for the same distance.
        // Use a distance where lane milli-AU < RiskBand1Max but fracture-scaled exceeds it.
        // Distance = 0.6 AU => milli-AU = 600 (lane: band 1 = MED, score = 600).
        // Fracture: 600 * 200 / 100 = 1200 => band 1 (MED, score 1200 > 600).
        var state = new SimState(202);
        state.Nodes.Add("src", new Node { Id = "src", Position = new Vector3(0, 0, 0), IsFractureNode = true });
        state.Nodes.Add("dst", new Node { Id = "dst", Position = new Vector3(0.6f, 0, 0), IsFractureNode = false });

        bool ok = SimCore.Systems.RoutePlanner.TryPlanFractureRoute(state, "src", "dst", 1.0f, out var plan);

        Assert.That(ok, Is.True);

        // Lane edge milli-AU = round(0.6 * 1000) = 600. Lane risk score = 600.
        // Fracture risk score = 600 * 200 / 100 = 1200.
        // Lane band for 600: RiskBand0Max=500 => MED(1). Fracture band for 1200: RiskBand1Max=1500 => MED(1) still.
        // The key invariant: fracture RiskRating >= lane EdgeRiskBandV0 for same distance, and fracture
        // scaled score (1200) > lane score (600).
        // For a distance giving lane milli-AU just below 500: e.g. 0.4 AU => 400 milli => lane=LOW(0).
        // Fracture: 400*200/100=800 => MED(1) > LOW(0).
        // We test with 0.4 AU instead for a clear band difference.
        state.Nodes["src"] = new Node { Id = "src", Position = new Vector3(0, 0, 0), IsFractureNode = true };
        state.Nodes["dst"] = new Node { Id = "dst", Position = new Vector3(0.4f, 0, 0), IsFractureNode = false };

        ok = SimCore.Systems.RoutePlanner.TryPlanFractureRoute(state, "src", "dst", 1.0f, out plan);
        Assert.That(ok, Is.True);
        // Lane edge risk for 0.4 AU: milli=400 => BandLow (< 500). Fracture: 400*2=800 => BandMed (>= 500, < 1500).
        Assert.That(plan.RiskRating, Is.GreaterThan(SimCore.RiskModelV0.BandLow),
            "Fracture risk rating must exceed normal edge risk band for same distance.");
    }

    [Test]
    public void FractureTravel_IsDeterministic()
    {
        // Same inputs must produce identical outputs (no RNG, no timestamps).
        var state = new SimState(203);
        state.Nodes.Add("nodeA", new Node { Id = "nodeA", Position = new Vector3(1, 2, 3), IsFractureNode = true });
        state.Nodes.Add("nodeB", new Node { Id = "nodeB", Position = new Vector3(7, 5, 1), IsFractureNode = false });

        SimCore.Systems.RoutePlanner.TryPlanFractureRoute(state, "nodeA", "nodeB", 2.5f, out var plan1);
        SimCore.Systems.RoutePlanner.TryPlanFractureRoute(state, "nodeA", "nodeB", 2.5f, out var plan2);

        Assert.That(plan2.TotalTravelTicks, Is.EqualTo(plan1.TotalTravelTicks), "TotalTravelTicks must be deterministic.");
        Assert.That(plan2.FuelCost, Is.EqualTo(plan1.FuelCost), "FuelCost must be deterministic.");
        Assert.That(plan2.RiskRating, Is.EqualTo(plan1.RiskRating), "RiskRating must be deterministic.");
        Assert.That(plan2.IsFracture, Is.EqualTo(plan1.IsFracture));
    }

    // GATE.S6.FRACTURE.ECON_FEEDBACK.001 contract tests.

    [Test]
    public void FractureEconFeedback_IncreasesLaneSupply()
    {
        // Fracture goods at a lane hub market must increase inventory after ApplyFractureGoodsFlowV0.
        var state = new SimState(204);
        state.FractureUnlocked = true; // GATE.S6.FRACTURE_DISCOVERY.MODEL.001
        state.Nodes.Add("hub", new Node
        {
            Id = "hub",
            Position = Vector3.Zero,
            IsFractureNode = false,
            MarketId = "hub"
        });
        var market = new Market();
        market.Inventory[WellKnownGoodIds.ExoticCrystals] = 100;
        state.Markets.Add("hub", market);

        int before = market.Inventory[WellKnownGoodIds.ExoticCrystals];
        FractureSystem.ApplyFractureGoodsFlowV0(state);
        int after = market.Inventory[WellKnownGoodIds.ExoticCrystals];

        Assert.That(after, Is.GreaterThan(before),
            "Fracture goods at lane hub must increase inventory after flow.");
        // 10% of 100 = 10, so expected = 110.
        Assert.That(after, Is.EqualTo(110),
            "Expected 10% flow: 100 + 10 = 110.");
    }

    [Test]
    public void FractureEconFeedback_VolumeNeverDecreases()
    {
        // Total inventory of all goods at a lane hub must not decrease after ApplyFractureGoodsFlowV0.
        var state = new SimState(205);
        state.FractureUnlocked = true; // GATE.S6.FRACTURE_DISCOVERY.MODEL.001
        state.Nodes.Add("hub", new Node
        {
            Id = "hub",
            Position = Vector3.Zero,
            IsFractureNode = false,
            MarketId = "hub"
        });
        var market = new Market();
        market.Inventory[WellKnownGoodIds.ExoticMatter] = 50;
        market.Inventory[WellKnownGoodIds.ExoticCrystals] = 80;
        market.Inventory["fuel"] = 200; // non-fracture good, must be unchanged
        state.Markets.Add("hub", market);

        int totalBefore = market.Inventory.Values.Sum();
        FractureSystem.ApplyFractureGoodsFlowV0(state);
        int totalAfter = market.Inventory.Values.Sum();

        Assert.That(totalAfter, Is.GreaterThanOrEqualTo(totalBefore),
            "Total market volume must never decrease when fracture supply is applied.");
    }

    [Test]
    public void FractureEconFeedback_MinimumFlowIsOne()
    {
        // Even a tiny fracture stock (1 unit) must flow at least 1 unit per tick.
        // 10% of 1 = 0 (integer math), but must clamp to min 1.
        var state = new SimState(206);
        state.FractureUnlocked = true; // GATE.S6.FRACTURE_DISCOVERY.MODEL.001
        state.Nodes.Add("hub", new Node
        {
            Id = "hub",
            Position = Vector3.Zero,
            IsFractureNode = false,
            MarketId = "hub"
        });
        var market = new Market();
        market.Inventory[WellKnownGoodIds.SalvagedTech] = 1;
        state.Markets.Add("hub", market);

        int before = market.Inventory[WellKnownGoodIds.SalvagedTech];
        FractureSystem.ApplyFractureGoodsFlowV0(state);
        int after = market.Inventory[WellKnownGoodIds.SalvagedTech];

        Assert.That(after - before, Is.GreaterThanOrEqualTo(1),
            "Minimum flow must be 1 even when 10% rounds to 0.");
        // 10% of 1 = 0 floored, but min 1: expected after = 1 + 1 = 2.
        Assert.That(after, Is.EqualTo(2),
            "Expected min-flow: 1 + 1 = 2.");
    }
}
