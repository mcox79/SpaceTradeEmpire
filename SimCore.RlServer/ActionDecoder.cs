using SimCore;
using SimCore.Commands;
using SimCore.Content;
using SimCore.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimCore.RlServer;

/// <summary>
/// Maps a discrete action integer to SimCore commands.
///
/// Action layout (42 total):
///   0       = WAIT (advance 5 ticks)
///   1-13    = BUY good[i] at current market
///   14-26   = SELL good[i] at current market
///   27-32   = TRAVEL to neighbor[j]
///   33      = COMBAT first hostile at current node
///   34      = ACCEPT_MISSION (first systemic offer)
///   35      = ABANDON_MISSION
///   36      = UPGRADE_HAVEN
///   37      = START_RESEARCH (next available tech)
///   38      = CHOOSE_ENDGAME_REINFORCE
///   39      = CHOOSE_ENDGAME_NATURALIZE
///   40      = CHOOSE_ENDGAME_RENEGOTIATE
///   41      = CAPTURE_SHIP (weak hostile at current node)
/// </summary>
public static class ActionDecoder
{
    public const int TotalActions = 42;
    public const int WaitTicks = 5;
    public const int MaxTravelTicks = 50;
    public const int MaxBuyQty = 10;

    public struct ActionResult
    {
        public int TicksConsumed;
        public string ActionLabel;
        public float TradeProfit;
        public bool NewNodeVisited;
        public bool MissionCompleted;
        public bool HavenUpgraded;
        public bool ResearchCompleted;
    }

    public static ActionResult Execute(SimKernel kernel, int action, List<string> neighborNodeIds)
    {
        var state = kernel.State;
        var result = new ActionResult();

        if (action == 0)
        {
            for (int i = 0; i < WaitTicks; i++) kernel.Step();
            result.TicksConsumed = WaitTicks;
            result.ActionLabel = "wait";
            return result;
        }

        if (action >= 1 && action <= StateEncoder.GoodCount)
        {
            int goodIdx = action - 1;
            string goodId = StateEncoder.GoodOrder[goodIdx];
            var market = GetCurrentMarket(state);
            if (market != null)
            {
                long creditsBefore = state.PlayerCredits;
                kernel.EnqueueCommand(new BuyCommand(market.Id, goodId, MaxBuyQty));
                kernel.Step();
                result.TradeProfit = state.PlayerCredits - creditsBefore;
                result.ActionLabel = $"buy_{goodId}";
            }
            else
            {
                kernel.Step();
                result.ActionLabel = "buy_noop";
            }
            result.TicksConsumed = 1;
            return result;
        }

        if (action >= 14 && action <= 13 + StateEncoder.GoodCount)
        {
            int goodIdx = action - 14;
            string goodId = StateEncoder.GoodOrder[goodIdx];
            int held = state.PlayerCargo.TryGetValue(goodId, out var v) ? v : 0;
            var market = GetCurrentMarket(state);
            if (market != null && held > 0)
            {
                long creditsBefore = state.PlayerCredits;
                kernel.EnqueueCommand(new SellCommand(market.Id, goodId, held));
                kernel.Step();
                result.TradeProfit = state.PlayerCredits - creditsBefore;
                result.ActionLabel = $"sell_{goodId}";
            }
            else
            {
                kernel.Step();
                result.ActionLabel = "sell_noop";
            }
            result.TicksConsumed = 1;
            return result;
        }

        if (action >= 27 && action <= 26 + StateEncoder.MaxNeighbors)
        {
            int neighborIdx = action - 27;
            if (neighborIdx < neighborNodeIds.Count)
            {
                string targetNodeId = neighborNodeIds[neighborIdx];
                var playerFleet = StateEncoder.GetPlayerFleet(state);
                if (playerFleet != null && playerFleet.State == FleetState.Idle)
                {
                    int visitedBefore = state.PlayerVisitedNodeIds.Count;
                    kernel.EnqueueCommand(new TravelCommand("fleet_trader_1", targetNodeId));
                    kernel.Step();
                    result.TicksConsumed = 1;

                    for (int i = 0; i < MaxTravelTicks; i++)
                    {
                        if (playerFleet.State == FleetState.Idle) break;
                        kernel.Step();
                        result.TicksConsumed++;
                    }

                    if (playerFleet.State == FleetState.Idle)
                    {
                        kernel.EnqueueCommand(new PlayerArriveCommand(playerFleet.CurrentNodeId));
                        kernel.Step();
                        result.TicksConsumed++;
                    }

                    result.NewNodeVisited = state.PlayerVisitedNodeIds.Count > visitedBefore;
                    result.ActionLabel = $"travel_{targetNodeId}";
                }
                else
                {
                    kernel.Step();
                    result.TicksConsumed = 1;
                    result.ActionLabel = "travel_noop";
                }
            }
            else
            {
                kernel.Step();
                result.TicksConsumed = 1;
                result.ActionLabel = "travel_invalid";
            }
            return result;
        }

        if (action == 33)
        {
            var playerFleet = StateEncoder.GetPlayerFleet(state);
            var hostile = state.Fleets.Values.FirstOrDefault(f =>
                !string.Equals(f.OwnerId, "player", StringComparison.Ordinal)
                && string.Equals(f.CurrentNodeId, state.PlayerLocationNodeId, StringComparison.Ordinal)
                && f.HullHp > 0);

            if (playerFleet != null && hostile != null)
            {
                kernel.EnqueueCommand(new StartCombatCommand("fleet_trader_1", hostile.Id));
                kernel.Step();
                kernel.EnqueueCommand(new ClearCombatCommand());
                kernel.Step();
                result.TicksConsumed = 2;
                result.ActionLabel = $"combat_{hostile.Id}";
            }
            else
            {
                kernel.Step();
                result.TicksConsumed = 1;
                result.ActionLabel = "combat_noop";
            }
            return result;
        }

        if (action == 34)
        {
            // ACCEPT_MISSION — accept first systemic offer
            var offer = state.SystemicOffers?.FirstOrDefault();
            if (offer != null)
            {
                // SystemicMissionSystem handles this via AcceptSystemicMission
                // For headless, we directly manipulate mission state
                SimCore.Systems.SystemicMissionSystem.AcceptSystemicMission(state, offer.OfferId);
                kernel.Step();
                result.ActionLabel = $"accept_mission_{offer.OfferId}";
            }
            else
            {
                kernel.Step();
                result.ActionLabel = "accept_mission_noop";
            }
            result.TicksConsumed = 1;
            return result;
        }

        if (action == 35)
        {
            // ABANDON_MISSION
            if (state.Missions != null)
            {
                state.Missions.ActiveMissionId = "";
                state.Missions.ActiveSteps?.Clear();
                state.Missions.CurrentStepIndex = 0;
            }
            kernel.Step();
            result.TicksConsumed = 1;
            result.ActionLabel = "abandon_mission";
            return result;
        }

        if (action == 36)
        {
            // UPGRADE_HAVEN
            kernel.EnqueueCommand(new UpgradeHavenCommand());
            kernel.Step();
            result.TicksConsumed = 1;
            result.HavenUpgraded = true;
            result.ActionLabel = "upgrade_haven";
            return result;
        }

        if (action == 37)
        {
            // START_RESEARCH — pick next unlockable tech
            var allTechs = TechContentV0.AllTechs;
            var unlocked = state.Tech?.UnlockedTechIds ?? new HashSet<string>();
            var nextTech = allTechs?.FirstOrDefault(t => !unlocked.Contains(t.TechId));
            if (nextTech != null && state.Tech != null)
            {
                SimCore.Systems.ResearchSystem.StartResearch(state, nextTech.TechId);
                kernel.Step();
                result.ActionLabel = $"start_research_{nextTech.TechId}";
            }
            else
            {
                kernel.Step();
                result.ActionLabel = "research_noop";
            }
            result.TicksConsumed = 1;
            return result;
        }

        if (action >= 38 && action <= 40)
        {
            // CHOOSE_ENDGAME
            EndgamePath[] paths = { EndgamePath.Reinforce, EndgamePath.Naturalize, EndgamePath.Renegotiate };
            var chosen = paths[action - 38];
            var haven = state.Haven;
            if (haven != null && (int)haven.Tier >= 4 && haven.ChosenEndgamePath == EndgamePath.None)
            {
                haven.ChosenEndgamePath = chosen;
                kernel.Step();
                result.ActionLabel = $"choose_endgame_{chosen.ToString().ToLowerInvariant()}";
            }
            else
            {
                kernel.Step();
                result.ActionLabel = "endgame_noop";
            }
            result.TicksConsumed = 1;
            return result;
        }

        // Action 41 removed (was CAPTURE_SHIP — mechanic cut)

        // Unknown action
        kernel.Step();
        result.TicksConsumed = 1;
        result.ActionLabel = "unknown";
        return result;
    }

    private static Market? GetCurrentMarket(SimState state)
    {
        return StateEncoder.GetMarketAtNode(state, state.PlayerLocationNodeId);
    }
}
