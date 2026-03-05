using System.Collections.Generic;
using SimCore.Entities;

namespace SimCore.Content;

// GATE.S1.MISSION.CONTENT.001: Static mission registry (content data layer).
// Content definitions live here, not in SimCore/Systems/, per Tweak Routing Policy.
public static class MissionContentV0
{
    public static readonly IReadOnlyList<MissionDef> AllMissions = new List<MissionDef>
    {
        new MissionDef
        {
            MissionId = "mission_matched_luggage",
            Title = "Matched Luggage",
            Description = "A simple trade run: buy goods at your starting station, travel to a neighbor, and sell them for profit.",
            Prerequisites = new List<string>(),
            CreditReward = 50,
            Steps = new List<MissionStepDef>
            {
                new MissionStepDef
                {
                    StepIndex = 0,
                    ObjectiveText = "Dock at starting station",
                    TriggerType = MissionTriggerType.ArriveAtNode,
                    TargetNodeId = "$PLAYER_START",
                },
                new MissionStepDef
                {
                    StepIndex = 1,
                    ObjectiveText = "Buy cargo",
                    TriggerType = MissionTriggerType.HaveCargoMin,
                    TargetGoodId = "$MARKET_GOOD_1",
                    TargetQuantity = 1,
                },
                new MissionStepDef
                {
                    StepIndex = 2,
                    ObjectiveText = "Travel to destination",
                    TriggerType = MissionTriggerType.ArriveAtNode,
                    TargetNodeId = "$ADJACENT_1",
                },
                new MissionStepDef
                {
                    StepIndex = 3,
                    ObjectiveText = "Sell cargo at destination",
                    TriggerType = MissionTriggerType.NoCargoAtNode,
                    TargetNodeId = "$ADJACENT_1",
                    TargetGoodId = "$MARKET_GOOD_1",
                },
            }
        },
        // GATE.S1.MISSION.CONTENT_WAVE.001: hauling mission — bulk cargo delivery
        new MissionDef
        {
            MissionId = "mission_bulk_hauler",
            Title = "Bulk Hauler",
            Description = "A station needs a large fuel shipment. Buy fuel and deliver it to the neighboring system.",
            Prerequisites = new List<string>(),
            CreditReward = 80,
            Steps = new List<MissionStepDef>
            {
                new MissionStepDef
                {
                    StepIndex = 0,
                    ObjectiveText = "Acquire fuel",
                    TriggerType = MissionTriggerType.HaveCargoMin,
                    TargetGoodId = "fuel",
                    TargetQuantity = 3,
                },
                new MissionStepDef
                {
                    StepIndex = 1,
                    ObjectiveText = "Deliver fuel to destination",
                    TriggerType = MissionTriggerType.ArriveAtNode,
                    TargetNodeId = "$ADJACENT_1",
                },
                new MissionStepDef
                {
                    StepIndex = 2,
                    ObjectiveText = "Sell fuel at destination",
                    TriggerType = MissionTriggerType.NoCargoAtNode,
                    TargetNodeId = "$ADJACENT_1",
                    TargetGoodId = "fuel",
                },
            }
        },
        // GATE.S1.MISSION.CONTENT_WAVE.001: combat patrol — clear hostiles near a system
        new MissionDef
        {
            MissionId = "mission_patrol_duty",
            Title = "Patrol Duty",
            Description = "Hostile fleets threaten trade lanes. Travel to the target system and eliminate any threats.",
            Prerequisites = new List<string>(),
            CreditReward = 120,
            Steps = new List<MissionStepDef>
            {
                new MissionStepDef
                {
                    StepIndex = 0,
                    ObjectiveText = "Travel to patrol zone",
                    TriggerType = MissionTriggerType.ArriveAtNode,
                    TargetNodeId = "$ADJACENT_1",
                },
                new MissionStepDef
                {
                    StepIndex = 1,
                    ObjectiveText = "Return to base",
                    TriggerType = MissionTriggerType.ArriveAtNode,
                    TargetNodeId = "$PLAYER_START",
                },
            }
        },
        // GATE.S1.MISSION.CONTENT_WAVE.001: multi-hop delivery — longer trade route
        new MissionDef
        {
            MissionId = "mission_long_haul",
            Title = "Long Haul Express",
            Description = "A merchant consortium needs ore moved across two systems. Pick up ore, travel through a waypoint, and deliver to the final destination.",
            Prerequisites = new List<string> { "mission_matched_luggage" },
            CreditReward = 150,
            Steps = new List<MissionStepDef>
            {
                new MissionStepDef
                {
                    StepIndex = 0,
                    ObjectiveText = "Acquire ore",
                    TriggerType = MissionTriggerType.HaveCargoMin,
                    TargetGoodId = "ore",
                    TargetQuantity = 5,
                },
                new MissionStepDef
                {
                    StepIndex = 1,
                    ObjectiveText = "Travel to waypoint",
                    TriggerType = MissionTriggerType.ArriveAtNode,
                    TargetNodeId = "$ADJACENT_1",
                },
                new MissionStepDef
                {
                    StepIndex = 2,
                    ObjectiveText = "Sell ore at destination",
                    TriggerType = MissionTriggerType.NoCargoAtNode,
                    TargetNodeId = "$ADJACENT_1",
                    TargetGoodId = "ore",
                },
            }
        },
    };
}
