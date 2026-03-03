using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimCore.Content;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.S4.INDU_STRUCT.CHAIN_GRAPH.001
// Computes production chains from the content registry recipe graph.
// A chain is a sequence of recipes that transforms raw inputs into a final output.
// Deterministic: Ordinal sort, no timestamps.
public static class ChainAnalysis
{
    public sealed class ChainReport
    {
        public string ChainId { get; set; } = "";
        public int Depth { get; set; }
        public List<string> RecipeSequence { get; set; } = new();
        public List<string> RawInputs { get; set; } = new();
        public string FinalOutput { get; set; } = "";
    }

    public sealed class ChainValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Violations { get; set; } = new();
        public List<ChainReport> Chains { get; set; } = new();
    }

    // Build chain graph from recipes. Each recipe's outputs become the next recipe's inputs.
    // A "raw input" is a good that no recipe produces. A "final output" is a good that is
    // produced but not consumed by any other recipe in the registry.
    public static ChainValidationResult Analyze(ContentRegistryLoader.ContentRegistryV0 registry)
    {
        if (registry is null) throw new ArgumentNullException(nameof(registry));

        var result = new ChainValidationResult();

        // Map: good_id → recipes that produce it
        var producedBy = new Dictionary<string, List<ContentRegistryLoader.RecipeDefV0>>(StringComparer.Ordinal);
        // Map: good_id → recipes that consume it
        var consumedBy = new Dictionary<string, List<ContentRegistryLoader.RecipeDefV0>>(StringComparer.Ordinal);
        // Set of all produced good IDs
        var allProduced = new HashSet<string>(StringComparer.Ordinal);
        // Set of all consumed good IDs
        var allConsumed = new HashSet<string>(StringComparer.Ordinal);

        foreach (var recipe in registry.Recipes)
        {
            foreach (var output in recipe.Outputs)
            {
                allProduced.Add(output.GoodId);
                if (!producedBy.TryGetValue(output.GoodId, out var pList))
                {
                    pList = new List<ContentRegistryLoader.RecipeDefV0>();
                    producedBy[output.GoodId] = pList;
                }
                pList.Add(recipe);
            }

            foreach (var input in recipe.Inputs)
            {
                allConsumed.Add(input.GoodId);
                if (!consumedBy.TryGetValue(input.GoodId, out var cList))
                {
                    cList = new List<ContentRegistryLoader.RecipeDefV0>();
                    consumedBy[input.GoodId] = cList;
                }
                cList.Add(recipe);
            }
        }

        // Final outputs: produced but not consumed by any recipe
        var finalOutputs = new List<string>();
        foreach (var good in allProduced)
        {
            if (!allConsumed.Contains(good))
                finalOutputs.Add(good);
        }
        finalOutputs.Sort(StringComparer.Ordinal);

        var zero = IndustryTweaksV0.Zero;

        // For each final output, trace backwards through the recipe graph to build chains
        foreach (var finalGood in finalOutputs)
        {
            if (!producedBy.TryGetValue(finalGood, out var producers)) continue;

            foreach (var endRecipe in producers)
            {
                var chain = new ChainReport { FinalOutput = finalGood };
                var rawInputs = new HashSet<string>(StringComparer.Ordinal);
                var recipeSeq = new List<string>();

                // BFS backwards from the end recipe
                TraceChain(endRecipe, producedBy, recipeSeq, rawInputs, allProduced, depth: zero, maxDepth: CatalogTweaksV0.ChainMaxTraceDepth);

                // Reverse so chain reads left-to-right (raw → final)
                recipeSeq.Reverse();

                chain.RecipeSequence = recipeSeq;
                chain.Depth = recipeSeq.Count;
                chain.RawInputs = rawInputs.OrderBy(x => x, StringComparer.Ordinal).ToList();
                chain.ChainId = $"chain_{finalGood}_{endRecipe.Id}";

                result.Chains.Add(chain);
            }
        }

        // Sort chains deterministically
        result.Chains.Sort((a, b) => string.CompareOrdinal(a.ChainId, b.ChainId));

        // Validate constraints
        foreach (var chain in result.Chains)
        {
            if (chain.Depth > CatalogTweaksV0.ChainMaxDepth)
            {
                result.IsValid = false;
                result.Violations.Add($"DEPTH_EXCEEDED: {chain.ChainId} depth={chain.Depth} (max {CatalogTweaksV0.ChainMaxDepth})");
            }

            // Count byproducts: outputs of any recipe in the chain that are not the final output
            // and not consumed by the next recipe in the chain
            var byproductCount = CountByproducts(chain, registry);
            if (byproductCount > CatalogTweaksV0.ChainMaxByproducts)
            {
                result.IsValid = false;
                result.Violations.Add($"BYPRODUCT_EXCEEDED: {chain.ChainId} byproducts={byproductCount} (max {CatalogTweaksV0.ChainMaxByproducts})");
            }
        }

        result.Violations.Sort(StringComparer.Ordinal);
        return result;
    }

    private static void TraceChain(
        ContentRegistryLoader.RecipeDefV0 recipe,
        Dictionary<string, List<ContentRegistryLoader.RecipeDefV0>> producedBy,
        List<string> recipeSeq,
        HashSet<string> rawInputs,
        HashSet<string> allProduced,
        int depth,
        int maxDepth)
    {
        if (depth > maxDepth) return; // safety: prevent infinite recursion

        recipeSeq.Add(recipe.Id);

        // Sort inputs by GoodId for determinism
        var sortedInputs = recipe.Inputs.OrderBy(x => x.GoodId, StringComparer.Ordinal).ToList();
        var one = IndustryTweaksV0.One;

        foreach (var input in sortedInputs)
        {
            if (!allProduced.Contains(input.GoodId))
            {
                // This is a raw input — no recipe produces it
                rawInputs.Add(input.GoodId);
            }
            else if (producedBy.TryGetValue(input.GoodId, out var producers))
            {
                // Trace the first producer (deterministic: sorted by recipe Id)
                var sorted = producers.OrderBy(r => r.Id, StringComparer.Ordinal).ToList();
                // Only trace the first (deterministic) producer to avoid exponential fan-out
                if (sorted.Count > IndustryTweaksV0.Zero && !recipeSeq.Contains(sorted[IndustryTweaksV0.Zero].Id))
                {
                    TraceChain(sorted[IndustryTweaksV0.Zero], producedBy, recipeSeq, rawInputs, allProduced, depth + one, maxDepth);
                }
            }
        }
    }

    private static int CountByproducts(ChainReport chain, ContentRegistryLoader.ContentRegistryV0 registry)
    {
        // Collect all intermediate goods consumed within the chain
        var recipeMap = new Dictionary<string, ContentRegistryLoader.RecipeDefV0>(StringComparer.Ordinal);
        foreach (var r in registry.Recipes)
            recipeMap[r.Id] = r;

        var chainRecipeIds = new HashSet<string>(chain.RecipeSequence, StringComparer.Ordinal);
        var consumedInChain = new HashSet<string>(StringComparer.Ordinal);
        var producedInChain = new HashSet<string>(StringComparer.Ordinal);

        foreach (var rid in chain.RecipeSequence)
        {
            if (!recipeMap.TryGetValue(rid, out var r)) continue;
            foreach (var inp in r.Inputs) consumedInChain.Add(inp.GoodId);
            foreach (var outp in r.Outputs) producedInChain.Add(outp.GoodId);
        }

        // Byproducts: produced in chain but not consumed in chain AND not the final output
        int count = IndustryTweaksV0.Zero;
        foreach (var good in producedInChain)
        {
            if (string.Equals(good, chain.FinalOutput, StringComparison.Ordinal)) continue;
            if (!consumedInChain.Contains(good)) count++;
        }

        return count;
    }

    // Deterministic report text (no timestamps, LF only, Ordinal sort)
    public static string BuildReportText(ChainValidationResult result)
    {
        var sb = new StringBuilder(CatalogTweaksV0.ChainReportBufferSize);
        sb.Append("CHAIN_ANALYSIS_REPORT_V0\n");
        sb.Append("chain_count=").Append(result.Chains.Count).Append('\n');
        sb.Append("is_valid=").Append(result.IsValid ? "true" : "false").Append('\n');
        sb.Append("violation_count=").Append(result.Violations.Count).Append('\n');

        foreach (var v in result.Violations)
            sb.Append("violation=").Append(v).Append('\n');

        foreach (var chain in result.Chains)
        {
            sb.Append("chain=").Append(chain.ChainId);
            sb.Append("|depth=").Append(chain.Depth);
            sb.Append("|recipes=").Append(string.Join(",", chain.RecipeSequence));
            sb.Append("|raw_inputs=").Append(string.Join(",", chain.RawInputs));
            sb.Append("|final_output=").Append(chain.FinalOutput);
            sb.Append('\n');
        }

        return sb.ToString();
    }
}
