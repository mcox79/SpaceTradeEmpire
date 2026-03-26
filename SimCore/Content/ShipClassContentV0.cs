using System;
using System.Collections.Generic;

namespace SimCore.Content;

// GATE.S18.SHIP_MODULES.SHIP_CLASS.001: 8 ship classes per ship_modules_v0.md.
public sealed class ShipClassDef
{
    public string ClassId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int SlotCount { get; set; }
    public int BasePower { get; set; }
    public int CargoCapacity { get; set; }
    public int Mass { get; set; }
    public int ScanRange { get; set; }
    // Base zone armor by facing: [Fore, Port, Stbd, Aft]
    public int[] BaseZoneArmor { get; set; } = new int[4];
    public int CoreHull { get; set; }
    public int BaseShield { get; set; }
    public int BaseFuelCapacity { get; set; }
    // GATE.T59.SHIP.VARIANT_DEFS.001: Faction variant fields.
    // Empty for base/ancient classes. Set for faction variants.
    public string FactionId { get; set; } = "";
    public string BaseClassId { get; set; } = "";
    // Weapon damage modifier in basis points (10000 = 100% = no change).
    public int WeaponDamageModBps { get; set; } = 10000;
    // Fracture resistance in basis points (0 = none, 5000 = 50% less hull damage in Phase 2+).
    public int FractureResistBps { get; set; }
}

public static class ShipClassContentV0
{
    public static readonly IReadOnlyList<ShipClassDef> AllClasses = new List<ShipClassDef>
    {
        new ShipClassDef
        {
            ClassId = "shuttle", DisplayName = "Shuttle",
            // GATE.T59.SHIP.BALANCE_PASS.001: power 20→25 (quality-of-life for early game module fitting).
            SlotCount = 3, BasePower = 25, CargoCapacity = 30, Mass = 15, ScanRange = 60,
            BaseZoneArmor = new[] { 15, 10, 10, 10 }, CoreHull = 40, BaseShield = 20, BaseFuelCapacity = 200,
        },
        new ShipClassDef
        {
            ClassId = "corvette", DisplayName = "Corvette",
            SlotCount = 5, BasePower = 40, CargoCapacity = 50, Mass = 30, ScanRange = 80,
            BaseZoneArmor = new[] { 25, 20, 20, 15 }, CoreHull = 60, BaseShield = 35, BaseFuelCapacity = 500,
        },
        new ShipClassDef
        {
            ClassId = "clipper", DisplayName = "Clipper",
            SlotCount = 4, BasePower = 35, CargoCapacity = 60, Mass = 25, ScanRange = 100,
            BaseZoneArmor = new[] { 15, 15, 15, 30 }, CoreHull = 50, BaseShield = 30, BaseFuelCapacity = 600,
        },
        new ShipClassDef
        {
            ClassId = "frigate", DisplayName = "Frigate",
            // GATE.T59.SHIP.BALANCE_PASS.001: cargo 40→50 (trade viability vs corvette).
            SlotCount = 6, BasePower = 55, CargoCapacity = 50, Mass = 45, ScanRange = 70,
            BaseZoneArmor = new[] { 35, 25, 25, 15 }, CoreHull = 70, BaseShield = 40, BaseFuelCapacity = 400,
        },
        new ShipClassDef
        {
            ClassId = "hauler", DisplayName = "Hauler",
            SlotCount = 4, BasePower = 30, CargoCapacity = 120, Mass = 60, ScanRange = 50,
            BaseZoneArmor = new[] { 20, 25, 25, 25 }, CoreHull = 80, BaseShield = 30, BaseFuelCapacity = 800,
        },
        new ShipClassDef
        {
            ClassId = "cruiser", DisplayName = "Cruiser",
            SlotCount = 8, BasePower = 75, CargoCapacity = 60, Mass = 70, ScanRange = 90,
            BaseZoneArmor = new[] { 30, 30, 30, 25 }, CoreHull = 100, BaseShield = 50, BaseFuelCapacity = 600,
        },
        new ShipClassDef
        {
            ClassId = "carrier", DisplayName = "Carrier",
            // GATE.T59.SHIP.BALANCE_PASS.001: scan 120→140, cargo 80→100 (differentiate from cruiser).
            SlotCount = 7, BasePower = 65, CargoCapacity = 100, Mass = 80, ScanRange = 140,
            BaseZoneArmor = new[] { 20, 25, 25, 20 }, CoreHull = 90, BaseShield = 45, BaseFuelCapacity = 1000,
        },
        new ShipClassDef
        {
            ClassId = "dreadnought", DisplayName = "Dreadnought",
            SlotCount = 10, BasePower = 100, CargoCapacity = 50, Mass = 120, ScanRange = 80,
            BaseZoneArmor = new[] { 50, 45, 45, 35 }, CoreHull = 150, BaseShield = 80, BaseFuelCapacity = 500,
        },

        // GATE.S8.LATTICE_DRONES.ENTITY.001: Lattice drone — small, agile, light armor.
        new ShipClassDef
        {
            ClassId = "lattice_drone", DisplayName = "Lattice Drone",
            SlotCount = 1, BasePower = 10, CargoCapacity = 0, Mass = 10, ScanRange = 30,
            BaseZoneArmor = new[] { 5, 5, 5, 5 }, CoreHull = 40, BaseShield = 20, BaseFuelCapacity = 0,
        },

        // GATE.S8.ANCIENT_HULLS.CONTENT.001: 3 ancient ship hulls (discovered at deep void sites, restored at Haven T3+).
        new ShipClassDef
        {
            ClassId = "ancient_bastion", DisplayName = "Hull Type XV-1",
            SlotCount = Tweaks.AncientHullTweaksV0.BastionSlotCount,
            BasePower = Tweaks.AncientHullTweaksV0.BastionBasePower,
            CargoCapacity = Tweaks.AncientHullTweaksV0.BastionCargoCapacity,
            Mass = Tweaks.AncientHullTweaksV0.BastionMass,
            ScanRange = Tweaks.AncientHullTweaksV0.BastionScanRange,
            BaseZoneArmor = new[] { Tweaks.AncientHullTweaksV0.BastionArmorFore, Tweaks.AncientHullTweaksV0.BastionArmorPort, Tweaks.AncientHullTweaksV0.BastionArmorStbd, Tweaks.AncientHullTweaksV0.BastionArmorAft },
            CoreHull = Tweaks.AncientHullTweaksV0.BastionCoreHull,
            BaseShield = Tweaks.AncientHullTweaksV0.BastionBaseShield,
            BaseFuelCapacity = Tweaks.AncientHullTweaksV0.BastionBaseFuelCapacity,
        },
        new ShipClassDef
        {
            ClassId = "ancient_seeker", DisplayName = "Hull Type XV-2",
            SlotCount = Tweaks.AncientHullTweaksV0.SeekerSlotCount,
            BasePower = Tweaks.AncientHullTweaksV0.SeekerBasePower,
            CargoCapacity = Tweaks.AncientHullTweaksV0.SeekerCargoCapacity,
            Mass = Tweaks.AncientHullTweaksV0.SeekerMass,
            ScanRange = Tweaks.AncientHullTweaksV0.SeekerScanRange,
            BaseZoneArmor = new[] { Tweaks.AncientHullTweaksV0.SeekerArmorFore, Tweaks.AncientHullTweaksV0.SeekerArmorPort, Tweaks.AncientHullTweaksV0.SeekerArmorStbd, Tweaks.AncientHullTweaksV0.SeekerArmorAft },
            CoreHull = Tweaks.AncientHullTweaksV0.SeekerCoreHull,
            BaseShield = Tweaks.AncientHullTweaksV0.SeekerBaseShield,
            BaseFuelCapacity = Tweaks.AncientHullTweaksV0.SeekerBaseFuelCapacity,
        },
        new ShipClassDef
        {
            ClassId = "ancient_threshold", DisplayName = "Hull Type XV-3",
            SlotCount = Tweaks.AncientHullTweaksV0.ThresholdSlotCount,
            BasePower = Tweaks.AncientHullTweaksV0.ThresholdBasePower,
            CargoCapacity = Tweaks.AncientHullTweaksV0.ThresholdCargoCapacity,
            Mass = Tweaks.AncientHullTweaksV0.ThresholdMass,
            ScanRange = Tweaks.AncientHullTweaksV0.ThresholdScanRange,
            BaseZoneArmor = new[] { Tweaks.AncientHullTweaksV0.ThresholdArmorFore, Tweaks.AncientHullTweaksV0.ThresholdArmorPort, Tweaks.AncientHullTweaksV0.ThresholdArmorStbd, Tweaks.AncientHullTweaksV0.ThresholdArmorAft },
            CoreHull = Tweaks.AncientHullTweaksV0.ThresholdCoreHull,
            BaseShield = Tweaks.AncientHullTweaksV0.ThresholdBaseShield,
            BaseFuelCapacity = Tweaks.AncientHullTweaksV0.ThresholdBaseFuelCapacity,
        },

        // GATE.T59.SHIP.VARIANT_DEFS.001: 12 faction ship variants per faction_equipment_and_research_v0.md Part 4.
        // Each variant modifies a base class with faction-specific bonuses/maluses.
        // Variants require faction rep 75+ to purchase, cost 30% more than base.
        // Speed mods applied as inverse mass change (faster = lighter, slower = heavier).

        // --- Concord: defensive engineering. Shields up, speed down. ---
        new ShipClassDef
        {
            ClassId = "watchman", DisplayName = "Watchman-class Frigate",
            FactionId = "concord", BaseClassId = "frigate",
            SlotCount = 6, BasePower = 55, CargoCapacity = 50, Mass = 50, ScanRange = 70,
            BaseZoneArmor = new[] { 35, 25, 25, 15 }, CoreHull = 70, BaseShield = 48, BaseFuelCapacity = 400,
            // +20% shield (40→48), -10% speed (+10% mass 45→50). 1 weapon slot→utility.
        },
        new ShipClassDef
        {
            ClassId = "sentinel", DisplayName = "Sentinel-class Cruiser",
            FactionId = "concord", BaseClassId = "cruiser",
            SlotCount = 8, BasePower = 75, CargoCapacity = 60, Mass = 81, ScanRange = 90,
            BaseZoneArmor = new[] { 35, 35, 35, 29 }, CoreHull = 100, BaseShield = 58, BaseFuelCapacity = 600,
            // +15% shield (50→58), +15% zone armor all, -15% speed (+15% mass 70→81).
        },
        new ShipClassDef
        {
            ClassId = "guardian", DisplayName = "Guardian-class Carrier",
            FactionId = "concord", BaseClassId = "carrier",
            SlotCount = 7, BasePower = 65, CargoCapacity = 100, Mass = 80, ScanRange = 140,
            BaseZoneArmor = new[] { 20, 25, 25, 20 }, CoreHull = 90, BaseShield = 54, BaseFuelCapacity = 1000,
            WeaponDamageModBps = 9000,
            // +20% shield (45→54), -10% weapons damage (10000→9000 bps). 1 cargo slot→utility.
        },

        // --- Chitin: speed and intelligence. Scan far, move fast, fragile. ---
        new ShipClassDef
        {
            ClassId = "gambit", DisplayName = "Gambit-class Corvette",
            FactionId = "chitin", BaseClassId = "corvette",
            SlotCount = 5, BasePower = 40, CargoCapacity = 50, Mass = 26, ScanRange = 100,
            BaseZoneArmor = new[] { 25, 20, 20, 15 }, CoreHull = 48, BaseShield = 35, BaseFuelCapacity = 500,
            // +25% scan (80→100), +15% speed (-15% mass 30→26), -20% hull (60→48). 1 utility→engine.
        },
        new ShipClassDef
        {
            ClassId = "wager", DisplayName = "Wager-class Clipper",
            FactionId = "chitin", BaseClassId = "clipper",
            SlotCount = 4, BasePower = 35, CargoCapacity = 60, Mass = 20, ScanRange = 120,
            BaseZoneArmor = new[] { 15, 15, 15, 30 }, CoreHull = 45, BaseShield = 26, BaseFuelCapacity = 600,
            // +20% scan (100→120), +20% speed (-20% mass 25→20), -15% shield (30→26), -10% hull (50→45).
        },

        // --- Weavers: structural resilience. Armor, hull, slow. ---
        new ShipClassDef
        {
            ClassId = "spindle", DisplayName = "Spindle-class Hauler",
            FactionId = "weavers", BaseClassId = "hauler",
            SlotCount = 4, BasePower = 30, CargoCapacity = 120, Mass = 75, ScanRange = 50,
            BaseZoneArmor = new[] { 26, 33, 33, 33 }, CoreHull = 96, BaseShield = 30, BaseFuelCapacity = 800,
            // +30% zone armor all, +20% hull (80→96), -25% speed (+25% mass 60→75). 1 engine→utility.
        },
        new ShipClassDef
        {
            ClassId = "loom", DisplayName = "Loom-class Cruiser",
            FactionId = "weavers", BaseClassId = "cruiser",
            SlotCount = 8, BasePower = 75, CargoCapacity = 60, Mass = 84, ScanRange = 90,
            BaseZoneArmor = new[] { 38, 38, 38, 31 }, CoreHull = 115, BaseShield = 50, BaseFuelCapacity = 600,
            // +25% zone armor all, +15% hull (100→115), -20% speed (+20% mass 70→84).
        },

        // --- Valorin: aggression and speed. Hit fast, loot, shields optional. ---
        new ShipClassDef
        {
            ClassId = "fang", DisplayName = "Fang-class Corvette",
            FactionId = "valorin", BaseClassId = "corvette",
            SlotCount = 5, BasePower = 40, CargoCapacity = 50, Mass = 23, ScanRange = 80,
            BaseZoneArmor = new[] { 25, 20, 20, 15 }, CoreHull = 60, BaseShield = 28, BaseFuelCapacity = 500,
            WeaponDamageModBps = 11500,
            // +25% speed (-25% mass 30→23), +15% weapon damage (10000→11500 bps), -20% shield (35→28).
        },
        new ShipClassDef
        {
            ClassId = "runner", DisplayName = "Runner-class Clipper",
            FactionId = "valorin", BaseClassId = "clipper",
            SlotCount = 4, BasePower = 35, CargoCapacity = 60, Mass = 20, ScanRange = 100,
            BaseZoneArmor = new[] { 15, 15, 15, 30 }, CoreHull = 45, BaseShield = 26, BaseFuelCapacity = 600,
            // +20% speed (-20% mass 25→20), -15% shield (30→26), -10% hull (50→45). 1 utility→cargo.
        },
        new ShipClassDef
        {
            ClassId = "raider", DisplayName = "Raider-class Frigate",
            FactionId = "valorin", BaseClassId = "frigate",
            // GATE.T59.SHIP.BALANCE_PASS.001: Raider nerfed — cargo bonus removed, hull malus added.
            // Was +15% cargo, now +0%. Added -15% hull. Ratio now 2:2 (speed+dmg vs shield+hull).
            SlotCount = 6, BasePower = 55, CargoCapacity = 50, Mass = 36, ScanRange = 70,
            BaseZoneArmor = new[] { 35, 25, 25, 15 }, CoreHull = 60, BaseShield = 32, BaseFuelCapacity = 400,
            WeaponDamageModBps = 11000,
            // +20% speed (-20% mass 45→36), +10% weapon damage, -20% shield (40→32), -15% hull (70→60).
        },

        // --- Communion: exploration and perception. See everything, fight nothing. ---
        new ShipClassDef
        {
            ClassId = "wanderer", DisplayName = "Wanderer-class Shuttle",
            FactionId = "communion", BaseClassId = "shuttle",
            SlotCount = 3, BasePower = 20, CargoCapacity = 30, Mass = 15, ScanRange = 84,
            BaseZoneArmor = new[] { 15, 10, 10, 10 }, CoreHull = 36, BaseShield = 20, BaseFuelCapacity = 200,
            WeaponDamageModBps = 7000,
            // +40% scan (60→84), -30% weapon damage (10000→7000 bps), -10% hull (40→36). 1 weapon→utility.
        },
        new ShipClassDef
        {
            ClassId = "pilgrim", DisplayName = "Pilgrim-class Clipper",
            FactionId = "communion", BaseClassId = "clipper",
            SlotCount = 4, BasePower = 35, CargoCapacity = 60, Mass = 25, ScanRange = 130,
            BaseZoneArmor = new[] { 15, 15, 15, 30 }, CoreHull = 50, BaseShield = 30, BaseFuelCapacity = 600,
            WeaponDamageModBps = 7500, FractureResistBps = 5000,
            // +30% scan (100→130), -25% weapon damage (10000→7500 bps), +fracture resistance (5000 bps = 50%).
        },
    };

    private static readonly Dictionary<string, ShipClassDef> _byId;

    static ShipClassContentV0()
    {
        _byId = new Dictionary<string, ShipClassDef>(StringComparer.Ordinal);
        foreach (var c in AllClasses)
            _byId[c.ClassId] = c;
    }

    public static ShipClassDef? GetById(string classId)
    {
        if (string.IsNullOrEmpty(classId)) return null;
        return _byId.TryGetValue(classId, out var def) ? def : null;
    }
}
