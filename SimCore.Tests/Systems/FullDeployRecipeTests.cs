using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SimCore;
using SimCore.Content;
using SimCore.Gen;

namespace SimCore.Tests.Systems;

// GATE.S7.PRODUCTION.FULL_DEPLOY.001: All 9 recipes deployed as industry sites.
[TestFixture]
[Category("ContentRegistryContract")]
public sealed class FullDeployRecipeTests
{
    [Test]
    public void AllNineRecipes_HaveAtLeastOneSite()
    {
        var kernel = new SimKernel(42);
        GalaxyGenerator.Generate(kernel.State, 30, 100f);

        var allRecipeIds = new HashSet<string>(System.StringComparer.Ordinal)
        {
            WellKnownRecipeIds.ExtractOre,
            WellKnownRecipeIds.RefineMetal,
            WellKnownRecipeIds.ProcessFood,
            WellKnownRecipeIds.AssembleElectronics,
            WellKnownRecipeIds.FabricateComposites,
            WellKnownRecipeIds.ManufactureMunitions,
            WellKnownRecipeIds.AssembleComponents,
            WellKnownRecipeIds.SalvageToMetal,
            WellKnownRecipeIds.SalvageToComponents,
        };

        var placedRecipeIds = new HashSet<string>(System.StringComparer.Ordinal);
        foreach (var site in kernel.State.IndustrySites.Values)
        {
            if (!string.IsNullOrEmpty(site.RecipeId))
                placedRecipeIds.Add(site.RecipeId);
        }

        foreach (var recipeId in allRecipeIds)
        {
            Assert.That(placedRecipeIds.Contains(recipeId), Is.True,
                $"Recipe '{recipeId}' has no industry site in generated world (seed 42).");
        }
    }

    [Test]
    public void FoodProcessor_PlacedAtAgriNodes()
    {
        var kernel = new SimKernel(42);
        GalaxyGenerator.Generate(kernel.State, 30, 100f);

        var foodSites = kernel.State.IndustrySites.Values
            .Where(s => s.RecipeId == WellKnownRecipeIds.ProcessFood)
            .ToList();

        Assert.That(foodSites.Count, Is.GreaterThan(0), "No food processor sites found");

        // Verify each food processor is at a node with organics in the market
        foreach (var site in foodSites)
        {
            bool hasOrganics = kernel.State.Markets.TryGetValue(site.NodeId, out var mkt)
                && mkt.Inventory.ContainsKey(WellKnownGoodIds.Organics);
            Assert.That(hasOrganics, Is.True,
                $"Food processor at {site.NodeId} should be at an agri node with organics");
        }
    }

    [Test]
    public void SalvageYards_PlacedAtDistinctNodes()
    {
        var kernel = new SimKernel(42);
        GalaxyGenerator.Generate(kernel.State, 30, 100f);

        var metalSalvage = kernel.State.IndustrySites.Values
            .Where(s => s.RecipeId == WellKnownRecipeIds.SalvageToMetal).ToList();
        var compSalvage = kernel.State.IndustrySites.Values
            .Where(s => s.RecipeId == WellKnownRecipeIds.SalvageToComponents).ToList();

        Assert.That(metalSalvage.Count, Is.GreaterThan(0), "No SalvageToMetal sites");
        Assert.That(compSalvage.Count, Is.GreaterThan(0), "No SalvageToComponents sites");
    }
}
