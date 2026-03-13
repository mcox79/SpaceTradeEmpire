using System.Collections.Generic;
using SimCore.Entities;

namespace SimCore.Content;

// GATE.S1.MISSION.CONTENT.001: Static mission registry (content data layer).
// Content definitions live here, not in SimCore/Systems/, per Tweak Routing Policy.
public static class MissionContentV0
{
    // GATE.S9.MISSION_EVOL.REWARDS.001: Test support — mutable mission list backing the readonly view.
    private static readonly List<MissionDef> _missions = new();
    public static readonly IReadOnlyList<MissionDef> AllMissions = _missions;

    // Test-only: register/unregister custom mission definitions.
    public static void RegisterTestMission(MissionDef def) { _missions.Add(def); }
    public static void UnregisterTestMission(string missionId) { _missions.RemoveAll(m => m.MissionId == missionId); }

    static MissionContentV0()
    {
        _missions.AddRange(_builtInMissions);
    }

    private static readonly List<MissionDef> _builtInMissions = new List<MissionDef>
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
        // GATE.S9.MISSIONS.MINING_CONTENT.001: Mining survey — introduces resource extraction gameplay.
        new MissionDef
        {
            MissionId = "mission_mining_survey",
            Title = "Mining Survey",
            Description = "A nearby system has untapped mineral deposits. Acquire composites for mining equipment, travel to the site, and deploy extraction infrastructure.",
            Prerequisites = new List<string> { "mission_matched_luggage" },
            CreditReward = 200,
            Steps = new List<MissionStepDef>
            {
                new MissionStepDef
                {
                    StepIndex = 0,
                    ObjectiveText = "Acquire composites for mining equipment",
                    TriggerType = MissionTriggerType.HaveCargoMin,
                    TargetGoodId = "composites",
                    TargetQuantity = 2,
                },
                new MissionStepDef
                {
                    StepIndex = 1,
                    ObjectiveText = "Travel to mining site",
                    TriggerType = MissionTriggerType.ArriveAtNode,
                    TargetNodeId = "$ADJACENT_1",
                },
                new MissionStepDef
                {
                    StepIndex = 2,
                    ObjectiveText = "Deploy mining equipment at site",
                    TriggerType = MissionTriggerType.NoCargoAtNode,
                    TargetNodeId = "$ADJACENT_1",
                    TargetGoodId = "composites",
                },
            }
        },
        // GATE.S9.MISSIONS.MINING_CONTENT.001: Ore extraction run — follow-up mining mission.
        new MissionDef
        {
            MissionId = "mission_ore_extraction",
            Title = "Ore Extraction Run",
            Description = "The mining equipment is operational. Travel to the extraction site, collect the ore output, and deliver it to a buyer.",
            Prerequisites = new List<string> { "mission_mining_survey" },
            CreditReward = 250,
            Steps = new List<MissionStepDef>
            {
                new MissionStepDef
                {
                    StepIndex = 0,
                    ObjectiveText = "Travel to extraction site",
                    TriggerType = MissionTriggerType.ArriveAtNode,
                    TargetNodeId = "$ADJACENT_1",
                },
                new MissionStepDef
                {
                    StepIndex = 1,
                    ObjectiveText = "Collect extracted ore",
                    TriggerType = MissionTriggerType.HaveCargoMin,
                    TargetGoodId = "ore",
                    TargetQuantity = 5,
                },
                new MissionStepDef
                {
                    StepIndex = 2,
                    ObjectiveText = "Deliver ore to buyer",
                    TriggerType = MissionTriggerType.NoCargoAtNode,
                    TargetNodeId = "$PLAYER_START",
                    TargetGoodId = "ore",
                },
            }
        },
        // GATE.S9.MISSIONS.RESEARCH_CONTENT.001: First research mission — introduces tech unlock.
        new MissionDef
        {
            MissionId = "mission_first_research",
            Title = "First Research",
            Description = "A station's tech lab offers research access. Acquire electronics for lab calibration, dock at the research station, and deliver the equipment to start a research project.",
            Prerequisites = new List<string> { "mission_mining_survey" },
            CreditReward = 300,
            Steps = new List<MissionStepDef>
            {
                new MissionStepDef
                {
                    StepIndex = 0,
                    ObjectiveText = "Acquire electronics for lab calibration",
                    TriggerType = MissionTriggerType.HaveCargoMin,
                    TargetGoodId = "electronics",
                    TargetQuantity = 2,
                },
                new MissionStepDef
                {
                    StepIndex = 1,
                    ObjectiveText = "Dock at research station",
                    TriggerType = MissionTriggerType.ArriveAtNode,
                    TargetNodeId = "$ADJACENT_1",
                },
                new MissionStepDef
                {
                    StepIndex = 2,
                    ObjectiveText = "Deliver electronics to initiate research",
                    TriggerType = MissionTriggerType.NoCargoAtNode,
                    TargetNodeId = "$ADJACENT_1",
                    TargetGoodId = "electronics",
                },
            }
        },
        // GATE.S9.MISSIONS.RESEARCH_CONTENT.001: Follow-up research delivery.
        new MissionDef
        {
            MissionId = "mission_research_materials",
            Title = "Research Materials",
            Description = "The research station's project needs exotic matter to continue. Source exotic matter and deliver it to advance the tech unlock.",
            Prerequisites = new List<string> { "mission_first_research" },
            CreditReward = 400,
            Steps = new List<MissionStepDef>
            {
                new MissionStepDef
                {
                    StepIndex = 0,
                    ObjectiveText = "Acquire exotic matter",
                    TriggerType = MissionTriggerType.HaveCargoMin,
                    TargetGoodId = "exotic_matter",
                    TargetQuantity = 1,
                },
                new MissionStepDef
                {
                    StepIndex = 1,
                    ObjectiveText = "Deliver to research station",
                    TriggerType = MissionTriggerType.ArriveAtNode,
                    TargetNodeId = "$ADJACENT_1",
                },
                new MissionStepDef
                {
                    StepIndex = 2,
                    ObjectiveText = "Complete exotic matter delivery",
                    TriggerType = MissionTriggerType.NoCargoAtNode,
                    TargetNodeId = "$ADJACENT_1",
                    TargetGoodId = "exotic_matter",
                },
            }
        },
        // GATE.S9.MISSIONS.CONSTRUCTION_CONTENT.001: First construction mission.
        new MissionDef
        {
            MissionId = "mission_first_build",
            Title = "First Build",
            Description = "A frontier station needs structural materials for expansion. Acquire composites and rare metals, transport them to the construction site, and deliver the materials.",
            Prerequisites = new List<string> { "mission_first_research" },
            CreditReward = 350,
            Steps = new List<MissionStepDef>
            {
                new MissionStepDef
                {
                    StepIndex = 0,
                    ObjectiveText = "Acquire composites for construction",
                    TriggerType = MissionTriggerType.HaveCargoMin,
                    TargetGoodId = "composites",
                    TargetQuantity = 3,
                },
                new MissionStepDef
                {
                    StepIndex = 1,
                    ObjectiveText = "Travel to construction site",
                    TriggerType = MissionTriggerType.ArriveAtNode,
                    TargetNodeId = "$ADJACENT_1",
                },
                new MissionStepDef
                {
                    StepIndex = 2,
                    ObjectiveText = "Deliver construction materials",
                    TriggerType = MissionTriggerType.NoCargoAtNode,
                    TargetNodeId = "$ADJACENT_1",
                    TargetGoodId = "composites",
                },
            }
        },
        // GATE.S9.MISSIONS.CONSTRUCTION_CONTENT.001: Large-scale construction follow-up.
        new MissionDef
        {
            MissionId = "mission_station_expansion",
            Title = "Station Expansion",
            Description = "The frontier station's expansion needs rare metals for structural reinforcement. A larger delivery with a bigger payoff.",
            Prerequisites = new List<string> { "mission_first_build" },
            CreditReward = 500,
            Steps = new List<MissionStepDef>
            {
                new MissionStepDef
                {
                    StepIndex = 0,
                    ObjectiveText = "Acquire rare metals",
                    TriggerType = MissionTriggerType.HaveCargoMin,
                    TargetGoodId = "rare_metals",
                    TargetQuantity = 3,
                },
                new MissionStepDef
                {
                    StepIndex = 1,
                    ObjectiveText = "Transport to expansion site",
                    TriggerType = MissionTriggerType.ArriveAtNode,
                    TargetNodeId = "$ADJACENT_1",
                },
                new MissionStepDef
                {
                    StepIndex = 2,
                    ObjectiveText = "Deliver rare metals to station",
                    TriggerType = MissionTriggerType.NoCargoAtNode,
                    TargetNodeId = "$ADJACENT_1",
                    TargetGoodId = "rare_metals",
                },
            }
        },
    };
}
