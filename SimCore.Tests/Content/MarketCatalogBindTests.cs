using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SimCore;
using SimCore.Content;
using SimCore.Gen;

namespace SimCore.Tests.Content
{
    [TestFixture]
    public sealed class MarketCatalogBindTests
    {
        [Test]
        [Category("MarketCatalogBind")]
        public void MarketCatalogBind_FreshWorld_HasCatalogGoods()
        {
            // Load catalog from embedded default.
            var reg = ContentRegistryLoader.LoadFromJsonOrThrow(ContentRegistryLoader.DefaultRegistryJsonV0);
            var catalogIds = new HashSet<string>(reg.Goods.Select(g => g.Id), StringComparer.Ordinal);

            // Generate a fresh world passing the registry for guard validation.
            var sim = new SimKernel(42);
            GalaxyGenerator.Generate(sim.State, GalaxyGenerator.StarterRegionNodeCount, 100f,
                new GalaxyGenerator.GalaxyGenOptions { Registry = reg });

            // Each market must have >= 2 inventory keys, all present in catalog.
            int marketCount = 0;
            foreach (var mkt in sim.State.Markets.Values)
            {
                var keys = mkt.Inventory.Keys.ToList();
                Assert.That(keys.Count, Is.GreaterThanOrEqualTo(2),
                    $"Market({mkt.Id}) must have >=2 goods in inventory.");

                foreach (var key in keys)
                {
                    Assert.That(catalogIds.Contains(key), Is.True,
                        $"Market({mkt.Id}) inventory key '{key}' not found in catalog goods.");
                }
                marketCount++;
            }

            Assert.That(marketCount, Is.GreaterThanOrEqualTo(1),
                "At least 1 market must exist after generation.");
        }
    }
}
