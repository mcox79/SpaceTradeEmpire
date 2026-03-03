using NUnit.Framework;
using SimCore;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Gen;
using SimCore.Systems;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimCore.Tests;

public class IndustryTests
{
    // ─── GATE.S4.INDU_STRUCT.RECIPE_BIND.001 ───

    [Test]
    public void RecipeBind_ValidRecipeId_PassesValidation()
    {
        var state = new SimState(1);
        var mkt = new Market { Id = "node_0" };
        state.Markets.Add("node_0", mkt);

        var site = new IndustrySite
        {
            Id = "site_1",
            NodeId = "node_0",
            RecipeId = "recipe_extract_ore",
            Inputs = new Dictionary<string, int> { { "fuel", 1 } },
            Outputs = new Dictionary<string, int> { { "ore", 5 } }
        };
        state.IndustrySites.Add("site_1", site);

        var registry = ContentRegistryLoader.LoadFromJsonOrThrow(ContentRegistryLoader.DefaultRegistryJsonV0);
        Assert.DoesNotThrow(() => IndustrySystem.ValidateRecipeBindings(state, registry));
    }

    [Test]
    public void RecipeBind_InvalidRecipeId_ThrowsValidation()
    {
        var state = new SimState(1);
        var mkt = new Market { Id = "node_0" };
        state.Markets.Add("node_0", mkt);

        var site = new IndustrySite
        {
            Id = "site_1",
            NodeId = "node_0",
            RecipeId = "recipe_does_not_exist",
            Inputs = new Dictionary<string, int> { { "ore", 2 } },
            Outputs = new Dictionary<string, int> { { "food", 1 } }
        };
        state.IndustrySites.Add("site_1", site);

        var registry = ContentRegistryLoader.LoadFromJsonOrThrow(ContentRegistryLoader.DefaultRegistryJsonV0);
        var ex = Assert.Throws<InvalidOperationException>(() => IndustrySystem.ValidateRecipeBindings(state, registry));
        Assert.That(ex!.Message, Does.Contain("recipe_does_not_exist"));
    }

    [Test]
    public void RecipeBind_EmptyRecipeId_SkipsValidation()
    {
        var state = new SimState(1);
        var mkt = new Market { Id = "node_0" };
        state.Markets.Add("node_0", mkt);

        var site = new IndustrySite
        {
            Id = "site_1",
            NodeId = "node_0",
            RecipeId = "",
            Inputs = new Dictionary<string, int>(),
            Outputs = new Dictionary<string, int> { { "fuel", 5 } }
        };
        state.IndustrySites.Add("site_1", site);

        var registry = ContentRegistryLoader.LoadFromJsonOrThrow(ContentRegistryLoader.DefaultRegistryJsonV0);
        Assert.DoesNotThrow(() => IndustrySystem.ValidateRecipeBindings(state, registry));
    }

    // ─── Pre-existing tests ───

    [Test]
    public void Industry_ConsumesInputs_AndProducesOutputs()
    {
        var state = new SimState(123);
        
        // Setup Market
        var mkt = new Market { Id = "mkt_1" };
        mkt.Inventory["ore"] = 10;
        state.Markets.Add("mkt_1", mkt);
        
        // Setup Site (Refinery)
        var site = new IndustrySite 
        { 
            Id = "site_1", 
            NodeId = "mkt_1",
            Inputs = new Dictionary<string, int> { { "ore", 2 } },
            Outputs = new Dictionary<string, int> { { "metal", 1 } }
        };
        state.IndustrySites.Add("site_1", site);

        // TICK 1
        IndustrySystem.Process(state);

        // Assert: 10 - 2 = 8 Ore. 0 + 1 = 1 Metal.
        Assert.That(mkt.Inventory["ore"], Is.EqualTo(8));
        Assert.That(mkt.Inventory["metal"], Is.EqualTo(1));
    }

    [Test]
    public void Industry_PartialProduction_ScalesDown()
    {
        var state = new SimState(123);
        var mkt = new Market { Id = "mkt_1" };
        mkt.Inventory["ore"] = 5; // Needs 10 for full batch
        state.Markets.Add("mkt_1", mkt);

        var site = new IndustrySite 
        { 
            Id = "site_1", 
            NodeId = "mkt_1",
            Inputs = new Dictionary<string, int> { { "ore", 10 } },
            Outputs = new Dictionary<string, int> { { "metal", 4 } }
        };
        state.IndustrySites.Add("site_1", site);

        // TICK 1
        IndustrySystem.Process(state);

        // Ratio = 5 / 10 = 0.5
        // Consumed = floor(10 * 0.5) = 5
        // Produced = floor(4 * 0.5) = 2
        Assert.That(mkt.Inventory["ore"], Is.EqualTo(0));
        Assert.That(mkt.Inventory["metal"], Is.EqualTo(2));
    }

    // ─── GATE.S4.INDU_STRUCT.SHORTFALL_LOG.001 ───

    [Test]
    public void IndustryShortfall_StarvedRefinery_EmitsShortfallEvent()
    {
        var state = new SimState(42);
        var mkt = new Market { Id = "node_0" };
        mkt.Inventory["ore"] = 0;   // starved
        mkt.Inventory["fuel"] = 0;  // starved
        state.Markets.Add("node_0", mkt);

        var site = new IndustrySite
        {
            Id = "refinery_1",
            NodeId = "node_0",
            RecipeId = "recipe_refine_ore_to_metal",
            Inputs = new Dictionary<string, int> { { "ore", 10 }, { "fuel", 1 } },
            Outputs = new Dictionary<string, int> { { "metal", 5 } }
        };
        state.IndustrySites.Add("refinery_1", site);

        IndustrySystem.Process(state);

        Assert.That(state.ShortfallEventLog.Count, Is.GreaterThan(0), "Should emit shortfall events when starved.");
        var evt = state.ShortfallEventLog[0];
        Assert.That(evt.SiteId, Is.EqualTo("refinery_1"));
        Assert.That(evt.RecipeId, Is.EqualTo("recipe_refine_ore_to_metal"));
        Assert.That(evt.EfficiencyBps, Is.EqualTo(0));
        Assert.That(evt.AvailableQty, Is.EqualTo(0));
    }

    [Test]
    public void IndustryShortfall_FullySupplied_NoShortfallEvents()
    {
        var state = new SimState(42);
        var mkt = new Market { Id = "node_0" };
        mkt.Inventory["ore"] = 100;
        mkt.Inventory["fuel"] = 100;
        state.Markets.Add("node_0", mkt);

        var site = new IndustrySite
        {
            Id = "refinery_1",
            NodeId = "node_0",
            RecipeId = "recipe_refine_ore_to_metal",
            Inputs = new Dictionary<string, int> { { "ore", 10 }, { "fuel", 1 } },
            Outputs = new Dictionary<string, int> { { "metal", 5 } }
        };
        state.IndustrySites.Add("refinery_1", site);

        IndustrySystem.Process(state);

        Assert.That(state.ShortfallEventLog.Count, Is.EqualTo(0), "No shortfall events when fully supplied.");
    }

    // ─── GATE.S4.INDU_STRUCT.PLAYABLE_VIEW.001 ───

    [Test]
    public void NodeIndustry_QueryShape_ReturnsExpectedFields()
    {
        // Contract: IndustrySites queryable by NodeId with RecipeId, Efficiency, HealthBps, Outputs.
        var state = new SimState(1);
        var mkt = new Market { Id = "star_3" };
        mkt.Inventory["metal"] = 100;
        state.Markets.Add("star_3", mkt);

        var site = new IndustrySite
        {
            Id = "forge_3",
            NodeId = "star_3",
            RecipeId = "recipe_forge_hull_plating",
            Inputs = new Dictionary<string, int> { { "metal", 5 } },
            Outputs = new Dictionary<string, int> { { "hull_plating", 1 } },
            HealthBps = 9500,
            Efficiency = 0.95f
        };
        state.IndustrySites.Add("forge_3", site);

        // Query sites for node_id = "star_3"
        var nodeSites = new List<IndustrySite>();
        foreach (var kv in state.IndustrySites)
        {
            if (kv.Value.NodeId == "star_3")
                nodeSites.Add(kv.Value);
        }

        Assert.That(nodeSites.Count, Is.EqualTo(1));
        var s = nodeSites[0];
        Assert.That(s.Id, Is.EqualTo("forge_3"));
        Assert.That(s.RecipeId, Is.EqualTo("recipe_forge_hull_plating"));
        Assert.That(s.HealthBps, Is.EqualTo(9500));
        Assert.That(s.Outputs.ContainsKey("hull_plating"), Is.True);
    }

    // ─── GATE.S4.INDU_STRUCT.EPIC_CLOSE.001 ───

    [Test]
    public void IndustryChainScenario_Seed1_3StepChain_HullPlatingProduced_ShortfallOnOreCut()
    {
        // 1. Generate world with seed 1
        var state = new SimState(1);
        GalaxyGenerator.Generate(state, 20, 100f);

        // 2. Verify chain analysis: 3-step chain ending at hull_plating
        var registry = ContentRegistryLoader.LoadFromJsonOrThrow(ContentRegistryLoader.DefaultRegistryJsonV0);
        var chainResult = ChainAnalysis.Analyze(registry);
        Assert.That(chainResult.IsValid, Is.True, "Chain analysis must be valid.");

        var hullChain = chainResult.Chains.FirstOrDefault(c => c.FinalOutput == "hull_plating");
        Assert.That(hullChain, Is.Not.Null, "hull_plating chain must exist.");
        Assert.That(hullChain!.Depth, Is.LessThanOrEqualTo(3));

        // 3. Verify recipe bindings are valid
        IndustrySystem.ValidateRecipeBindings(state, registry);

        // 4. Verify forge sites exist
        var forgeSites = state.IndustrySites.Values.Where(s => s.RecipeId == "recipe_forge_hull_plating").ToList();
        Assert.That(forgeSites.Count, Is.GreaterThan(0), "World must contain hull_plating forges.");

        // 5. Run 2000 ticks — check hull_plating produced at forge nodes
        for (int t = 0; t < 2000; t++)
        {
            IndustrySystem.Process(state);
            state.AdvanceTick();
        }

        // Check if any market has hull_plating > 0
        bool hullPlatProduced = false;
        foreach (var forgeS in forgeSites)
        {
            if (state.Markets.TryGetValue(forgeS.NodeId, out var mkt))
            {
                if (mkt.Inventory.TryGetValue("hull_plating", out var qty) && qty > 0)
                {
                    hullPlatProduced = true;
                    break;
                }
            }
        }
        Assert.That(hullPlatProduced, Is.True, "hull_plating must be produced after 2000 ticks.");

        // 6. Cut ore supply: zero out all ore across all markets
        foreach (var mkt in state.Markets.Values)
        {
            if (mkt.Inventory.ContainsKey("ore"))
                mkt.Inventory["ore"] = 0;
        }

        // Clear shortfall log to isolate post-cut events
        state.ShortfallEventLog.Clear();

        // Run 100 more ticks — refineries should report shortfall
        for (int t = 0; t < 100; t++)
        {
            IndustrySystem.Process(state);
            state.AdvanceTick();
        }

        var oreShortfalls = state.ShortfallEventLog.Where(e => e.MissingGoodId == "ore").ToList();
        Assert.That(oreShortfalls.Count, Is.GreaterThan(0), "Ore shortfall events must be emitted after ore supply cut.");

        // 7. Chain report byte-stable across 2 runs
        var report1 = ChainAnalysis.BuildReportText(ChainAnalysis.Analyze(registry));
        var report2 = ChainAnalysis.BuildReportText(ChainAnalysis.Analyze(registry));
        Assert.That(report2, Is.EqualTo(report1), "Chain report must be byte-stable across 2 runs.");
    }
}