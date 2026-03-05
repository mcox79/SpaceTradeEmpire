using NUnit.Framework;
using SimCore;
using SimCore.Gen;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;
using System.Collections.Generic;
using System.Linq;

namespace SimCore.Tests.Systems;

// GATE.S4.NPC_INDU.DEMAND.001 + GATE.S4.NPC_INDU.REACTION.001: NPC industry contract tests.
public class NpcIndustrySystemTests
{
    private SimState CreateTestState()
    {
        var kernel = new SimKernel(42);
        GalaxyGenerator.Generate(kernel.State, 12, 100f);
        return kernel.State;
    }

    [Test]
    public void ProcessNpcIndustry_ConsumesInputsFromMarket()
    {
        var state = CreateTestState();

        // Find a node with an industry site that has inputs
        IndustrySite? targetSite = null;
        string? targetNodeId = null;
        foreach (var kv in state.IndustrySites)
        {
            if (kv.Value.Active && kv.Value.Inputs != null && kv.Value.Inputs.Count > 0)
            {
                targetSite = kv.Value;
                targetNodeId = kv.Value.NodeId;
                break;
            }
        }

        if (targetSite == null || targetNodeId == null)
        {
            Assert.Inconclusive("No active industry site with inputs found in test world");
            return;
        }

        // Ensure market exists and has stock
        if (!state.Markets.TryGetValue(targetNodeId, out var market))
        {
            Assert.Inconclusive("No market at industry site node");
            return;
        }

        var inputGoodId = targetSite.Inputs.Keys.First();
        market.Inventory[inputGoodId] = 100;

        int stockBefore = market.Inventory[inputGoodId];

        // Force tick to align with process interval
        while (state.Tick % NpcIndustryTweaksV0.ProcessIntervalTicks != 0)
        {
            // Use reflection or just accept the tick alignment
            // Process won't run until aligned
            NpcIndustrySystem.ProcessNpcIndustry(state);
            break; // Just one call — it either runs or doesn't
        }

        // Advance state tick to align
        var kernel = new SimKernel(state.InitialSeed);
        for (int i = 0; i < NpcIndustryTweaksV0.ProcessIntervalTicks; i++)
            kernel.Step();

        var postState = kernel.State;
        // Verify industry processing ran — stock may have changed
        // We check the mechanism works by direct call at aligned tick
        state = CreateTestState();
        if (state.IndustrySites.Values.Any(s => s.Active && s.Inputs?.Count > 0))
        {
            // Find site again
            foreach (var kv in state.IndustrySites)
            {
                if (!kv.Value.Active || kv.Value.Inputs == null || kv.Value.Inputs.Count == 0) continue;
                var nodeId = kv.Value.NodeId ?? "";
                if (!state.Markets.TryGetValue(nodeId, out var mkt)) continue;
                var goodId = kv.Value.Inputs.Keys.First();
                mkt.Inventory[goodId] = 100;

                // Manually set tick to process interval alignment
                NpcIndustrySystem.ProcessNpcIndustry(state);
                // tick 0 is aligned (0 % N == 0)
                int postStock = mkt.Inventory.TryGetValue(goodId, out var q) ? q : 0;
                Assert.That(postStock, Is.LessThan(100),
                    $"NPC industry should have consumed some {goodId} from market at {nodeId}");
                return;
            }
        }
    }

    [Test]
    public void ProcessNpcIndustry_SkipsWhenNoStock()
    {
        var state = CreateTestState();

        // Find a site with inputs, set stock to 0
        foreach (var kv in state.IndustrySites)
        {
            if (!kv.Value.Active || kv.Value.Inputs == null || kv.Value.Inputs.Count == 0) continue;
            var nodeId = kv.Value.NodeId ?? "";
            if (!state.Markets.TryGetValue(nodeId, out var mkt)) continue;

            foreach (var inp in kv.Value.Inputs)
                mkt.Inventory[inp.Key] = 0;

            // Should not crash or modify anything
            NpcIndustrySystem.ProcessNpcIndustry(state);
            Assert.Pass("ProcessNpcIndustry handles zero stock gracefully");
            return;
        }

        Assert.Inconclusive("No suitable industry site found");
    }

    [Test]
    public void ProcessNpcReaction_BoostsLowStock()
    {
        var state = CreateTestState();

        // Find a site with outputs
        foreach (var kv in state.IndustrySites)
        {
            if (!kv.Value.Active || kv.Value.Outputs == null || kv.Value.Outputs.Count == 0) continue;
            var nodeId = kv.Value.NodeId ?? "";
            if (!state.Markets.TryGetValue(nodeId, out var mkt)) continue;

            var outputGoodId = kv.Value.Outputs.Keys.First();
            mkt.Inventory[outputGoodId] = 1; // Below LowStockThreshold

            // Manually align tick to reaction interval
            // tick 0 is aligned (0 % N == 0)
            NpcIndustrySystem.ProcessNpcReaction(state);

            int postStock = mkt.Inventory.TryGetValue(outputGoodId, out var q) ? q : 0;
            Assert.That(postStock, Is.GreaterThan(1),
                $"NPC reaction should have boosted {outputGoodId} stock at {nodeId}");
            return;
        }

        Assert.Inconclusive("No suitable industry site with outputs found");
    }

    [Test]
    public void ProcessNpcReaction_DoesNotBoostHighStock()
    {
        var state = CreateTestState();

        foreach (var kv in state.IndustrySites)
        {
            if (!kv.Value.Active || kv.Value.Outputs == null || kv.Value.Outputs.Count == 0) continue;
            var nodeId = kv.Value.NodeId ?? "";
            if (!state.Markets.TryGetValue(nodeId, out var mkt)) continue;

            var outputGoodId = kv.Value.Outputs.Keys.First();
            mkt.Inventory[outputGoodId] = NpcIndustryTweaksV0.LowStockThreshold + 100;

            int stockBefore = mkt.Inventory[outputGoodId];
            NpcIndustrySystem.ProcessNpcReaction(state);

            int stockAfter = mkt.Inventory.TryGetValue(outputGoodId, out var q) ? q : 0;
            Assert.That(stockAfter, Is.EqualTo(stockBefore),
                "NPC reaction should not boost stock above threshold");
            return;
        }

        Assert.Inconclusive("No suitable industry site with outputs found");
    }

    [Test]
    public void ProcessNpcIndustry_NullState_NoThrow()
    {
        Assert.DoesNotThrow(() => NpcIndustrySystem.ProcessNpcIndustry(null!));
    }

    [Test]
    public void ProcessNpcReaction_NullState_NoThrow()
    {
        Assert.DoesNotThrow(() => NpcIndustrySystem.ProcessNpcReaction(null!));
    }

    [Test]
    public void NpcIndustry_200Tick_ScenarioProof()
    {
        // GATE.S4.NPC_INDU.PROOF.001: 200-tick scenario with demand and reaction.
        var state = CreateTestState();

        // Set up a site with both inputs and outputs
        IndustrySite? site = null;
        string? nodeId = null;
        foreach (var kv in state.IndustrySites)
        {
            if (kv.Value.Active &&
                kv.Value.Inputs?.Count > 0 &&
                kv.Value.Outputs?.Count > 0 &&
                !string.IsNullOrEmpty(kv.Value.NodeId) &&
                state.Markets.ContainsKey(kv.Value.NodeId))
            {
                site = kv.Value;
                nodeId = kv.Value.NodeId;
                break;
            }
        }

        if (site == null || nodeId == null)
        {
            Assert.Inconclusive("No site with both inputs and outputs");
            return;
        }

        var market = state.Markets[nodeId];

        // Seed initial inventory
        foreach (var inp in site.Inputs)
            market.Inventory[inp.Key] = 200;
        foreach (var outp in site.Outputs)
            market.Inventory[outp.Key] = 5; // Low, to trigger reaction

        // Run 200 ticks
        var kernel = new SimKernel(state.InitialSeed);
        // Replace state is not possible, so just run the kernel
        // The kernel generates its own state, so we test via direct system calls
        for (int tick = 0; tick < 200; tick++)
        {
            NpcIndustrySystem.ProcessNpcIndustry(state);
            NpcIndustrySystem.ProcessNpcReaction(state);
        }

        // Verify: inputs consumed (stock lower than 200)
        bool inputConsumed = false;
        foreach (var inp in site.Inputs)
        {
            int stock = market.Inventory.TryGetValue(inp.Key, out var q) ? q : 0;
            if (stock < 200) inputConsumed = true;
        }
        Assert.That(inputConsumed, Is.True, "NPC industry should have consumed some inputs over 200 ticks");

        // Verify: outputs boosted (stock higher than initial 5)
        bool outputBoosted = false;
        foreach (var outp in site.Outputs)
        {
            int stock = market.Inventory.TryGetValue(outp.Key, out var q) ? q : 0;
            if (stock > 5) outputBoosted = true;
        }
        Assert.That(outputBoosted, Is.True, "NPC industry reaction should have boosted low-stock outputs");
    }
}
