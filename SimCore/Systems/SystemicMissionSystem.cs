using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.S9.SYSTEMIC.TRIGGER_ENGINE.001: World-state mission trigger detection.
// Scans markets and warfronts for conditions that generate procedural mission offers.
public static class SystemicMissionSystem
{
    private const int STRUCT_ZERO = 0; // STRUCTURAL: modulo guard, empty-collection checks, default dict values
    private const int STRUCT_STEP_1 = 1; // STRUCTURAL: second step index in multi-step missions
    private const int STRUCT_NOT_FOUND = -1; // STRUCTURAL: sentinel for not-found index

    private sealed class Scratch
    {
        public readonly HashSet<string> ExistingKeys = new(StringComparer.Ordinal);
        public readonly List<string> SortedWarfrontIds = new();
        public readonly List<string> SortedMarketKeys = new();
        public readonly List<string> SortedNodeKeys = new();
        public readonly List<string> SortedGoodIds = new();
        public readonly Dictionary<string, string> MarketIdToNodeId = new(StringComparer.Ordinal);
    }
    private static readonly ConditionalWeakTable<SimState, Scratch> s_scratch = new();

    public static void Process(SimState state)
    {
        if (state.Tick % SystemicMissionTweaksV0.ScanIntervalTicks != STRUCT_ZERO) return;

        // Expire stale offers.
        for (int i = state.SystemicOffers.Count - 1; i >= 0; i--)
        {
            if (state.Tick >= state.SystemicOffers[i].ExpiryTick)
                state.SystemicOffers.RemoveAt(i);
        }

        // Don't exceed max offers.
        if (state.SystemicOffers.Count >= SystemicMissionTweaksV0.MaxSystemicOffers) return;

        var scratch = s_scratch.GetOrCreateValue(state);

        // Build set of existing offer keys to avoid duplicates.
        var existingKeys = scratch.ExistingKeys;
        existingKeys.Clear();
        foreach (var o in state.SystemicOffers)
            existingKeys.Add($"{(int)o.TriggerType}|{o.NodeId}|{o.GoodId}");

        // Scan triggers in deterministic order.
        // Pre-build reverse map: MarketId → NodeId (avoids O(n²) search in ScanPriceSpike).
        var marketToNode = scratch.MarketIdToNodeId;
        marketToNode.Clear();
        foreach (var nkv in state.Nodes)
        {
            if (!string.IsNullOrEmpty(nkv.Value.MarketId))
                marketToNode[nkv.Value.MarketId] = nkv.Key;
        }

        ScanWarDemand(state, existingKeys, scratch);
        ScanPriceSpike(state, existingKeys, scratch);
        ScanSupplyShortage(state, existingKeys, scratch);
    }

    // WAR_DEMAND: Warfront goods shortage at contested nodes.
    private static void ScanWarDemand(SimState state, HashSet<string> existingKeys, Scratch scratch)
    {
        if (state.Warfronts is null || state.Warfronts.Count == STRUCT_ZERO) return;

        var sortedWarfrontIds = scratch.SortedWarfrontIds;
        sortedWarfrontIds.Clear();
        foreach (var k in state.Warfronts.Keys) sortedWarfrontIds.Add(k);
        sortedWarfrontIds.Sort(StringComparer.Ordinal);
        foreach (var wfId in sortedWarfrontIds)
        {
            var wf = state.Warfronts[wfId];
            if (wf.Intensity < WarfrontIntensity.Skirmish) continue;

            foreach (var nodeId in wf.ContestedNodeIds)
            {
                if (state.SystemicOffers.Count >= SystemicMissionTweaksV0.MaxSystemicOffers) return;
                if (!state.Nodes.TryGetValue(nodeId, out var node)) continue;
                if (string.IsNullOrEmpty(node.MarketId)) continue;
                if (!state.Markets.TryGetValue(node.MarketId, out var market)) continue;

                CheckWarGood(state, existingKeys, market, nodeId, WellKnownGoodIds.Munitions);
                CheckWarGood(state, existingKeys, market, nodeId, WellKnownGoodIds.Composites);
                CheckWarGood(state, existingKeys, market, nodeId, WellKnownGoodIds.Fuel);
            }
        }
    }

    private static void CheckWarGood(SimState state, HashSet<string> existingKeys,
        Market market, string nodeId, string goodId)
    {
        if (state.SystemicOffers.Count >= SystemicMissionTweaksV0.MaxSystemicOffers) return;

        var stock = market.Inventory.TryGetValue(goodId, out var qty) ? qty : STRUCT_ZERO;
        if (stock >= SystemicMissionTweaksV0.WarDemandInventoryThreshold) return;

        var key = $"{(int)SystemicTriggerType.WarDemand}|{nodeId}|{goodId}";
        if (existingKeys.Contains(key)) return;

        EmitOffer(state, existingKeys, SystemicTriggerType.WarDemand, nodeId, goodId);
    }

    // PRICE_SPIKE: Good price exceeds threshold × base price.
    private static void ScanPriceSpike(SimState state, HashSet<string> existingKeys, Scratch scratch)
    {
        var sortedMarketKeys = scratch.SortedMarketKeys;
        sortedMarketKeys.Clear();
        foreach (var k in state.Markets.Keys) sortedMarketKeys.Add(k);
        sortedMarketKeys.Sort(StringComparer.Ordinal);
        foreach (var marketKey in sortedMarketKeys)
        {
            if (state.SystemicOffers.Count >= SystemicMissionTweaksV0.MaxSystemicOffers) return;

            var market = state.Markets[marketKey];
            // Find the node that owns this market (via pre-built reverse map).
            if (!scratch.MarketIdToNodeId.TryGetValue(market.Id, out var nodeId)
                || string.IsNullOrEmpty(nodeId)) continue;

            var sortedGoodIds = scratch.SortedGoodIds;
            sortedGoodIds.Clear();
            foreach (var g in market.Inventory.Keys) sortedGoodIds.Add(g);
            sortedGoodIds.Sort(StringComparer.Ordinal);
            foreach (var goodId in sortedGoodIds)
            {
                if (state.SystemicOffers.Count >= SystemicMissionTweaksV0.MaxSystemicOffers) return;

                int midPrice = market.GetMidPrice(goodId);
                int threshold = Market.BasePrice * SystemicMissionTweaksV0.PriceSpikeThresholdPct / CombatTweaksV0.NeutralPct;
                if (midPrice < threshold) continue;

                var key = $"{(int)SystemicTriggerType.PriceSpike}|{nodeId}|{goodId}";
                if (existingKeys.Contains(key)) continue;

                EmitOffer(state, existingKeys, SystemicTriggerType.PriceSpike, nodeId, goodId);
            }
        }
    }

    // SUPPLY_SHORTAGE: Low inventory at high-instability nodes.
    private static void ScanSupplyShortage(SimState state, HashSet<string> existingKeys, Scratch scratch)
    {
        var sortedNodeKeys = scratch.SortedNodeKeys;
        sortedNodeKeys.Clear();
        foreach (var k in state.Nodes.Keys) sortedNodeKeys.Add(k);
        sortedNodeKeys.Sort(StringComparer.Ordinal);
        foreach (var nodeKey in sortedNodeKeys)
        {
            if (state.SystemicOffers.Count >= SystemicMissionTweaksV0.MaxSystemicOffers) return;

            var node = state.Nodes[nodeKey];
            if (node.InstabilityLevel < SystemicMissionTweaksV0.SupplyShortageInstabilityMin) continue;
            if (string.IsNullOrEmpty(node.MarketId)) continue;
            if (!state.Markets.TryGetValue(node.MarketId, out var market)) continue;

            var sortedGoodIds = scratch.SortedGoodIds;
            sortedGoodIds.Clear();
            foreach (var g in market.Inventory.Keys) sortedGoodIds.Add(g);
            sortedGoodIds.Sort(StringComparer.Ordinal);
            foreach (var goodId in sortedGoodIds)
            {
                if (state.SystemicOffers.Count >= SystemicMissionTweaksV0.MaxSystemicOffers) return;

                var stock = market.Inventory.TryGetValue(goodId, out var qty) ? qty : STRUCT_ZERO;
                if (stock >= SystemicMissionTweaksV0.SupplyShortageInventoryThreshold) continue;

                var key = $"{(int)SystemicTriggerType.SupplyShortage}|{nodeKey}|{goodId}";
                if (existingKeys.Contains(key)) continue;

                EmitOffer(state, existingKeys, SystemicTriggerType.SupplyShortage, nodeKey, goodId);
            }
        }
    }

    // GATE.S9.SYSTEMIC.OFFER_GEN.001: Build a MissionDef from a systemic offer.
    // Template-driven: each trigger type generates a different mission structure.
    public static MissionDef BuildMissionFromOffer(SystemicMissionOffer offer)
    {
        var def = new MissionDef
        {
            MissionId = offer.OfferId,
            DeadlineTicks = SystemicMissionTweaksV0.OfferExpiryTicks,
        };

        switch (offer.TriggerType)
        {
            case SystemicTriggerType.WarDemand:
                def.Title = $"War Supply: {offer.GoodId}";
                def.Description = $"Deliver {SystemicMissionTweaksV0.WarDemandDeliveryQty} {offer.GoodId} to {offer.NodeId} to aid the war effort.";
                def.CreditReward = SystemicMissionTweaksV0.WarDemandCreditReward;
                def.Steps = new List<MissionStepDef>
                {
                    new MissionStepDef
                    {
                        StepIndex = STRUCT_ZERO,
                        ObjectiveText = $"Acquire {SystemicMissionTweaksV0.WarDemandDeliveryQty} {offer.GoodId}",
                        TriggerType = MissionTriggerType.HaveCargoMin,
                        TargetGoodId = offer.GoodId,
                        TargetQuantity = SystemicMissionTweaksV0.WarDemandDeliveryQty,
                    },
                    new MissionStepDef
                    {
                        StepIndex = STRUCT_STEP_1,
                        ObjectiveText = $"Deliver to {offer.NodeId}",
                        TriggerType = MissionTriggerType.ArriveAtNode,
                        TargetNodeId = offer.NodeId,
                    },
                };
                break;

            case SystemicTriggerType.PriceSpike:
                def.Title = $"Trade Opportunity: {offer.GoodId}";
                def.Description = $"High prices for {offer.GoodId} at {offer.NodeId}. Sell there for profit.";
                def.CreditReward = SystemicMissionTweaksV0.PriceSpikeCreditReward;
                def.Steps = new List<MissionStepDef>
                {
                    new MissionStepDef
                    {
                        StepIndex = STRUCT_ZERO,
                        ObjectiveText = $"Sell {offer.GoodId} at {offer.NodeId}",
                        TriggerType = MissionTriggerType.NoCargoAtNode,
                        TargetNodeId = offer.NodeId,
                        TargetGoodId = offer.GoodId,
                    },
                };
                break;

            case SystemicTriggerType.SupplyShortage:
                def.Title = $"Supply Run: {offer.GoodId}";
                def.Description = $"Supply shortage of {offer.GoodId} at {offer.NodeId} due to instability.";
                def.CreditReward = SystemicMissionTweaksV0.SupplyRunCreditReward;
                def.Steps = new List<MissionStepDef>
                {
                    new MissionStepDef
                    {
                        StepIndex = STRUCT_ZERO,
                        ObjectiveText = $"Acquire {SystemicMissionTweaksV0.SupplyRunDeliveryQty} {offer.GoodId}",
                        TriggerType = MissionTriggerType.HaveCargoMin,
                        TargetGoodId = offer.GoodId,
                        TargetQuantity = SystemicMissionTweaksV0.SupplyRunDeliveryQty,
                    },
                    new MissionStepDef
                    {
                        StepIndex = STRUCT_STEP_1,
                        ObjectiveText = $"Deliver to {offer.NodeId}",
                        TriggerType = MissionTriggerType.ArriveAtNode,
                        TargetNodeId = offer.NodeId,
                    },
                };
                break;
        }

        return def;
    }

    // GATE.S9.SYSTEMIC.OFFER_GEN.001: Get all systemic offers as acceptabe MissionDefs.
    public static List<MissionDef> GetAvailableSystemicMissions(SimState state)
    {
        var result = new List<MissionDef>();
        if (state.SystemicOffers is null) return result;

        // Skip if player already has an active mission.
        if (state.Missions is not null && !string.IsNullOrEmpty(state.Missions.ActiveMissionId))
            return result;

        foreach (var offer in state.SystemicOffers)
        {
            if (state.Tick >= offer.ExpiryTick) continue;
            result.Add(BuildMissionFromOffer(offer));
        }

        return result;
    }

    // GATE.S9.SYSTEMIC.OFFER_GEN.001: Accept a systemic mission by offer ID.
    // Removes the offer from SystemicOffers and activates the built mission.
    public static bool AcceptSystemicMission(SimState state, string offerId)
    {
        if (state is null || string.IsNullOrEmpty(offerId)) return false;

        var offerIndex = STRUCT_NOT_FOUND;
        for (int i = STRUCT_ZERO; i < state.SystemicOffers.Count; i++)
        {
            if (string.Equals(state.SystemicOffers[i].OfferId, offerId, StringComparison.Ordinal))
            {
                offerIndex = i;
                break;
            }
        }
        if (offerIndex < STRUCT_ZERO) return false;

        var offer = state.SystemicOffers[offerIndex];
        var missionDef = BuildMissionFromOffer(offer);

        state.Missions ??= new MissionState();
        if (!string.IsNullOrEmpty(state.Missions.ActiveMissionId)) return false;

        // Directly populate active mission from the built def (systemic missions aren't in static registry).
        state.Missions.ActiveMissionId = missionDef.MissionId;
        state.Missions.CurrentStepIndex = STRUCT_ZERO;
        state.Missions.ActiveSteps.Clear();

        foreach (var stepDef in missionDef.Steps)
        {
            state.Missions.ActiveSteps.Add(new MissionActiveStep
            {
                StepIndex = stepDef.StepIndex,
                ObjectiveText = stepDef.ObjectiveText,
                TriggerType = stepDef.TriggerType,
                TargetNodeId = stepDef.TargetNodeId,
                TargetGoodId = stepDef.TargetGoodId,
                TargetQuantity = stepDef.TargetQuantity,
                Completed = false,
            });
        }

        if (missionDef.DeadlineTicks > STRUCT_ZERO)
            state.Missions.MissionDeadlineTick = state.Tick + missionDef.DeadlineTicks;

        // Remove the offer.
        state.SystemicOffers.RemoveAt(offerIndex);

        return true;
    }

    private static void EmitOffer(SimState state, HashSet<string> existingKeys,
        SystemicTriggerType triggerType, string nodeId, string goodId)
    {
        var offer = new SystemicMissionOffer
        {
            OfferId = $"SYS|{(int)triggerType}|{nodeId}|{goodId}|{state.Tick}",
            TriggerType = triggerType,
            NodeId = nodeId,
            GoodId = goodId,
            CreatedTick = state.Tick,
            ExpiryTick = state.Tick + SystemicMissionTweaksV0.OfferExpiryTicks,
        };

        state.SystemicOffers.Add(offer);
        existingKeys.Add($"{(int)triggerType}|{nodeId}|{goodId}");
    }
}
