using System;
using System.Collections.Generic;
using System.Linq;
using SimCore.Entities;

namespace SimCore.Systems;

/// <summary>
/// Slice 1: lane flow with deterministic delay arrivals.
/// Delay is computed as Ceil(edge.Distance) ticks, clamped to >= 1.
/// </summary>
public static class LaneFlowSystem
{
	public static bool TryEnqueueTransfer(
		SimState state,
		string fromNodeId,
		string toNodeId,
		string goodId,
		int quantity,
		string transferId)
	{
		if (state is null) throw new ArgumentNullException(nameof(state));
		if (string.IsNullOrWhiteSpace(fromNodeId)) return false;
		if (string.IsNullOrWhiteSpace(toNodeId)) return false;
		if (fromNodeId == toNodeId) return false;
		if (string.IsNullOrWhiteSpace(goodId)) return false;
		if (quantity <= 0) return false;
		if (string.IsNullOrWhiteSpace(transferId)) return false;

		if (!MapQueries.TryGetEdgeId(state, fromNodeId, toNodeId, out var edgeId)) return false;
		if (!state.Edges.TryGetValue(edgeId, out var edge)) return false;

		if (!state.Nodes.TryGetValue(fromNodeId, out var fromNode)) return false;
		if (!state.Nodes.TryGetValue(toNodeId, out var toNode)) return false;

		var fromMarketId = fromNode.MarketId ?? "";
		var toMarketId = toNode.MarketId ?? "";
		if (string.IsNullOrWhiteSpace(fromMarketId)) return false;
		if (string.IsNullOrWhiteSpace(toMarketId)) return false;

		if (!state.Markets.TryGetValue(fromMarketId, out var fromMarket)) return false;
		if (!state.Markets.TryGetValue(toMarketId, out var toMarket)) return false;

		if (state.InFlightTransfers.Any(x => string.Equals(x.Id, transferId, StringComparison.Ordinal)))
		{
			return false;
		}

		var removed = InventoryLedger.TryRemoveMarket(fromMarket.Inventory, goodId, quantity);
		if (!removed) return false;

		var delayTicks = ComputeDelayTicks(edge);
		var departTick = state.Tick;
		var arriveTick = checked(departTick + delayTicks);

		state.InFlightTransfers.Add(new InFlightTransfer
		{
			Id = transferId,
			EdgeId = edgeId,
			FromNodeId = fromNodeId,
			ToNodeId = toNodeId,
			FromMarketId = fromMarketId,
			ToMarketId = toMarketId,
			GoodId = goodId,
			Quantity = quantity,
			DepartTick = departTick,
			ArriveTick = arriveTick
		});

		return true;
	}

	public static void Process(SimState state)
	{
		if (state is null) throw new ArgumentNullException(nameof(state));
		if (state.InFlightTransfers.Count == 0) return;

		var now = state.Tick;

		var due = state.InFlightTransfers
			.Where(x => x.ArriveTick <= now)
			.OrderBy(x => x.ArriveTick)
			.ThenBy(x => x.EdgeId, StringComparer.Ordinal)
			.ThenBy(x => x.Id, StringComparer.Ordinal)
			.ToList();

		if (due.Count == 0) return;

		foreach (var t in due)
		{
			if (t.Quantity <= 0) continue;
			if (string.IsNullOrWhiteSpace(t.ToMarketId)) continue;
			if (!state.Markets.TryGetValue(t.ToMarketId, out var toMarket)) continue;

			InventoryLedger.AddMarket(toMarket.Inventory, t.GoodId, t.Quantity);
		}

		var dueIds = new HashSet<string>(due.Select(x => x.Id), StringComparer.Ordinal);
		state.InFlightTransfers.RemoveAll(x => dueIds.Contains(x.Id));
	}

	private static int ComputeDelayTicks(Edge edge)
	{
		var d = edge.Distance;
		if (float.IsNaN(d) || float.IsInfinity(d)) return 1;
		if (d <= 0f) return 1;

		var ticks = (int)MathF.Ceiling(d);
		return Math.Max(1, ticks);
	}
}
