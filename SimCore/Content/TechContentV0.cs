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
