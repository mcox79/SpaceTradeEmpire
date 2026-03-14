using System.Collections.Generic;

namespace SimCore.Content;

// GATE.S8.NARRATIVE.WARFRONT_COMMENTARY.001: Per-faction warfront commentary by intensity.
public static class WarfrontCommentaryContentV0
{
    public sealed class CommentaryEntry
    {
        public string FactionId { get; init; } = "";
        public int Intensity { get; init; }  // 0=Peace..4=TotalWar
        public string Text { get; init; } = "";
    }

    public static readonly List<CommentaryEntry> AllCommentary = new()
    {
        // Concord (Human peacekeeping federation)
        new() { FactionId = "concord", Intensity = 0, Text = "Concord maintains diplomatic presence in the sector. Trade routes remain stable under their protection." },
        new() { FactionId = "concord", Intensity = 1, Text = "Concord has raised alert status; their patrols have increased along disputed borders." },
        new() { FactionId = "concord", Intensity = 2, Text = "Concord forces have responded to border skirmishes with measured restraint and strategic positioning." },
        new() { FactionId = "concord", Intensity = 3, Text = "Concord mobilizes for full-scale conflict; battlegroups engage across multiple frontlines with overwhelming force." },
        new() { FactionId = "concord", Intensity = 4, Text = "Concord's existence hangs in balance. All available assets are committed to a desperate final stand." },

        // Chitin (Insectoid hive, adaptable)
        new() { FactionId = "chitin", Intensity = 0, Text = "Chitin colonies maintain equilibrium with neighboring territories. Hive operations proceed according to the collective will." },
        new() { FactionId = "chitin", Intensity = 1, Text = "Chitin swarms probe sector boundaries; adaptive behaviors suggest tactical reconnaissance underway." },
        new() { FactionId = "chitin", Intensity = 2, Text = "Chitin raiders strike supply lines in coordinated attacks. The hive consolidates territory through calculated incursions." },
        new() { FactionId = "chitin", Intensity = 3, Text = "Chitin deploys full hive strength; waves of fighters and capital ships surge across contested space in relentless assaults." },
        new() { FactionId = "chitin", Intensity = 4, Text = "The hive is locked in existential struggle. Every drone and collective mind is sacrificed for dominance or extinction." },

        // Weavers (Silicon constructors)
        new() { FactionId = "weavers", Intensity = 0, Text = "Weavers construct and maintain infrastructure in their territory. Their presence ensures systematic order and stability." },
        new() { FactionId = "weavers", Intensity = 1, Text = "Weavers activate defensive protocols and strategic asset repositioning. Logic circuits hum with analytical tension." },
        new() { FactionId = "weavers", Intensity = 2, Text = "Weavers deploy surgical strikes against hostile positions. Each action is calculated for maximum efficiency and minimal deviation." },
        new() { FactionId = "weavers", Intensity = 3, Text = "Weaver fabricators work overtime producing instruments of war. Combat algorithms optimize for systematic annihilation of threats." },
        new() { FactionId = "weavers", Intensity = 4, Text = "Weavers face existential logic failure. All processing power is devoted to a war calculation with binary outcome: victory or deletion." },

        // Valorin (Mammalian warriors)
        new() { FactionId = "valorin", Intensity = 0, Text = "Valorin clans uphold honor through disciplined presence. Trade and territorial agreements are honored without question." },
        new() { FactionId = "valorin", Intensity = 1, Text = "Valorin warriors sharpen blades and steel themselves. Honor demands readiness for conflict that may yet be averted." },
        new() { FactionId = "valorin", Intensity = 2, Text = "Valorin blood feuds ignite across borders. Clan pride and retribution drive increasingly aggressive raids and counter-raids." },
        new() { FactionId = "valorin", Intensity = 3, Text = "Valorin fleets converge for glorious battle. Every warrior embraces the honor of open combat against worthy foes across all fronts." },
        new() { FactionId = "valorin", Intensity = 4, Text = "Valorin faces their ultimate test of honor. The warrior clans pour every ounce of strength into a struggle for survival and legacy." },

        // Communion (Ethereal mystics)
        new() { FactionId = "communion", Intensity = 0, Text = "Communion observes and transcends mundane concerns. Their presence brings subtle balance and esoteric understanding to the region." },
        new() { FactionId = "communion", Intensity = 1, Text = "Communion senses disturbances in the underlying order. Prophetic warnings ripple through their networks of shared consciousness." },
        new() { FactionId = "communion", Intensity = 2, Text = "Communion emerges from isolation to defend sacred principles. Ethereal forces manifest in response to profane threats." },
        new() { FactionId = "communion", Intensity = 3, Text = "Communion unleashes transcendent wrath. Reality bends as their true power manifests in overwhelming conflicts across multiple dimensions of existence." },
        new() { FactionId = "communion", Intensity = 4, Text = "Communion faces the unthinkable: existential erasure. All mystical might converges in a desperate struggle to preserve their essence and destiny." }
    };

    public static string GetCommentary(string factionId, int intensity)
    {
        foreach (var c in AllCommentary)
            if (c.FactionId == factionId && c.Intensity == intensity) return c.Text;
        return "";
    }
}
