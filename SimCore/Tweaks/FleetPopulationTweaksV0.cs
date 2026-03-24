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
    // Unclaimed: independent traders and prospectors — border space still has traffic

    public static readonly (int Traders, int Haulers, int Patrols) Concord   = (1, 1, 1); // 3 total
    public static readonly (int Traders, int Haulers, int Patrols) Chitin    = (2, 0, 1); // 3 total
    public static readonly (int Traders, int Haulers, int Patrols) Weavers   = (1, 2, 0); // 3 total
    public static readonly (int Traders, int Haulers, int Patrols) Valorin   = (2, 1, 3); // 6 total — swarm
    public static readonly (int Traders, int Haulers, int Patrols) Communion = (1, 0, 1); // 2 total — sparse
    public static readonly (int Traders, int Haulers, int Patrols) Unclaimed = (2, 1, 0); // 3 total — independents

    // GATE.T55.COMBAT.PIRATE_FACTION.001: Pirates are patrol-only — no traders or haulers.
    public static readonly (int Traders, int Haulers, int Patrols) Pirate = (0, 0, 1); // 1 patrol per seeded node

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
            FactionTweaksV0.PirateId    => Pirate,
            _ => Unclaimed,
        };
    }

    // ── Starter node density floor ──
    // Ensures the player's starting system has enough NPCs to feel alive at boot.
    public const int StarterMinTraders = 2;
    public const int StarterMinPatrols = 2;

    // ── Hauler-specific tuning ──

    // Max cargo units per hauler trip (traders carry MaxTradeUnitsPerTrip = 10).
    public const int HaulerMaxCargoUnits = 30;

    // Hauler evaluation interval in ticks (traders use EvalIntervalTicks = 15).
    public const int HaulerEvalIntervalTicks = 30;

    // Hauler trade search radius in hops (traders search 1-hop adjacent only).
    public const int HaulerEvalRadiusHops = 2;

    // ── Dynamic fleet population replacement ──
    // Goods consumed to spawn a replacement fleet at a station.
    public const string ReplacementGood1 = "metal";
    public const int ReplacementMetalCost = 50;
    public const string ReplacementGood2 = "components";
    public const int ReplacementComponentsCost = 20;

    // ── Faction market bias (GATE.T30.GALPOP.MARKET_DIVERSITY.006) ──
    // Surplus/deficit amounts applied per faction territory node at generation.
    // Pentagon ring: Concord→Weavers→Chitin→Valorin→Communion→Concord (need chain).
    // Raised from 200/100 to 600/400 so all 12 goods create tradeable price differentials.

    public const int SurplusAmount = 600;
    public const int DeficitAmount = 400;

    // Returns (surplusGoods, deficitGoods) for a faction's controlled nodes.
    // Each good gets SurplusAmount added or DeficitAmount subtracted from initial inventory.
    // Expanded to cover all 12 goods — organics/ore added to faction biases so every good
    // has at least one faction surplus and one deficit, creating geographic price variation.
    public static (string[] Surplus, string[] Deficit) GetMarketBias(string factionId)
    {
        return factionId switch
        {
            FactionTweaksV0.ConcordId   => (new[] { "food", "fuel", "organics" },           new[] { "composites", "rare_metals" }),
            FactionTweaksV0.ChitinId    => (new[] { "electronics", "components" },           new[] { "rare_metals", "organics" }),
            FactionTweaksV0.WeaversId   => (new[] { "composites", "metal", "organics" },     new[] { "electronics", "munitions" }),
            FactionTweaksV0.ValorinId   => (new[] { "rare_metals", "munitions", "ore" },     new[] { "exotic_crystals", "food" }),
            FactionTweaksV0.CommunionId => (new[] { "exotic_crystals" },                     new[] { "food", "fuel", "ore" }),
            _ => (new[] { "ore", "fuel" },                                                    new[] { "metal" }),
        };
    }
}
