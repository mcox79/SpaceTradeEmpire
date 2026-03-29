using SimCore.Tweaks;

namespace SimCore.Commands;

public class PlayerArriveCommand : ICommand
{
    public string TargetNodeId { get; set; } = "";

    public PlayerArriveCommand(string targetNodeId)
    {
        TargetNodeId = targetNodeId ?? "";
    }

    public void Execute(SimState state)
    {
        if (string.IsNullOrWhiteSpace(TargetNodeId)) return;
        if (!state.Nodes.ContainsKey(TargetNodeId)) return;

        // GATE.T67.ECON.SINK_PARITY.001: Charge transit fee on teleport arrival (parity with TravelCommand).
        // Headless bots use PlayerArriveCommand (instant teleport) instead of TravelCommand + MovementSystem.
        // Without this fee, bot sink_faucet reads 0.001 instead of the real ~0.04.
        string previousNode = state.PlayerLocationNodeId ?? "";
        if (!string.IsNullOrEmpty(previousNode) && previousNode != TargetNodeId)
        {
            // Find connecting edge to compute transit cost.
            Entities.Edge? connectingEdge = null;
            foreach (var edge in state.Edges.Values)
            {
                if ((edge.FromNodeId == previousNode && edge.ToNodeId == TargetNodeId) ||
                    (edge.FromNodeId == TargetNodeId && edge.ToNodeId == previousNode))
                {
                    connectingEdge = edge;
                    break;
                }
            }
            if (connectingEdge != null)
            {
                int cost = TravelCommand.ComputeTransitCost(connectingEdge);
                if (cost > 0 && state.PlayerCredits >= cost)
                    state.PlayerCredits -= cost;
            }

            // Register arrival so FleetUpkeepSystem charges docking fee.
            state.ArrivalsThisTick.Add(("fleet_trader_1", "", TargetNodeId));
        }

        state.PlayerLocationNodeId = TargetNodeId;

        // Keep player fleet entity in sync with PlayerLocationNodeId.
        if (state.Fleets.TryGetValue("fleet_trader_1", out var playerFleet))
        {
            playerFleet.CurrentNodeId = TargetNodeId;
            playerFleet.State = Entities.FleetState.Idle;
        }

        bool isNew = state.PlayerVisitedNodeIds.Add(TargetNodeId);
        // GATE.S12.PROGRESSION.STATS.001: Track nodes visited.
        if (isNew && state.PlayerStats != null)
            state.PlayerStats.NodesVisited = state.PlayerVisitedNodeIds.Count;

        // GATE.T67.FO.SILENCE_DECISIONS.001: Increment decision counter for FO silence tracking.
        if (state.FirstOfficer != null) state.FirstOfficer.DecisionsSinceLastLine++;

        // GATE.T67.PACING.STREAK_BREAKER.001: Record action type for streak tracking.
        if (state.PlayerStats != null)
        {
            string actionType = "travel";
            if (string.Equals(state.PlayerStats.LastActionType, actionType, System.StringComparison.Ordinal))
                state.PlayerStats.ConsecutiveActionStreak++;
            else
            {
                state.PlayerStats.LastActionType = actionType;
                state.PlayerStats.ConsecutiveActionStreak = 1; // STRUCTURAL: first action of new type
            }
        }
    }
}
