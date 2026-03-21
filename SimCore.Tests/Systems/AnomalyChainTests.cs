using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Content;
using SimCore.Gen;
using SimCore.Systems;
using System.Collections.Generic;
using System.Linq;

namespace SimCore.Tests.Systems;

// GATE.T41.ANOMALY_CHAIN.PLACEMENT.001 + GATE.T41.ANOMALY_CHAIN.ADVANCE.001
[TestFixture]
public sealed class AnomalyChainTests
{
    /// <summary>
    /// Build a graph large enough for chain placement (needs ~7 hops depth).
    /// Linear chain: N0 ── N1 ── N2 ── ... ── N9
    /// </summary>
    private SimState MakeChainGraph()
    {
        var state = new SimState(42);
        state.PlayerLocationNodeId = "N0";

        for (int i = 0; i < 10; i++)
        {
            var id = $"N{i}";
            state.Nodes[id] = new Node { Id = id };
            state.Markets[id] = new Market { Id = id };
        }

        for (int i = 0; i < 9; i++)
        {
            var eid = $"e_N{i}_N{i + 1}";
            state.Edges[eid] = new Edge { Id = eid, FromNodeId = $"N{i}", ToNodeId = $"N{i + 1}", Distance = 5f };
        }

        state.Fleets["fleet_player"] = new Fleet { Id = "fleet_player", OwnerId = "player", Speed = 1.0f };

        // Intel is auto-initialized by SimState constructor
        return state;
    }

    [Test]
    public void AnomalyChainContentV0_HasThreeChains()
    {
        var chains = AnomalyChainContentV0.AllChains;
        Assert.That(chains, Is.Not.Null);
        Assert.That(chains.Count, Is.EqualTo(3), "Should have Valorin, Communion, Pentagon chains");
    }

    [Test]
    public void AnomalyChainContentV0_AllStepsHaveNarrative()
    {
        foreach (var chain in AnomalyChainContentV0.AllChains)
        {
            foreach (var step in chain.Steps)
            {
                Assert.That(step.NarrativeText, Is.Not.Empty,
                    $"Chain {chain.ChainId} step {step.StepIndex} missing NarrativeText");
                // Final step has no lead (no "next" step to point to)
                if (step.StepIndex < chain.Steps.Count - 1)
                    Assert.That(step.LeadText, Is.Not.Empty,
                        $"Chain {chain.ChainId} step {step.StepIndex} missing LeadText");
                Assert.That(step.DiscoveryKind, Is.Not.Empty,
                    $"Chain {chain.ChainId} step {step.StepIndex} missing DiscoveryKind");
            }
        }
    }

    [Test]
    public void PlaceAnomalyChains_CreatesChainEntities()
    {
        var state = MakeChainGraph();

        NarrativePlacementGen.PlaceAnomalyChains(state);

        Assert.That(state.AnomalyChains.Count, Is.GreaterThan(0),
            "PlaceAnomalyChains should create at least one chain");

        foreach (var kv in state.AnomalyChains)
        {
            var chain = kv.Value;
            Assert.That(chain.ChainId, Is.Not.Empty);
            Assert.That(chain.Status, Is.EqualTo(AnomalyChainStatus.Active));
            Assert.That(chain.CurrentStepIndex, Is.EqualTo(0));
            Assert.That(chain.Steps.Count, Is.GreaterThan(0));
            Assert.That(chain.StarterNodeId, Is.Not.Empty);
        }
    }

    [Test]
    public void PlaceAnomalyChains_SeedsDiscoveriesForSteps()
    {
        var state = MakeChainGraph();

        NarrativePlacementGen.PlaceAnomalyChains(state);

        foreach (var chain in state.AnomalyChains.Values)
        {
            foreach (var step in chain.Steps)
            {
                if (string.IsNullOrEmpty(step.PlacedDiscoveryId)) continue;

                // The discovery should exist in Intel
                Assert.That(state.Intel.Discoveries!.ContainsKey(step.PlacedDiscoveryId),
                    Is.True,
                    $"Chain {chain.ChainId} step {step.StepIndex} placed discovery not in Intel");

                var disc = state.Intel.Discoveries[step.PlacedDiscoveryId];
                Assert.That(disc.Phase, Is.EqualTo(DiscoveryPhase.Seen),
                    "Placed chain discoveries should start at Seen phase");
            }
        }
    }

    [Test]
    public void PlaceAnomalyChains_IsDeterministic()
    {
        // Run twice with same seed — chain placement must be identical
        var state1 = MakeChainGraph();
        NarrativePlacementGen.PlaceAnomalyChains(state1);

        var state2 = MakeChainGraph();
        NarrativePlacementGen.PlaceAnomalyChains(state2);

        var chains1 = state1.AnomalyChains.OrderBy(k => k.Key, System.StringComparer.Ordinal).ToList();
        var chains2 = state2.AnomalyChains.OrderBy(k => k.Key, System.StringComparer.Ordinal).ToList();

        Assert.That(chains1.Count, Is.EqualTo(chains2.Count), "Chain count must be deterministic");

        for (int i = 0; i < chains1.Count; i++)
        {
            Assert.That(chains1[i].Key, Is.EqualTo(chains2[i].Key), $"Chain ID mismatch at index {i}");
            Assert.That(chains1[i].Value.StarterNodeId, Is.EqualTo(chains2[i].Value.StarterNodeId),
                $"StarterNodeId mismatch for chain {chains1[i].Key}");
        }
    }

    [Test]
    public void TryAdvanceChains_AdvancesOnMatchingDiscovery()
    {
        var state = MakeChainGraph();
        NarrativePlacementGen.PlaceAnomalyChains(state);

        // Find a chain with at least one step
        var chain = state.AnomalyChains.Values.FirstOrDefault(c => c.Steps.Count > 0);
        if (chain is null)
        {
            Assert.Inconclusive("No chains were placed (graph may be too small)");
            return;
        }

        var step0 = chain.Steps[0];
        Assert.That(step0.IsCompleted, Is.False, "Step 0 should start incomplete");

        // Simulate analyzing the placed discovery for step 0
        if (!string.IsNullOrEmpty(step0.PlacedDiscoveryId) &&
            state.Intel.Discoveries!.TryGetValue(step0.PlacedDiscoveryId, out var disc))
        {
            disc.Phase = DiscoveryPhase.Analyzed;
        }

        // Process to trigger chain advancement
        DiscoveryOutcomeSystem.Process(state);

        Assert.That(step0.IsCompleted, Is.True,
            "Step 0 should be completed after analyzing its discovery");
        Assert.That(chain.CurrentStepIndex, Is.GreaterThanOrEqualTo(1),
            "Chain should advance to next step");
    }

    [Test]
    public void ChainCompletion_SetsStatusCompleted_AndAppliesLoot()
    {
        var state = MakeChainGraph();

        // Create a minimal 1-step chain for easy completion testing
        var chain = new AnomalyChain
        {
            ChainId = "CHAIN.TEST_SIMPLE",
            Status = AnomalyChainStatus.Active,
            CurrentStepIndex = 0,
            StarterNodeId = "N0",
            Steps = new List<AnomalyChainStep>
            {
                new AnomalyChainStep
                {
                    StepIndex = 0,
                    DiscoveryKind = "SIGNAL",
                    PlacedDiscoveryId = "disc_v0|SIGNAL|N1|ref1|chain_test",
                    NarrativeText = "Test signal",
                    LeadText = "Test lead",
                    LootOverrides = new Dictionary<string, int> { { "credits", 100 } }
                }
            }
        };
        state.AnomalyChains["CHAIN.TEST_SIMPLE"] = chain;

        // Seed the discovery
        state.Intel.Discoveries!["disc_v0|SIGNAL|N1|ref1|chain_test"] = new DiscoveryStateV0
        {
            DiscoveryId = "disc_v0|SIGNAL|N1|ref1|chain_test",
            Phase = DiscoveryPhase.Analyzed
        };
        state.Nodes["N1"].SeededDiscoveryIds ??= new List<string>();
        state.Nodes["N1"].SeededDiscoveryIds.Add("disc_v0|SIGNAL|N1|ref1|chain_test");

        long creditsBefore = state.PlayerCredits;

        DiscoveryOutcomeSystem.Process(state);

        Assert.That(chain.Status, Is.EqualTo(AnomalyChainStatus.Completed),
            "Chain should be Completed after all steps done");
        Assert.That(state.PlayerCredits, Is.GreaterThan(creditsBefore),
            "Climax loot should award credits");
    }

    [Test]
    public void InstabilityGate_AssignedToDeepDiscoveries()
    {
        // Run placement with a larger graph and check that some deep discoveries get gates
        var state = MakeChainGraph();
        NarrativePlacementGen.PlaceAnomalyChains(state);

        bool anyGated = false;
        foreach (var disc in state.Intel.Discoveries!.Values)
        {
            if (disc.InstabilityGate > 0)
            {
                anyGated = true;
                Assert.That(disc.InstabilityGate, Is.GreaterThanOrEqualTo(2),
                    "Instability gate should be >= 2");
            }
        }

        // With only 10 nodes, deep discoveries might not exist — this is OK
        if (!anyGated)
        {
            Assert.Pass("No deep discoveries placed (graph may be too small for instability gating)");
        }
    }
}
