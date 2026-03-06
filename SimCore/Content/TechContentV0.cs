using System.Collections.Generic;

namespace SimCore.Content;

// GATE.S4.TECH.CORE.001: Static tech tree definitions.
public sealed class TechDef
{
    public string TechId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public int ResearchTicks { get; set; } = 10;
    public int CreditCost { get; set; } = 100;
    public int Tier { get; set; } = 1;
    public List<string> Prerequisites { get; set; } = new();
    public List<string> UnlockEffects { get; set; } = new();
    // GATE.S8.RESEARCH_SUSTAIN.MODEL.001: Goods consumed per sustain cycle (key = goodId, value = qty).
    public Dictionary<string, int> SustainInputs { get; set; } = new();
    // Ticks between sustain consumption cycles (0 = use default from ResearchTweaksV0).
    public int SustainIntervalTicks { get; set; } = 0;
}

public static class TechContentV0
{
    public static readonly IReadOnlyList<TechDef> AllTechs = new List<TechDef>
    {
        new TechDef
        {
            TechId = "improved_thrusters",
            DisplayName = "Improved Thrusters",
            Description = "Increases fleet travel speed.",
            ResearchTicks = 8,
            CreditCost = 80,
            Prerequisites = new List<string>(),
            UnlockEffects = new List<string> { "speed_bonus_20pct" },
            SustainInputs = new Dictionary<string, int> { ["fuel"] = 2, ["metal"] = 1 },
            SustainIntervalTicks = 60,
        },
        new TechDef
        {
            TechId = "shield_mk2",
            DisplayName = "Shield Mk2",
            Description = "Unlocks improved shield module for installation.",
            ResearchTicks = 12,
            CreditCost = 120,
            Prerequisites = new List<string>(),
            UnlockEffects = new List<string> { "unlock_module_shield_mk2" },
            SustainInputs = new Dictionary<string, int> { ["metal"] = 2, ["ore"] = 3 },
            SustainIntervalTicks = 60,
        },
        new TechDef
        {
            TechId = "weapon_systems_2",
            DisplayName = "Weapon Systems II",
            Description = "Unlocks advanced weapon modules.",
            ResearchTicks = 15,
            CreditCost = 150,
            Prerequisites = new List<string>(),
            UnlockEffects = new List<string> { "unlock_module_cannon_mk2" },
            SustainInputs = new Dictionary<string, int> { ["metal"] = 3, ["fuel"] = 1 },
            SustainIntervalTicks = 60,
        },
        new TechDef
        {
            TechId = "advanced_refining",
            DisplayName = "Advanced Refining",
            Description = "Boosts production efficiency at industry sites.",
            ResearchTicks = 20,
            CreditCost = 200,
            Tier = 2,
            Prerequisites = new List<string> { "improved_thrusters" },
            UnlockEffects = new List<string> { "production_efficiency_10pct" },
            SustainInputs = new Dictionary<string, int> { ["hull_plating"] = 1, ["metal"] = 3 },
            SustainIntervalTicks = 60,
        },
        new TechDef
        {
            TechId = "fracture_drive",
            DisplayName = "Fracture Drive",
            Description = "Increases fleet tech level, unlocking higher-tier fracture nodes.",
            ResearchTicks = 25,
            CreditCost = 300,
            Tier = 3,
            Prerequisites = new List<string> { "shield_mk2", "weapon_systems_2" },
            UnlockEffects = new List<string> { "tech_level_increase_1" },
            SustainInputs = new Dictionary<string, int> { ["composite_armor"] = 1, ["exotic_crystals"] = 2, ["electronics"] = 1 },
            SustainIntervalTicks = 60,
        },
        // GATE.S4.CATALOG.TECH_WAVE.001: 5 new techs across tiers 1-3
        new TechDef
        {
            TechId = "engine_efficiency",
            DisplayName = "Engine Efficiency",
            Description = "Optimizes fuel consumption, reducing travel costs.",
            ResearchTicks = 10,
            CreditCost = 90,
            Prerequisites = new List<string>(),
            UnlockEffects = new List<string> { "fuel_cost_reduction_15pct" },
            SustainInputs = new Dictionary<string, int> { ["fuel"] = 3 },
            SustainIntervalTicks = 60,
        },
        new TechDef
        {
            TechId = "cargo_expansion",
            DisplayName = "Cargo Expansion",
            Description = "Unlocks larger cargo bay modules for increased hauling capacity.",
            ResearchTicks = 8,
            CreditCost = 70,
            Prerequisites = new List<string>(),
            UnlockEffects = new List<string> { "unlock_module_cargo_bay_mk2" },
            SustainInputs = new Dictionary<string, int> { ["metal"] = 2, ["ore"] = 2 },
            SustainIntervalTicks = 60,
        },
        new TechDef
        {
            TechId = "sensor_suite",
            DisplayName = "Sensor Suite",
            Description = "Improves scanning range, revealing hidden details about nearby systems.",
            ResearchTicks = 12,
            CreditCost = 110,
            Prerequisites = new List<string> { "engine_efficiency" },
            Tier = 2,
            UnlockEffects = new List<string> { "unlock_module_scanner_mk2", "scan_range_increase_1" },
            SustainInputs = new Dictionary<string, int> { ["electronics"] = 2, ["metal"] = 1 },
            SustainIntervalTicks = 60,
        },
        new TechDef
        {
            TechId = "reinforced_hull",
            DisplayName = "Reinforced Hull",
            Description = "Advanced hull composites increase maximum hull integrity.",
            ResearchTicks = 14,
            CreditCost = 130,
            Prerequisites = new List<string> { "shield_mk2" },
            Tier = 2,
            UnlockEffects = new List<string> { "unlock_module_hull_plating_mk2" },
            SustainInputs = new Dictionary<string, int> { ["hull_plating"] = 2, ["metal"] = 2 },
            SustainIntervalTicks = 60,
        },
        new TechDef
        {
            TechId = "weapon_calibration",
            DisplayName = "Weapon Calibration",
            Description = "Precision targeting algorithms boost weapon accuracy and damage output.",
            ResearchTicks = 18,
            CreditCost = 200,
            Prerequisites = new List<string> { "weapon_systems_2", "sensor_suite" },
            Tier = 3,
            UnlockEffects = new List<string> { "unlock_module_laser_mk2" },
            SustainInputs = new Dictionary<string, int> { ["exotic_crystals"] = 1, ["electronics"] = 2, ["hull_plating"] = 1 },
            SustainIntervalTicks = 60,
        },
        // GATE.S7.PLANET.TECH_GATE.001: Planetary landing tech for harsh environments.
        new TechDef
        {
            TechId = "planetary_landing_mk1",
            DisplayName = "Planetary Landing Mk1",
            Description = "Heat shielding and pressure suits enable landing on volcanic and barren worlds.",
            ResearchTicks = 10,
            CreditCost = 80,
            Prerequisites = new List<string>(),
            Tier = 1,
            UnlockEffects = new List<string> { "enable_harsh_landing" },
            SustainInputs = new Dictionary<string, int> { ["metal"] = 1, ["ore"] = 2 },
            SustainIntervalTicks = 60,
        },
        // GATE.S10.TRADE_INTEL.TECH.001: Trade network tech for extended scanner range.
        new TechDef
        {
            TechId = "trade_network",
            DisplayName = "Trade Network",
            Description = "Establishes an intelligence network across systems, extending market price scanning to two hops.",
            ResearchTicks = 20,
            CreditCost = 250,
            Prerequisites = new List<string> { "sensor_suite" },
            Tier = 3,
            UnlockEffects = new List<string> { "scan_range_increase_1" },
            SustainInputs = new Dictionary<string, int> { ["electronics"] = 3, ["hull_plating"] = 1, ["metal"] = 2 },
            SustainIntervalTicks = 60,
        },
    };

    private static readonly Dictionary<string, TechDef> _byId;

    static TechContentV0()
    {
        _byId = new Dictionary<string, TechDef>(System.StringComparer.Ordinal);
        foreach (var t in AllTechs)
            _byId[t.TechId] = t;
    }

    public static TechDef? GetById(string techId)
    {
        if (string.IsNullOrEmpty(techId)) return null;
        return _byId.TryGetValue(techId, out var def) ? def : null;
    }

    public static bool PrerequisitesMet(string techId, HashSet<string> unlocked)
    {
        var def = GetById(techId);
        if (def == null) return false;
        foreach (var prereq in def.Prerequisites)
        {
            if (!unlocked.Contains(prereq)) return false;
        }
        return true;
    }
}
