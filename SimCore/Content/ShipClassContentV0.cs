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
}

public static class ShipClassContentV0
{
    public static readonly IReadOnlyList<ShipClassDef> AllClasses = new List<ShipClassDef>
    {
        new ShipClassDef
        {
            ClassId = "shuttle", DisplayName = "Shuttle",
            SlotCount = 3, BasePower = 20, CargoCapacity = 30, Mass = 15, ScanRange = 60,
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
            SlotCount = 6, BasePower = 55, CargoCapacity = 40, Mass = 45, ScanRange = 70,
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
            SlotCount = 7, BasePower = 65, CargoCapacity = 80, Mass = 80, ScanRange = 120,
            BaseZoneArmor = new[] { 20, 25, 25, 20 }, CoreHull = 90, BaseShield = 45, BaseFuelCapacity = 1000,
        },
        new ShipClassDef
        {
            ClassId = "dreadnought", DisplayName = "Dreadnought",
            SlotCount = 10, BasePower = 100, CargoCapacity = 50, Mass = 120, ScanRange = 80,
            BaseZoneArmor = new[] { 50, 45, 45, 35 }, CoreHull = 150, BaseShield = 80, BaseFuelCapacity = 500,
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
