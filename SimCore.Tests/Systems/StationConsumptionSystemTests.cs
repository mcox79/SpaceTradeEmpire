using NUnit.Framework;
using SimCore;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;

namespace SimCore.Tests.Systems;

[TestFixture]
[Category("StationConsumptionSystem")]
public sealed class StationConsumptionSystemTests
{
    private static SimState CreateState(int seed = 42)
    {
        var state = new SimState(seed);
        state.Markets["mkt_A"] = new Market
        {
            Id = "mkt_A",
            Inventory = new() { [WellKnownGoodIds.Food] = 100, [WellKnownGoodIds.Fuel] = 100 }
        };
        state.Markets["mkt_B"] = new Market
        {
            Id = "mkt_B",
            Inventory = new() { [WellKnownGoodIds.Food] = 50, [WellKnownGoodIds.Fuel] = 50 }
        };
        return state;
    }

    [Test]
    public void CadenceTick_ConsumesFoodAndFuel()
    {
        var state = CreateState();
        int cadence = StationConsumptionTweaksV0.CadenceTicks;
        int batchFood = StationConsumptionTweaksV0.FoodPerTick * cadence;

        // Advance to cadence tick (skip tick 0)
        while (state.Tick == 0 || state.Tick % cadence != 0)
            state.AdvanceTick();

        StationConsumptionSystem.Process(state);

        Assert.That(state.Markets["mkt_A"].Inventory[WellKnownGoodIds.Food],
            Is.EqualTo(100 - batchFood));
        Assert.That(state.Markets["mkt_A"].Inventory[WellKnownGoodIds.Fuel],
            Is.EqualTo(100 - batchFood));
    }

    [Test]
    public void Tick0_SkipsConsumption()
    {
        var state = CreateState();
        // state.Tick is 0 at creation

        StationConsumptionSystem.Process(state);

        Assert.That(state.Markets["mkt_A"].Inventory[WellKnownGoodIds.Food], Is.EqualTo(100));
    }

    [Test]
    public void HavenMarket_IsExempt()
    {
        var state = CreateState();
        state.Haven.MarketId = "mkt_A";
        state.Haven.Discovered = true;
        int cadence = StationConsumptionTweaksV0.CadenceTicks;

        while (state.Tick == 0 || state.Tick % cadence != 0)
            state.AdvanceTick();

        StationConsumptionSystem.Process(state);

        // Haven market should be untouched
        Assert.That(state.Markets["mkt_A"].Inventory[WellKnownGoodIds.Food], Is.EqualTo(100));
        // Non-haven market should consume
        Assert.That(state.Markets["mkt_B"].Inventory[WellKnownGoodIds.Food], Is.LessThan(50));
    }

    [Test]
    public void LowStock_ClampsToAvailable()
    {
        var state = CreateState();
        state.Markets["mkt_A"].Inventory[WellKnownGoodIds.Food] = 3; // Less than batch
        int cadence = StationConsumptionTweaksV0.CadenceTicks;

        while (state.Tick == 0 || state.Tick % cadence != 0)
            state.AdvanceTick();

        StationConsumptionSystem.Process(state);

        // Should consume only what's available, floor at 0
        Assert.That(state.Markets["mkt_A"].Inventory[WellKnownGoodIds.Food],
            Is.GreaterThanOrEqualTo(0));
    }
}
