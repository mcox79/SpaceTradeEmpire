using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;

namespace SimCore.Tests.Systems;

// GATE.S6.ANOMALY.REWARD_LOOT.001
[TestFixture]
public sealed class RewardLootTests
{
    [Test]
    public void Derelict_ProducesSalvagedTech()
    {
        var enc = new AnomalyEncounter
        {
            EncounterId = "enc_1",
            Family = "DERELICT",
            NodeId = "n1"
        };

        var state = new SimState(42);
        DiscoveryOutcomeSystem.GenerateLootByFamily(enc, state);

        Assert.That(enc.LootItems, Contains.Key("salvaged_tech"));
        Assert.That(enc.LootItems["salvaged_tech"], Is.GreaterThan(0));
        Assert.That(enc.CreditReward, Is.GreaterThan(0));
    }

    [Test]
    public void Ruin_ProducesAnomalySamples()
    {
        var enc = new AnomalyEncounter
        {
            EncounterId = "enc_2",
            Family = "RUIN",
            NodeId = "n1"
        };

        var state = new SimState(42);
        DiscoveryOutcomeSystem.GenerateLootByFamily(enc, state);

        Assert.That(enc.LootItems, Contains.Key("anomaly_samples"));
        Assert.That(enc.LootItems["anomaly_samples"], Is.GreaterThan(0));
        Assert.That(enc.CreditReward, Is.GreaterThanOrEqualTo(75));
    }

    [Test]
    public void Signal_ProducesDiscoveryLead()
    {
        var enc = new AnomalyEncounter
        {
            EncounterId = "enc_3",
            Family = "SIGNAL",
            NodeId = "n1"
        };

        var state = new SimState(42);
        state.Nodes["n1"] = new Node { Id = "n1", Kind = NodeKind.Station };
        state.Nodes["n2"] = new Node { Id = "n2", Kind = NodeKind.Station };
        state.Edges["e1"] = new Edge { Id = "e1", FromNodeId = "n1", ToNodeId = "n2", Distance = 1f };

        DiscoveryOutcomeSystem.GenerateLootByFamily(enc, state);

        Assert.That(enc.DiscoveryLeadNodeId, Is.Not.Empty);
    }

    [Test]
    public void UnknownFamily_NoLootItems()
    {
        var enc = new AnomalyEncounter
        {
            EncounterId = "enc_4",
            Family = "OUTCOME",
            NodeId = "n1"
        };

        var state = new SimState(42);
        DiscoveryOutcomeSystem.GenerateLootByFamily(enc, state);

        Assert.That(enc.LootItems, Is.Empty);
    }

    [Test]
    public void EachFamily_ProducesExpectedRewardType()
    {
        var state = new SimState(42);
        state.Nodes["n1"] = new Node { Id = "n1", Kind = NodeKind.Station };

        var families = new[] { "DERELICT", "RUIN", "SIGNAL" };
        foreach (var family in families)
        {
            var enc = new AnomalyEncounter
            {
                EncounterId = $"enc_{family}",
                Family = family,
                NodeId = "n1"
            };
            DiscoveryOutcomeSystem.GenerateLootByFamily(enc, state);

            // All families should produce some reward.
            bool hasLoot = enc.LootItems.Count > 0;
            bool hasCredits = enc.CreditReward > 0;
            bool hasLead = !string.IsNullOrEmpty(enc.DiscoveryLeadNodeId);

            Assert.That(hasLoot || hasCredits || hasLead, Is.True,
                $"Family {family} should produce at least one reward type");
        }
    }
}
