using SimCore.Entities;
using SimCore.Tweaks;
using System;
using System.Linq;
using System.Numerics;

namespace SimCore.Systems;

public static class FractureSystem
{

    // GATE.S6.FRACTURE.ACCESS_MODEL.001: Access check result.
    public sealed class FractureAccessResult
    {
        public bool Allowed { get; init; }
        public string Reason { get; init; } = "";
    }

    // GATE.S6.FRACTURE.ACCESS_MODEL.001: Check whether a fleet may enter a fracture node.
    // Check 1: fleet.HullHpMax >= MinHullHpMaxForFracture (durability threshold).
    // Check 2: fleet.TechLevel >= node.FractureTier (tech level gate, only if node.FractureTier > 0).
    // Unknown node → Allowed=false, Reason="node not found".
    // Both checks pass → Allowed=true.
    // Deterministic: no RNG, no timestamps, single-pass evaluation.
    public static FractureAccessResult FractureAccessCheck(SimState state, string fleetId, string nodeId)
    {
        if (!state.Fleets.TryGetValue(fleetId, out var fleet))
            return new FractureAccessResult { Allowed = false, Reason = "fleet not found" };

        if (!state.Nodes.TryGetValue(nodeId, out var node))
            return new FractureAccessResult { Allowed = false, Reason = "node not found" };

        // Check 1: hull durability threshold.
        if (fleet.HullHpMax < FractureTweaksV0.MinHullHpMaxForFracture)
            return new FractureAccessResult
            {
                Allowed = false,
                Reason = $"hull_hp_max {fleet.HullHpMax} below minimum {FractureTweaksV0.MinHullHpMaxForFracture}"
            };

        // Check 2: tech level vs node fracture tier (only gated when tier > MinFractureTierForGating).
        if (node.FractureTier > FractureTweaksV0.MinFractureTierForGating && fleet.TechLevel < node.FractureTier)
            return new FractureAccessResult
            {
                Allowed = false,
                Reason = $"tech_level {fleet.TechLevel} below fracture_tier {node.FractureTier}"
            };

        return new FractureAccessResult { Allowed = true, Reason = "" };
    }

    // STRUCTURAL: integer arithmetic helpers for pricing math.
    private const int STRUCT_PRICE_FLOOR = 1; // STRUCTURAL: minimum valid price
    private const int STRUCT_SPREAD_HALVER = 2; // STRUCTURAL: buy/sell is mid ± spread/2
    private const int STRUCT_BPS_DIVISOR = 10000; // STRUCTURAL: basis-point denominator
    private const int STRUCT_BPS_ROUND_HALF = 5000; // STRUCTURAL: half-up rounding term for BPS
    private const int STRUCT_PCT_DIVISOR = 100; // STRUCTURAL: int-pct denominator
    private const int STRUCT_ALIVE_CHECK = 0; // STRUCTURAL: alive/positive boundary check
    private const float STRUCT_TRACE_FLOOR = 0f; // STRUCTURAL: trace clamp floor

    // GATE.S6.FRACTURE.MARKET_MODEL.001: Fracture market pricing snapshot.
    // All math is deterministic integer arithmetic; no RNG.
    public sealed class FracturePriceResult
    {
        // Mid price after volatility scaling.
        public int Mid { get; init; }
        // Buy price (market sells to player) with 2x spread.
        public int Buy { get; init; }
        // Sell price (player sells to market) with 2x spread.
        public int Sell { get; init; }
        // Effective volume cap for this market/good (units).
        public int VolumeCap { get; init; }
    }

    // GATE.S6.FRACTURE.MARKET_MODEL.001: Compute fracture-adjusted prices for a good at a fracture node.
    // stock: current inventory of the good at this market.
    // Returns deterministic buy/sell/mid with 1.5x volatility and 2x spread vs lane baseline.
    public static FracturePriceResult FracturePricingV0(int stock, int laneIdealStock = Market.IdealStock)
    {
        // Baseline mid: same linear scarcity curve as lane markets.
        int baseMid = Market.BasePrice + (laneIdealStock - stock);
        if (baseMid < STRUCT_PRICE_FLOOR) baseMid = STRUCT_PRICE_FLOOR;

        // Volatility: amplify deviation from BasePrice by FractureVolatilityPct%.
        int deviation = baseMid - Market.BasePrice;
        long scaledDeviation = (long)deviation * FractureTweaksV0.FractureVolatilityPct / STRUCT_PCT_DIVISOR;
        int scaledMid = (int)(Market.BasePrice + scaledDeviation);
        if (scaledMid < STRUCT_PRICE_FLOOR) scaledMid = STRUCT_PRICE_FLOOR;

        // Spread: FractureSpreadPct% of the lane SpreadBps applied to scaledMid.
        long laneSpreadNumer = (long)scaledMid * Market.SpreadBps;
        int laneSpreadPct = (int)((laneSpreadNumer + STRUCT_BPS_ROUND_HALF) / STRUCT_BPS_DIVISOR);
        int laneSpread = Math.Max(Market.MinSpread, laneSpreadPct);
        int fractureSpread = laneSpread * FractureTweaksV0.FractureSpreadPct / STRUCT_PCT_DIVISOR;
        if (fractureSpread < FractureTweaksV0.MinFractureSpread) fractureSpread = FractureTweaksV0.MinFractureSpread;

        int buy = scaledMid + (fractureSpread / STRUCT_SPREAD_HALVER);
        int sell = scaledMid - (fractureSpread / STRUCT_SPREAD_HALVER);
        if (buy < STRUCT_PRICE_FLOOR) buy = STRUCT_PRICE_FLOOR;
        if (sell < STRUCT_PRICE_FLOOR) sell = STRUCT_PRICE_FLOOR;

        // Volume cap: FractureVolumeCapPct% of laneIdealStock, minimum 1.
        int volCap = Math.Max(STRUCT_PRICE_FLOOR, laneIdealStock * FractureTweaksV0.FractureVolumeCapPct / STRUCT_PCT_DIVISOR);

        return new FracturePriceResult
        {
            Mid = scaledMid,
            Buy = buy,
            Sell = sell,
            VolumeCap = volCap
        };
    }

    // Deterministic ordering contract:
    // - Primary key: Fleet.Id (dictionary key)
    // - Sort: StringComparer.Ordinal
    // - Filter: only fleets in FractureTraveling state
    public static string[] GetFractureFleetProcessOrder(SimState state)
    {
        return state.Fleets.Keys
            .Where(id => state.Fleets[id].State == FleetState.FractureTraveling)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
    }

    // GATE.S7.FRACTURE.OFFLANE_ROUTES.001: Offlane route validation + cost result.
    public sealed class OfflaneRouteResult
    {
        public bool Valid { get; init; }
        public string Reason { get; init; } = "";
        public float Distance { get; init; }
        public int FuelCost { get; init; }
        public int HullStress { get; init; }
    }

    // GATE.S7.FRACTURE.OFFLANE_ROUTES.001: Compute whether an offlane fracture jump is valid
    // and its cost. Non-adjacent nodes are reachable through fracture space without edge adjacency.
    // Cost scales linearly with Euclidean distance between nodes.
    public static OfflaneRouteResult ComputeOfflaneRoute(SimState state, string fleetId, string fromNodeId, string toNodeId)
    {
        if (state is null || string.IsNullOrEmpty(fleetId))
            return new OfflaneRouteResult { Valid = false, Reason = "null state or fleet" };

        if (string.IsNullOrEmpty(fromNodeId) || string.IsNullOrEmpty(toNodeId))
            return new OfflaneRouteResult { Valid = false, Reason = "missing node ID" };

        if (string.Equals(fromNodeId, toNodeId, StringComparison.Ordinal))
            return new OfflaneRouteResult { Valid = false, Reason = "same node" };

        if (!state.FractureUnlocked)
            return new OfflaneRouteResult { Valid = false, Reason = "fracture not unlocked" };

        if (!state.Fleets.TryGetValue(fleetId, out var fleet))
            return new OfflaneRouteResult { Valid = false, Reason = "fleet not found" };

        if (fleet.TechLevel < FractureTweaksV0.OfflaneMinTechLevel)
            return new OfflaneRouteResult { Valid = false, Reason = $"tech_level {fleet.TechLevel} below minimum {FractureTweaksV0.OfflaneMinTechLevel}" };

        if (!state.Nodes.TryGetValue(fromNodeId, out var fromNode))
            return new OfflaneRouteResult { Valid = false, Reason = "origin node not found" };

        if (!state.Nodes.TryGetValue(toNodeId, out var toNode))
            return new OfflaneRouteResult { Valid = false, Reason = "target node not found" };

        float dist = Vector3.Distance(fromNode.Position, toNode.Position);
        if (dist < FractureTweaksV0.OfflaneMinDistance) dist = FractureTweaksV0.OfflaneMinDistance;

        int fuelCost = Math.Max(STRUCT_PRICE_FLOOR, (int)(dist * FractureTweaksV0.OfflaneFuelCostPerUnit));
        int hullStress = Math.Max(STRUCT_PRICE_FLOOR, (int)(dist * FractureTweaksV0.OfflaneHullStressPerUnit));

        if (fleet.FuelCurrent < fuelCost)
            return new OfflaneRouteResult { Valid = false, Reason = "insufficient fuel", Distance = dist, FuelCost = fuelCost, HullStress = hullStress };

        return new OfflaneRouteResult { Valid = true, Distance = dist, FuelCost = fuelCost, HullStress = hullStress };
    }

    // GATE.S6.FRACTURE.ECON_FEEDBACK.001 — Fracture goods flow into lane hub markets.
    // For each non-fracture node with a market: if fracture goods exist in inventory,
    // increase supply by FractureGoodsFlowRatePct% of fracture stock per tick (integer math, min 1).
    // Fracture goods: exotic_matter, exotic_crystals, salvaged_tech.
    // Econ invariant: lane market total volume never decreases when fracture supply increases.
    // Deterministic: iterates goods in Ordinal order.
    public static void ApplyFractureGoodsFlowV0(SimState state)
    {
        if (state is null) return;
        // GATE.S6.FRACTURE_DISCOVERY.MODEL.001: Gated behind discovery unlock.
        if (!state.FractureUnlocked) return;

        // Fracture good IDs (stable constants).
        var fractureGoodIds = new[]
        {
            SimCore.Content.WellKnownGoodIds.ExoticCrystals,
            SimCore.Content.WellKnownGoodIds.ExoticMatter,
            SimCore.Content.WellKnownGoodIds.SalvagedTech
        };

        // Process nodes in deterministic order (Ordinal by node id).
        var nodeIds = new System.Collections.Generic.List<string>(state.Nodes.Keys);
        nodeIds.Sort(StringComparer.Ordinal);

        foreach (var nodeId in nodeIds)
        {
            if (!state.Nodes.TryGetValue(nodeId, out var node)) continue;

            // Only lane hub nodes (non-fracture) with a market.
            if (node.IsFractureNode) continue;
            if (string.IsNullOrEmpty(node.MarketId)) continue;
            if (!state.Markets.TryGetValue(nodeId, out var market)) continue;

            // For each fracture good, flow 10% of current stock (min 1) into supply.
            foreach (var goodId in fractureGoodIds)
            {
                if (!market.Inventory.TryGetValue(goodId, out var stock)) continue;
                if (stock <= default(int)) continue;

                // Integer math: floor(stock * FractureGoodsFlowRatePct / 100), min 1.
                var flow = stock * FractureTweaksV0.FractureGoodsFlowRatePct / STRUCT_PCT_DIVISOR;
                if (flow < STRUCT_PRICE_FLOOR) flow = STRUCT_PRICE_FLOOR;

                // Add flow to supply (total inventory never decreases).
                market.Inventory[goodId] = checked(stock + flow);
            }
        }
    }

    public static void Process(SimState state)
    {
        // GATE.S6.FRACTURE_DISCOVERY.MODEL.001: Gated behind discovery unlock.
        if (!state.FractureUnlocked) return;

        var orderedFleetIds = GetFractureFleetProcessOrder(state);

        foreach (var fleetId in orderedFleetIds)
        {
            var fleet = state.Fleets[fleetId];

            if (!state.Nodes.TryGetValue(fleet.CurrentNodeId, out var startNode)) continue;
            if (!state.Nodes.TryGetValue(fleet.DestinationNodeId, out var endNode)) continue;

            // Distance Calculation (Euclidean for Fracture/Void)
            float dist = Vector3.Distance(startNode.Position, endNode.Position);
            if (dist < 0.1f) dist = 0.1f;

            // Progress
            float progressStep = fleet.Speed / dist;
            fleet.TravelProgress += progressStep;

            if (fleet.TravelProgress >= 1.0f)
            {
                // Arrival Logic
                fleet.TravelProgress = 0f;
                fleet.CurrentNodeId = fleet.DestinationNodeId;
                fleet.DestinationNodeId = "";
                fleet.State = FleetState.Idle;

                // GATE.S6.FRACTURE.COST_MODEL.001: Hull stress on arrival.
                if (fleet.HullHp > STRUCT_ALIVE_CHECK)
                {
                    fleet.HullHp = Math.Max(STRUCT_PRICE_FLOOR, fleet.HullHp - FractureTweaksV0.FractureHullStressPerJump);
                }

                // GATE.S6.FRACTURE.COST_MODEL.001: Trace accumulation at destination.
                endNode.Trace += FractureTweaksV0.FractureTracePerArrival;
            }
        }

        // GATE.S6.FRACTURE.DETECTION_REP.001: Detect fracture trace + apply rep penalty.
        DetectFractureUse(state);
    }

    // GATE.S6.FRACTURE.DETECTION_REP.001: Factions detect fracture use via trace signature.
    // When node.Trace >= threshold and node has a controlling faction, apply rep penalty to player.
    // Also decays trace naturally per tick.
    public static void DetectFractureUse(SimState state)
    {
        if (state is null) return;

        var nodeIds = new System.Collections.Generic.List<string>(state.Nodes.Keys);
        nodeIds.Sort(StringComparer.Ordinal);

        foreach (var nodeId in nodeIds)
        {
            if (!state.Nodes.TryGetValue(nodeId, out var node)) continue;

            // Natural trace decay.
            if (node.Trace > STRUCT_TRACE_FLOOR)
            {
                node.Trace -= FractureTweaksV0.TraceDecayPerTick;
                if (node.Trace < STRUCT_TRACE_FLOOR) node.Trace = STRUCT_TRACE_FLOOR;
            }

            // Detection: trace above threshold + node has controlling faction.
            if (node.Trace < FractureTweaksV0.TraceDetectionThreshold) continue;
            if (!state.NodeFactionId.TryGetValue(nodeId, out var factionId)) continue;
            if (string.IsNullOrEmpty(factionId)) continue;

            // Apply rep penalty to player for detected fracture use.
            ReputationSystem.AdjustReputation(state, factionId, FractureTweaksV0.FractureDetectionRepPenalty);
        }
    }
}
