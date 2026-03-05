using System.Collections.Generic;
using SimCore.Entities;

namespace SimCore.Content;

// GATE.S4.CONSTR_PROG.MODEL.001: Construction project definitions.
// Content layer — static definitions, no mutable state.
public static class ConstructionContentV0
{
    public static readonly IReadOnlyList<ConstructionDef> AllProjects = new List<ConstructionDef>
    {
        new ConstructionDef
        {
            ProjectDefId = "constr_depot_v0",
            DisplayName = "Supply Depot",
            Type = ConstructionType.Depot,
            TotalSteps = 3,
            TicksPerStep = 20,
            CreditCostPerStep = 150,
            Prerequisites = new List<string>(),
            Description = "A supply depot increases local storage capacity and enables logistics staging."
        },
        new ConstructionDef
        {
            ProjectDefId = "constr_shipyard_v0",
            DisplayName = "Shipyard",
            Type = ConstructionType.Shipyard,
            TotalSteps = 4,
            TicksPerStep = 30,
            CreditCostPerStep = 300,
            Prerequisites = new List<string> { "tech_basic_engineering" },
            Description = "A shipyard enables fleet construction and advanced refit operations."
        },
        new ConstructionDef
        {
            ProjectDefId = "constr_refinery_v0",
            DisplayName = "Refinery",
            Type = ConstructionType.Refinery,
            TotalSteps = 3,
            TicksPerStep = 25,
            CreditCostPerStep = 200,
            Prerequisites = new List<string>(),
            Description = "A refinery processes raw materials into refined goods for manufacturing."
        },
        new ConstructionDef
        {
            ProjectDefId = "constr_science_center_v0",
            DisplayName = "Science Center",
            Type = ConstructionType.ScienceCenter,
            TotalSteps = 5,
            TicksPerStep = 35,
            CreditCostPerStep = 400,
            Prerequisites = new List<string> { "tech_basic_engineering", "tech_sensor_array" },
            Description = "A science center provides research throughput and artifact analysis capability."
        },
    };

    private static readonly Dictionary<string, ConstructionDef> _byId;

    static ConstructionContentV0()
    {
        _byId = new Dictionary<string, ConstructionDef>(System.StringComparer.Ordinal);
        foreach (var d in AllProjects)
            _byId[d.ProjectDefId] = d;
    }

    public static ConstructionDef? GetById(string projectDefId)
    {
        if (string.IsNullOrEmpty(projectDefId)) return null;
        return _byId.TryGetValue(projectDefId, out var def) ? def : null;
    }

    public static bool PrerequisitesMet(string projectDefId, HashSet<string> unlockedTechIds)
    {
        var def = GetById(projectDefId);
        if (def == null) return false;
        foreach (var prereq in def.Prerequisites)
            if (!unlockedTechIds.Contains(prereq)) return false;
        return true;
    }
}
