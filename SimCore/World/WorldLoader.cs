using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SimCore.Entities;
using SimCore.Schemas;

namespace SimCore.World;

public static class WorldLoader
{
	/// <summary>
	/// Clears SimState world collections and applies a WorldDefinition deterministically.
	/// Determinism rule: insertion order is stable (sorted by Id) and validation is strict.
	/// </summary>
	public static void Apply(SimState state, WorldDefinition def)
	{
		if (state is null) throw new ArgumentNullException(nameof(state));
		if (def is null) throw new ArgumentNullException(nameof(def));

		state.Markets.Clear();
		state.Nodes.Clear();
		state.Edges.Clear();
		state.Fleets.Clear();
		state.IndustrySites.Clear();

		// MARKETS
		foreach (var m in (def.Markets ?? new List<WorldMarket>()).OrderBy(x => x.Id, StringComparer.Ordinal))
		{
			RequireNonEmptyId(m.Id, "Market.Id");
			if (state.Markets.ContainsKey(m.Id)) throw new InvalidOperationException($"Duplicate market id: {m.Id}");

			var market = new Market { Id = m.Id };

			foreach (var kv in (m.Inventory ?? new Dictionary<string, int>()).OrderBy(k => k.Key, StringComparer.Ordinal))
			{
				if (string.IsNullOrWhiteSpace(kv.Key)) throw new InvalidOperationException($"Market {m.Id} has empty good id key.");
				market.Inventory[kv.Key] = kv.Value;
			}

			state.Markets.Add(m.Id, market);
		}

		// NODES
		foreach (var n in (def.Nodes ?? new List<WorldNode>()).OrderBy(x => x.Id, StringComparer.Ordinal))
		{
			RequireNonEmptyId(n.Id, "Node.Id");
			if (state.Nodes.ContainsKey(n.Id)) throw new InvalidOperationException($"Duplicate node id: {n.Id}");

			var kind = ParseNodeKind(n.Kind);
			var pos = ParseVec3(n.Pos, n.Id);

			if (!string.IsNullOrWhiteSpace(n.MarketId) && !state.Markets.ContainsKey(n.MarketId))
				throw new InvalidOperationException($"Node {n.Id} references missing market id: {n.MarketId}");

			var node = new Node
			{
				Id = n.Id,
				Kind = kind,
				Name = n.Name ?? "",
				Position = pos,
				MarketId = n.MarketId ?? ""
			};

			state.Nodes.Add(n.Id, node);
		}

		// EDGES
		foreach (var e in (def.Edges ?? new List<WorldEdge>()).OrderBy(x => x.Id, StringComparer.Ordinal))
		{
			RequireNonEmptyId(e.Id, "Edge.Id");
			RequireNonEmptyId(e.FromNodeId, "Edge.FromNodeId");
			RequireNonEmptyId(e.ToNodeId, "Edge.ToNodeId");

			if (state.Edges.ContainsKey(e.Id)) throw new InvalidOperationException($"Duplicate edge id: {e.Id}");
			if (!state.Nodes.ContainsKey(e.FromNodeId)) throw new InvalidOperationException($"Edge {e.Id} FromNodeId missing: {e.FromNodeId}");
			if (!state.Nodes.ContainsKey(e.ToNodeId)) throw new InvalidOperationException($"Edge {e.Id} ToNodeId missing: {e.ToNodeId}");

			var edge = new Edge
			{
				Id = e.Id,
				FromNodeId = e.FromNodeId,
				ToNodeId = e.ToNodeId,
				Distance = e.Distance,
				TotalCapacity = e.TotalCapacity,
				UsedCapacity = 0,
				Heat = 0f
			};

			state.Edges.Add(e.Id, edge);
		}

		// PLAYER START (optional)
		if (def.Player is not null)
		{
			state.PlayerCredits = def.Player.Credits;

			state.PlayerCargo.Clear();
			foreach (var kv in (def.Player.Cargo ?? new Dictionary<string, int>()).OrderBy(k => k.Key, StringComparer.Ordinal))
			{
				if (string.IsNullOrWhiteSpace(kv.Key)) throw new InvalidOperationException("Player cargo contains empty good id key.");
				state.PlayerCargo[kv.Key] = kv.Value;
			}

			if (!string.IsNullOrWhiteSpace(def.Player.LocationNodeId))
			{
				if (!state.Nodes.ContainsKey(def.Player.LocationNodeId))
					throw new InvalidOperationException($"Player.LocationNodeId missing: {def.Player.LocationNodeId}");
				state.PlayerLocationNodeId = def.Player.LocationNodeId;
			}

                        state.PlayerSelectedDestinationNodeId = "";

                        // SLICE 2 / GATE.FLEET.001:
                        // Deterministic single trader fleet bound to the player start.
                        // WorldLoader clears fleets above, so we must recreate this here.
                        const string playerFleetId = "fleet_trader_1";
                        if (!state.Fleets.ContainsKey(playerFleetId))
                        {
                                var f = new Fleet
                                {
                                        Id = playerFleetId,
                                        OwnerId = "player",
                                        CurrentNodeId = state.PlayerLocationNodeId,
                                        DestinationNodeId = "",
                                        CurrentEdgeId = "",
                                        State = FleetState.Docked,
                                        TravelProgress = 0f,
                                        Speed = 0.5f,
                                        CurrentTask = "Docked",
                                        CurrentJob = null,
                                        Supplies = 100
                                };

                                state.Fleets.Add(playerFleetId, f);
                        }
                }
        }


	private static void RequireNonEmptyId(string id, string label)
	{
		if (string.IsNullOrWhiteSpace(id)) throw new InvalidOperationException($"{label} must be non-empty.");
	}

	private static NodeKind ParseNodeKind(string? kind)
	{
		var k = (kind ?? "").Trim();
		if (k.Equals("Star", StringComparison.OrdinalIgnoreCase)) return NodeKind.Star;
		if (k.Equals("Station", StringComparison.OrdinalIgnoreCase)) return NodeKind.Station;
		if (k.Equals("Waypoint", StringComparison.OrdinalIgnoreCase)) return NodeKind.Waypoint;
		throw new InvalidOperationException($"Unknown node kind: '{kind}'. Expected Star | Station | Waypoint.");
	}

	private static Vector3 ParseVec3(float[]? pos, string nodeId)
	{
		if (pos is null || pos.Length != 3)
			throw new InvalidOperationException($"Node {nodeId} Pos must be a float[3].");
		return new Vector3(pos[0], pos[1], pos[2]);
	}
}
