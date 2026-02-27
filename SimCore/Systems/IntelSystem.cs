using SimCore.Entities;

namespace SimCore.Systems;

public static class IntelSystem
{
	// Slice 1 rule: local market id is the same as PlayerLocationNodeId
	private static string GetLocalMarketId(SimState state) => state.PlayerLocationNodeId ?? "";

	public static void Process(SimState state)
	{
		if (state is null) throw new ArgumentNullException(nameof(state));

		// Ensure intel book exists on state (see Edit 4 below)
		if (state.Intel is null) state.Intel = new IntelBook();

		var localMarketId = GetLocalMarketId(state);
		if (string.IsNullOrWhiteSpace(localMarketId)) return;
		if (!state.Markets.TryGetValue(localMarketId, out var localMarket)) return;

		// Refresh intel ONLY for the local market
		// Deterministic ordering: iterate goods in ordinal order
		var goodIds = localMarket.Inventory.Keys.OrderBy(x => x, StringComparer.Ordinal).ToList();
		foreach (var goodId in goodIds)
		{
			var qty = localMarket.Inventory.TryGetValue(goodId, out var v) ? v : 0;
			var key = IntelBook.Key(localMarketId, goodId);

			if (!state.Intel.Observations.TryGetValue(key, out var obs))
			{
				obs = new IntelObservation();
				state.Intel.Observations[key] = obs;
			}

			obs.ObservedTick = state.Tick;
			obs.ObservedInventoryQty = qty;
		}
	}

	public static MarketGoodView GetMarketGoodView(SimState state, string targetMarketId, string goodId)
	{
		if (state is null) throw new ArgumentNullException(nameof(state));
		if (string.IsNullOrWhiteSpace(targetMarketId)) throw new ArgumentException("targetMarketId required", nameof(targetMarketId));
		if (string.IsNullOrWhiteSpace(goodId)) throw new ArgumentException("goodId required", nameof(goodId));

		var localMarketId = GetLocalMarketId(state);

		// Local truth
		if (string.Equals(localMarketId, targetMarketId, StringComparison.Ordinal))
		{
			if (!state.Markets.TryGetValue(targetMarketId, out var m))
			{
				return new MarketGoodView { Kind = MarketGoodViewKind.LocalTruth, ExactInventoryQty = 0, AgeTicks = 0, InventoryBand = InventoryBand.Unknown };
			}

			var qty = m.Inventory.TryGetValue(goodId, out var v) ? v : 0;

			return new MarketGoodView
			{
				Kind = MarketGoodViewKind.LocalTruth,
				ExactInventoryQty = qty,
				InventoryBand = InventoryBand.Unknown,
				AgeTicks = 0
			};
		}

		// Remote intel
		if (state.Intel is null) state.Intel = new IntelBook();

		var key = IntelBook.Key(targetMarketId, goodId);
		if (!state.Intel.Observations.TryGetValue(key, out var obs))
		{
			return new MarketGoodView
			{
				Kind = MarketGoodViewKind.RemoteIntel,
				ExactInventoryQty = 0,
				InventoryBand = InventoryBand.Unknown,
				AgeTicks = -1
			};
		}

		var age = state.Tick - obs.ObservedTick;
		if (age < 0) age = 0;

		return new MarketGoodView
		{
			Kind = MarketGoodViewKind.RemoteIntel,
			ExactInventoryQty = 0,
			InventoryBand = BandInventory(obs.ObservedInventoryQty),
			AgeTicks = age
		};
	}

	public static InventoryBand BandInventory(int qty)
	{
		// Deterministic, fixed thresholds. Adjust if you prefer, but then lock tests accordingly.
		if (qty <= 0) return InventoryBand.VeryLow;
		if (qty <= 10) return InventoryBand.Low;
		if (qty <= 50) return InventoryBand.Medium;
		if (qty <= 200) return InventoryBand.High;
		return InventoryBand.VeryHigh;
	}

	// GATE.S3_6.DISCOVERY_STATE.001
	// Stable listing of discoveries: DiscoveryId asc (ordinal).
	public static IReadOnlyList<DiscoveryStateV0> GetDiscoveriesAscending(SimState state)
	{
		if (state is null) throw new ArgumentNullException(nameof(state));
		if (state.Intel?.Discoveries is null)
			return Array.Empty<DiscoveryStateV0>();

		return state.Intel.Discoveries.Values
			.OrderBy(d => d.DiscoveryId, StringComparer.Ordinal)
			.ToList();
	}

	// GATE.S3_6.DISCOVERY_STATE.001
	// Returns the reason code for a scan attempt on the given discovery.
	public static DiscoveryReasonCode GetScanReasonCode(SimState state, string discoveryId)
	{
		if (state is null) throw new ArgumentNullException(nameof(state));
		if (string.IsNullOrEmpty(discoveryId)) return DiscoveryReasonCode.NotSeen;
		if (state.Intel?.Discoveries is null || !state.Intel.Discoveries.TryGetValue(discoveryId, out var d))
			return DiscoveryReasonCode.NotSeen;
		if (d.Phase == DiscoveryPhase.Analyzed)
			return DiscoveryReasonCode.AlreadyAnalyzed;
		return DiscoveryReasonCode.Ok;
	}

	// GATE.S3_6.DISCOVERY_STATE.001
	// Returns the reason code for an analyze attempt on the given discovery.
	public static DiscoveryReasonCode GetAnalyzeReasonCode(SimState state, string discoveryId)
	{
		if (state is null) throw new ArgumentNullException(nameof(state));
		if (string.IsNullOrEmpty(discoveryId)) return DiscoveryReasonCode.NotSeen;
		if (state.Intel?.Discoveries is null || !state.Intel.Discoveries.TryGetValue(discoveryId, out var d))
			return DiscoveryReasonCode.NotSeen;
		if (d.Phase == DiscoveryPhase.Analyzed)
			return DiscoveryReasonCode.AlreadyAnalyzed;
		return DiscoveryReasonCode.Ok;
	}
}
