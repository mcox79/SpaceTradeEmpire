using System.Collections.Generic;

namespace SimCore.Content;

// GATE.S8.MEGAPROJECT.ENTITY.001: Megaproject type definitions.
public class MegaprojectDef
{
    public string TypeId { get; init; } = "";
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public int Stages { get; init; } = 3;
    public int TicksPerStage { get; init; } = 100;
    public int CreditCost { get; init; } = 5000;

    // Per-stage supply requirements: goodId → quantity needed.
    public Dictionary<string, int> SupplyPerStage { get; init; } = new();

    // Map rule mutation type applied on completion.
    public MegaprojectMutationType MutationType { get; init; } = MegaprojectMutationType.None;

    // Minimum faction rep required to start at the target node.
    public int MinFactionRep { get; init; } = 0;
}

public enum MegaprojectMutationType
{
    None = 0,
    FractureAnchor = 1,    // Create permanent void lane endpoint
    TradeCorridor = 2,     // Reduce transit time between nodes
    SensorPylon = 3        // Extend scan range in region
}

// GATE.S8.MEGAPROJECT.ENTITY.001: Canonical megaproject set.
public static class MegaprojectContentV0
{
    public static readonly MegaprojectDef FractureAnchor = new()
    {
        TypeId = "fracture_anchor",
        Name = "Fracture Anchor",
        Description = "Stabilizes a fracture point, creating a permanent void lane endpoint at this node.",
        Stages = 3,
        TicksPerStage = Tweaks.MegaprojectTweaksV0.AnchorTicksPerStage,
        CreditCost = Tweaks.MegaprojectTweaksV0.AnchorCreditCost,
        SupplyPerStage = new()
        {
            { WellKnownGoodIds.ExoticMatter, Tweaks.MegaprojectTweaksV0.AnchorExoticMatterPerStage },
            { WellKnownGoodIds.Composites, Tweaks.MegaprojectTweaksV0.AnchorCompositesPerStage },
        },
        MutationType = MegaprojectMutationType.FractureAnchor,
        MinFactionRep = Tweaks.MegaprojectTweaksV0.MinFactionRepToStart,
    };

    public static readonly MegaprojectDef TradeCorridor = new()
    {
        TypeId = "trade_corridor",
        Name = "Trade Corridor",
        Description = "Creates a high-throughput lane between two connected nodes with reduced transit time.",
        Stages = 4,
        TicksPerStage = Tweaks.MegaprojectTweaksV0.CorridorTicksPerStage,
        CreditCost = Tweaks.MegaprojectTweaksV0.CorridorCreditCost,
        SupplyPerStage = new()
        {
            { WellKnownGoodIds.RareMetals, Tweaks.MegaprojectTweaksV0.CorridorRareMetalsPerStage },
            { WellKnownGoodIds.Electronics, Tweaks.MegaprojectTweaksV0.CorridorElectronicsPerStage },
        },
        MutationType = MegaprojectMutationType.TradeCorridor,
        MinFactionRep = Tweaks.MegaprojectTweaksV0.MinFactionRepToStart,
    };

    public static readonly MegaprojectDef SensorPylon = new()
    {
        TypeId = "sensor_pylon",
        Name = "Sensor Pylon Network",
        Description = "Deploys sensor pylons extending scan range across a 3-hop region, revealing hidden void sites.",
        Stages = 3,
        TicksPerStage = Tweaks.MegaprojectTweaksV0.PylonTicksPerStage,
        CreditCost = Tweaks.MegaprojectTweaksV0.PylonCreditCost,
        SupplyPerStage = new()
        {
            { WellKnownGoodIds.Electronics, Tweaks.MegaprojectTweaksV0.PylonElectronicsPerStage },
            { WellKnownGoodIds.ExoticCrystals, Tweaks.MegaprojectTweaksV0.PylonExoticCrystalsPerStage },
        },
        MutationType = MegaprojectMutationType.SensorPylon,
        MinFactionRep = Tweaks.MegaprojectTweaksV0.MinFactionRepToStart,
    };

    public static readonly MegaprojectDef[] All = { FractureAnchor, TradeCorridor, SensorPylon };

    public static MegaprojectDef? GetByTypeId(string typeId)
    {
        foreach (var def in All)
            if (def.TypeId == typeId) return def;
        return null;
    }
}
