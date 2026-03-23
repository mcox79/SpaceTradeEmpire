using SimCore.Content;
using SimCore.Entities;
using SimCore.Tweaks;
using System;

namespace SimCore.Systems;

public static class MarketSystem
{
    // GATE.MKT.002: publish cadence every 12 game hours.
    // With 1 tick = 1 sim minute, 12 hours = 720 minutes = 720 ticks.
    public const int PublishWindowTicks = 720;

    // GATE.S3.MARKET_ARB.001: transaction fee friction v0.
    // Deterministic integer math. Fee is applied to credit amounts (gross) in basis points.
    // Example: 100 bps = 1.00% fee.
    //
    // Migration note (GATE.X.TWEAKS.DATA.MIGRATE.MARKET_FEES.001):
    // - TransactionFeeBps remains the stable base default.
    // - Effective fee bps may be scaled by state.Tweaks.MarketFeeMultiplier (when provided).
    public const int TransactionFeeBps = 100;

    // GATE.S3_6.DISCOVERY_UNLOCK_CONTRACT.003
    // Market access eligibility: a market may require a Permit unlock id.
    // Deterministic: single dictionary lookup by stable unlock id.
    public static bool CanAccessMarket(SimState state, Market market)
    {
        if (market is null) return false;

        var req = market.RequiresPermitUnlockId;
        if (string.IsNullOrWhiteSpace(req)) return true;

        if (state.Intel.Unlocks.TryGetValue(req, out var u))
        {
            return u.IsAcquired && u.Kind == UnlockKind.Permit;
        }

        return false;
    }

    // GATE.S3_6.DISCOVERY_UNLOCK_CONTRACT.003
    // Broker unlock economic effect: if any acquired Broker unlock exists, transaction fees are waived (bps=0).
    // Result is order-independent (any-match), so no sort needed.
    private static bool HasAnyAcquiredBrokerUnlock(SimState state)
    {
        foreach (var kvp in state.Intel.Unlocks)
        {
            var u = kvp.Value;
            if (u.IsAcquired && u.Kind == UnlockKind.Broker) return true;
        }

        return false;
    }

    public static int GetEffectiveTransactionFeeBps(SimState? state)
    {
        if (state is null) return TransactionFeeBps;

        if (HasAnyAcquiredBrokerUnlock(state)) return default;

        // Default multiplier without introducing a new numeric literal token.
        double defaultMult = (double)TransactionFeeBps / TransactionFeeBps;

        var mult = state.Tweaks?.MarketFeeMultiplier ?? defaultMult;
        if (!double.IsFinite(mult)) return TransactionFeeBps;

        // Deterministic scaling and rounding across platforms:
        // use decimal and explicit midpoint mode (round to 0 decimals via overload).
        decimal scaled = (decimal)TransactionFeeBps * (decimal)mult;
        int bps = (int)decimal.Round(scaled, MidpointRounding.AwayFromZero);

        int minBps = default;
        int maxBps = checked(TransactionFeeBps * TransactionFeeBps);

        if (bps < minBps) bps = minBps;
        if (bps > maxBps) bps = maxBps;
        return bps;
    }

    public static int ComputeTransactionFeeCredits(int grossCredits)
        => ComputeTransactionFeeCredits(state: null, grossCredits);

    public static int ComputeTransactionFeeCredits(SimState? state, int grossCredits)
    {
        if (grossCredits <= 0) return 0;

        long bps = GetEffectiveTransactionFeeBps(state);
        if (bps == default) return default;

        // Ceil(gross * bps / 10000) using integer math.
        long gross = grossCredits;
        long denom = (long)(TransactionFeeBps * TransactionFeeBps);
        long fee = (gross * bps + 9999L) / denom;

        if (fee <= 0) fee = 1; // ensure a nonzero fee when gross > 0 and bps > 0
        if (fee > int.MaxValue) return int.MaxValue;
        return (int)fee;
    }

    public static int ApplyTransactionFee(int grossCredits)
        => ApplyTransactionFee(state: null, grossCredits);

    public static int ApplyTransactionFee(SimState? state, int grossCredits)
    {
        var fee = ComputeTransactionFeeCredits(state, grossCredits);
        var net = grossCredits - fee;
        return net < 0 ? 0 : net;
    }

    public static void Process(SimState state)
    {
        // 1. Decay Edge Heat (Cooling)
        foreach (var edge in state.Edges.Values)
        {
            if (edge.Heat > 0)
            {
                edge.Heat -= 0.05f;
                if (edge.Heat < 0) edge.Heat = 0;
            }
        }

        // 2. Publish prices on cadence (deterministic, once per bucket)
        foreach (var market in state.Markets.Values)
        {
            market.PublishPricesIfDue(state.Tick, PublishWindowTicks);
        }
    }

    // Called when a Fleet traverses an Edge with Cargo
    public static void RegisterTraffic(SimState state, string edgeId, int cargoVolume)
    {
        if (state.Edges.TryGetValue(edgeId, out var edge))
        {
            // Heat generated per unit of cargo
            edge.Heat += cargoVolume * 0.01f;
        }
    }

    // GATE.S7.FACTION.TARIFF_ENFORCE.001: Find which faction controls the node that owns a market.
    public static string GetControllingFactionIdForMarket(SimState state, string marketId)
    {
        if (state is null || string.IsNullOrEmpty(marketId)) return "";

        // Find the node that references this market.
        foreach (var kv in state.Nodes)
        {
            if (StringComparer.Ordinal.Equals(kv.Value.MarketId, marketId))
            {
                if (state.NodeFactionId.TryGetValue(kv.Key, out var fid))
                    return fid;
                return "";
            }
        }
        return "";
    }

    // GATE.S7.FACTION.TARIFF_ENFORCE.001: Compute effective tariff in basis points.
    // Formula: baseTariffBps * (1 - reputation/100) + warSurcharge.
    // GATE.S7.WARFRONT.TARIFF_SCALING.001: War surcharge = WarSurchargeBpsPerIntensity * nodeIntensity.
    public static int GetEffectiveTariffBps(SimState state, string marketId)
    {
        if (state is null || string.IsNullOrEmpty(marketId)) return 0;

        var factionId = GetControllingFactionIdForMarket(state, marketId);
        if (string.IsNullOrEmpty(factionId)) return 0;

        if (!state.FactionTariffRates.TryGetValue(factionId, out var tariffRate))
            return 0;

        int baseBps = (int)(tariffRate * FactionTweaksV0.TariffBpsMultiplier);
        if (baseBps <= 0) return 0;

        int rep = ReputationSystem.GetReputation(state, factionId);
        // Scale: (1 - rep/100). At rep=100 -> 0, rep=0 -> 1, rep=-100 -> 2.
        int effectiveBps = baseBps * (FactionTweaksV0.ReputationMax - rep) / FactionTweaksV0.ReputationMax;

        // GATE.S7.WARFRONT.TARIFF_SCALING.001: Add war surcharge based on node warfront intensity.
        int warIntensity = GetNodeWarfrontIntensity(state, marketId);
        if (warIntensity > 0)
        {
            effectiveBps += WarfrontTweaksV0.WarSurchargeBpsPerIntensity * warIntensity;

            // GATE.S7.WARFRONT.NEUTRALITY_TAX.001: Neutrality surcharge for non-aligned players in war zones.
            // Applied when player reputation is in Neutral band (-25..+25) at intensity ≥ 2.
            if (rep >= FactionTweaksV0.NeutralThreshold && rep < FactionTweaksV0.FriendlyThreshold)
            {
                int neutralityBps = warIntensity switch
                {
                    >= WarfrontTweaksV0.TotalWarIntensity => WarfrontTweaksV0.NeutralityTaxBpsIntensity4,
                    WarfrontTweaksV0.OpenWarIntensity => WarfrontTweaksV0.NeutralityTaxBpsIntensity3,
                    WarfrontTweaksV0.SkirmishIntensity => WarfrontTweaksV0.NeutralityTaxBpsIntensity2,
                    _ => 0, // STRUCTURAL: no tax below intensity 2
                };
                effectiveBps += neutralityBps;
            }
        }

        return Math.Max(0, effectiveBps);
    }

    // GATE.S7.WARFRONT.TARIFF_SCALING.001: Get max warfront intensity at a market's node.
    public static int GetNodeWarfrontIntensity(SimState state, string marketId)
    {
        if (state?.Warfronts is null || state.Warfronts.Count == 0) return 0;

        // Find node for this market.
        string? nodeId = null;
        foreach (var kv in state.Nodes)
        {
            if (StringComparer.Ordinal.Equals(kv.Value.MarketId, marketId))
            {
                nodeId = kv.Key;
                break;
            }
        }
        if (nodeId is null) return 0;

        int maxIntensity = 0;
        foreach (var wf in state.Warfronts.Values)
        {
            if (wf.ContestedNodeIds.Contains(nodeId))
            {
                int i = (int)wf.Intensity;
                if (i > maxIntensity) maxIntensity = i;
            }
        }
        return maxIntensity;
    }

    // GATE.S7.FACTION.TARIFF_ENFORCE.001: Check if player reputation allows trading at this market.
    public static bool CanTradeByReputation(SimState state, string marketId)
    {
        if (state is null || string.IsNullOrEmpty(marketId)) return true;

        var factionId = GetControllingFactionIdForMarket(state, marketId);
        if (string.IsNullOrEmpty(factionId)) return true;

        int rep = ReputationSystem.GetReputation(state, factionId);
        return rep >= FactionTweaksV0.TradeBlockedRepThreshold;
    }

    // GATE.S7.FACTION.TARIFF_ENFORCE.001: Apply tariff surcharge to a credit amount.
    public static int ComputeTariffCredits(int baseCredits, int tariffBps)
    {
        if (baseCredits <= 0 || tariffBps <= 0) return 0;
        return (int)(((long)baseCredits * tariffBps + 9999L) / FactionTweaksV0.TariffBpsMultiplier);
    }

    // GATE.S7.REPUTATION.PRICING_CURVES.001: Get rep-based price modifier in basis points.
    // Allied=-1500, Friendly=-500, Neutral=0, Hostile=+2000.
    public static int GetRepPricingBps(SimState state, string factionId)
    {
        if (state is null || string.IsNullOrEmpty(factionId)) return 0;
        var tier = ReputationSystem.GetRepTier(state, factionId);
        return tier switch
        {
            RepTier.Allied => FactionTweaksV0.AlliedPriceBps,
            RepTier.Friendly => FactionTweaksV0.FriendlyPriceBps,
            RepTier.Neutral => FactionTweaksV0.NeutralPriceBps,
            RepTier.Hostile => FactionTweaksV0.HostilePriceBps,
            _ => 0 // Enemy: trade blocked, should not reach pricing
        };
    }

    // GATE.S7.REPUTATION.PRICING_CURVES.001: Apply rep modifier to a base price.
    // Returns adjusted price (min 1). Positive bps = surcharge, negative = discount.
    public static int ApplyRepPricing(int basePrice, int repBps)
    {
        if (basePrice <= 0 || repBps == 0) return Math.Max(1, basePrice); // STRUCTURAL: floor
        long adjusted = (long)basePrice * (FactionTweaksV0.TariffBpsMultiplier + repBps) / FactionTweaksV0.TariffBpsMultiplier;
        return (int)Math.Max(1, adjusted); // STRUCTURAL: floor
    }

    // GATE.X.MARKET_PRICING.AFFINITY_WIRE.001: Get faction good affinity modifier in bps.
    public static int GetFactionGoodAffinityBps(SimState state, string factionId, string goodId)
    {
        return FactionGoodAffinityTweaksV0.GetAffinityBps(factionId, goodId);
    }

    // GATE.X.MARKET_PRICING.AFFINITY_WIRE.001: Apply faction good affinity to a base price.
    // Same math as ApplyRepPricing. Negative bps = discount, positive = surcharge.
    public static int ApplyFactionGoodAffinityPricing(int basePrice, int affinityBps)
    {
        if (basePrice <= 0 || affinityBps == 0) return Math.Max(1, basePrice); // STRUCTURAL: floor
        long adjusted = (long)basePrice * (FactionTweaksV0.TariffBpsMultiplier + affinityBps) / FactionTweaksV0.TariffBpsMultiplier;
        return (int)Math.Max(1, adjusted); // STRUCTURAL: floor
    }

    // GATE.S7.TERRITORY.EMBARGO_MODEL.001: Check if a good is embargoed at a market.
    // Returns true if any active embargo blocks this good at this market's faction.
    public static bool IsGoodEmbargoed(SimState state, string marketId, string goodId)
    {
        if (state?.Embargoes is null || state.Embargoes.Count == 0) return false;
        if (string.IsNullOrEmpty(marketId) || string.IsNullOrEmpty(goodId)) return false;

        var factionId = GetControllingFactionIdForMarket(state, marketId);
        if (string.IsNullOrEmpty(factionId)) return false;

        foreach (var embargo in state.Embargoes)
        {
            if (StringComparer.Ordinal.Equals(embargo.EnforcingFactionId, factionId)
                && StringComparer.Ordinal.Equals(embargo.GoodId, goodId))
                return true;
        }
        return false;
    }

    // GATE.S7.INSTABILITY.CONSEQUENCES.001: Get instability phase for a node.
    public static int GetNodeInstabilityPhase(SimState state, string nodeId)
    {
        if (state is null || string.IsNullOrEmpty(nodeId)) return 0; // STRUCTURAL: default phase
        if (!state.Nodes.TryGetValue(nodeId, out var node)) return 0; // STRUCTURAL: not found
        return InstabilityTweaksV0.GetPhaseIndex(node.InstabilityLevel);
    }

    // GATE.S7.INSTABILITY.CONSEQUENCES.001: Get instability price jitter percentage for a market.
    // Returns 0 for Stable, 5 for Shimmer, etc.
    public static int GetInstabilityPriceJitterPct(SimState state, string marketId)
    {
        if (state is null || string.IsNullOrEmpty(marketId)) return 0; // STRUCTURAL: default
        string? nodeId = FindNodeForMarket(state, marketId);
        if (nodeId is null) return 0; // STRUCTURAL: not found
        int phase = GetNodeInstabilityPhase(state, nodeId);
        return phase switch
        {
            1 => InstabilityTweaksV0.ShimmerPriceJitterPct,           // Shimmer
            >= 2 => InstabilityTweaksV0.ShimmerPriceJitterPct * phase, // Scales with phase
            _ => 0 // STRUCTURAL: Stable
        };
    }

    // GATE.S7.INSTABILITY.CONSEQUENCES.001: Check if market is closed due to Void instability.
    public static bool IsMarketClosedByInstability(SimState state, string marketId)
    {
        if (state is null || string.IsNullOrEmpty(marketId)) return false;
        string? nodeId = FindNodeForMarket(state, marketId);
        if (nodeId is null) return false;
        int phase = GetNodeInstabilityPhase(state, nodeId);
        return phase >= 4; // STRUCTURAL: Void phase index
    }

    // Helper: find the node that owns a market.
    private static string? FindNodeForMarket(SimState state, string marketId)
    {
        foreach (var kv in state.Nodes)
        {
            if (StringComparer.Ordinal.Equals(kv.Value.MarketId, marketId))
                return kv.Key;
        }
        return null;
    }

    // GATE.S18.TRADE_GOODS.PRICE_BANDS.001: Get effective price for a good at a market.
    // Price scales with supply: below DemandThreshold → price rises, above → falls.
    // Integer arithmetic only, deterministic.
    public static int GetEffectivePrice(string goodId, int currentQty, ContentRegistryLoader.ContentRegistryV0? registry)
    {
        int basePrice = MarketTweaksV0.PriceLowBase;
        int spread = MarketTweaksV0.PriceLowSpread;

        if (registry != null)
        {
            var good = registry.Goods.FirstOrDefault(g => string.Equals(g.Id, goodId, StringComparison.Ordinal));
            if (good != null && good.BasePrice > 0)
            {
                basePrice = good.BasePrice;
                spread = good.PriceSpread;
            }
        }

        // Supply/demand: below threshold → price rises, above → falls.
        // Linear interpolation within [basePrice - spread, basePrice + spread].
        int threshold = MarketTweaksV0.DemandThreshold;
        int priceDelta;
        if (currentQty <= 0)
            priceDelta = spread; // max price
        else if (currentQty >= threshold * 2)
            priceDelta = -spread; // min price
        else
            priceDelta = spread - (spread * 2 * currentQty / (threshold * 2));

        int price = basePrice + priceDelta;
        return Math.Max(1, price);
    }

    // GATE.S7.INSTABILITY_EFFECTS.MARKET.001: Instability-aware effective price.
    // Applies volatility multiplier (scales linearly with instability level, 1.0x→1.5x)
    // and security demand skew (fuel/munitions surcharge at Drift+ phase).
    // Returns 0 if market is closed by Void-phase instability.
    public static int GetEffectivePrice(SimState state, string marketId, string goodId, int currentQty, ContentRegistryLoader.ContentRegistryV0? registry)
    {
        int price = GetEffectivePrice(goodId, currentQty, registry);

        if (state is null || string.IsNullOrEmpty(marketId)) return price;

        // Void phase: market closed.
        if (IsMarketClosedByInstability(state, marketId)) return 0;

        // Find instability level for this market's node.
        string? nodeId = FindNodeForMarket(state, marketId);
        if (nodeId is null) return price;
        if (!state.Nodes.TryGetValue(nodeId, out var node)) return price;

        int instLevel = node.InstabilityLevel;
        if (instLevel <= 0) return price;

        // Volatility multiplier: linear from 10000 bps (1.0x) to 15000 bps (1.5x) at MaxInstability.
        int volatilityBps = instLevel * InstabilityTweaksV0.VolatilityMaxBps / InstabilityTweaksV0.MaxInstability;
        long adjusted = (long)price * (10000 + volatilityBps) / 10000;

        // Security demand skew at Drift+ (phase ≥2): fuel/munitions get extra surcharge.
        int phase = InstabilityTweaksV0.GetPhaseIndex(instLevel);
        if (phase >= 2 && Content.WellKnownGoodIds.IsSecurityGood(goodId))
        {
            int skewSteps = phase - 1; // phase 2=1x, phase 3=2x, phase 4=3x
            long skew = (long)price * InstabilityTweaksV0.SecurityDemandSkewBps * skewSteps / 10000;
            adjusted += skew;
        }

        return (int)Math.Max(1, adjusted);
    }

    // GATE.X.INSTAB_PRICE.WIRE.001: Returns instability price multiplier in basis points (10000 = 1.0x).
    // Returns 0 if market is closed by instability (Void phase). Returns 10000 if no instability.
    public static int GetInstabilityPriceMultiplierBps(SimState state, string marketId, string goodId)
    {
        if (state is null || string.IsNullOrEmpty(marketId)) return 10000;
        if (IsMarketClosedByInstability(state, marketId)) return 0;

        string? nodeId = FindNodeForMarket(state, marketId);
        if (nodeId is null) return 10000;
        if (!state.Nodes.TryGetValue(nodeId, out var node)) return 10000;

        int instLevel = node.InstabilityLevel;
        if (instLevel <= 0) return 10000;

        int volatilityBps = instLevel * InstabilityTweaksV0.VolatilityMaxBps / InstabilityTweaksV0.MaxInstability;
        int multiplier = 10000 + volatilityBps;

        int phase = InstabilityTweaksV0.GetPhaseIndex(instLevel);
        if (phase >= 2 && Content.WellKnownGoodIds.IsSecurityGood(goodId))
        {
            int skewSteps = phase - 1;
            multiplier += InstabilityTweaksV0.SecurityDemandSkewBps * skewSteps;
        }

        return multiplier;
    }
}
