using System;
using System.Collections.Generic;

namespace SimCore.Systems;

/// <summary>
/// Single mutation surface for inventories (markets and player cargo).
/// Rules:
/// - GoodId must be non-empty
/// - Quantity must be > 0
/// - No negative counts allowed
/// - For PlayerCargo: keys are removed when count becomes 0
/// - For Market inventories: keys are preserved even when count becomes 0 (backwards compatible with tests and callers)
/// </summary>
public static class InventoryLedger
{
	public static int Get(Dictionary<string, int> inv, string goodId)
	{
		if (inv is null) throw new ArgumentNullException(nameof(inv));
		if (string.IsNullOrWhiteSpace(goodId)) throw new ArgumentException("goodId must be non-empty.", nameof(goodId));
		return inv.TryGetValue(goodId, out var v) ? v : 0;
	}

	// Market-specific helpers (preserve zero keys)
	public static void AddMarket(Dictionary<string, int> inv, string goodId, int quantity) =>
		Add(inv, goodId, quantity, preserveZeroKey: true);

	public static bool TryRemoveMarket(Dictionary<string, int> inv, string goodId, int quantity) =>
		TryRemove(inv, goodId, quantity, preserveZeroKey: true);

	public static bool TryTransferMarket(Dictionary<string, int> from, Dictionary<string, int> to, string goodId, int quantity) =>
		TryTransfer(from, to, goodId, quantity, preserveZeroKey: true);

	// Cargo-specific helpers (remove zero keys)
	public static void AddCargo(Dictionary<string, int> inv, string goodId, int quantity) =>
		Add(inv, goodId, quantity, preserveZeroKey: false);

	public static bool TryRemoveCargo(Dictionary<string, int> inv, string goodId, int quantity) =>
		TryRemove(inv, goodId, quantity, preserveZeroKey: false);

	public static bool TryTransferCargo(Dictionary<string, int> from, Dictionary<string, int> to, string goodId, int quantity) =>
		TryTransfer(from, to, goodId, quantity, preserveZeroKey: false);

	// Core implementation
	public static void Add(Dictionary<string, int> inv, string goodId, int quantity, bool preserveZeroKey)
	{
		if (inv is null) throw new ArgumentNullException(nameof(inv));
		ValidateInputs(goodId, quantity);

		var current = inv.TryGetValue(goodId, out var v) ? v : 0;
		var next = checked(current + quantity);

		if (next < 0) throw new InvalidOperationException("Inventory cannot become negative via Add.");

		if (next == 0 && !preserveZeroKey)
		{
			inv.Remove(goodId);
			return;
		}

		inv[goodId] = next;
	}

	public static bool TryRemove(Dictionary<string, int> inv, string goodId, int quantity, bool preserveZeroKey)
	{
		if (inv is null) throw new ArgumentNullException(nameof(inv));
		ValidateInputs(goodId, quantity);

		var current = inv.TryGetValue(goodId, out var v) ? v : 0;
		if (current < quantity) return false;

		var next = current - quantity;

		if (next == 0 && !preserveZeroKey)
		{
			inv.Remove(goodId);
			return true;
		}

		inv[goodId] = next;
		return true;
	}

	public static bool TryTransfer(Dictionary<string, int> from, Dictionary<string, int> to, string goodId, int quantity, bool preserveZeroKey)
	{
		if (from is null) throw new ArgumentNullException(nameof(from));
		if (to is null) throw new ArgumentNullException(nameof(to));
		ValidateInputs(goodId, quantity);

		if (!TryRemove(from, goodId, quantity, preserveZeroKey)) return false;
		Add(to, goodId, quantity, preserveZeroKey);
		return true;
	}

	private static void ValidateInputs(string goodId, int quantity)
	{
		if (string.IsNullOrWhiteSpace(goodId)) throw new ArgumentException("goodId must be non-empty.", nameof(goodId));
		if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "quantity must be > 0.");
	}
}
