using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;

namespace SimCore.Tests.Systems;

// GATE.T18.NARRATIVE.INSTRUMENT_DISAGREEMENT.001
[TestFixture]
public sealed class InstrumentDisagreementTests
{
    [SetUp]
    public void SetUp()
    {
        Market.ClearGoodBasePrices();
    }

    private static void AdvanceTo(SimState state, int targetTick)
    {
        while (state.Tick < targetTick)
            state.AdvanceTick();
    }

    private SimState MakeState()
    {
        var state = new SimState(42);

        state.Nodes["stable"] = new Node { Id = "stable", InstabilityLevel = 0, MarketId = "stable" };
        state.Nodes["shimmer"] = new Node
        {
            Id = "shimmer",
            InstabilityLevel = FractureWeightTweaksV0.STRUCT_PhaseShimmerMin,
            MarketId = "shimmer"
        };
        state.Nodes["fracture"] = new Node
        {
            Id = "fracture",
            InstabilityLevel = FractureWeightTweaksV0.STRUCT_PhaseFractureMin,
            MarketId = "fracture"
        };

        // Markets with stock to generate prices
        var stableMarket = new Market { Id = "stable" };
        stableMarket.Inventory["food"] = Market.IdealStock;
        state.Markets["stable"] = stableMarket;

        var shimmerMarket = new Market { Id = "shimmer" };
        shimmerMarket.Inventory["food"] = Market.IdealStock;
        state.Markets["shimmer"] = shimmerMarket;

        var fractureMarket = new Market { Id = "fracture" };
        fractureMarket.Inventory["food"] = Market.IdealStock;
        state.Markets["fracture"] = fractureMarket;

        // Edges for ETA tests
        state.Edges["edge_stable"] = new Edge
        {
            Id = "edge_stable",
            FromNodeId = "stable",
            ToNodeId = "stable",
            Distance = 10f
        };
        state.Edges["edge_fracture"] = new Edge
        {
            Id = "edge_fracture",
            FromNodeId = "stable",
            ToNodeId = "fracture",
            Distance = 10f
        };

        return state;
    }

    [Test]
    public void StableNode_BothReadingsEqual()
    {
        var state = MakeState();

        int std = InstrumentDisagreementSystem.ComputeStandardPriceReading(
            state, "stable", "food");
        int frac = InstrumentDisagreementSystem.ComputeFracturePriceReading(
            state, "stable", "food");

        // At ideal stock, mid price = BasePrice = 100
        Assert.That(std, Is.EqualTo(Market.BasePrice));
        Assert.That(frac, Is.EqualTo(Market.BasePrice));
    }

    [Test]
    public void UnstableNode_ReadingsPositive()
    {
        var state = MakeState();
        AdvanceTo(state, 100);

        int std = InstrumentDisagreementSystem.ComputeStandardPriceReading(
            state, "shimmer", "food");
        int frac = InstrumentDisagreementSystem.ComputeFracturePriceReading(
            state, "shimmer", "food");

        Assert.That(std, Is.GreaterThan(0));
        Assert.That(frac, Is.GreaterThan(0));
    }

    [Test]
    public void PriceReading_IsDeterministic()
    {
        var state = MakeState();
        AdvanceTo(state, 100);

        int std1 = InstrumentDisagreementSystem.ComputeStandardPriceReading(
            state, "shimmer", "food");
        int std2 = InstrumentDisagreementSystem.ComputeStandardPriceReading(
            state, "shimmer", "food");

        Assert.That(std1, Is.EqualTo(std2));
    }

    [Test]
    public void PriceReading_NeverZero()
    {
        for (int t = 0; t < 50; t++)
        {
            var state = MakeState();
            AdvanceTo(state, t);
            int std = InstrumentDisagreementSystem.ComputeStandardPriceReading(
                state, "fracture", "food");
            int frac = InstrumentDisagreementSystem.ComputeFracturePriceReading(
                state, "fracture", "food");

            Assert.That(std, Is.GreaterThan(0), $"Standard reading was 0 at tick {t}");
            Assert.That(frac, Is.GreaterThan(0), $"Fracture reading was 0 at tick {t}");
        }
    }

    [Test]
    public void StableEdge_BothEtaReadingsEqual()
    {
        var state = MakeState();

        int stdEta = InstrumentDisagreementSystem.ComputeStandardEtaReading(
            state, "edge_stable");
        int fracEta = InstrumentDisagreementSystem.ComputeFractureEtaReading(
            state, "edge_stable");

        Assert.That(stdEta, Is.EqualTo(fracEta));
    }

    [Test]
    public void FractureEdge_StandardOverestimates()
    {
        var state = MakeState();

        int stdEta = InstrumentDisagreementSystem.ComputeStandardEtaReading(
            state, "edge_fracture");
        int fracEta = InstrumentDisagreementSystem.ComputeFractureEtaReading(
            state, "edge_fracture");

        Assert.That(stdEta, Is.GreaterThanOrEqualTo(fracEta));
    }

    [Test]
    public void MissingNode_ReturnsZero()
    {
        var state = MakeState();
        int result = InstrumentDisagreementSystem.ComputeStandardPriceReading(
            state, "nonexistent", "food");
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void MissingEdge_ReturnsOne()
    {
        var state = MakeState();
        int result = InstrumentDisagreementSystem.ComputeStandardEtaReading(
            state, "nonexistent");
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void ShimmerNode_ReadingsDifferFromEachOther()
    {
        // At shimmer instability, both sensors drift but with different salts
        bool foundDifference = false;
        for (int t = 0; t < 50; t++)
        {
            var state = MakeState();
            AdvanceTo(state, t);
            int std = InstrumentDisagreementSystem.ComputeStandardPriceReading(
                state, "shimmer", "food");
            int frac = InstrumentDisagreementSystem.ComputeFracturePriceReading(
                state, "shimmer", "food");
            if (std != frac) { foundDifference = true; break; }
        }
        Assert.That(foundDifference, Is.True,
            "Standard and fracture readings never differed at shimmer across 50 ticks");
    }

    [Test]
    public void NodeWithNoMarket_PriceReadingReturnsZero()
    {
        var state = MakeState();
        // Add a node with no matching market
        state.Nodes["orphan"] = new Node
        {
            Id = "orphan",
            InstabilityLevel = FractureWeightTweaksV0.STRUCT_PhaseShimmerMin,
            MarketId = "orphan"
        };

        int result = InstrumentDisagreementSystem.ComputeStandardPriceReading(
            state, "orphan", "food");
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void FractureEdge_FractureEtaMoreAccurate()
    {
        var state = MakeState();
        int stdEta = InstrumentDisagreementSystem.ComputeStandardEtaReading(
            state, "edge_fracture");
        int fracEta = InstrumentDisagreementSystem.ComputeFractureEtaReading(
            state, "edge_fracture");

        // Standard overestimates; fracture is closer to true travel time
        // Both should be positive
        Assert.That(stdEta, Is.GreaterThan(0));
        Assert.That(fracEta, Is.GreaterThan(0));
        // Standard should be >= fracture (overestimates)
        Assert.That(stdEta, Is.GreaterThanOrEqualTo(fracEta));
    }
}
