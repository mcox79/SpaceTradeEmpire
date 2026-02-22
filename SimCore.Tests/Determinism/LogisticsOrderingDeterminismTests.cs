using System.Linq;
using NUnit.Framework;
using SimCore.Schemas;
using SimCore.Systems;
using SimCore.World;

namespace SimCore.Tests.Determinism;

[TestFixture]
public sealed class LogisticsOrderingDeterminismTests
{
    private static SimKernel KernelTwoFleetsAlreadyAtSource()
    {
        var k = new SimKernel(seed: 123);

        var def = new WorldDefinition
        {
            WorldId = "micro_world_logi_order_001",
            Markets =
            {
                new WorldMarket { Id = "mkt_a", Inventory = new() { ["ore"] = 0 } },
                new WorldMarket { Id = "mkt_b", Inventory = new() { ["ore"] = 50 } },
                new WorldMarket { Id = "mkt_c", Inventory = new() { ["ore"] = 0 } }
            },
            Nodes =
            {
                new WorldNode { Id = "stn_a", Kind = "Station", Name = "A", MarketId = "mkt_a", Pos = new float[] { 0f, 0f, 0f } },
                new WorldNode { Id = "stn_b", Kind = "Station", Name = "B", MarketId = "mkt_b", Pos = new float[] { 1f, 0f, 0f } },
                new WorldNode { Id = "stn_c", Kind = "Station", Name = "C", MarketId = "mkt_c", Pos = new float[] { 2f, 0f, 0f } }
            },
            Edges =
            {
                new WorldEdge { Id = "lane_ab", FromNodeId = "stn_a", ToNodeId = "stn_b", Distance = 1.0f, TotalCapacity = 5 },
                new WorldEdge { Id = "lane_bc", FromNodeId = "stn_b", ToNodeId = "stn_c", Distance = 1.0f, TotalCapacity = 5 }
            },
            Player = new WorldPlayerStart { Credits = 0, LocationNodeId = "stn_a" }
        };

        WorldLoader.Apply(k.State, def);

        var s = k.State;

        // Ensure a second fleet exists deterministically.
        s.Fleets["fleet_trader_2"] = new SimCore.Entities.Fleet
        {
            Id = "fleet_trader_2",
            OwnerId = "player",
            CurrentNodeId = "stn_b",
            State = SimCore.Entities.FleetState.Idle,
            Speed = 1.0f
        };

        // Place fleet_trader_1 at source too.
        s.Fleets["fleet_trader_1"].CurrentNodeId = "stn_b";
        s.Fleets["fleet_trader_1"].State = SimCore.Entities.FleetState.Idle;
        s.Fleets["fleet_trader_1"].Speed = 1.0f;

        // Plan logistics for both fleets from B to C (both already at source node stn_b).
        var f1 = s.Fleets["fleet_trader_1"];
        var f2 = s.Fleets["fleet_trader_2"];

        Assert.That(LogisticsSystem.PlanLogistics(s, f1, "mkt_b", "mkt_c", "ore", 5), Is.True);
        Assert.That(LogisticsSystem.PlanLogistics(s, f2, "mkt_b", "mkt_c", "ore", 5), Is.True);

        return k;
    }

    private static string RunOnceAndSummarizeTick0Events()
    {
        var k = KernelTwoFleetsAlreadyAtSource();

        // Single step: LogisticsSystem.AdvanceJobState should issue pickup intents and phase change
        // for both fleets during tick 0. Seqs are finalized at end of tick 0 (AdvanceTick).
        k.Step();

        var s = k.State;

        // Events emitted during tick 0 will have Event.Tick == 0 even though state.Tick is now 1.
        var tick0 = s.LogisticsEventLog.Where(e => e.Tick == 0).ToList();
        Assert.That(tick0.Count, Is.GreaterThanOrEqualTo(4)); // 2 fleets x (PickupIssued + PhaseChangedToDeliver) plus JobPlanned(s)

        // Deterministic ordering rule: FleetId ascending, then within-fleet emission order.
        var ordered = tick0
            .OrderBy(e => e.FleetId, System.StringComparer.Ordinal)
            .ThenBy(e => e.Seq) // Seq must reflect finalized order within tick
            .ToList();

        // Must match actual order in the log (tick0 subset in list order).
        var actual = s.LogisticsEventLog.Where(e => e.Tick == 0).ToList();
        Assert.That(actual.Select(e => e.Seq).SequenceEqual(ordered.Select(e => e.Seq)), Is.True);

        // Stronger: all fleet_trader_1 events must appear before any fleet_trader_2 events (within tick 0).
        var firstFleet2 = actual.FindIndex(e => e.FleetId == "fleet_trader_2");
        Assert.That(firstFleet2, Is.GreaterThanOrEqualTo(0));
        Assert.That(actual.Take(firstFleet2).All(e => e.FleetId == "fleet_trader_1"), Is.True);

        // Seq must be strictly increasing inside tick 0 list order.
        for (var i = 1; i < actual.Count; i++)
        {
            Assert.That(actual[i].Seq, Is.GreaterThan(actual[i - 1].Seq));
        }

        // Return a compact signature for determinism across fresh runs.
        return string.Join("|", actual.Select(e => $"{e.Tick}:{e.Seq}:{e.FleetId}:{(int)e.Type}:{e.GoodId}:{e.Amount}:{e.SourceNodeId}->{e.TargetNodeId}"));
    }

    [Test]
    public void LogisticsEvents_SameTick_OrderIsDeterministic_UnderMultiFleetContention()
    {
        var a = RunOnceAndSummarizeTick0Events();
        var b = RunOnceAndSummarizeTick0Events();
        Assert.That(b, Is.EqualTo(a));
    }

    [Test]
    public void TweaksConfigHash_IsDeterministic_AndOverrideChangesIt()
    {
        // Default kernel uses stable defaults.
        var k0 = new SimKernel(seed: 123);
        var h0a = k0.State.TweaksHash;

        var k0b = new SimKernel(seed: 123);
        var h0b = k0b.State.TweaksHash;

        Assert.That(string.IsNullOrWhiteSpace(h0a), Is.False);
        Assert.That(h0b, Is.EqualTo(h0a));

        // Override wins deterministically and produces a different hash.
        var overrideJson = "{\"version\":0,\"risk_scalar\":2.0,\"market_fee_multiplier\":1.25}";
        var k1 = new SimKernel(seed: 123, tweakConfigJsonOverride: overrideJson);
        var h1a = k1.State.TweaksHash;

        var k1b = new SimKernel(seed: 123, tweakConfigJsonOverride: overrideJson);
        var h1b = k1b.State.TweaksHash;

        Assert.That(string.IsNullOrWhiteSpace(h1a), Is.False);
        Assert.That(h1b, Is.EqualTo(h1a));
        Assert.That(h1a, Is.Not.EqualTo(h0a));
    }

    [Test]
    public void RoutePlanner_RiskTweaks_OverrideChangesChoice_Deterministically()
    {
        static string RunOnce(string? tweakOverrideJson)
        {
            var s = new SimCore.SimState(seed: 1);
            s.LoadTweaksFromJsonOverride(tweakOverrideJson);

            s.Nodes["n_a"] = new SimCore.Entities.Node { Id = "n_a", MarketId = "m_a" };
            s.Nodes["n_b"] = new SimCore.Entities.Node { Id = "n_b", MarketId = "m_b" };
            s.Nodes["n_c"] = new SimCore.Entities.Node { Id = "n_c", MarketId = "m_c" };

            // Two candidate paths from n_a to n_c:
            // - Direct: 1 hop, distance 1.99 => ticks=1 at speed=2.0, risk=1990
            // - Via B:  2 hops, distances 0.51+0.51 => ticks=2 at speed=2.0, risk=1020
            // Default (no overrides) preserves legacy behavior (fewest hops) => direct.
            // With risk_scalar override, score-based ordering activates and should prefer the lower-risk via-B path.
            s.Edges["e_ac"] = new SimCore.Entities.Edge { Id = "e_ac", FromNodeId = "n_a", ToNodeId = "n_c", Distance = 1.99f, TotalCapacity = 1 };
            s.Edges["e_ab"] = new SimCore.Entities.Edge { Id = "e_ab", FromNodeId = "n_a", ToNodeId = "n_b", Distance = 0.51f, TotalCapacity = 1 };
            s.Edges["e_bc"] = new SimCore.Entities.Edge { Id = "e_bc", FromNodeId = "n_b", ToNodeId = "n_c", Distance = 0.51f, TotalCapacity = 1 };

            Assert.That(SimCore.Systems.RoutePlanner.TryPlanChoice(s, "n_a", "n_c", speedAuPerTick: 2.0f, maxCandidates: 8, out var choice), Is.True);
            return choice.ChosenRouteId;
        }

        var d0 = RunOnce(null);
        var d1 = RunOnce(null);
        Assert.That(d1, Is.EqualTo(d0));
        Assert.That(d0, Is.EqualTo("n_a>n_c"));

        var overrideJson = "{\"version\":0,\"risk_scalar\":10.0}";
        var o0 = RunOnce(overrideJson);
        var o1 = RunOnce(overrideJson);
        Assert.That(o1, Is.EqualTo(o0));
        Assert.That(o0, Is.EqualTo("n_a>n_b>n_c"));

        Assert.That(o0, Is.Not.EqualTo(d0));
    }

    [Test]
    public void Logistics_LoopViabilityThreshold_FromTweaks_FiltersSuppliers_Deterministically()
    {
        static string RunOnce(string? tweakOverrideJson)
        {
            var s = new SimCore.SimState(seed: 1);
            s.LoadTweaksFromJsonOverride(tweakOverrideJson);

            // Destination market has a shortage (via IndustrySite input).
            s.Markets["mkt_dst"] = new SimCore.Entities.Market { Id = "mkt_dst", Inventory = new() { ["ore"] = 0 } };

            // Reachable supplier is barely above legacy cutoff (>10).
            s.Markets["mkt_lo"] = new SimCore.Entities.Market { Id = "mkt_lo", Inventory = new() { ["ore"] = 11 } };

            // Unreachable supplier has more inventory and will be tried first, but planning must fail.
            s.Markets["mkt_hi"] = new SimCore.Entities.Market { Id = "mkt_hi", Inventory = new() { ["ore"] = 100 } };

            s.Nodes["stn_dst"] = new SimCore.Entities.Node { Id = "stn_dst", MarketId = "mkt_dst" };
            s.Nodes["stn_lo"] = new SimCore.Entities.Node { Id = "stn_lo", MarketId = "mkt_lo" };
            s.Nodes["stn_hi"] = new SimCore.Entities.Node { Id = "stn_hi", MarketId = "mkt_hi" };

            // Only stn_dst <-> stn_lo are connected. stn_hi is isolated (unreachable).
            s.Edges["e_dl"] = new SimCore.Entities.Edge { Id = "e_dl", FromNodeId = "stn_dst", ToNodeId = "stn_lo", Distance = 1.0f, TotalCapacity = 1 };
            s.Edges["e_ld"] = new SimCore.Entities.Edge { Id = "e_ld", FromNodeId = "stn_lo", ToNodeId = "stn_dst", Distance = 1.0f, TotalCapacity = 1 };

            s.Fleets["fleet_trader_1"] = new SimCore.Entities.Fleet
            {
                Id = "fleet_trader_1",
                OwnerId = "player",
                CurrentNodeId = "stn_dst",
                State = SimCore.Entities.FleetState.Idle,
                Speed = 1.0f
            };

            s.IndustrySites["site_dst"] = new SimCore.Entities.IndustrySite
            {
                Id = "site_dst",
                NodeId = "stn_dst",
                Inputs = new() { ["ore"] = 1 }
            };

            SimCore.Systems.LogisticsSystem.Process(s);

            var f = s.Fleets["fleet_trader_1"];
            return f.CurrentJob?.SourceNodeId ?? "";
        }

        // Default cutoff is legacy (>10), so reachable supplier with qty=11 should be used.
        var d0 = RunOnce(null);
        var d1 = RunOnce(null);
        Assert.That(d1, Is.EqualTo(d0));
        Assert.That(d0, Is.EqualTo("stn_lo"));

        // Override cutoff to 11 means supplier must have qty > 11, so qty=11 becomes ineligible.
        // Only unreachable supplier remains, so planning deterministically fails and no job is assigned.
        var overrideJson = "{\"version\":0,\"loop_viability_threshold\":11.0}";
        var o0 = RunOnce(overrideJson);
        var o1 = RunOnce(overrideJson);
        Assert.That(o1, Is.EqualTo(o0));
        Assert.That(o0, Is.EqualTo(""));

        Assert.That(o0, Is.Not.EqualTo(d0));
    }

    [Test]
    public void LaneCapacityDefault_FromTweaks_IsDeterministic_AndOverrideChangesQueueing()
    {
        static (int Delivered, int Remaining, int NextArriveTick) RunOnce(string? tweakOverrideJson)
        {
            var s = new SimCore.SimState(seed: 1);
            s.LoadTweaksFromJsonOverride(tweakOverrideJson);

            s.Markets["mkt_src"] = new SimCore.Entities.Market { Id = "mkt_src", Inventory = new() { ["ore"] = 0 } };
            s.Markets["mkt_dst"] = new SimCore.Entities.Market { Id = "mkt_dst", Inventory = new() { ["ore"] = 0 } };

            s.Nodes["n_src"] = new SimCore.Entities.Node { Id = "n_src", MarketId = "mkt_src" };
            s.Nodes["n_dst"] = new SimCore.Entities.Node { Id = "n_dst", MarketId = "mkt_dst" };

            // TotalCapacity <= 0 means "unspecified", so effective capacity comes from tweaks (if > 0) else unlimited.
            s.Edges["lane_sd"] = new SimCore.Entities.Edge
            {
                Id = "lane_sd",
                FromNodeId = "n_src",
                ToNodeId = "n_dst",
                Distance = 1.0f,
                TotalCapacity = 0
            };

            // Due immediately at tick 0.
            s.InFlightTransfers.Add(new SimCore.Entities.InFlightTransfer
            {
                Id = "t1",
                EdgeId = "lane_sd",
                FromNodeId = "n_src",
                ToNodeId = "n_dst",
                FromMarketId = "mkt_src",
                ToMarketId = "mkt_dst",
                GoodId = "ore",
                Quantity = 10,
                DepartTick = 0,
                ArriveTick = 0
            });

            SimCore.Systems.LaneFlowSystem.Process(s);

            var delivered = s.Markets["mkt_dst"].Inventory.GetValueOrDefault("ore", 0);

            // If capacity is effectively unlimited, the transfer can be fully delivered and removed.
            var t1 = s.InFlightTransfers.FirstOrDefault(x => x.Id == "t1");
            var remaining = t1?.Quantity ?? 0;
            var nextArrive = t1?.ArriveTick ?? 0;

            return (delivered, remaining, nextArrive);
        }

        // Default tweaks should preserve legacy behavior (unlimited when TotalCapacity <= 0).
        var a0 = RunOnce(null);
        var a1 = RunOnce(null);
        Assert.That(a1, Is.EqualTo(a0));
        Assert.That(a0.Delivered, Is.EqualTo(10));
        Assert.That(a0.Remaining, Is.EqualTo(0));

        // Override default capacity to force deterministic queueing.
        var overrideJson = "{\"version\":0,\"default_lane_capacity_k\":1}";
        var b0 = RunOnce(overrideJson);
        var b1 = RunOnce(overrideJson);
        Assert.That(b1, Is.EqualTo(b0));
        Assert.That(b0.Delivered, Is.EqualTo(1));
        Assert.That(b0.Remaining, Is.EqualTo(9));
        Assert.That(b0.NextArriveTick, Is.EqualTo(1));

        // Behavior must differ from default run.
        Assert.That(b0, Is.Not.EqualTo(a0));
    }
}
