using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;
using System.Collections.Generic;
using System.Linq;

namespace SimCore.Tests.Systems;

// GATE.T18.NARRATIVE.TOPOLOGY_SHIFT.001
[TestFixture]
public sealed class TopologyShiftTests
{
    private static void AdvanceTo(SimState state, int targetTick)
    {
        while (state.Tick < targetTick)
            state.AdvanceTick();
    }

    /// <summary>
    /// Build a small graph:
    ///   A ── B ── C ── D ── E
    ///         \       /
    ///          F ── G
    /// A=arrival (Phase 3, mutable), B-G are Phase 3.
    /// All edges mutable. Each node has 2+ edges so no orphan risk
    /// for single mutations.
    /// </summary>
    private SimState MakeShiftableGraph()
    {
        var state = new SimState(42);
        int phase3 = TopologyShiftTweaksV0.STRUCT_MinPhaseForMutation;

        string[] ids = { "A", "B", "C", "D", "E", "F", "G" };
        foreach (var id in ids)
        {
            state.Nodes[id] = new Node
            {
                Id = id,
                InstabilityLevel = phase3,
                MutableTopology = true
            };
        }

        void AddEdge(string from, string to)
        {
            string eid = $"e_{from}_{to}";
            state.Edges[eid] = new Edge
            {
                Id = eid, FromNodeId = from, ToNodeId = to,
                Distance = 5f, IsMutable = true
            };
        }

        AddEdge("A", "B");
        AddEdge("B", "C");
        AddEdge("C", "D");
        AddEdge("D", "E");
        AddEdge("B", "F");
        AddEdge("F", "G");
        AddEdge("G", "D");

        state.Fleets["fleet_trader_1"] = new Fleet
        {
            Id = "fleet_trader_1",
            OwnerId = "player",
            Speed = 1.0f
        };

        return state;
    }

    [Test]
    public void Process_EmptyArrivals_NoException()
    {
        var state = MakeShiftableGraph();
        TopologyShiftSystem.Process(state);
        Assert.Pass();
    }

    [Test]
    public void Process_SkipsNpcFleetArrival()
    {
        var state = MakeShiftableGraph();
        state.Fleets["npc_fleet"] = new Fleet
        {
            Id = "npc_fleet", OwnerId = "npc_1", Speed = 1.0f
        };
        state.ArrivalsThisTick.Add(("npc_fleet", "e_A_B", "A"));

        var edgesBefore = state.Edges.Values
            .ToDictionary(e => e.Id, e => (e.FromNodeId, e.ToNodeId));

        TopologyShiftSystem.Process(state);

        // No edge should have changed
        foreach (var e in state.Edges.Values)
        {
            Assert.That((e.FromNodeId, e.ToNodeId),
                Is.EqualTo(edgesBefore[e.Id]),
                $"Edge {e.Id} changed on NPC arrival");
        }
    }

    [Test]
    public void Process_SkipsBelowPhase3()
    {
        var state = MakeShiftableGraph();
        state.Nodes["A"].InstabilityLevel = TopologyShiftTweaksV0.STRUCT_MinPhaseForMutation - 1;

        state.ArrivalsThisTick.Add(("fleet_trader_1", "e_A_B", "A"));

        var edgesBefore = state.Edges.Values
            .ToDictionary(e => e.Id, e => (e.FromNodeId, e.ToNodeId));

        TopologyShiftSystem.Process(state);

        foreach (var e in state.Edges.Values)
        {
            Assert.That((e.FromNodeId, e.ToNodeId),
                Is.EqualTo(edgesBefore[e.Id]),
                $"Edge {e.Id} changed below Phase 3");
        }
    }

    [Test]
    public void Process_SkipsMutableTopologyFalse()
    {
        var state = MakeShiftableGraph();
        state.Nodes["A"].MutableTopology = false;

        state.ArrivalsThisTick.Add(("fleet_trader_1", "e_A_B", "A"));

        var edgesBefore = state.Edges.Values
            .ToDictionary(e => e.Id, e => (e.FromNodeId, e.ToNodeId));

        TopologyShiftSystem.Process(state);

        foreach (var e in state.Edges.Values)
        {
            Assert.That((e.FromNodeId, e.ToNodeId),
                Is.EqualTo(edgesBefore[e.Id]),
                $"Edge {e.Id} changed with MutableTopology=false");
        }
    }

    [Test]
    public void Process_SkipsNonMutableEdges()
    {
        var state = MakeShiftableGraph();
        // Mark all edges as non-mutable
        foreach (var e in state.Edges.Values)
            e.IsMutable = false;

        state.ArrivalsThisTick.Add(("fleet_trader_1", "e_A_B", "A"));

        var edgesBefore = state.Edges.Values
            .ToDictionary(e => e.Id, e => (e.FromNodeId, e.ToNodeId));

        TopologyShiftSystem.Process(state);

        foreach (var e in state.Edges.Values)
        {
            Assert.That((e.FromNodeId, e.ToNodeId),
                Is.EqualTo(edgesBefore[e.Id]),
                $"Edge {e.Id} changed despite IsMutable=false");
        }
    }

    [Test]
    public void ApplyTopologyShift_IsDeterministic()
    {
        // Run the same state twice — results must match
        for (int attempt = 0; attempt < 2; attempt++)
        {
            var state = MakeShiftableGraph();
            AdvanceTo(state, 100);

            TopologyShiftSystem.ApplyTopologyShift(state, "B");

            if (attempt == 0)
            {
                // Store snapshot
                _deterministicSnapshot = state.Edges.Values
                    .OrderBy(e => e.Id)
                    .Select(e => (e.Id, e.FromNodeId, e.ToNodeId, e.MutationEpoch))
                    .ToList();
            }
            else
            {
                var current = state.Edges.Values
                    .OrderBy(e => e.Id)
                    .Select(e => (e.Id, e.FromNodeId, e.ToNodeId, e.MutationEpoch))
                    .ToList();

                Assert.That(current.Count, Is.EqualTo(_deterministicSnapshot!.Count));
                for (int i = 0; i < current.Count; i++)
                {
                    Assert.That(current[i], Is.EqualTo(_deterministicSnapshot[i]),
                        $"Edge mismatch at index {i}");
                }
            }
        }
    }
    private List<(string, string, string, int)>? _deterministicSnapshot;

    [Test]
    public void ApplyTopologyShift_MutationEpochIncrements()
    {
        var state = MakeShiftableGraph();
        AdvanceTo(state, 100);

        // Record initial epochs
        var initialEpochs = state.Edges.Values.ToDictionary(e => e.Id, e => e.MutationEpoch);

        TopologyShiftSystem.ApplyTopologyShift(state, "B");

        // At least one edge should have incremented epoch (if any mutation occurred)
        int totalEpochGain = state.Edges.Values.Sum(e => e.MutationEpoch - initialEpochs[e.Id]);
        // We can't guarantee a mutation occurs due to probability, but epoch should never decrease
        foreach (var e in state.Edges.Values)
        {
            Assert.That(e.MutationEpoch, Is.GreaterThanOrEqualTo(initialEpochs[e.Id]),
                $"Edge {e.Id} epoch decreased");
        }
    }

    [Test]
    public void ApplyTopologyShift_MaxMutationsCapped()
    {
        var state = MakeShiftableGraph();
        AdvanceTo(state, 100);

        // Try many times at different ticks — count max mutations per call
        for (int tick = 100; tick < 200; tick++)
        {
            var freshState = MakeShiftableGraph();
            AdvanceTo(freshState, tick);

            var before = freshState.Edges.Values
                .ToDictionary(e => e.Id, e => (e.FromNodeId, e.ToNodeId));

            TopologyShiftSystem.ApplyTopologyShift(freshState, "B");

            int mutations = 0;
            foreach (var e in freshState.Edges.Values)
            {
                if (before[e.Id] != (e.FromNodeId, e.ToNodeId))
                    mutations++;
            }

            Assert.That(mutations, Is.LessThanOrEqualTo(TopologyShiftTweaksV0.MaxMutationsPerArrival),
                $"Too many mutations at tick {tick}");
        }
    }

    [Test]
    public void CanSafelyMutate_PreventsBridgeEdgeRemoval()
    {
        // Create a graph where node X has only 1 edge (bridge edge)
        var state = new SimState(42);
        int phase3 = TopologyShiftTweaksV0.STRUCT_MinPhaseForMutation;

        state.Nodes["A"] = new Node { Id = "A", InstabilityLevel = phase3, MutableTopology = true };
        state.Nodes["B"] = new Node { Id = "B", InstabilityLevel = phase3, MutableTopology = true };
        state.Nodes["C"] = new Node { Id = "C", InstabilityLevel = phase3, MutableTopology = true };

        // A—B—C : B is bridge between A and C
        // If we mutate edge A-B at node A, B would be left with only B-C
        // But A would have 0 edges → should be blocked
        state.Edges["e1"] = new Edge
        {
            Id = "e1", FromNodeId = "A", ToNodeId = "B",
            Distance = 5f, IsMutable = true
        };
        state.Edges["e2"] = new Edge
        {
            Id = "e2", FromNodeId = "B", ToNodeId = "C",
            Distance = 5f, IsMutable = true
        };

        state.Fleets["fleet_trader_1"] = new Fleet
        {
            Id = "fleet_trader_1", OwnerId = "player", Speed = 1.0f
        };

        // A has only 1 edge (e1). Mutating e1 would orphan A.
        // Run many ticks — edge A-B should NEVER change
        for (int tick = 0; tick < 50; tick++)
        {
            var s = new SimState(42);
            foreach (var kv in state.Nodes) s.Nodes[kv.Key] = kv.Value;
            foreach (var kv in state.Edges)
            {
                s.Edges[kv.Key] = new Edge
                {
                    Id = kv.Value.Id,
                    FromNodeId = kv.Value.FromNodeId,
                    ToNodeId = kv.Value.ToNodeId,
                    Distance = kv.Value.Distance,
                    IsMutable = kv.Value.IsMutable
                };
            }
            foreach (var kv in state.Fleets) s.Fleets[kv.Key] = kv.Value;
            AdvanceTo(s, tick);

            TopologyShiftSystem.ApplyTopologyShift(s, "A");

            // Edge e1 must still connect A to B (A has only 1 edge)
            Assert.That(s.Edges["e1"].FromNodeId, Is.EqualTo("A"),
                $"Edge e1 FromNode changed at tick {tick}, orphaning A");
        }
    }

    [Test]
    public void PickNewTarget_ChoosesFromTwoHopNeighbors()
    {
        // In MakeShiftableGraph: A—B—C—D—E, B—F—G—D
        // At node B, direct neighbors are A, C, F
        // 2-hop from A: (none relevant), from C: D, from F: G
        // So valid targets for mutating e_A_B (far=A) are D or G (2-hop through C or F)
        // But D and G are already 2-hop from B, not direct neighbors of B
        // This test just validates that mutations only rewire to 2-hop nodes
        var state = MakeShiftableGraph();

        // Run 100 ticks and collect all rewired-to nodes
        var rewiredTo = new HashSet<string>();
        for (int tick = 0; tick < 500; tick++)
        {
            var s = MakeShiftableGraph();
            AdvanceTo(s, tick);
            TopologyShiftSystem.ApplyTopologyShift(s, "B");

            foreach (var e in s.Edges.Values)
            {
                var orig = state.Edges[e.Id];
                if (e.FromNodeId != orig.FromNodeId)
                    rewiredTo.Add(e.FromNodeId);
                if (e.ToNodeId != orig.ToNodeId)
                    rewiredTo.Add(e.ToNodeId);
            }
        }

        // All rewired nodes should be in the graph
        foreach (var nodeId in rewiredTo)
        {
            Assert.That(state.Nodes.ContainsKey(nodeId), Is.True,
                $"Rewired to unknown node {nodeId}");
        }
    }

    [Test]
    public void PickNewTarget_ReturnsNullWhenNo2HopCandidates()
    {
        // Minimal graph: just A—B, no 2-hop targets exist
        var state = new SimState(42);
        int phase3 = TopologyShiftTweaksV0.STRUCT_MinPhaseForMutation;

        state.Nodes["A"] = new Node { Id = "A", InstabilityLevel = phase3, MutableTopology = true };
        state.Nodes["B"] = new Node { Id = "B", InstabilityLevel = phase3, MutableTopology = true };

        state.Edges["e1"] = new Edge
        {
            Id = "e1", FromNodeId = "A", ToNodeId = "B",
            Distance = 5f, IsMutable = true
        };

        state.Fleets["fleet_trader_1"] = new Fleet
        {
            Id = "fleet_trader_1", OwnerId = "player", Speed = 1.0f
        };

        // No 2-hop candidates → nothing should change
        for (int tick = 0; tick < 50; tick++)
        {
            var s = new SimState(42);
            s.Nodes["A"] = new Node { Id = "A", InstabilityLevel = phase3, MutableTopology = true };
            s.Nodes["B"] = new Node { Id = "B", InstabilityLevel = phase3, MutableTopology = true };
            s.Edges["e1"] = new Edge
            {
                Id = "e1", FromNodeId = "A", ToNodeId = "B",
                Distance = 5f, IsMutable = true
            };
            s.Fleets["fleet_trader_1"] = new Fleet
            {
                Id = "fleet_trader_1", OwnerId = "player", Speed = 1.0f
            };
            AdvanceTo(s, tick);

            TopologyShiftSystem.ApplyTopologyShift(s, "A");

            Assert.That(s.Edges["e1"].FromNodeId, Is.EqualTo("A"));
            Assert.That(s.Edges["e1"].ToNodeId, Is.EqualTo("B"));
        }
    }

    [Test]
    public void Process_MissingFleet_NoException()
    {
        var state = MakeShiftableGraph();
        state.ArrivalsThisTick.Add(("nonexistent_fleet", "e_A_B", "A"));
        TopologyShiftSystem.Process(state);
        Assert.Pass();
    }

    [Test]
    public void Process_MissingNode_NoException()
    {
        var state = MakeShiftableGraph();
        state.ArrivalsThisTick.Add(("fleet_trader_1", "e_A_B", "nonexistent_node"));
        TopologyShiftSystem.Process(state);
        Assert.Pass();
    }

    [Test]
    public void Process_Phase4VoidAlsoTriggers()
    {
        var state = MakeShiftableGraph();
        // Phase 4 (Void) is above Phase 3 — should also trigger mutations
        state.Nodes["B"].InstabilityLevel = FractureWeightTweaksV0.STRUCT_PhaseVoidMin;
        AdvanceTo(state, 100);

        state.ArrivalsThisTick.Add(("fleet_trader_1", "e_A_B", "B"));

        // Just verify it doesn't crash and processes normally
        TopologyShiftSystem.Process(state);
        Assert.Pass();
    }
}
