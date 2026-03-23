using System.Collections.Generic;

namespace SimCore.Content;

// GATE.T48.TEMPLATE.SCHEMA.001: Mission template schema with variable slots and twist definitions.
// Templates define reusable mission archetypes that are instantiated at runtime with world-state bindings.
public static class MissionTemplateContentV0
{
    public enum Archetype
    {
        Supply = 0,
        Explore = 1,
        Combat = 2,
        Politics = 3,
    }

    public enum ObjectiveType
    {
        Deliver = 0,
        Visit = 1,
        Scan = 2,
        Destroy = 3,
        Escort = 4,
    }

    public sealed class TemplateStepDef
    {
        public int StepIndex { get; init; }
        public ObjectiveType Objective { get; init; }
        public List<string> VariableSlots { get; init; } = new();
        public string CompletionCondition { get; init; } = "";
    }

    public sealed class TwistSlotDef
    {
        public string TwistType { get; init; } = "";
        public int WeightBps { get; init; } // weight in basis points (10000 = 100%)
    }

    public sealed class RewardFormulaDef
    {
        public int BaseCredits { get; init; }
        public int PerTwistBonusBps { get; init; } // bonus multiplier per active twist in bps
    }

    public sealed class MissionTemplateDef
    {
        public string TemplateId { get; init; } = "";
        public Archetype Archetype { get; init; }
        public string DisplayName { get; init; } = "";
        public IReadOnlyList<TemplateStepDef> Steps { get; init; } = new List<TemplateStepDef>();
        public IReadOnlyList<TwistSlotDef> TwistSlotDefs { get; init; } = new List<TwistSlotDef>();
        public RewardFormulaDef Reward { get; init; } = new();
        public int RequiredRepTier { get; init; } = -1; // -1 = no requirement
        public string FactionId { get; init; } = ""; // empty = universal
    }

    // ──────────────────────────────────────────────────────────────────────────
    // SUPPLY TEMPLATES (14 total)
    // ──────────────────────────────────────────────────────────────────────────

    // GATE.T48.TEMPLATE.SUPPLY_SET.001: 4 supply/logistics templates.
    public static readonly MissionTemplateDef EmergencyResupply = new()
    {
        TemplateId = "emergency_resupply",
        Archetype = Archetype.Supply,
        DisplayName = "Emergency Resupply",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Deliver, VariableSlots = new List<string> { "$GOOD_1", "$TARGET_NODE" }, CompletionCondition = "deliver_good_to_shortage_node" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "blockade", WeightBps = 2000 },
            new() { TwistType = "price_spike", WeightBps = 3000 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 300, PerTwistBonusBps = 3000 },
    };

    public static readonly MissionTemplateDef BulkContract = new()
    {
        TemplateId = "bulk_contract",
        Archetype = Archetype.Supply,
        DisplayName = "Bulk Contract",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Deliver, VariableSlots = new List<string> { "$GOOD_1", "$TARGET_NODE" }, CompletionCondition = "deliver_bulk_station_1" },
            new() { StepIndex = 1, Objective = ObjectiveType.Deliver, VariableSlots = new List<string> { "$GOOD_1", "$TARGET_NODE" }, CompletionCondition = "deliver_bulk_station_2" },
            new() { StepIndex = 2, Objective = ObjectiveType.Deliver, VariableSlots = new List<string> { "$GOOD_1", "$TARGET_NODE" }, CompletionCondition = "deliver_bulk_station_3" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "rival_runner", WeightBps = 2500 },
            new() { TwistType = "shortage_shift", WeightBps = 2000 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 500, PerTwistBonusBps = 2500 },
    };

    public static readonly MissionTemplateDef TradeRoutePioneer = new()
    {
        TemplateId = "trade_route_pioneer",
        Archetype = Archetype.Supply,
        DisplayName = "Trade Route Pioneer",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Visit, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "visit_profitable_node_1" },
            new() { StepIndex = 1, Objective = ObjectiveType.Visit, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "visit_profitable_node_2" },
            new() { StepIndex = 2, Objective = ObjectiveType.Visit, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "visit_profitable_node_3" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "price_spike", WeightBps = 3000 },
            new() { TwistType = "intelligence", WeightBps = 2000 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 400, PerTwistBonusBps = 2000 },
    };

    public static readonly MissionTemplateDef WarfrontSupply = new()
    {
        TemplateId = "warfront_supply",
        Archetype = Archetype.Supply,
        DisplayName = "Warfront Supply Run",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Deliver, VariableSlots = new List<string> { "$GOOD_1", "$TARGET_NODE" }, CompletionCondition = "deliver_munitions_to_warfront" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "blockade", WeightBps = 3500 },
            new() { TwistType = "ambush", WeightBps = 2500 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 450, PerTwistBonusBps = 3500 },
    };

    // GATE.T51.TEMPLATE.SUPPLY_AUTHOR.001: 10 additional supply templates.
    public static readonly MissionTemplateDef PharmaceuticalRush = new()
    {
        TemplateId = "pharmaceutical_rush",
        Archetype = Archetype.Supply,
        DisplayName = "Pharmaceutical Rush",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Deliver, VariableSlots = new List<string> { "$GOOD_1", "$TARGET_NODE" }, CompletionCondition = "deliver_pharmaceuticals_urgent" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "shortage_shift", WeightBps = 3500 },
            new() { TwistType = "price_spike", WeightBps = 2500 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 350, PerTwistBonusBps = 3000 },
    };

    public static readonly MissionTemplateDef HarvestHaul = new()
    {
        TemplateId = "harvest_haul",
        Archetype = Archetype.Supply,
        DisplayName = "Harvest Haul",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Deliver, VariableSlots = new List<string> { "$GOOD_1", "$TARGET_NODE" }, CompletionCondition = "collect_harvest_from_agri_node" },
            new() { StepIndex = 1, Objective = ObjectiveType.Deliver, VariableSlots = new List<string> { "$GOOD_1", "$TARGET_NODE" }, CompletionCondition = "deliver_harvest_to_hub" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "rival_runner", WeightBps = 2500 },
            new() { TwistType = "shortage_shift", WeightBps = 2000 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 350, PerTwistBonusBps = 2000 },
    };

    public static readonly MissionTemplateDef FuelDepotRun = new()
    {
        TemplateId = "fuel_depot_run",
        Archetype = Archetype.Supply,
        DisplayName = "Fuel Depot Run",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Deliver, VariableSlots = new List<string> { "$GOOD_1", "$TARGET_NODE" }, CompletionCondition = "deliver_fuel_to_depot_1" },
            new() { StepIndex = 1, Objective = ObjectiveType.Deliver, VariableSlots = new List<string> { "$GOOD_1", "$TARGET_NODE" }, CompletionCondition = "deliver_fuel_to_depot_2" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "blockade", WeightBps = 2500 },
            new() { TwistType = "price_spike", WeightBps = 3000 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 400, PerTwistBonusBps = 2500 },
    };

    public static readonly MissionTemplateDef ConstructionMaterials = new()
    {
        TemplateId = "construction_materials",
        Archetype = Archetype.Supply,
        DisplayName = "Construction Materials",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Deliver, VariableSlots = new List<string> { "$GOOD_1", "$TARGET_NODE" }, CompletionCondition = "deliver_materials_to_construction_site" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "shortage_shift", WeightBps = 3000 },
            new() { TwistType = "rival_runner", WeightBps = 2000 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 300, PerTwistBonusBps = 2500 },
    };

    public static readonly MissionTemplateDef LuxuryGoodsDelivery = new()
    {
        TemplateId = "luxury_goods_delivery",
        Archetype = Archetype.Supply,
        DisplayName = "Luxury Goods Delivery",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Deliver, VariableSlots = new List<string> { "$GOOD_1", "$TARGET_NODE" }, CompletionCondition = "deliver_luxury_goods_to_station" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "price_spike", WeightBps = 4000 },
            new() { TwistType = "contraband_mixed", WeightBps = 2000 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 500, PerTwistBonusBps = 3500 },
    };

    public static readonly MissionTemplateDef StarportProvisioning = new()
    {
        TemplateId = "starport_provisioning",
        Archetype = Archetype.Supply,
        DisplayName = "Starport Provisioning",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Deliver, VariableSlots = new List<string> { "$GOOD_1", "$TARGET_NODE" }, CompletionCondition = "deliver_provisions_batch_1" },
            new() { StepIndex = 1, Objective = ObjectiveType.Deliver, VariableSlots = new List<string> { "$GOOD_1", "$TARGET_NODE" }, CompletionCondition = "deliver_provisions_batch_2" },
            new() { StepIndex = 2, Objective = ObjectiveType.Deliver, VariableSlots = new List<string> { "$GOOD_1", "$TARGET_NODE" }, CompletionCondition = "deliver_provisions_batch_3" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "shortage_shift", WeightBps = 2500 },
            new() { TwistType = "rival_runner", WeightBps = 2500 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 550, PerTwistBonusBps = 2500 },
    };

    public static readonly MissionTemplateDef RefugeeAidPackage = new()
    {
        TemplateId = "refugee_aid_package",
        Archetype = Archetype.Supply,
        DisplayName = "Refugee Aid Package",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Deliver, VariableSlots = new List<string> { "$GOOD_1", "$TARGET_NODE" }, CompletionCondition = "deliver_aid_to_refugee_station" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "blockade", WeightBps = 3000 },
            new() { TwistType = "ambush", WeightBps = 2000 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 250, PerTwistBonusBps = 3500 },
    };

    public static readonly MissionTemplateDef MiningEquipmentShuttle = new()
    {
        TemplateId = "mining_equipment_shuttle",
        Archetype = Archetype.Supply,
        DisplayName = "Mining Equipment Shuttle",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Deliver, VariableSlots = new List<string> { "$GOOD_1", "$TARGET_NODE" }, CompletionCondition = "deliver_equipment_to_mining_node" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "shortage_shift", WeightBps = 2500 },
            new() { TwistType = "price_spike", WeightBps = 2000 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 300, PerTwistBonusBps = 2500 },
    };

    public static readonly MissionTemplateDef ColdChainTransport = new()
    {
        TemplateId = "cold_chain_transport",
        Archetype = Archetype.Supply,
        DisplayName = "Cold Chain Transport",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Deliver, VariableSlots = new List<string> { "$GOOD_1", "$TARGET_NODE" }, CompletionCondition = "deliver_perishable_cargo_1" },
            new() { StepIndex = 1, Objective = ObjectiveType.Deliver, VariableSlots = new List<string> { "$GOOD_1", "$TARGET_NODE" }, CompletionCondition = "deliver_perishable_cargo_2" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "price_spike", WeightBps = 3500 },
            new() { TwistType = "shortage_shift", WeightBps = 2500 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 450, PerTwistBonusBps = 3000 },
    };

    public static readonly MissionTemplateDef ColonySeedShipment = new()
    {
        TemplateId = "colony_seed_shipment",
        Archetype = Archetype.Supply,
        DisplayName = "Colony Seed Shipment",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Deliver, VariableSlots = new List<string> { "$GOOD_1", "$TARGET_NODE" }, CompletionCondition = "deliver_seed_cargo_to_colony" },
            new() { StepIndex = 1, Objective = ObjectiveType.Visit, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "confirm_colony_receipt" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "blockade", WeightBps = 2500 },
            new() { TwistType = "rival_runner", WeightBps = 2000 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 500, PerTwistBonusBps = 2500 },
    };

    // ──────────────────────────────────────────────────────────────────────────
    // EXPLORE TEMPLATES (11 total)
    // ──────────────────────────────────────────────────────────────────────────

    // GATE.T48.TEMPLATE.EXPLORE_SET.001: 4 exploration templates.
    public static readonly MissionTemplateDef DeepScanSurvey = new()
    {
        TemplateId = "deep_scan_survey",
        Archetype = Archetype.Explore,
        DisplayName = "Deep Scan Survey",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Scan, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "scan_unvisited_system_1" },
            new() { StepIndex = 1, Objective = ObjectiveType.Scan, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "scan_unvisited_system_2" },
            new() { StepIndex = 2, Objective = ObjectiveType.Scan, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "scan_unvisited_system_3" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "shortage_shift", WeightBps = 2000 },
            new() { TwistType = "intelligence", WeightBps = 3000 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 350, PerTwistBonusBps = 2500 },
    };

    public static readonly MissionTemplateDef DerelictInvestigation = new()
    {
        TemplateId = "derelict_investigation",
        Archetype = Archetype.Explore,
        DisplayName = "Derelict Investigation",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Visit, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "reach_distant_discovery" },
            new() { StepIndex = 1, Objective = ObjectiveType.Scan, VariableSlots = new List<string> { "$DISCOVERY_ID" }, CompletionCondition = "analyze_discovery" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "ambush", WeightBps = 2500 },
            new() { TwistType = "contraband_mixed", WeightBps = 1500 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 500, PerTwistBonusBps = 3000 },
    };

    public static readonly MissionTemplateDef FractureProbe = new()
    {
        TemplateId = "fracture_probe",
        Archetype = Archetype.Explore,
        DisplayName = "Fracture Probe",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Visit, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "enter_fracture_space" },
            new() { StepIndex = 1, Objective = ObjectiveType.Visit, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "survive_and_return" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "ambush", WeightBps = 3000 },
            new() { TwistType = "shortage_shift", WeightBps = 2000 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 600, PerTwistBonusBps = 3500 },
    };

    public static readonly MissionTemplateDef MappingExpedition = new()
    {
        TemplateId = "mapping_expedition",
        Archetype = Archetype.Explore,
        DisplayName = "Mapping Expedition",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Visit, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "visit_chain_node_1" },
            new() { StepIndex = 1, Objective = ObjectiveType.Visit, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "visit_chain_node_2" },
            new() { StepIndex = 2, Objective = ObjectiveType.Visit, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "visit_chain_node_3" },
            new() { StepIndex = 3, Objective = ObjectiveType.Visit, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "visit_chain_node_4" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "rival_runner", WeightBps = 2000 },
            new() { TwistType = "intelligence", WeightBps = 2500 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 450, PerTwistBonusBps = 2000 },
    };

    // GATE.T51.TEMPLATE.EXPLORE_AUTHOR.001: 7 additional exploration templates.
    public static readonly MissionTemplateDef SignalSourceInvestigation = new()
    {
        TemplateId = "signal_source_investigation",
        Archetype = Archetype.Explore,
        DisplayName = "Signal Source Investigation",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Visit, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "reach_signal_origin" },
            new() { StepIndex = 1, Objective = ObjectiveType.Scan, VariableSlots = new List<string> { "$DISCOVERY_ID" }, CompletionCondition = "analyze_signal_source" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "ambush", WeightBps = 3000 },
            new() { TwistType = "intelligence", WeightBps = 2500 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 400, PerTwistBonusBps = 3000 },
    };

    public static readonly MissionTemplateDef UnchartedSystemSurvey = new()
    {
        TemplateId = "uncharted_system_survey",
        Archetype = Archetype.Explore,
        DisplayName = "Uncharted System Survey",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Visit, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "enter_uncharted_system" },
            new() { StepIndex = 1, Objective = ObjectiveType.Scan, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "full_system_scan" },
            new() { StepIndex = 2, Objective = ObjectiveType.Scan, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "catalog_system_bodies" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "shortage_shift", WeightBps = 2000 },
            new() { TwistType = "ambush", WeightBps = 2500 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 500, PerTwistBonusBps = 2500 },
    };

    public static readonly MissionTemplateDef AsteroidBeltMapping = new()
    {
        TemplateId = "asteroid_belt_mapping",
        Archetype = Archetype.Explore,
        DisplayName = "Asteroid Belt Mapping",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Scan, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "scan_belt_sector_1" },
            new() { StepIndex = 1, Objective = ObjectiveType.Scan, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "scan_belt_sector_2" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "rival_runner", WeightBps = 2500 },
            new() { TwistType = "intelligence", WeightBps = 2000 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 300, PerTwistBonusBps = 2000 },
    };

    public static readonly MissionTemplateDef AbandonedOutpostRecon = new()
    {
        TemplateId = "abandoned_outpost_recon",
        Archetype = Archetype.Explore,
        DisplayName = "Abandoned Outpost Recon",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Visit, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "reach_abandoned_outpost" },
            new() { StepIndex = 1, Objective = ObjectiveType.Scan, VariableSlots = new List<string> { "$DISCOVERY_ID" }, CompletionCondition = "scan_outpost_systems" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "ambush", WeightBps = 3500 },
            new() { TwistType = "contraband_mixed", WeightBps = 2000 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 450, PerTwistBonusBps = 3000 },
    };

    public static readonly MissionTemplateDef GasGiantProbe = new()
    {
        TemplateId = "gas_giant_probe",
        Archetype = Archetype.Explore,
        DisplayName = "Gas Giant Probe",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Visit, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "approach_gas_giant" },
            new() { StepIndex = 1, Objective = ObjectiveType.Scan, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "deploy_atmospheric_probe" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "shortage_shift", WeightBps = 2000 },
            new() { TwistType = "intelligence", WeightBps = 3500 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 550, PerTwistBonusBps = 2500 },
    };

    public static readonly MissionTemplateDef DeepSpaceRelaySetup = new()
    {
        TemplateId = "deep_space_relay_setup",
        Archetype = Archetype.Explore,
        DisplayName = "Deep Space Relay Setup",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Visit, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "reach_relay_installation_point" },
            new() { StepIndex = 1, Objective = ObjectiveType.Deliver, VariableSlots = new List<string> { "$GOOD_1", "$TARGET_NODE" }, CompletionCondition = "deploy_relay_components" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "blockade", WeightBps = 2000 },
            new() { TwistType = "ambush", WeightBps = 2500 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 600, PerTwistBonusBps = 3000 },
    };

    public static readonly MissionTemplateDef NebulaEdgeSurvey = new()
    {
        TemplateId = "nebula_edge_survey",
        Archetype = Archetype.Explore,
        DisplayName = "Nebula Edge Survey",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Visit, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "reach_nebula_boundary" },
            new() { StepIndex = 1, Objective = ObjectiveType.Scan, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "scan_nebula_composition" },
            new() { StepIndex = 2, Objective = ObjectiveType.Scan, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "catalog_nebula_phenomena" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "intelligence", WeightBps = 3000 },
            new() { TwistType = "shortage_shift", WeightBps = 2000 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 500, PerTwistBonusBps = 2500 },
    };

    // ──────────────────────────────────────────────────────────────────────────
    // COMBAT TEMPLATES (12 total)
    // ──────────────────────────────────────────────────────────────────────────

    // GATE.T48.TEMPLATE.COMBAT_SET.001: 3 combat/security templates.
    public static readonly MissionTemplateDef EscortConvoy = new()
    {
        TemplateId = "escort_convoy",
        Archetype = Archetype.Combat,
        DisplayName = "Escort Convoy",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Escort, VariableSlots = new List<string> { "$FACTION_1", "$TARGET_NODE" }, CompletionCondition = "escort_through_hostile_1" },
            new() { StepIndex = 1, Objective = ObjectiveType.Escort, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "escort_through_hostile_2" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "ambush", WeightBps = 3500 },
            new() { TwistType = "blockade", WeightBps = 2500 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 550, PerTwistBonusBps = 3000 },
    };

    public static readonly MissionTemplateDef PirateBounty = new()
    {
        TemplateId = "pirate_bounty",
        Archetype = Archetype.Combat,
        DisplayName = "Pirate Bounty",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Destroy, VariableSlots = new List<string> { "$HOSTILE_COUNT", "$TARGET_NODE" }, CompletionCondition = "destroy_hostile_fleets" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "ambush", WeightBps = 4000 },
            new() { TwistType = "rival_runner", WeightBps = 2000 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 400, PerTwistBonusBps = 3500 },
    };

    public static readonly MissionTemplateDef LanePatrol = new()
    {
        TemplateId = "lane_patrol",
        Archetype = Archetype.Combat,
        DisplayName = "Lane Patrol",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Visit, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "patrol_contested_node_1" },
            new() { StepIndex = 1, Objective = ObjectiveType.Visit, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "patrol_contested_node_2" },
            new() { StepIndex = 2, Objective = ObjectiveType.Visit, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "patrol_contested_node_3" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "ambush", WeightBps = 3000 },
            new() { TwistType = "intelligence", WeightBps = 2500 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 350, PerTwistBonusBps = 2500 },
    };

    // GATE.T51.TEMPLATE.COMBAT_AUTHOR.001: 9 additional combat templates.
    public static readonly MissionTemplateDef SystemDefense = new()
    {
        TemplateId = "system_defense",
        Archetype = Archetype.Combat,
        DisplayName = "System Defense",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Visit, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "reach_threatened_system" },
            new() { StepIndex = 1, Objective = ObjectiveType.Destroy, VariableSlots = new List<string> { "$HOSTILE_COUNT", "$TARGET_NODE" }, CompletionCondition = "repel_attackers" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "ambush", WeightBps = 3500 },
            new() { TwistType = "blockade", WeightBps = 2500 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 500, PerTwistBonusBps = 3000 },
    };

    public static readonly MissionTemplateDef BlockadeRunner = new()
    {
        TemplateId = "blockade_runner",
        Archetype = Archetype.Combat,
        DisplayName = "Blockade Runner",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Deliver, VariableSlots = new List<string> { "$GOOD_1", "$TARGET_NODE" }, CompletionCondition = "breach_blockade_with_cargo" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "blockade", WeightBps = 4500 },
            new() { TwistType = "ambush", WeightBps = 3000 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 600, PerTwistBonusBps = 4000 },
    };

    public static readonly MissionTemplateDef PirateNestClearance = new()
    {
        TemplateId = "pirate_nest_clearance",
        Archetype = Archetype.Combat,
        DisplayName = "Pirate Nest Clearance",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Visit, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "locate_pirate_nest" },
            new() { StepIndex = 1, Objective = ObjectiveType.Destroy, VariableSlots = new List<string> { "$HOSTILE_COUNT", "$TARGET_NODE" }, CompletionCondition = "eliminate_pirate_fleet" },
            new() { StepIndex = 2, Objective = ObjectiveType.Destroy, VariableSlots = new List<string> { "$HOSTILE_COUNT", "$TARGET_NODE" }, CompletionCondition = "destroy_pirate_base" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "ambush", WeightBps = 4000 },
            new() { TwistType = "rival_runner", WeightBps = 2000 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 700, PerTwistBonusBps = 3500 },
    };

    public static readonly MissionTemplateDef VipExtraction = new()
    {
        TemplateId = "vip_extraction",
        Archetype = Archetype.Combat,
        DisplayName = "VIP Extraction",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Visit, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "reach_extraction_zone" },
            new() { StepIndex = 1, Objective = ObjectiveType.Escort, VariableSlots = new List<string> { "$FACTION_1", "$TARGET_NODE" }, CompletionCondition = "escort_vip_to_safety" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "ambush", WeightBps = 3500 },
            new() { TwistType = "intelligence", WeightBps = 2500 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 550, PerTwistBonusBps = 3500 },
        RequiredRepTier = 1, // Friendly or better
    };

    public static readonly MissionTemplateDef SalvageGuard = new()
    {
        TemplateId = "salvage_guard",
        Archetype = Archetype.Combat,
        DisplayName = "Salvage Guard",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Visit, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "reach_salvage_site" },
            new() { StepIndex = 1, Objective = ObjectiveType.Destroy, VariableSlots = new List<string> { "$HOSTILE_COUNT", "$TARGET_NODE" }, CompletionCondition = "defend_salvage_operation" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "ambush", WeightBps = 3000 },
            new() { TwistType = "rival_runner", WeightBps = 2500 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 400, PerTwistBonusBps = 3000 },
    };

    public static readonly MissionTemplateDef DroneSwarmSuppression = new()
    {
        TemplateId = "drone_swarm_suppression",
        Archetype = Archetype.Combat,
        DisplayName = "Drone Swarm Suppression",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Visit, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "locate_drone_swarm" },
            new() { StepIndex = 1, Objective = ObjectiveType.Destroy, VariableSlots = new List<string> { "$HOSTILE_COUNT", "$TARGET_NODE" }, CompletionCondition = "suppress_drone_swarm" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "ambush", WeightBps = 3500 },
            new() { TwistType = "shortage_shift", WeightBps = 2000 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 450, PerTwistBonusBps = 3000 },
    };

    public static readonly MissionTemplateDef OutpostSiegeRelief = new()
    {
        TemplateId = "outpost_siege_relief",
        Archetype = Archetype.Combat,
        DisplayName = "Outpost Siege Relief",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Visit, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "reach_besieged_outpost" },
            new() { StepIndex = 1, Objective = ObjectiveType.Destroy, VariableSlots = new List<string> { "$HOSTILE_COUNT", "$TARGET_NODE" }, CompletionCondition = "break_siege_forces" },
            new() { StepIndex = 2, Objective = ObjectiveType.Deliver, VariableSlots = new List<string> { "$GOOD_1", "$TARGET_NODE" }, CompletionCondition = "resupply_outpost" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "blockade", WeightBps = 4000 },
            new() { TwistType = "ambush", WeightBps = 3000 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 750, PerTwistBonusBps = 3500 },
    };

    public static readonly MissionTemplateDef FrontierGarrison = new()
    {
        TemplateId = "frontier_garrison",
        Archetype = Archetype.Combat,
        DisplayName = "Frontier Garrison",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Visit, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "garrison_frontier_node_1" },
            new() { StepIndex = 1, Objective = ObjectiveType.Visit, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "garrison_frontier_node_2" },
            new() { StepIndex = 2, Objective = ObjectiveType.Destroy, VariableSlots = new List<string> { "$HOSTILE_COUNT", "$TARGET_NODE" }, CompletionCondition = "repel_frontier_incursion" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "ambush", WeightBps = 3000 },
            new() { TwistType = "intelligence", WeightBps = 2000 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 500, PerTwistBonusBps = 2500 },
    };

    public static readonly MissionTemplateDef HostileFleetInterdiction = new()
    {
        TemplateId = "hostile_fleet_interdiction",
        Archetype = Archetype.Combat,
        DisplayName = "Hostile Fleet Interdiction",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Scan, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "scan_hostile_fleet_position" },
            new() { StepIndex = 1, Objective = ObjectiveType.Destroy, VariableSlots = new List<string> { "$HOSTILE_COUNT", "$TARGET_NODE" }, CompletionCondition = "interdict_hostile_fleet" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "ambush", WeightBps = 4000 },
            new() { TwistType = "intelligence", WeightBps = 3000 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 650, PerTwistBonusBps = 3500 },
        RequiredRepTier = 2, // Neutral or better
    };

    // ──────────────────────────────────────────────────────────────────────────
    // POLITICS TEMPLATES (9 total)
    // ──────────────────────────────────────────────────────────────────────────

    // GATE.T48.TEMPLATE.POLITICS_SET.001: 3 reputation/politics templates.
    public static readonly MissionTemplateDef DiplomaticCourier = new()
    {
        TemplateId = "diplomatic_courier",
        Archetype = Archetype.Politics,
        DisplayName = "Diplomatic Courier",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Deliver, VariableSlots = new List<string> { "$FACTION_1", "$TARGET_NODE" }, CompletionCondition = "deliver_intel_to_faction_station" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "blockade", WeightBps = 2000 },
            new() { TwistType = "intelligence", WeightBps = 3000 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 350, PerTwistBonusBps = 2500 },
        RequiredRepTier = 2, // Neutral or better
    };

    public static readonly MissionTemplateDef TradeEmbargoRun = new()
    {
        TemplateId = "trade_embargo_run",
        Archetype = Archetype.Politics,
        DisplayName = "Trade Embargo Run",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Deliver, VariableSlots = new List<string> { "$GOOD_1", "$FACTION_1", "$TARGET_NODE" }, CompletionCondition = "smuggle_goods_to_embargoed_node" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "blockade", WeightBps = 4000 },
            new() { TwistType = "contraband_mixed", WeightBps = 3000 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 600, PerTwistBonusBps = 4000 },
    };

    public static readonly MissionTemplateDef FactionFavor = new()
    {
        TemplateId = "faction_favor",
        Archetype = Archetype.Politics,
        DisplayName = "Faction Favor",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Deliver, VariableSlots = new List<string> { "$FACTION_1", "$TARGET_NODE" }, CompletionCondition = "trade_at_faction_station_1" },
            new() { StepIndex = 1, Objective = ObjectiveType.Deliver, VariableSlots = new List<string> { "$FACTION_1", "$TARGET_NODE" }, CompletionCondition = "trade_at_faction_station_2" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "price_spike", WeightBps = 2500 },
            new() { TwistType = "rival_runner", WeightBps = 2000 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 300, PerTwistBonusBps = 2000 },
        RequiredRepTier = 1, // Friendly or better
    };

    // GATE.T51.TEMPLATE.POLITICS_AUTHOR.001: 6 additional politics templates.
    public static readonly MissionTemplateDef DefectorExtraction = new()
    {
        TemplateId = "defector_extraction",
        Archetype = Archetype.Politics,
        DisplayName = "Defector Extraction",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Visit, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "reach_defector_location" },
            new() { StepIndex = 1, Objective = ObjectiveType.Escort, VariableSlots = new List<string> { "$FACTION_1", "$TARGET_NODE" }, CompletionCondition = "escort_defector_to_safe_haven" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "ambush", WeightBps = 3500 },
            new() { TwistType = "intelligence", WeightBps = 2500 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 550, PerTwistBonusBps = 3500 },
        RequiredRepTier = 2, // Neutral or better
    };

    public static readonly MissionTemplateDef TradeAgreementEscort = new()
    {
        TemplateId = "trade_agreement_escort",
        Archetype = Archetype.Politics,
        DisplayName = "Trade Agreement Escort",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Escort, VariableSlots = new List<string> { "$FACTION_1", "$TARGET_NODE" }, CompletionCondition = "escort_trade_delegation_1" },
            new() { StepIndex = 1, Objective = ObjectiveType.Escort, VariableSlots = new List<string> { "$FACTION_1", "$TARGET_NODE" }, CompletionCondition = "escort_trade_delegation_2" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "blockade", WeightBps = 3000 },
            new() { TwistType = "ambush", WeightBps = 2500 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 500, PerTwistBonusBps = 3000 },
        RequiredRepTier = 1, // Friendly or better
    };

    public static readonly MissionTemplateDef FactionIntelDrop = new()
    {
        TemplateId = "faction_intel_drop",
        Archetype = Archetype.Politics,
        DisplayName = "Faction Intel Drop",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Deliver, VariableSlots = new List<string> { "$FACTION_1", "$TARGET_NODE" }, CompletionCondition = "deliver_intelligence_package" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "intelligence", WeightBps = 4000 },
            new() { TwistType = "contraband_mixed", WeightBps = 2500 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 400, PerTwistBonusBps = 3000 },
    };

    public static readonly MissionTemplateDef DisputedTerritorySurvey = new()
    {
        TemplateId = "disputed_territory_survey",
        Archetype = Archetype.Politics,
        DisplayName = "Disputed Territory Survey",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Visit, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "enter_disputed_zone" },
            new() { StepIndex = 1, Objective = ObjectiveType.Scan, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "survey_territorial_claim" },
            new() { StepIndex = 2, Objective = ObjectiveType.Deliver, VariableSlots = new List<string> { "$FACTION_1", "$TARGET_NODE" }, CompletionCondition = "deliver_survey_report" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "ambush", WeightBps = 3000 },
            new() { TwistType = "intelligence", WeightBps = 2500 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 450, PerTwistBonusBps = 2500 },
    };

    public static readonly MissionTemplateDef SanctionsRunner = new()
    {
        TemplateId = "sanctions_runner",
        Archetype = Archetype.Politics,
        DisplayName = "Sanctions Runner",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Deliver, VariableSlots = new List<string> { "$GOOD_1", "$FACTION_1", "$TARGET_NODE" }, CompletionCondition = "deliver_sanctioned_goods" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "blockade", WeightBps = 4500 },
            new() { TwistType = "contraband_mixed", WeightBps = 3500 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 700, PerTwistBonusBps = 4000 },
    };

    public static readonly MissionTemplateDef PeaceEnvoy = new()
    {
        TemplateId = "peace_envoy",
        Archetype = Archetype.Politics,
        DisplayName = "Peace Envoy",
        Steps = new List<TemplateStepDef>
        {
            new() { StepIndex = 0, Objective = ObjectiveType.Visit, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "visit_faction_capital_1" },
            new() { StepIndex = 1, Objective = ObjectiveType.Deliver, VariableSlots = new List<string> { "$FACTION_1", "$TARGET_NODE" }, CompletionCondition = "deliver_peace_terms" },
            new() { StepIndex = 2, Objective = ObjectiveType.Visit, VariableSlots = new List<string> { "$TARGET_NODE" }, CompletionCondition = "visit_faction_capital_2" },
        },
        TwistSlotDefs = new List<TwistSlotDef>
        {
            new() { TwistType = "blockade", WeightBps = 2500 },
            new() { TwistType = "intelligence", WeightBps = 3000 },
        },
        Reward = new RewardFormulaDef { BaseCredits = 500, PerTwistBonusBps = 2500 },
        RequiredRepTier = 3, // Allied
    };

    // ──────────────────────────────────────────────────────────────────────────
    // All templates registry.
    // ──────────────────────────────────────────────────────────────────────────
    public static readonly IReadOnlyList<MissionTemplateDef> AllTemplates = new List<MissionTemplateDef>
    {
        // Supply (14)
        EmergencyResupply, BulkContract, TradeRoutePioneer, WarfrontSupply,
        PharmaceuticalRush, HarvestHaul, FuelDepotRun, ConstructionMaterials,
        LuxuryGoodsDelivery, StarportProvisioning, RefugeeAidPackage,
        MiningEquipmentShuttle, ColdChainTransport, ColonySeedShipment,
        // Explore (11)
        DeepScanSurvey, DerelictInvestigation, FractureProbe, MappingExpedition,
        SignalSourceInvestigation, UnchartedSystemSurvey, AsteroidBeltMapping,
        AbandonedOutpostRecon, GasGiantProbe, DeepSpaceRelaySetup, NebulaEdgeSurvey,
        // Combat (12)
        EscortConvoy, PirateBounty, LanePatrol,
        SystemDefense, BlockadeRunner, PirateNestClearance, VipExtraction,
        SalvageGuard, DroneSwarmSuppression, OutpostSiegeRelief, FrontierGarrison,
        HostileFleetInterdiction,
        // Politics (9)
        DiplomaticCourier, TradeEmbargoRun, FactionFavor,
        DefectorExtraction, TradeAgreementEscort, FactionIntelDrop,
        DisputedTerritorySurvey, SanctionsRunner, PeaceEnvoy,
    };
}
