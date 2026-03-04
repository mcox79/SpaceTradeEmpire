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
    };
}
