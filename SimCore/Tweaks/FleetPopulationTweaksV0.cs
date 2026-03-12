using System;
using System.Collections.Generic;

namespace SimCore.Tweaks;

// GATE.T30.GALPOP.FLEET_TWEAKS.001: Per-faction fleet population composition.
// Each faction seeds its controlled nodes with a specific fleet mix reflecting doctrine.
public static class FleetPopulationTweaksV0
{
    // (Traders, Haulers, Patrols) per controlled node.
    // Concord:   balanced doctrine — moderate everything
    // Chitin:    information traders — more traders, no haulers
    // Weavers:   builders — heavy hauling, no patrol (patient ambush, not active patrol)
    // Valorin:   swarm doctrine — 3-4x density, heavy patrol presence
    // Communion: sparse scouts — minimal presence, fragile
    // Unclaimed: wandering trader only

    public static readonly (int Traders, int Haulers, int Patrols) Concord   = (1, 1, 1); // 3 total
    public static readonly (int Traders, int Haulers, int Patrols) Chitin    = (2, 0, 1); // 3 total
    public static readonly (int Traders, int Haulers, int Patrols) Weavers   = (1, 2, 0); // 3 total
    public static readonly (int Traders, int Haulers, int Patrols) Valorin   = (2, 1, 3); // 6 total — swarm
    public static readonly (int Traders, int Haulers, int Patrols) Communion = (1, 0, 1); // 2 total — sparse
    public static readonly (int Traders, int Haulers, int Patrols) Unclaimed = (1, 0, 0); // 1 total

    public static (int Traders, int Haulers, int Patrols) GetComposition(string factionId)
    {
        if (string.IsNullOrEmpty(factionId)) return Unclaimed;
        return factionId switch
        {
            FactionTweaksV0.ConcordId   => Concord,
            FactionTweaksV0.ChitinId    => Chitin,
            FactionTweaksV0.WeaversId   => Weavers,
            FactionTweaksV0.ValorinId   => Valorin,
            FactionTweaksV0.CommunionId => Communion,
            _ => Unclaimed,
        };
    }

    // ── Hauler-specific tuning ──

    // Max cargo units per hauler trip (traders carry MaxTradeUnitsPerTrip = 10).
    public const int HaulerMaxCargoUnits = 30;

    // Hauler evaluation interval in ticks (traders use EvalIntervalTicks = 15).
    public const int HaulerEvalIntervalTicks = 30;

    // Hauler trade search radius in hops (traders search 1-hop adjacent only).
    public const int HaulerEvalRadiusHops = 2;

    // ── Faction market bias (GATE.T30.GALPOP.MARKET_DIVERSITY.006) ──
    // Surplus/deficit amounts applied per faction territory node at generation.
    // Pentagon ring: Concord→Weavers→Chitin→Valorin→Communion→Concord (need chain).

    public const int SurplusAmount = 200;
    public const int DeficitAmount = 100;

    // Returns (surplusGoods, deficitGoods) for a faction's controlled nodes.
    // Each good gets SurplusAmount added or DeficitAmount subtracted from initial inventory.
    public static (string[] Surplus, string[] Deficit) GetMarketBias(string factionId)
    {
        return factionId switch
        {
            FactionTweaksV0.ConcordId   => (new[] { "food", "fuel" },              new[] { "composites" }),
            FactionTweaksV0.ChitinId    => (new[] { "electronics", "components" },  new[] { "rare_metals" }),
            FactionTweaksV0.WeaversId   => (new[] { "composites", "metal" },        new[] { "electronics" }),
            FactionTweaksV0.ValorinId   => (new[] { "rare_metals", "munitions" },   new[] { "exotic_crystals" }),
            FactionTweaksV0.CommunionId => (new[] { "exotic_crystals" },            new[] { "food", "fuel" }),
            _ => (Array.Empty<string>(), Array.Empty<string>()),
        };
    }
}
