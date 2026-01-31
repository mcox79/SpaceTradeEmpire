using SimCore.Entities;
using System.Collections.Generic;

namespace SimCore.Systems;

public static class IndustrySystem
{
    // HARDCODED RECIPES FOR SLICE 2 (Will move to JSON later)
    private static readonly Dictionary<string, Recipe> _recipes = new()
    {
        { "smelt_iron", new Recipe 
            { 
                Id = "smelt_iron", 
                Name = "Iron Smelting", 
                DurationTicks = 5,
                Inputs = new() { { "ore_iron", 2 } },
                Outputs = new() { { "alloy_steel", 1 } }
            } 
        },
        { "refine_fuel", new Recipe 
            { 
                Id = "refine_fuel", 
                Name = "Fuel Refining", 
                DurationTicks = 3,
                Inputs = new() { { "ore_gold", 1 } }, // Placeholder recipe
                Outputs = new() { { "fuel", 5 } }
            } 
        }
    };

    public static void Process(SimState state)
    {
        foreach (var market in state.Markets.Values)
        {
            foreach (var industry in market.Industries.Values)
            {
                if (!industry.IsActive) continue;
                ProcessIndustry(market, industry);
            }
        }
    }

    private static void ProcessIndustry(Market market, Industry industry)
    {
        if (!_recipes.TryGetValue(industry.RecipeId, out var recipe)) return;

        // 1. IF IDLE: Try to Start
        if (industry.Progress == 0)
        {
            if (CanAfford(market, recipe))
            {
                ConsumeInputs(market, recipe);
                industry.Progress = 1;
            }
        }
        // 2. IF WORKING: Advance
        else
        {
            industry.Progress++;
            if (industry.Progress >= recipe.DurationTicks)
            {
                ProduceOutputs(market, recipe);
                industry.Progress = 0; // Reset to Idle
            }
        }
    }

    private static bool CanAfford(Market market, Recipe recipe)
    {
        foreach (var input in recipe.Inputs)
        {
            if (!market.Inventory.ContainsKey(input.Key) || market.Inventory[input.Key] < input.Value)
                return false;
        }
        return true;
    }

    private static void ConsumeInputs(Market market, Recipe recipe)
    {
        foreach (var input in recipe.Inputs)
        {
            market.Inventory[input.Key] -= input.Value;
        }
    }

    private static void ProduceOutputs(Market market, Recipe recipe)
    {
        foreach (var output in recipe.Outputs)
        {
            if (!market.Inventory.ContainsKey(output.Key)) market.Inventory[output.Key] = 0;
            market.Inventory[output.Key] += output.Value;
        }
    }
}