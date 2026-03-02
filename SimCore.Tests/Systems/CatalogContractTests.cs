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
