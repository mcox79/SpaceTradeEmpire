using NUnit.Framework;
using SimCore;
using SimCore.Gen;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;
using System.Collections.Generic;

namespace SimCore.Tests.Systems;

// GATE.S8.THREAT.SUPPLY_SHOCK.001: Supply shock tests.
[TestFixture]
public sealed class SupplyShockTests
{
    private SimKernel CreateKernel(int seed = 42)
    {
        var kernel = new SimKernel(seed);
        GalaxyGenerator.Generate(kernel.State, 12, 100f);
        return kernel;
    }

    [Test]
    public void SupplyShock_SkirmishReducesEfficiency()
    {
        var kernel = CreateKernel();
        var state = kernel.State;

        // Pick a node with an industry site.
        string nodeId = "";
        string siteId = "";
        foreach (var kv in state.IndustrySites)
        {
            if (!string.IsNullOrEmpty(kv.Value.NodeId))
            {
                nodeId = kv.Value.NodeId;
                siteId = kv.Key;
                break;
            }
        }
        if (string.IsNullOrEmpty(nodeId))
        {
            Assert.Inconclusive("No industry sites found in generated galaxy");
            return;
        }

        // Create a warfront at Skirmish intensity covering that node.
        state.Warfronts["wf_test"] = new WarfrontState
        {
            Id = "wf_test",
            CombatantA = "faction_a",
            CombatantB = "faction_b",
            Intensity = WarfrontIntensity.Skirmish,
            ContestedNodeIds = new List<string> { nodeId }
        };

        // Set site efficiency to 1.0 (full).
        state.IndustrySites[siteId].Efficiency = 1.0f;

        SupplyShockSystem.Process(state);

        float expected = (100 - ThreatTweaksV0.SkirmishOutputReductionPct) / 100f;
        Assert.That(state.IndustrySites[siteId].Efficiency, Is.LessThanOrEqualTo(expected + 0.001f));
    }

    [Test]
    public void SupplyShock_OpenWarHaltsProduction()
    {
        var kernel = CreateKernel();
        var state = kernel.State;

        string nodeId = "";
        string siteId = "";
        foreach (var kv in state.IndustrySites)
        {
            if (!string.IsNullOrEmpty(kv.Value.NodeId))
            {
                nodeId = kv.Value.NodeId;
                siteId = kv.Key;
                break;
            }
        }
        if (string.IsNullOrEmpty(nodeId))
        {
            Assert.Inconclusive("No industry sites found");
            return;
        }

        state.Warfronts["wf_test"] = new WarfrontState
        {
            Id = "wf_test",
            CombatantA = "faction_a",
            CombatantB = "faction_b",
            Intensity = WarfrontIntensity.OpenWar,
            ContestedNodeIds = new List<string> { nodeId }
        };

        state.IndustrySites[siteId].Efficiency = 1.0f;

        SupplyShockSystem.Process(state);

        Assert.That(state.IndustrySites[siteId].Efficiency, Is.EqualTo(0f).Within(0.001f));
    }

    [Test]
    public void SupplyShock_PeaceDoesNotAffectSites()
    {
        var kernel = CreateKernel();
        var state = kernel.State;

        string nodeId = "";
        string siteId = "";
        foreach (var kv in state.IndustrySites)
        {
            if (!string.IsNullOrEmpty(kv.Value.NodeId))
            {
                nodeId = kv.Value.NodeId;
                siteId = kv.Key;
                break;
            }
        }
        if (string.IsNullOrEmpty(nodeId))
        {
            Assert.Inconclusive("No industry sites found");
            return;
        }

        state.Warfronts["wf_test"] = new WarfrontState
        {
            Id = "wf_test",
            CombatantA = "faction_a",
            CombatantB = "faction_b",
            Intensity = WarfrontIntensity.Peace,
            ContestedNodeIds = new List<string> { nodeId }
        };

        state.IndustrySites[siteId].Efficiency = 1.0f;

        SupplyShockSystem.Process(state);

        Assert.That(state.IndustrySites[siteId].Efficiency, Is.EqualTo(1.0f).Within(0.001f));
    }

    [Test]
    public void SupplyShock_UncontestedSiteUnaffected()
    {
        var kernel = CreateKernel();
        var state = kernel.State;

        // Create a warfront at a node with no industry site.
        state.Warfronts["wf_test"] = new WarfrontState
        {
            Id = "wf_test",
            CombatantA = "faction_a",
            CombatantB = "faction_b",
            Intensity = WarfrontIntensity.OpenWar,
            ContestedNodeIds = new List<string> { "nonexistent_node" }
        };

        // All sites should remain at their current efficiency.
        var efficiencies = new Dictionary<string, float>();
        foreach (var kv in state.IndustrySites)
            efficiencies[kv.Key] = kv.Value.Efficiency;

        SupplyShockSystem.Process(state);

        foreach (var kv in state.IndustrySites)
            Assert.That(kv.Value.Efficiency, Is.EqualTo(efficiencies[kv.Key]).Within(0.001f));
    }
}
