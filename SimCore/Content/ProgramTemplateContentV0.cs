using System.Collections.Generic;

namespace SimCore.Content;

// GATE.S7.AUTOMATION.TEMPLATES.001: Predefined automation program templates.
public sealed class ProgramTemplateDef
{
    public string TemplateId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public string ProgramKind { get; set; } = "";  // Maps to ProgramKind enum name
    public int DefaultCadenceTicks { get; set; } = 10;
}

public static class ProgramTemplateContentV0
{
    public static readonly IReadOnlyList<ProgramTemplateDef> AllTemplates = new List<ProgramTemplateDef>
    {
        new ProgramTemplateDef
        {
            TemplateId = "template_buy_low_sell_high",
            DisplayName = "Buy Low, Sell High",
            Description = "Automatically buys a good at one market and sells at another for profit.",
            ProgramKind = "TradeCharterV0",
            DefaultCadenceTicks = 10,
        },
        new ProgramTemplateDef
        {
            TemplateId = "template_supply_route",
            DisplayName = "Supply Route",
            Description = "Maintains a steady supply of goods between two stations.",
            ProgramKind = "AutoBuy",
            DefaultCadenceTicks = 15,
        },
        new ProgramTemplateDef
        {
            TemplateId = "template_arbitrage_loop",
            DisplayName = "Arbitrage Loop",
            Description = "Exploits price differences across multiple markets in a circuit.",
            ProgramKind = "TradeCharterV0",
            DefaultCadenceTicks = 8,
        },
        new ProgramTemplateDef
        {
            TemplateId = "template_war_supply_run",
            DisplayName = "War Supply Run",
            Description = "Delivers munitions and fuel to warfront stations.",
            ProgramKind = "TradeCharterV0",
            DefaultCadenceTicks = 12,
        },
        new ProgramTemplateDef
        {
            TemplateId = "template_fuel_hauler",
            DisplayName = "Fuel Hauler",
            Description = "Buys fuel from refineries and distributes to stations in need.",
            ProgramKind = "ResourceTapV0",
            DefaultCadenceTicks = 20,
        },
    };

    private static readonly Dictionary<string, ProgramTemplateDef> _byId;

    static ProgramTemplateContentV0()
    {
        _byId = new Dictionary<string, ProgramTemplateDef>(System.StringComparer.Ordinal);
        foreach (var t in AllTemplates)
            _byId[t.TemplateId] = t;
    }

    public static ProgramTemplateDef? GetById(string templateId)
    {
        if (string.IsNullOrEmpty(templateId)) return null;
        return _byId.TryGetValue(templateId, out var def) ? def : null;
    }
}
