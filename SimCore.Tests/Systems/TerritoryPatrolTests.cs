using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;

namespace SimCore.Tests.Systems;

// GATE.S7.TERRITORY.PATROL_RESPONSE.001
public class TerritoryPatrolTests
{
    [Test]
    public void Open_NoEngagement()
    {
        Assert.That(NpcFleetCombatSystem.GetPatrolResponse(TerritoryRegime.Open, 0),
            Is.EqualTo(PatrolResponse.None));
        Assert.That(NpcFleetCombatSystem.GetPatrolResponse(TerritoryRegime.Open, 100),
            Is.EqualTo(PatrolResponse.None));
    }

    [Test]
    public void Guarded_ScanWarning()
    {
        Assert.That(NpcFleetCombatSystem.GetPatrolResponse(TerritoryRegime.Guarded, 0),
            Is.EqualTo(PatrolResponse.ScanWarning));
    }

    [Test]
    public void Restricted_NoCargo_ScanWarning()
    {
        Assert.That(NpcFleetCombatSystem.GetPatrolResponse(TerritoryRegime.Restricted, 0),
            Is.EqualTo(PatrolResponse.ScanWarning));
    }

    [Test]
    public void Restricted_HighCargo_Pursue()
    {
        int cargo = FactionTweaksV0.CargoThresholdForPursuit + 1;
        Assert.That(NpcFleetCombatSystem.GetPatrolResponse(TerritoryRegime.Restricted, cargo),
            Is.EqualTo(PatrolResponse.Pursue));
    }

    [Test]
    public void Restricted_ExactThreshold_ScanWarning()
    {
        // At threshold, not above — still scan warning
        Assert.That(NpcFleetCombatSystem.GetPatrolResponse(
            TerritoryRegime.Restricted, FactionTweaksV0.CargoThresholdForPursuit),
            Is.EqualTo(PatrolResponse.ScanWarning));
    }

    [Test]
    public void Hostile_AttackOnSight()
    {
        Assert.That(NpcFleetCombatSystem.GetPatrolResponse(TerritoryRegime.Hostile, 0),
            Is.EqualTo(PatrolResponse.AttackOnSight));
    }

    [Test]
    public void FromState_OpenTerritory()
    {
        var state = new SimState(42);
        state.NodeFactionId["node_a"] = "faction_0";
        state.FactionTradePolicy["faction_0"] = 0; // Open
        ReputationSystem.AdjustReputation(state, "faction_0", 80); // Allied
        state.Fleets["player_fleet"] = new Fleet { Id = "player_fleet", OwnerId = "player" };

        var response = NpcFleetCombatSystem.GetPatrolResponse(state, "node_a", "player_fleet");
        Assert.That(response, Is.EqualTo(PatrolResponse.None));
    }

    [Test]
    public void FromState_RestrictedWithCargo()
    {
        var state = new SimState(42);
        state.NodeFactionId["node_a"] = "faction_0";
        state.FactionTradePolicy["faction_0"] = 1; // Guarded
        ReputationSystem.AdjustReputation(state, "faction_0", -30); // Hostile rep → Restricted regime
        var fleet = new Fleet { Id = "player_fleet", OwnerId = "player" };
        fleet.Cargo["ore"] = 10;
        state.Fleets["player_fleet"] = fleet;

        var response = NpcFleetCombatSystem.GetPatrolResponse(state, "node_a", "player_fleet");
        Assert.That(response, Is.EqualTo(PatrolResponse.Pursue));
    }
}
