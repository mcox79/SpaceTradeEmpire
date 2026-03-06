using NUnit.Framework;
using SimCore;
using SimCore.Entities;

namespace SimCore.Tests.Systems;

// GATE.S6.ANOMALY.ENCOUNTER_MODEL.001
[TestFixture]
public sealed class AnomalyEncounterTests
{
    [Test]
    public void AnomalyEncounter_CanCreateAndStore()
    {
        var state = new SimState(42);
        var encounter = new AnomalyEncounter
        {
            EncounterId = "AE1",
            NodeId = "star_0",
            DiscoveryId = "DISC_001",
            Family = "DERELICT",
            Difficulty = 2,
            Status = AnomalyEncounterStatus.Pending,
            CreatedTick = state.Tick
        };
        state.AnomalyEncounters[encounter.EncounterId] = encounter;

        Assert.That(state.AnomalyEncounters, Has.Count.EqualTo(1));
        Assert.That(state.AnomalyEncounters["AE1"].Family, Is.EqualTo("DERELICT"));
        Assert.That(state.AnomalyEncounters["AE1"].Status, Is.EqualTo(AnomalyEncounterStatus.Pending));
    }

    [Test]
    public void AnomalyEncounter_CompletionUpdatesStatus()
    {
        var encounter = new AnomalyEncounter
        {
            EncounterId = "AE1",
            Family = "RUIN",
            Status = AnomalyEncounterStatus.Pending
        };
        encounter.Status = AnomalyEncounterStatus.Completed;
        Assert.That(encounter.Status, Is.EqualTo(AnomalyEncounterStatus.Completed));
    }

    [Test]
    public void AnomalyEncounter_LootFieldsDefaultEmpty()
    {
        var encounter = new AnomalyEncounter();
        Assert.That(encounter.LootItems, Is.Empty);
        Assert.That(encounter.CreditReward, Is.EqualTo(0));
        Assert.That(encounter.DiscoveryLeadNodeId, Is.EqualTo(""));
    }
}
