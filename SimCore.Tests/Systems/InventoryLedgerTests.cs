using NUnit.Framework;
using SimCore.Systems;

namespace SimCore.Tests.Systems;

public sealed class InventoryLedgerTests
{
	[Test]
	public void Remove_More_Than_Available_ReturnsFalse_And_DoesNotChange_Market()
	{
		var inv = new System.Collections.Generic.Dictionary<string, int> { ["ore"] = 5 };

		var ok = InventoryLedger.TryRemoveMarket(inv, "ore", 6);

		Assert.That(ok, Is.False);
		Assert.That(InventoryLedger.Get(inv, "ore"), Is.EqualTo(5));
	}

	[Test]
	public void Add_Then_Remove_Returns_To_Original_Cargo_Removes_Zero_Key()
	{
		var inv = new System.Collections.Generic.Dictionary<string, int> { ["food"] = 2 };

		InventoryLedger.AddCargo(inv, "food", 3);
		var ok = InventoryLedger.TryRemoveCargo(inv, "food", 3);

		Assert.That(ok, Is.True);
		Assert.That(InventoryLedger.Get(inv, "food"), Is.EqualTo(2));
		Assert.That(inv.ContainsKey("food"), Is.True);

		var ok2 = InventoryLedger.TryRemoveCargo(inv, "food", 2);
		Assert.That(ok2, Is.True);
		Assert.That(InventoryLedger.Get(inv, "food"), Is.EqualTo(0));
		Assert.That(inv.ContainsKey("food"), Is.False);
	}

	[Test]
	public void Remove_To_Zero_Market_Preserves_Zero_Key()
	{
		var inv = new System.Collections.Generic.Dictionary<string, int> { ["fuel"] = 2 };

		var ok = InventoryLedger.TryRemoveMarket(inv, "fuel", 2);

		Assert.That(ok, Is.True);
		Assert.That(InventoryLedger.Get(inv, "fuel"), Is.EqualTo(0));
		Assert.That(inv.ContainsKey("fuel"), Is.True);
		Assert.That(inv["fuel"], Is.EqualTo(0));
	}

	[Test]
	public void Transfer_Conserves_Total_Market()
	{
		var a = new System.Collections.Generic.Dictionary<string, int> { ["fuel"] = 10 };
		var b = new System.Collections.Generic.Dictionary<string, int> { ["fuel"] = 1 };

		var before = InventoryLedger.Get(a, "fuel") + InventoryLedger.Get(b, "fuel");

		var ok = InventoryLedger.TryTransferMarket(a, b, "fuel", 7);

		Assert.That(ok, Is.True);

		var after = InventoryLedger.Get(a, "fuel") + InventoryLedger.Get(b, "fuel");
		Assert.That(after, Is.EqualTo(before));
		Assert.That(InventoryLedger.Get(a, "fuel"), Is.EqualTo(3));
		Assert.That(InventoryLedger.Get(b, "fuel"), Is.EqualTo(8));
	}
}
