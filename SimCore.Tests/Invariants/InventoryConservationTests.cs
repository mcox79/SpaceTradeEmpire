using NUnit.Framework;
using SimCore.Systems;

namespace SimCore.Tests.Invariants
{
	[TestFixture]
	public class InventoryConservationTests
	{
		[Test]
		public void Ledger_Transfer_Conserves_Total_Units_For_A_Good_MarketSemantics()
		{
			var a = new System.Collections.Generic.Dictionary<string, int> { ["ore"] = 10 };
			var b = new System.Collections.Generic.Dictionary<string, int> { ["ore"] = 3 };

			var before = InventoryLedger.Get(a, "ore") + InventoryLedger.Get(b, "ore");

			var ok = InventoryLedger.TryTransferMarket(a, b, "ore", 4);
			Assert.That(ok, Is.True);

			var after = InventoryLedger.Get(a, "ore") + InventoryLedger.Get(b, "ore");

			Assert.That(after, Is.EqualTo(before));
			Assert.That(InventoryLedger.Get(a, "ore"), Is.EqualTo(6));
			Assert.That(InventoryLedger.Get(b, "ore"), Is.EqualTo(7));
		}

		[Test]
		public void Ledger_Never_Allows_Negative_Inventory_MarketSemantics()
		{
			var a = new System.Collections.Generic.Dictionary<string, int> { ["food"] = 1 };

			var ok = InventoryLedger.TryRemoveMarket(a, "food", 2);
			Assert.That(ok, Is.False);

			Assert.That(InventoryLedger.Get(a, "food"), Is.EqualTo(1));
		}
	}
}
