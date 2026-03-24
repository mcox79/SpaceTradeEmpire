using SimCore;
using SimCore.Content;
using SimCore.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimCore.RlServer;

/// <summary>
/// Extracts a fixed-size observation vector from SimState.
/// All values normalized to roughly [0, 1] or [-1, 1] for stable RL training.
///
/// Layout (232 floats):
///   [0-6]     PlayerState (7)
///   [7-45]    CurrentMarketPrices (13*3=39)
///   [46-58]   PlayerCargo (13)
///   [59-136]  NeighborPrices (6*13=78)
///   [137-146] MissionState (10)
///   [147-161] HavenStatus (15)
///   [162-181] FleetRoster (4*5=20)
///   [182-186] FactionRep (5)
///   [187-191] TechTree (5)
///   [192-215] Fragments (16+8=24)
///   [216-223] EndgameProgress (8)
///   [224-226] RiskMeters (3)
///   [227-231] Discovery (5)
/// </summary>
public static class StateEncoder
{
    public static readonly string[] GoodOrder = new[]
    {
        WellKnownGoodIds.Fuel, WellKnownGoodIds.Ore, WellKnownGoodIds.Organics, WellKnownGoodIds.RareMetals,
        WellKnownGoodIds.Metal, WellKnownGoodIds.Food, WellKnownGoodIds.Composites, WellKnownGoodIds.Electronics,
        WellKnownGoodIds.Munitions, WellKnownGoodIds.Components,
        WellKnownGoodIds.ExoticCrystals, WellKnownGoodIds.SalvagedTech, WellKnownGoodIds.ExoticMatter,
    };

    public static readonly string[] FactionIds = { "concord", "chitin", "valorin", "weavers", "communion" };

    public const int GoodCount = 13;
    public const int MaxNeighbors = 6;
    public const int BaseObsSize = 7 + (GoodCount * 3) + GoodCount + (MaxNeighbors * GoodCount); // 137
    public const int ExpandedObsSize = 10 + 15 + 20 + 5 + 5 + 24 + 8 + 3 + 5; // 95
    public const int ObsSize = BaseObsSize + ExpandedObsSize; // 232

    public static float[] Encode(SimState state, int maxEpisodeTicks, List<string> neighborNodeIds)
    {
        var obs = new float[ObsSize];
        int idx = 0;

        // ── [0-6] Player state (7 floats) ──
        var playerFleet = GetPlayerFleet(state);
        int cargoCapacity = GetCargoCapacity(playerFleet);
        int totalCargo = state.PlayerCargo.Values.Sum();

        obs[idx++] = Clamp(state.PlayerCredits / 10000f, 0f, 5f);
        obs[idx++] = cargoCapacity > 0 ? Clamp((float)totalCargo / cargoCapacity, 0f, 1f) : 0f;
        obs[idx++] = playerFleet != null && playerFleet.HullHpMax > 0
            ? Clamp((float)playerFleet.HullHp / playerFleet.HullHpMax, 0f, 1f) : 1f;
        obs[idx++] = playerFleet != null && playerFleet.ShieldHpMax > 0
            ? Clamp((float)playerFleet.ShieldHp / playerFleet.ShieldHpMax, 0f, 1f) : 0f;
        obs[idx++] = playerFleet != null && playerFleet.FuelCapacity > 0
            ? Clamp((float)playerFleet.FuelCurrent / playerFleet.FuelCapacity, 0f, 1f) : 0f;
        obs[idx++] = maxEpisodeTicks > 0 ? Clamp((float)state.Tick / maxEpisodeTicks, 0f, 1f) : 0f;
        obs[idx++] = state.Nodes.Count > 0
            ? Clamp((float)state.PlayerVisitedNodeIds.Count / state.Nodes.Count, 0f, 1f) : 0f;

        // ── [7-45] Current market prices (39 floats) ──
        Market? currentMarket = GetMarketAtNode(state, state.PlayerLocationNodeId);
        for (int g = 0; g < GoodCount; g++)
        {
            if (currentMarket != null && currentMarket.Inventory.ContainsKey(GoodOrder[g]))
            {
                obs[idx++] = Clamp(currentMarket.Inventory[GoodOrder[g]] / (float)Market.IdealStock, 0f, 4f);
                obs[idx++] = Clamp(currentMarket.GetBuyPrice(GoodOrder[g]) / 200f, 0f, 3f);
                obs[idx++] = Clamp(currentMarket.GetSellPrice(GoodOrder[g]) / 200f, 0f, 3f);
            }
            else
            {
                obs[idx++] = 0f; obs[idx++] = 0f; obs[idx++] = 0f;
            }
        }

        // ── [46-58] Player cargo (13 floats) ──
        for (int g = 0; g < GoodCount; g++)
        {
            int held = state.PlayerCargo.TryGetValue(GoodOrder[g], out var v) ? v : 0;
            obs[idx++] = cargoCapacity > 0 ? Clamp((float)held / cargoCapacity, 0f, 1f) : 0f;
        }

        // ── [59-136] Neighbor sell prices (78 floats) ──
        for (int n = 0; n < MaxNeighbors; n++)
        {
            if (n < neighborNodeIds.Count)
            {
                Market? nMarket = GetMarketAtNode(state, neighborNodeIds[n]);
                for (int g = 0; g < GoodCount; g++)
                {
                    if (nMarket != null && nMarket.Inventory.ContainsKey(GoodOrder[g]))
                        obs[idx++] = Clamp(nMarket.GetSellPrice(GoodOrder[g]) / 200f, 0f, 3f);
                    else
                        obs[idx++] = 0f;
                }
            }
            else
            {
                for (int g = 0; g < GoodCount; g++)
                    obs[idx++] = 0f;
            }
        }

        // ── [137-146] Mission state (10 floats) ──
        idx = EncodeMissionState(state, obs, idx);

        // ── [147-161] Haven status (15 floats) ──
        idx = EncodeHavenStatus(state, obs, idx);

        // ── [162-181] Fleet roster (20 floats) ──
        idx = EncodeFleetRoster(state, obs, idx);

        // ── [182-186] Faction rep (5 floats) ──
        idx = EncodeFactionRep(state, obs, idx);

        // ── [187-191] Tech tree (5 floats) ──
        idx = EncodeTechTree(state, obs, idx);

        // ── [192-215] Fragments (24 floats) ──
        idx = EncodeFragments(state, obs, idx);

        // ── [216-223] Endgame progress (8 floats) ──
        idx = EncodeEndgame(state, obs, idx);

        // ── [224-226] Risk meters (3 floats) ──
        idx = EncodeRiskMeters(state, obs, idx);

        // ── [227-231] Discovery (5 floats) ──
        idx = EncodeDiscovery(state, obs, idx);

        return obs;
    }

    static int EncodeMissionState(SimState state, float[] obs, int idx)
    {
        bool hasActive = !string.IsNullOrEmpty(state.Missions?.ActiveMissionId);
        obs[idx++] = hasActive ? 1f : 0f;
        if (hasActive && state.Missions != null)
        {
            int total = state.Missions.ActiveSteps?.Count ?? 1;
            obs[idx++] = Clamp((float)state.Missions.CurrentStepIndex / Math.Max(total, 1), 0f, 1f);
        }
        else
        {
            obs[idx++] = 0f;
        }
        idx += 3; // mission type one-hot (placeholder)
        // Systemic offers count
        obs[idx++] = Clamp((state.SystemicOffers?.Count ?? 0) / 10f, 0f, 1f);
        // Available missions count
        obs[idx++] = 0f; // would need MissionSystem.GetAvailableMissions — approximate
        idx += 3; // padding
        return idx;
    }

    static int EncodeHavenStatus(SimState state, float[] obs, int idx)
    {
        var haven = state.Haven;
        obs[idx++] = haven != null && haven.Discovered ? 1f : 0f;
        obs[idx++] = haven != null ? Clamp((float)(int)haven.Tier / 5f, 0f, 1f) : 0f;
        obs[idx++] = haven != null ? Clamp(haven.UpgradeTicksRemaining / 200f, 0f, 1f) : 0f;
        obs[idx++] = haven != null ? Clamp(haven.StoredShipIds.Count / 3f, 0f, 1f) : 0f;
        obs[idx++] = haven != null ? Clamp(haven.InstalledFragmentIds.Count / 16f, 0f, 1f) : 0f;
        // Fabricator
        obs[idx++] = haven != null && (int)haven.Tier >= 4 ? 1f : 0f; // fab available
        obs[idx++] = haven != null && !string.IsNullOrEmpty(haven.FabricatingModuleId) ? 1f : 0f;
        obs[idx++] = haven != null ? Clamp(haven.FabricationTicksRemaining / 100f, 0f, 1f) : 0f;
        // Research
        var tech = state.Tech;
        obs[idx++] = tech != null && !string.IsNullOrEmpty(tech.CurrentResearchTechId) ? 1f : 0f;
        float rProg = tech?.ResearchProgressTicks ?? 0;
        float rTotal = tech?.ResearchTotalTicks ?? 1;
        obs[idx++] = Clamp(rProg / Math.Max(rTotal, 1f), 0f, 1f);
        obs[idx++] = Clamp((tech?.TechLevel ?? 0) / 5f, 0f, 1f);
        idx += 4; // padding
        return idx;
    }

    static int EncodeFleetRoster(SimState state, float[] obs, int idx)
    {
        var playerFleets = state.Fleets.Values
            .Where(f => string.Equals(f.OwnerId, "player", StringComparison.Ordinal))
            .OrderBy(f => f.Id, StringComparer.Ordinal)
            .Take(4)
            .ToList();

        for (int i = 0; i < 4; i++)
        {
            if (i < playerFleets.Count)
            {
                var f = playerFleets[i];
                obs[idx++] = f.HullHpMax > 0 ? Clamp((float)f.HullHp / f.HullHpMax, 0f, 1f) : 1f;
                int cap = GetCargoCapacity(f);
                int cargo = 0; // fleet cargo not tracked per-fleet in PlayerCargo
                obs[idx++] = cap > 0 ? Clamp((float)cargo / cap, 0f, 1f) : 0f;
                obs[idx++] = f.Id == "fleet_trader_1" ? 1f : 0f;
                obs[idx++] = f.CurrentNodeId == state.PlayerLocationNodeId ? 1f : 0f;
                obs[idx++] = f.State != FleetState.Idle ? 1f : 0f;
            }
            else
            {
                idx += 5;
            }
        }
        return idx;
    }

    static int EncodeFactionRep(SimState state, float[] obs, int idx)
    {
        foreach (var fid in FactionIds)
        {
            float rep = 0f;
            if (state.FactionReputation.TryGetValue(fid, out var r))
                rep = r;
            obs[idx++] = Clamp(rep / 100f, -1f, 1f);
        }
        return idx;
    }

    static int EncodeTechTree(SimState state, float[] obs, int idx)
    {
        obs[idx++] = Clamp((state.Tech?.TechLevel ?? 0) / 5f, 0f, 1f);
        int unlocked = state.Tech?.UnlockedTechIds?.Count ?? 0;
        int total = TechContentV0.AllTechs?.Count ?? 1;
        obs[idx++] = Clamp((float)unlocked / Math.Max(total, 1), 0f, 1f);
        idx += 3; // padding
        return idx;
    }

    static int EncodeFragments(SimState state, float[] obs, int idx)
    {
        // 16 collected flags — fragments are in AdaptationFragments dict, collected when CollectedTick >= 0
        var fragments = state.AdaptationFragments;
        for (int i = 0; i < 16; i++)
        {
            string fragId = $"frag_{i}";
            bool isCollected = fragments != null && fragments.TryGetValue(fragId, out var frag) && frag.CollectedTick >= 0;
            obs[idx++] = isCollected ? 1f : 0f;
        }
        // 8 resonance pair flags
        var deposited = state.Haven?.InstalledFragmentIds ?? new List<string>();
        for (int i = 0; i < 8; i++)
        {
            obs[idx++] = 0f; // resonance pairs need ResonanceContentV0 — stub
        }
        return idx;
    }

    static int EncodeEndgame(SimState state, float[] obs, int idx)
    {
        var haven = state.Haven;
        var path = haven?.ChosenEndgamePath ?? EndgamePath.None;
        obs[idx++] = path != EndgamePath.None ? 1f : 0f;
        obs[idx++] = path == EndgamePath.Reinforce ? 1f : 0f;
        obs[idx++] = path == EndgamePath.Naturalize ? 1f : 0f;
        obs[idx++] = path == EndgamePath.Renegotiate ? 1f : 0f;
        obs[idx++] = 0f; // EndgameCompletionPercent not available — stub
        obs[idx++] = state.GameResultValue != GameResult.InProgress ? 1f : 0f;
        obs[idx++] = state.GameResultValue == GameResult.Victory ? 1f : 0f;
        obs[idx++] = state.GameResultValue == GameResult.Death ? 1f : 0f;
        return idx;
    }

    static int EncodeRiskMeters(SimState state, float[] obs, int idx)
    {
        // Risk meters are per-node/edge, not top-level. Use 0f defaults.
        obs[idx++] = 0f;
        obs[idx++] = 0f;
        obs[idx++] = 0f;
        return idx;
    }

    static int EncodeDiscovery(SimState state, float[] obs, int idx)
    {
        obs[idx++] = Clamp(state.ScannerChargesUsed / 5f, 0f, 1f);
        obs[idx++] = Clamp((state.AnomalyChains?.Count ?? 0) / 5f, 0f, 1f);
        obs[idx++] = state.Nodes.Count > 0
            ? Clamp((float)state.PlayerVisitedNodeIds.Count / state.Nodes.Count, 0f, 1f) : 0f;
        idx += 2; // padding
        return idx;
    }

    public static bool[] ComputeActionMask(SimState state, List<string> neighborNodeIds)
    {
        var mask = new bool[ActionDecoder.TotalActions];

        // WAIT always valid
        mask[0] = true;

        var playerFleet = GetPlayerFleet(state);
        bool isIdle = playerFleet != null && playerFleet.State == FleetState.Idle;
        Market? market = GetMarketAtNode(state, state.PlayerLocationNodeId);
        int cargoCapacity = GetCargoCapacity(playerFleet);
        int totalCargo = state.PlayerCargo.Values.Sum();

        // BUY (1-13)
        for (int g = 0; g < GoodCount; g++)
        {
            mask[1 + g] = isIdle && market != null
                && market.Inventory.ContainsKey(GoodOrder[g])
                && market.Inventory[GoodOrder[g]] > 0
                && state.PlayerCredits > 0
                && totalCargo < cargoCapacity;
        }

        // SELL (14-26)
        for (int g = 0; g < GoodCount; g++)
        {
            mask[14 + g] = isIdle && market != null
                && state.PlayerCargo.TryGetValue(GoodOrder[g], out var held) && held > 0;
        }

        // TRAVEL (27-32)
        for (int n = 0; n < MaxNeighbors; n++)
            mask[27 + n] = isIdle && n < neighborNodeIds.Count;

        // COMBAT (33)
        if (isIdle && playerFleet != null)
        {
            var hostile = state.Fleets.Values.FirstOrDefault(f =>
                !string.Equals(f.OwnerId, "player", StringComparison.Ordinal)
                && string.Equals(f.CurrentNodeId, state.PlayerLocationNodeId, StringComparison.Ordinal)
                && f.HullHp > 0);
            mask[33] = hostile != null;
        }

        // ACCEPT_MISSION (34) — available if no active mission and offers exist
        mask[34] = isIdle && string.IsNullOrEmpty(state.Missions?.ActiveMissionId)
            && (state.SystemicOffers?.Count ?? 0) > 0;

        // ABANDON_MISSION (35)
        mask[35] = !string.IsNullOrEmpty(state.Missions?.ActiveMissionId);

        // UPGRADE_HAVEN (36)
        var haven = state.Haven;
        mask[36] = isIdle && haven != null && haven.Discovered && (int)haven.Tier < 5
            && haven.UpgradeTicksRemaining <= 0;

        // START_RESEARCH (37)
        mask[37] = isIdle && string.IsNullOrEmpty(state.Tech?.CurrentResearchTechId)
            && (state.Tech?.UnlockedTechIds?.Count ?? 0) < (TechContentV0.AllTechs?.Count ?? 0);

        // CHOOSE_ENDGAME_REINFORCE (38), NATURALIZE (39), RENEGOTIATE (40)
        bool canChoose = haven != null && (int)haven.Tier >= 4 && haven.ChosenEndgamePath == EndgamePath.None;
        mask[38] = canChoose;
        mask[39] = canChoose;
        mask[40] = canChoose;

        // Action 41 removed (was CAPTURE_SHIP — mechanic cut)
        if (isIdle && haven != null && (int)haven.Tier >= 3)
        {
            var weakHostile = state.Fleets.Values.FirstOrDefault(f =>
                !string.Equals(f.OwnerId, "player", StringComparison.Ordinal)
                && string.Equals(f.CurrentNodeId, state.PlayerLocationNodeId, StringComparison.Ordinal)
                && f.HullHp > 0 && f.HullHpMax > 0
                && (float)f.HullHp / f.HullHpMax < 0.1f);
            mask[41] = weakHostile != null;
        }

        return mask;
    }

    // ── Helpers ──

    public static Fleet? GetPlayerFleet(SimState state) =>
        state.Fleets.TryGetValue("fleet_trader_1", out var f) ? f : null;

    public static int GetCargoCapacity(Fleet? playerFleet)
    {
        if (playerFleet == null) return 50;
        var classDef = ShipClassContentV0.GetById(playerFleet.ShipClassId);
        return classDef?.CargoCapacity ?? 50;
    }

    public static Market? GetMarketAtNode(SimState state, string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId)) return null;
        if (!state.Nodes.TryGetValue(nodeId, out var node)) return null;
        if (string.IsNullOrEmpty(node.MarketId)) return null;
        return state.Markets.TryGetValue(node.MarketId, out var m) ? m : null;
    }

    public static List<string> GetNeighborNodeIds(SimState state, string nodeId)
    {
        var neighbors = new List<string>();
        foreach (var edge in state.Edges.Values.OrderBy(e => e.Id, StringComparer.Ordinal))
        {
            if (string.Equals(edge.FromNodeId, nodeId, StringComparison.Ordinal))
                neighbors.Add(edge.ToNodeId);
            else if (string.Equals(edge.ToNodeId, nodeId, StringComparison.Ordinal))
                neighbors.Add(edge.FromNodeId);

            if (neighbors.Count >= MaxNeighbors) break;
        }
        return neighbors;
    }

    private static float Clamp(float val, float min, float max) =>
        val < min ? min : (val > max ? max : val);
}
