using System.Collections.Generic;
using SimCore.Entities;

namespace SimCore.Content;

// GATE.S7.PLANET.MODEL.001: Planet content definitions.
// Maps planet types to display names, default specializations, and type-distribution tables.
public static class PlanetContentV0
{
    // ── Display name prefixes per planet type ──

    public static string GetTypeDisplayName(PlanetType type) => type switch
    {
        PlanetType.Terrestrial => "Terrestrial World",
        PlanetType.Ice => "Ice World",
        PlanetType.Sand => "Desert World",
        PlanetType.Lava => "Volcanic World",
        PlanetType.Gaseous => "Gas Giant",
        PlanetType.Barren => "Barren Moon",
        _ => "Unknown World",
    };

    // ── Specialization affinity tables ──
    // For each planet type, which specializations are valid and their relative weight.
    // Higher weight = more likely to be selected during generation.

    public static readonly IReadOnlyDictionary<PlanetType, IReadOnlyList<(PlanetSpecialization Spec, int Weight)>>
        SpecializationAffinity = new Dictionary<PlanetType, IReadOnlyList<(PlanetSpecialization, int)>>
        {
            [PlanetType.Terrestrial] = new List<(PlanetSpecialization, int)>
            {
                (PlanetSpecialization.Agriculture, 40),
                (PlanetSpecialization.Manufacturing, 20),
                (PlanetSpecialization.HighTech, 25),
                (PlanetSpecialization.None, 15),
            },
            [PlanetType.Ice] = new List<(PlanetSpecialization, int)>
            {
                (PlanetSpecialization.FuelExtraction, 35),
                (PlanetSpecialization.Mining, 30),
                (PlanetSpecialization.None, 35),
            },
            [PlanetType.Sand] = new List<(PlanetSpecialization, int)>
            {
                (PlanetSpecialization.Mining, 50),
                (PlanetSpecialization.Manufacturing, 20),
                (PlanetSpecialization.None, 30),
            },
            [PlanetType.Lava] = new List<(PlanetSpecialization, int)>
            {
                (PlanetSpecialization.Manufacturing, 45),
                (PlanetSpecialization.Mining, 30),
                (PlanetSpecialization.None, 25),
            },
            [PlanetType.Gaseous] = new List<(PlanetSpecialization, int)>
            {
                (PlanetSpecialization.FuelExtraction, 50),
                (PlanetSpecialization.None, 50),
            },
            [PlanetType.Barren] = new List<(PlanetSpecialization, int)>
            {
                (PlanetSpecialization.Mining, 45),
                (PlanetSpecialization.FuelExtraction, 20),
                (PlanetSpecialization.None, 35),
            },
        };

    // ── World class bias ──
    // Shifts specialization selection toward certain outcomes per world class.
    // Returns bonus weight to add for the given specialization in this world class.

    public static int GetWorldClassBonus(string worldClassId, PlanetSpecialization spec)
    {
        return (worldClassId, spec) switch
        {
            ("CORE", PlanetSpecialization.HighTech) => 20,
            ("CORE", PlanetSpecialization.Agriculture) => 10,
            ("CORE", PlanetSpecialization.Manufacturing) => 10,
            ("FRONTIER", PlanetSpecialization.Mining) => 15,
            ("FRONTIER", PlanetSpecialization.Agriculture) => 10,
            ("RIM", PlanetSpecialization.FuelExtraction) => 20,
            ("RIM", PlanetSpecialization.Mining) => 15,
            _ => 0,
        };
    }

    // ── Planet type distribution per world class ──
    // Weights (out of 100) for each planet type within each world class.

    public static readonly IReadOnlyDictionary<string, IReadOnlyList<(PlanetType Type, int Weight)>>
        TypeDistribution = new Dictionary<string, IReadOnlyList<(PlanetType, int)>>
        {
            ["CORE"] = new List<(PlanetType, int)>
            {
                (PlanetType.Terrestrial, 40),
                (PlanetType.Ice, 10),
                (PlanetType.Sand, 15),
                (PlanetType.Lava, 5),
                (PlanetType.Gaseous, 15),
                (PlanetType.Barren, 15),
            },
            ["FRONTIER"] = new List<(PlanetType, int)>
            {
                (PlanetType.Terrestrial, 20),
                (PlanetType.Ice, 20),
                (PlanetType.Sand, 20),
                (PlanetType.Lava, 10),
                (PlanetType.Gaseous, 15),
                (PlanetType.Barren, 15),
            },
            ["RIM"] = new List<(PlanetType, int)>
            {
                (PlanetType.Terrestrial, 10),
                (PlanetType.Ice, 25),
                (PlanetType.Sand, 15),
                (PlanetType.Lava, 15),
                (PlanetType.Gaseous, 20),
                (PlanetType.Barren, 15),
            },
        };

    // Fallback distribution when world class is unknown.
    public static readonly IReadOnlyList<(PlanetType Type, int Weight)> DefaultTypeDistribution =
        new List<(PlanetType, int)>
        {
            (PlanetType.Terrestrial, 25),
            (PlanetType.Ice, 15),
            (PlanetType.Sand, 20),
            (PlanetType.Lava, 10),
            (PlanetType.Gaseous, 15),
            (PlanetType.Barren, 15),
        };

    // ── Star class content ──

    public static string GetStarClassName(StarClass cls) => cls switch
    {
        StarClass.ClassG => "G-type (Yellow)",
        StarClass.ClassK => "K-type (Orange)",
        StarClass.ClassM => "M-type (Red Dwarf)",
        StarClass.ClassF => "F-type (White-Yellow)",
        StarClass.ClassA => "A-type (White)",
        StarClass.ClassO => "O-type (Blue Giant)",
        _ => "Unknown Star",
    };

    // Star class distribution per world class.
    // CORE: mostly stable G/K stars (life-friendly).
    // RIM: more extreme stars (M dwarfs, A/O giants).
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<(StarClass Class, int Weight)>>
        StarDistribution = new Dictionary<string, IReadOnlyList<(StarClass, int)>>
        {
            ["CORE"] = new List<(StarClass, int)>
            {
                (StarClass.ClassG, 40),
                (StarClass.ClassK, 30),
                (StarClass.ClassF, 15),
                (StarClass.ClassM, 10),
                (StarClass.ClassA, 4),
                (StarClass.ClassO, 1),
            },
            ["FRONTIER"] = new List<(StarClass, int)>
            {
                (StarClass.ClassG, 20),
                (StarClass.ClassK, 25),
                (StarClass.ClassM, 25),
                (StarClass.ClassF, 15),
                (StarClass.ClassA, 10),
                (StarClass.ClassO, 5),
            },
            ["RIM"] = new List<(StarClass, int)>
            {
                (StarClass.ClassM, 35),
                (StarClass.ClassK, 20),
                (StarClass.ClassG, 10),
                (StarClass.ClassF, 10),
                (StarClass.ClassA, 15),
                (StarClass.ClassO, 10),
            },
        };

    public static readonly IReadOnlyList<(StarClass Class, int Weight)> DefaultStarDistribution =
        new List<(StarClass, int)>
        {
            (StarClass.ClassG, 25),
            (StarClass.ClassK, 25),
            (StarClass.ClassM, 20),
            (StarClass.ClassF, 15),
            (StarClass.ClassA, 10),
            (StarClass.ClassO, 5),
        };

    // Star visual color (RGBA) for GalaxyView rendering.
    public static (float R, float G, float B) GetStarColor(StarClass cls) => cls switch
    {
        StarClass.ClassG => (1.0f, 0.9f, 0.3f),   // Yellow
        StarClass.ClassK => (1.0f, 0.6f, 0.2f),   // Orange
        StarClass.ClassM => (1.0f, 0.3f, 0.2f),   // Red
        StarClass.ClassF => (1.0f, 1.0f, 0.7f),   // White-yellow
        StarClass.ClassA => (0.8f, 0.85f, 1.0f),   // White-blue
        StarClass.ClassO => (0.4f, 0.5f, 1.0f),   // Blue
        _ => (1.0f, 0.8f, 0.2f),
    };
}
