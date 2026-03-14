using System.Linq;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.S8.MEGAPROJECT.SYSTEM.001: Multi-stage megaproject construction system.
public static class MegaprojectSystem
{
    public sealed class StartResult
    {
        public bool Success { get; set; }
        public string Reason { get; set; } = "";
        public string MegaprojectId { get; set; } = "";
    }

    /// <summary>
    /// Start a megaproject at the given node. Validates faction rep, deducts credits.
    /// </summary>
    public static StartResult StartMegaproject(SimState state, string typeId, string nodeId, string fleetId)
    {
        if (string.IsNullOrEmpty(typeId))
            return new StartResult { Success = false, Reason = "empty_type" };

        var def = MegaprojectContentV0.GetByTypeId(typeId);
        if (def == null)
            return new StartResult { Success = false, Reason = "unknown_type" };

        if (string.IsNullOrEmpty(nodeId) || !state.Nodes.ContainsKey(nodeId))
            return new StartResult { Success = false, Reason = "invalid_node" };

        // Must have a market at the node (station present).
        if (!state.Markets.ContainsKey(nodeId))
            return new StartResult { Success = false, Reason = "no_station" };

        // Check faction rep at node.
        if (def.MinFactionRep > 0 && state.NodeFactionId.TryGetValue(nodeId, out var factionId))
        {
            int rep = state.FactionReputation.TryGetValue(factionId, out var r) ? r : 0;
            if (rep < def.MinFactionRep)
                return new StartResult { Success = false, Reason = "insufficient_reputation" };
        }

        // Check no existing megaproject at this node.
        foreach (var kv in state.Megaprojects)
        {
            if (kv.Value.NodeId == nodeId && !kv.Value.IsComplete)
                return new StartResult { Success = false, Reason = "node_occupied" };
        }

        // Check credits.
        if (state.PlayerCredits < def.CreditCost)
            return new StartResult { Success = false, Reason = "insufficient_credits" };

        // Deduct credits.
        state.PlayerCredits -= def.CreditCost;

        // Create megaproject.
        var id = $"mp_{state.NextMegaprojectSeq++}";
        var mp = new Megaproject
        {
            Id = id,
            TypeId = typeId,
            NodeId = nodeId,
            Stage = 0,
            MaxStages = def.Stages,
            ProgressTicks = 0,
            CompletedTick = -1, // STRUCTURAL: not complete
            OwnerId = fleetId
        };

        state.Megaprojects[id] = mp;
        return new StartResult { Success = true, MegaprojectId = id };
    }

    /// <summary>
    /// Deliver goods from player cargo to a megaproject's current stage.
    /// </summary>
    public static bool DeliverSupply(SimState state, string megaprojectId, string goodId, int quantity)
    {
        if (quantity <= 0) return false;
        if (!state.Megaprojects.TryGetValue(megaprojectId, out var mp)) return false;
        if (mp.IsComplete) return false;

        var def = MegaprojectContentV0.GetByTypeId(mp.TypeId);
        if (def == null) return false;

        // Check good is required for this stage.
        if (!def.SupplyPerStage.TryGetValue(goodId, out var requiredPerStage))
            return false;

        // Check player has goods.
        int playerQty = state.PlayerCargo.TryGetValue(goodId, out var pq) ? pq : 0;
        if (playerQty < quantity) return false;

        // Cap delivery to what's still needed.
        int delivered = mp.SupplyDelivered.TryGetValue(goodId, out var d) ? d : 0;
        int remaining = requiredPerStage - delivered;
        if (remaining <= 0) return false;
        int actual = System.Math.Min(quantity, remaining);

        // Transfer from player cargo.
        state.PlayerCargo[goodId] = playerQty - actual;
        if (state.PlayerCargo[goodId] <= 0)
            state.PlayerCargo.Remove(goodId);

        mp.SupplyDelivered[goodId] = delivered + actual;
        return true;
    }

    /// <summary>
    /// Per-tick processing: advance progress ticks when supply is met, advance stages.
    /// </summary>
    public static void Process(SimState state)
    {
        foreach (var kv in state.Megaprojects)
        {
            var mp = kv.Value;
            if (mp.IsComplete) continue;

            var def = MegaprojectContentV0.GetByTypeId(mp.TypeId);
            if (def == null) continue;

            // Check if all supply requirements met for current stage.
            if (!IsStageSupplied(mp, def)) continue;

            // Advance progress.
            mp.ProgressTicks++;

            if (mp.ProgressTicks >= def.TicksPerStage)
            {
                // Stage complete — advance.
                mp.Stage++;
                mp.ProgressTicks = 0;
                mp.SupplyDelivered.Clear();

                if (mp.Stage >= mp.MaxStages)
                {
                    // Megaproject complete.
                    mp.CompletedTick = state.Tick;

                    // GATE.S8.MEGAPROJECT.MAP_RULES.001: Apply map rule mutation once.
                    if (!mp.MutationApplied)
                        ApplyMutation(state, mp, def);
                }
            }
        }
    }

    public static bool IsStageSupplied(Megaproject mp, MegaprojectDef def)
    {
        foreach (var req in def.SupplyPerStage)
        {
            int delivered = mp.SupplyDelivered.TryGetValue(req.Key, out var d) ? d : 0;
            if (delivered < req.Value) return false;
        }
        return true;
    }

    // GATE.S8.MEGAPROJECT.MAP_RULES.001: Apply map rule mutation on completion.
    public static void ApplyMutation(SimState state, Megaproject mp, MegaprojectDef def)
    {
        mp.MutationApplied = true;

        switch (def.MutationType)
        {
            case MegaprojectMutationType.FractureAnchor:
                // Mark node as permanent void lane endpoint.
                if (state.Nodes.TryGetValue(mp.NodeId, out var anchorNode))
                {
                    anchorNode.IsFractureNode = true;
                }
                state.InvalidateRoutePlannerCaches();
                break;

            case MegaprojectMutationType.TradeCorridor:
                // Boost speed on all edges connected to this node.
                int boost = MegaprojectTweaksV0.CorridorTransitSpeedBoostPct;
                foreach (var edge in state.Edges.Values)
                {
                    if (edge.FromNodeId == mp.NodeId || edge.ToNodeId == mp.NodeId)
                    {
                        edge.SpeedMultiplierPct = 100 + boost; // STRUCTURAL: base 100
                    }
                }
                state.InvalidateRoutePlannerCaches();
                break;

            case MegaprojectMutationType.SensorPylon:
                // Register pylon node for extended scan range.
                state.SensorPylonNodes.Add(mp.NodeId);
                break;
        }
    }
}
