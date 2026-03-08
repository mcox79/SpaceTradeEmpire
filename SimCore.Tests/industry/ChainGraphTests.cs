using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SimCore.Content;
using SimCore.Systems;

namespace SimCore.Tests.Industry;

// GATE.S4.INDU_STRUCT.CHAIN_GRAPH.001
[TestFixture]
public sealed class ChainGraphTests
{
    [Test]
    public void ChainGraph_DefaultRegistry_ProducesExpectedChains()
    {
        var registry = ContentRegistryLoader.LoadFromJsonOrThrow(ContentRegistryLoader.DefaultRegistryJsonV0);
        var result = ChainAnalysis.Analyze(registry);

        Assert.That(result.IsValid, Is.True, "Default registry should produce valid chains.");
        Assert.That(result.Violations, Is.Empty);
        Assert.That(result.Chains.Count, Is.GreaterThan(0), "Should find at least one chain.");

        // munitions chain should exist: fuel + metal → munitions (depth 2 via ore→metal)
        var munChain = result.Chains.FirstOrDefault(c => c.FinalOutput == "munitions");
        Assert.That(munChain, Is.Not.Null, "Should find munitions chain.");
        Assert.That(munChain!.Depth, Is.LessThanOrEqualTo(3), "munitions chain depth <= 3.");
        Assert.That(munChain.RecipeSequence, Does.Contain("recipe_manufacture_munitions"));
        Assert.That(munChain.RawInputs, Does.Contain("fuel"), "fuel is a raw input (no recipe produces it).");
    }

    [Test]
    public void ChainGraph_Depth5_IsFlagged()
    {
        // Build a 5-deep chain: A→B→C→D→E→F (5 recipes, exceeds max 4)
        var registry = new ContentRegistryLoader.ContentRegistryV0
        {
            Version = 0,
            Goods = new List<ContentRegistryLoader.GoodDefV0>
            {
                new() { Id = "a" },
                new() { Id = "b" },
                new() { Id = "c" },
                new() { Id = "d" },
                new() { Id = "e" },
                new() { Id = "f" }
            },
            Recipes = new List<ContentRegistryLoader.RecipeDefV0>
            {
                new()
                {
                    Id = "r1",
                    Inputs = new List<ContentRegistryLoader.RecipeLineV0> { new() { GoodId = "a", Qty = 1 } },
                    Outputs = new List<ContentRegistryLoader.RecipeLineV0> { new() { GoodId = "b", Qty = 1 } }
                },
                new()
                {
                    Id = "r2",
                    Inputs = new List<ContentRegistryLoader.RecipeLineV0> { new() { GoodId = "b", Qty = 1 } },
                    Outputs = new List<ContentRegistryLoader.RecipeLineV0> { new() { GoodId = "c", Qty = 1 } }
                },
                new()
                {
                    Id = "r3",
                    Inputs = new List<ContentRegistryLoader.RecipeLineV0> { new() { GoodId = "c", Qty = 1 } },
                    Outputs = new List<ContentRegistryLoader.RecipeLineV0> { new() { GoodId = "d", Qty = 1 } }
                },
                new()
                {
                    Id = "r4",
                    Inputs = new List<ContentRegistryLoader.RecipeLineV0> { new() { GoodId = "d", Qty = 1 } },
                    Outputs = new List<ContentRegistryLoader.RecipeLineV0> { new() { GoodId = "e", Qty = 1 } }
                },
                new()
                {
                    Id = "r5",
                    Inputs = new List<ContentRegistryLoader.RecipeLineV0> { new() { GoodId = "e", Qty = 1 } },
                    Outputs = new List<ContentRegistryLoader.RecipeLineV0> { new() { GoodId = "f", Qty = 1 } }
                }
            },
            Modules = new List<ContentRegistryLoader.ModuleDefV0>()
        };

        var result = ChainAnalysis.Analyze(registry);

        Assert.That(result.IsValid, Is.False, "Depth-5 chain should be flagged.");
        Assert.That(result.Violations.Any(v => v.Contains("DEPTH_EXCEEDED")), Is.True);
    }

    [Test]
    public void ChainGraph_ReportText_IsDeterministic()
    {
        var registry = ContentRegistryLoader.LoadFromJsonOrThrow(ContentRegistryLoader.DefaultRegistryJsonV0);

        var r1 = ChainAnalysis.Analyze(registry);
        var r2 = ChainAnalysis.Analyze(registry);

        var text1 = ChainAnalysis.BuildReportText(r1);
        var text2 = ChainAnalysis.BuildReportText(r2);

        Assert.That(text2, Is.EqualTo(text1), "Chain report must be byte-stable across runs.");
        Assert.That(text1, Does.Contain("CHAIN_ANALYSIS_REPORT_V0"));
        Assert.That(text1, Does.Contain("is_valid=true"));
    }
}
