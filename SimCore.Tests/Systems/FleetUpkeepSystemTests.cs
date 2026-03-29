using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;

namespace SimCore.Tests.Systems;

[TestFixture]
[Category("FleetUpkeepSystem")]
public sealed class FleetUpkeepSystemTests
{
    private static SimState CreateState(int seed = 42)
    {
        var state = new SimState(seed);
        state.PlayerCredits = 10000;
        state.Fleets["fleet_trader_1"] = new Fleet
        {
            Id = "fleet_trader_1",
            OwnerId = "player",
            ShipClassId = "corvette",
            CurrentNodeId = "nodeA",
            State = FleetState.Docked
        };
        return state;
    }

    [Test]
    public void OffCycleTick_NoDeduction()
    {
        var state = CreateState();
        long creditsBefore = state.PlayerCredits;

        // Advance to 1 past cycle tick
        while (state.Tick % FleetUpkeepTweaksV0.UpkeepCycleTicks != 0)
            state.AdvanceTick();
        state.AdvanceTick(); // off-cycle

        FleetUpkeepSystem.Process(state);

        Assert.That(state.PlayerCredits, Is.EqualTo(creditsBefore));
    }

    [Test]
    public void CycleTick_DeductsUpkeep()
    {
        var state = CreateState();
        long creditsBefore = state.PlayerCredits;

        while (state.Tick % FleetUpkeepTweaksV0.UpkeepCycleTicks != 0)
            state.AdvanceTick();

        FleetUpkeepSystem.Process(state);

        Assert.That(state.PlayerCredits, Is.LessThan(creditsBefore));
    }

    [Test]
    public void DockedFleet_PaysReducedUpkeep()
    {
        var state = CreateState();
        state.Fleets["fleet_trader_1"].State = FleetState.Docked;

        while (state.Tick % FleetUpkeepTweaksV0.UpkeepCycleTicks != 0)
            state.AdvanceTick();

        long creditsBefore = state.PlayerCredits;
        FleetUpkeepSystem.Process(state);
        long dockedCost = creditsBefore - state.PlayerCredits;

        // Reset for traveling comparison
        var state2 = CreateState();
        state2.Fleets["fleet_trader_1"].State = FleetState.Traveling;

        while (state2.Tick % FleetUpkeepTweaksV0.UpkeepCycleTicks != 0)
            state2.AdvanceTick();

        long creditsBefore2 = state2.PlayerCredits;
        FleetUpkeepSystem.Process(state2);
        long travelingCost = creditsBefore2 - state2.PlayerCredits;

        Assert.That(dockedCost, Is.LessThan(travelingCost));
    }

    [Test]
    public void InsufficientCredits_IncrementsDelinquency()
    {
        var state = CreateState();
        // fh_14: Use dreadnought (upkeep 2500) so credits above safety net (500) but below upkeep.
        state.Fleets["fleet_trader_1"].ShipClassId = "dreadnought";
        state.PlayerCredits = FleetUpkeepTweaksV0.LowFundsThreshold; // Exactly at threshold, passes safety net

        while (state.Tick % FleetUpkeepTweaksV0.UpkeepCycleTicks != 0)
            state.AdvanceTick();

        FleetUpkeepSystem.Process(state);

        Assert.That(state.Fleets["fleet_trader_1"].UpkeepDelinquentCycles, Is.GreaterThan(0));
    }

    [Test]
    public void PaymentSuccess_ResetsDelinquency()
    {
        var state = CreateState();
        state.Fleets["fleet_trader_1"].UpkeepDelinquentCycles = 2;

        while (state.Tick % FleetUpkeepTweaksV0.UpkeepCycleTicks != 0)
            state.AdvanceTick();

        FleetUpkeepSystem.Process(state);

        Assert.That(state.Fleets["fleet_trader_1"].UpkeepDelinquentCycles, Is.EqualTo(0));
    }

    [Test]
    public void DelinquencyPastGrace_DisablesModule()
    {
        var state = CreateState();
        // fh_14: Use dreadnought (upkeep 2500) so credits above safety net (500) but below upkeep.
        state.Fleets["fleet_trader_1"].ShipClassId = "dreadnought";
        state.PlayerCredits = FleetUpkeepTweaksV0.LowFundsThreshold; // Exactly at threshold, passes safety net
        state.Fleets["fleet_trader_1"].UpkeepDelinquentCycles = FleetUpkeepTweaksV0.GracePeriodCycles + 1;
        state.Fleets["fleet_trader_1"].Slots.Add(new ModuleSlot
        {
            InstalledModuleId = "mod_basic_laser",
            PowerDraw = 10,
            Disabled = false
        });

        while (state.Tick % FleetUpkeepTweaksV0.UpkeepCycleTicks != 0)
            state.AdvanceTick();

        FleetUpkeepSystem.Process(state);

        // Module should be disabled after exceeding grace period
        Assert.That(state.Fleets["fleet_trader_1"].Slots[0].Disabled, Is.True);
    }
}
