using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;

namespace SimCore.Tests.Systems
{
    [TestFixture]
    public sealed class CatalogContractTests
    {
        private static readonly string[] REQUIRED_GOOD_FIELDS_ORDINAL =
        {
            "base_price_band",
            "display_name",
            "id",
            "stackable",
            "tier",
        };

        private static readonly string[] REQUIRED_RECIPE_FIELDS_ORDINAL =
        {
            "display_name",
            "id",
            "inputs",
            "outputs",
            "production_ticks",
        };

        private static readonly string[] REQUIRED_RECIPE_IO_FIELDS_ORDINAL =
        {
            "good_id",
            "qty",
        };

        [Test]
        [Category("CatalogContract")]
        public void ContentRegistryV0_GoodsSchema_AndOrdering_AreStable()
        {
            string repoRoot = FindRepoRootOrFail();
            string registryPath = Path.Combine(repoRoot, "docs", "content", "content_registry_v0.json");
            Assert.That(File.Exists(registryPath), Is.True, $"Missing registry file: {registryPath}");

            string json = File.ReadAllText(registryPath);
            using JsonDocument doc = JsonDocument.Parse(json);

            JsonElement root = doc.RootElement;
            Assert.That(root.ValueKind, Is.EqualTo(JsonValueKind.Object), "Root must be a JSON object.");

            Assert.That(root.TryGetProperty("goods", out JsonElement goods), Is.True, "Missing required top-level field: goods");
            Assert.That(goods.ValueKind, Is.EqualTo(JsonValueKind.Array), "goods must be an array");

            List<string> idsInOrder = new();
            HashSet<string> idsUnique = new(StringComparer.Ordinal);

            int goodsCount = goods.GetArrayLength();
            Assert.That(goodsCount, Is.GreaterThanOrEqualTo(2), "goods count must be >= 2");

            foreach (JsonElement good in goods.EnumerateArray())
            {
                Assert.That(good.ValueKind, Is.EqualTo(JsonValueKind.Object), "Each good must be an object.");

                // Field presence: deterministic fixed set, checked in Ordinal-sorted key order.
                foreach (string field in REQUIRED_GOOD_FIELDS_ORDINAL)
                {
                    Assert.That(good.TryGetProperty(field, out _), Is.True, $"Good missing required field: {field}");
                }

                string id = good.GetProperty("id").GetString() ?? string.Empty;
                string displayName = good.GetProperty("display_name").GetString() ?? string.Empty;
                string basePriceBand = good.GetProperty("base_price_band").GetString() ?? string.Empty;

                Assert.That(id.Length, Is.GreaterThan(0), "Good.id must be non-empty");
                Assert.That(displayName.Length, Is.GreaterThan(0), $"Good({id}).display_name must be non-empty");
                Assert.That(basePriceBand.Length, Is.GreaterThan(0), $"Good({id}).base_price_band must be non-empty");

                Assert.That(good.GetProperty("tier").ValueKind, Is.EqualTo(JsonValueKind.Number), $"Good({id}).tier must be a number");
                int tier = good.GetProperty("tier").GetInt32();
                Assert.That(tier, Is.GreaterThanOrEqualTo(0), $"Good({id}).tier must be >= 0");

                Assert.That(good.GetProperty("stackable").ValueKind, Is.EqualTo(JsonValueKind.True).Or.EqualTo(JsonValueKind.False),
                    $"Good({id}).stackable must be a boolean");
                _ = good.GetProperty("stackable").GetBoolean();

                idsInOrder.Add(id);
                Assert.That(idsUnique.Add(id), Is.True, $"Duplicate good id detected (Ordinal): {id}");
            }

            // Stable ordering: id Ordinal asc.
            List<string> sorted = idsInOrder.OrderBy(x => x, StringComparer.Ordinal).ToList();
            Assert.That(idsInOrder, Is.EqualTo(sorted),
                $"goods must be ordered by id Ordinal asc. Actual=[{string.Join(",", idsInOrder)}] Expected=[{string.Join(",", sorted)}]");
        }

        [Test]
        [Category("CatalogContract")]
        public void ContentRegistryV0_RecipesSchema_CrossRefs_AndOrdering_AreStable()
        {
            string repoRoot = FindRepoRootOrFail();
            string registryPath = Path.Combine(repoRoot, "docs", "content", "content_registry_v0.json");
            Assert.That(File.Exists(registryPath), Is.True, $"Missing registry file: {registryPath}");

            string json = File.ReadAllText(registryPath);
            using JsonDocument doc = JsonDocument.Parse(json);

            JsonElement root = doc.RootElement;
            Assert.That(root.ValueKind, Is.EqualTo(JsonValueKind.Object), "Root must be a JSON object.");

            Assert.That(root.TryGetProperty("goods", out JsonElement goods), Is.True, "Missing required top-level field: goods");
            Assert.That(goods.ValueKind, Is.EqualTo(JsonValueKind.Array), "goods must be an array");

            HashSet<string> goodsIds = new(StringComparer.Ordinal);
            foreach (JsonElement good in goods.EnumerateArray())
            {
                if (good.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                if (!good.TryGetProperty("id", out JsonElement idEl))
                {
                    continue;
                }

                string id = idEl.GetString() ?? string.Empty;
                if (id.Length > 0)
                {
                    goodsIds.Add(id);
                }
            }

            Assert.That(root.TryGetProperty("recipes", out JsonElement recipes), Is.True, "Missing required top-level field: recipes");
            Assert.That(recipes.ValueKind, Is.EqualTo(JsonValueKind.Array), "recipes must be an array");

            int recipeCount = recipes.GetArrayLength();
            Assert.That(recipeCount, Is.GreaterThanOrEqualTo(1), "recipes count must be >= 1");

            List<string> idsInOrder = new();
            HashSet<string> idsUnique = new(StringComparer.Ordinal);

            foreach (JsonElement recipe in recipes.EnumerateArray())
            {
                Assert.That(recipe.ValueKind, Is.EqualTo(JsonValueKind.Object), "Each recipe must be an object.");

                // Field presence: deterministic fixed set, checked in Ordinal-sorted key order.
                foreach (string field in REQUIRED_RECIPE_FIELDS_ORDINAL)
                {
                    Assert.That(recipe.TryGetProperty(field, out _), Is.True, $"Recipe missing required field: {field}");
                }

                string id = recipe.GetProperty("id").GetString() ?? string.Empty;
                string displayName = recipe.GetProperty("display_name").GetString() ?? string.Empty;

                Assert.That(id.Length, Is.GreaterThan(0), "Recipe.id must be non-empty");
                Assert.That(displayName.Length, Is.GreaterThan(0), $"Recipe({id}).display_name must be non-empty");

                Assert.That(recipe.GetProperty("production_ticks").ValueKind, Is.EqualTo(JsonValueKind.Number), $"Recipe({id}).production_ticks must be a number");
                int productionTicks = recipe.GetProperty("production_ticks").GetInt32();
                Assert.That(productionTicks, Is.GreaterThanOrEqualTo(0), $"Recipe({id}).production_ticks must be >= 0");

                void ValidateIoList(string listName)
                {
                    JsonElement list = recipe.GetProperty(listName);
                    Assert.That(list.ValueKind, Is.EqualTo(JsonValueKind.Array), $"Recipe({id}).{listName} must be an array");

                    List<string> goodIdsInOrder = new();

                    foreach (JsonElement io in list.EnumerateArray())
                    {
                        Assert.That(io.ValueKind, Is.EqualTo(JsonValueKind.Object), $"Recipe({id}).{listName} entries must be objects");

                        foreach (string field in REQUIRED_RECIPE_IO_FIELDS_ORDINAL)
                        {
                            Assert.That(io.TryGetProperty(field, out _), Is.True, $"Recipe({id}).{listName} entry missing required field: {field}");
                        }

                        string goodId = io.GetProperty("good_id").GetString() ?? string.Empty;
                        Assert.That(goodId.Length, Is.GreaterThan(0), $"Recipe({id}).{listName}.good_id must be non-empty");
                        Assert.That(goodsIds.Contains(goodId), Is.True, $"Recipe({id}).{listName}.good_id must reference an existing goods.id (Ordinal): {goodId}");

                        Assert.That(io.GetProperty("qty").ValueKind, Is.EqualTo(JsonValueKind.Number), $"Recipe({id}).{listName}.qty must be a number");
                        int qty = io.GetProperty("qty").GetInt32();
                        Assert.That(qty, Is.GreaterThan(0), $"Recipe({id}).{listName}.qty must be > 0");

                        goodIdsInOrder.Add(goodId);
                    }

                    // Deterministic ordering within IO lists: good_id Ordinal asc.
                    List<string> sortedIo = goodIdsInOrder.OrderBy(x => x, StringComparer.Ordinal).ToList();
                    Assert.That(goodIdsInOrder, Is.EqualTo(sortedIo),
                        $"Recipe({id}).{listName} must be ordered by good_id Ordinal asc. Actual=[{string.Join(",", goodIdsInOrder)}] Expected=[{string.Join(",", sortedIo)}]");
                }

                ValidateIoList("inputs");
                ValidateIoList("outputs");

                idsInOrder.Add(id);
                Assert.That(idsUnique.Add(id), Is.True, $"Duplicate recipe id detected (Ordinal): {id}");
            }

            // Stable ordering: id Ordinal asc.
            List<string> sorted = idsInOrder.OrderBy(x => x, StringComparer.Ordinal).ToList();
            Assert.That(idsInOrder, Is.EqualTo(sorted),
                $"recipes must be ordered by id Ordinal asc. Actual=[{string.Join(",", idsInOrder)}] Expected=[{string.Join(",", sorted)}]");
        }

        private static string FindRepoRootOrFail()
        {
            // Deterministic search upward from test directory until we find the expected file.
            string dir = TestContext.CurrentContext.TestDirectory;
            DirectoryInfo? cur = new DirectoryInfo(dir);

            while (cur != null)
            {
                string candidate = Path.Combine(cur.FullName, "docs", "content", "content_registry_v0.json");
                if (File.Exists(candidate))
                {
                    return cur.FullName;
                }

                cur = cur.Parent;
            }

            Assert.Fail("Could not locate repo root containing docs/content/content_registry_v0.json");
            return string.Empty;
        }
    }
}
