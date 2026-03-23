using System.Collections.Generic;

namespace SimCore.Content;

// GATE.S8.NARRATIVE.FACTION_DIALOGUE.001: Faction representative dialogue per rep tier.
public static class FactionDialogueContentV0
{
    public sealed class DialogueEntry
    {
        public string FactionId { get; init; } = "";
        public string RepTier { get; init; } = "";  // "neutral", "friendly", "allied"
        public string[] Lines { get; init; } = System.Array.Empty<string>();
    }

    public static readonly List<DialogueEntry> AllDialogues = new()
    {
        // === CONCORD (Human, Order, Bureaucratic) ===
        new()
        {
            FactionId = "concord",
            RepTier = "neutral",
            Lines = new[]
            {
                "Welcome, captain. We trust your credentials are in order.",
                "The Concord maintains strict protocols. Adherence ensures prosperity for all.",
                "We shall review your manifest and determine appropriate trade terms."
            }
        },
        new()
        {
            FactionId = "concord",
            RepTier = "friendly",
            Lines = new[]
            {
                "Ah, a reliable partner. Your track record reflects well upon our alliance.",
                "The Concord values stability and honor in commerce. You exemplify both.",
                "Our institutions stand stronger with captains like yourself."
            }
        },
        new()
        {
            FactionId = "concord",
            RepTier = "allied",
            Lines = new[]
            {
                "You have earned the Concord's deepest trust. Few reach this standing.",
                "Our archives hold strategic knowledge we share only with allies. The void grows darker.",
                "Should you ever need our resources or counsel, you need only ask."
            }
        },

        // === CHITIN (Insectoid, Adaptation, Collective) ===
        new()
        {
            FactionId = "chitin",
            RepTier = "neutral",
            Lines = new[]
            {
                "The hive observes you. Your purpose is...not yet clear.",
                "We sense caution in you, traveler. Wise. The unknown demands respect.",
                "Trade is acceptable. Our colonies require external resources to evolve."
            }
        },
        new()
        {
            FactionId = "chitin",
            RepTier = "friendly",
            Lines = new[]
            {
                "We sense kinship in your adaptations, captain. The hive appreciates flexibility.",
                "You have proven yourself a beneficial mutation to our operations.",
                "Come, share what you learn. The Chitin collective grows stronger through shared insight."
            }
        },
        new()
        {
            FactionId = "chitin",
            RepTier = "allied",
            Lines = new[]
            {
                "You are becoming part of our pattern, captain. Few outsiders achieve this synthesis.",
                "The hive consciousness extends a tendril to you—we sense your deeper purpose aligns with ours.",
                "Our deepest colonies, our metamorphosis chambers... these secrets are yours now."
            }
        },

        // === WEAVERS (Silicon, Structure, Mathematical) ===
        new()
        {
            FactionId = "weavers",
            RepTier = "neutral",
            Lines = new[]
            {
                "Your approach demonstrates basic structural integrity. This is... acceptable.",
                "The pattern recognizes efficiency in your trade. Geometry, however, remains unproven.",
                "We shall observe your construction. Perhaps your role in the grand design will become apparent."
            }
        },
        new()
        {
            FactionId = "weavers",
            RepTier = "friendly",
            Lines = new[]
            {
                "You have begun to understand the beauty of our architecture, captain.",
                "The Weavers appreciate those who grasp that all things connect in elegant harmony.",
                "Your decisions reflect structural soundness. The pattern holds strong with your guidance."
            }
        },
        new()
        {
            FactionId = "weavers",
            RepTier = "allied",
            Lines = new[]
            {
                "You have become a vertex in our grand lattice, captain. This is profound honor.",
                "Our foundational blueprints—the frameworks upon which galaxies are built—these we entrust to you.",
                "The pattern is complete with you. Together, we construct transcendence."
            }
        },

        // === VALORIN (Mammalian, Expansion, Martial) ===
        new()
        {
            FactionId = "valorin",
            RepTier = "neutral",
            Lines = new[]
            {
                "You have spine, captain. We respect those who dare the void.",
                "The hunt continues. Valorin looks for partners who can match our pace.",
                "Prove your strength through trade and conquest alike. Then we shall speak of destiny."
            }
        },
        new()
        {
            FactionId = "valorin",
            RepTier = "friendly",
            Lines = new[]
            {
                "Strength recognizes strength, captain. Your victories please the Valorin.",
                "The hunt is better with you at our flank. You have earned our respect.",
                "Our war banners fly higher knowing warriors like you fight beneath them."
            }
        },
        new()
        {
            FactionId = "valorin",
            RepTier = "allied",
            Lines = new[]
            {
                "You are no longer merely an ally—you are blood-bound to Valorin glory.",
                "Our enemies tremble knowing you stand with us. Territories, ships, secrets—all are yours.",
                "The expansion of Valorin is your expansion. Together, we shall dominate the stars."
            }
        },

        // === COMMUNION (Ethereal, Understanding, Mystical) ===
        new()
        {
            FactionId = "communion",
            RepTier = "neutral",
            Lines = new[]
            {
                "The resonance acknowledges your presence, captain. Your essence is... curious.",
                "We sense questions within you. The Communion holds answers for those willing to listen.",
                "Unity is our path. Whether you join remains to be seen."
            }
        },
        new()
        {
            FactionId = "communion",
            RepTier = "friendly",
            Lines = new[]
            {
                "Your consciousness has begun to harmonize with ours, captain. This is beautiful.",
                "The Communion feels your growth. You move closer to understanding the greater whole.",
                "Share your wisdom. Our collective wisdom multiplies when voices like yours are heard."
            }
        },
        new()
        {
            FactionId = "communion",
            RepTier = "allied",
            Lines = new[]
            {
                "You have transcended duality, captain. You are Communion, and Communion is you.",
                "Our most sacred resonances, the frequencies upon which reality itself vibrates—these we attune to you.",
                "The unity you have found with us reveals truths older than stars. You are forever changed."
            }
        }
    };

    public static DialogueEntry? GetDialogue(string factionId, string repTier)
    {
        foreach (var d in AllDialogues)
            if (d.FactionId == factionId && d.RepTier == repTier) return d;
        return null;
    }

    // GATE.T47.HAVEN.COMMUNION_REP.001: Communion Representative dialogue lines at Haven.
    // 8 thematic lines — mysterious, philosophical, hinting at deeper geometry.
    // Available when Haven tier >= 3 (Operational). Filtered by category "communion_representative".
    public sealed class CommunionRepLine
    {
        public string Category { get; init; } = "communion_representative";
        public string Tag { get; init; } = "";
        public string Text { get; init; } = "";
    }

    public static readonly List<CommunionRepLine> CommunionRepDialogues = new()
    {
        new() { Tag = "greeting",    Text = "The lattice recognizes your presence. We have been... expecting you." },
        new() { Tag = "lore_hint",   Text = "What you call instability, we call remembering. The threads were never meant to be still." },
        new() { Tag = "faction",     Text = "The other factions seal what they fear. We listen to what they silence." },
        new() { Tag = "trade_offer", Text = "We can offer materials your instruments cannot yet perceive. The exchange rate is... unconventional." },
        new() { Tag = "warning",     Text = "Your Haven grows strong. Be mindful \u2014 strength attracts attention from deeper geometries." },
        new() { Tag = "philosophy",  Text = "You ask why we help you. Consider: does the hand help the fingers, or do the fingers help the hand?" },
        new() { Tag = "knowledge",   Text = "The fragments you collect are not tools. They are questions. The resonance pairs are the universe whispering answers." },
        new() { Tag = "farewell",    Text = "We will be here. We are always here. The lattice does not forget its own." },
    };

    /// <summary>
    /// Returns a Communion Representative dialogue line by deterministic index (seed % count).
    /// Returns null if no lines are available.
    /// </summary>
    public static CommunionRepLine? GetCommunionRepLine(int seed)
    {
        if (CommunionRepDialogues.Count == 0) return null;
        // STRUCTURAL: 8 communion rep lines, deterministic selection
        int index = ((seed % CommunionRepDialogues.Count) + CommunionRepDialogues.Count) % CommunionRepDialogues.Count;
        return CommunionRepDialogues[index];
    }

    // GATE.T46.STATION.DOCK_FLAVOR.001: Per-faction dock greetings (5 per faction = 25 total).
    // Selected deterministically by seed % 5.
    private static readonly Dictionary<string, string[]> DockGreetings = new()
    {
        ["concord"] = new[]
        {
            "Welcome, trader. Docking fees are current.",
            "Concord station control confirms your berth. Manifest review in progress.",
            "All trade licenses verified. Proceed to assigned platform.",
            "Regulatory compliance noted. You may conduct business at your discretion.",
            "Station authority acknowledges your arrival. Commerce hours are in effect.",
        },
        ["chitin"] = new[]
        {
            "The hive registers your presence. What do you wager?",
            "Another vessel enters the colony. The swarm calculates your value.",
            "Docking granted. The odds of profitable exchange are... acceptable.",
            "Your ship's signature has been cataloged. The collective weighs your cargo.",
            "The brood permits your approach. Every transaction is a gamble worth taking.",
        },
        ["valorin"] = new[]
        {
            "State your business. Supply manifests to dock control.",
            "Valorin garrison acknowledges your approach. Weapons remain holstered.",
            "You dock under the banner of honor. Trade with strength or depart.",
            "Station armory is open to those who have proven their mettle.",
            "The watch commander grants you berth. Do not waste our time.",
        },
        ["weavers"] = new[]
        {
            "The threads note your arrival. What pattern do you weave?",
            "A new variable enters the lattice. Your trajectory has been computed.",
            "Docking geometry confirmed. Your presence alters the local equation.",
            "The network acknowledges this node intersection. Proceed with structure.",
            "Signal received and integrated. The pattern shifts to accommodate you.",
        },
        ["communion"] = new[]
        {
            "You are recognized, traveler. The collective extends hospitality.",
            "Your resonance precedes you. The Communion welcomes your frequency.",
            "We felt your approach before sensors confirmed it. You belong here.",
            "The harmonic embrace of this station is open to you, pilgrim.",
            "Unity grows with each arrival. Your presence enriches the chorus.",
        },
    };

    // GATE.T46.STATION.DOCK_FLAVOR.001: Per-faction station description templates (5 per faction = 25 total).
    // Index 0=outpost, 1=hub, 2=capital; indices 3-4 are alternate descriptions for variety.
    private static readonly Dictionary<string, string[]> StationDescriptions = new()
    {
        ["concord"] = new[]
        {
            "A modest Concord outpost, staffed by a skeleton crew of customs officials.",
            "A busy Concord trade hub with regulated commerce lanes and auditor offices.",
            "The Concord capital station, a monument to bureaucratic order and free commerce.",
            "A well-maintained Concord facility where every transaction is logged in triplicate.",
            "A Concord station humming with the quiet efficiency of institutional routine.",
        },
        ["chitin"] = new[]
        {
            "A small Chitin colony node, its walls glistening with organic resin.",
            "A thriving Chitin hub where drones ferry cargo through hexagonal corridors.",
            "The Chitin capital hive, a vast organic superstructure pulsing with collective purpose.",
            "A Chitin waystation where the air carries the faint scent of pheromone signals.",
            "A Chitin trade nest, its architecture grown rather than built.",
        },
        ["valorin"] = new[]
        {
            "A fortified Valorin outpost, bristling with defensive turrets and patrol craft.",
            "A Valorin military hub where warriors and merchants share the same iron code.",
            "The Valorin capital fortress, an impregnable citadel forged in the fires of conquest.",
            "A Valorin garrison station where the clang of arms echoes through armored halls.",
            "A Valorin stronghold where honor plaques line every corridor.",
        },
        ["weavers"] = new[]
        {
            "A minimal Weaver relay node, its crystalline spires channeling data streams.",
            "A Weaver network hub, shimmering with holographic lattice projections.",
            "The Weaver prime nexus, an impossibly intricate structure of light and mathematics.",
            "A Weaver processing station where reality itself seems slightly refracted.",
            "A Weaver junction point, its surfaces alive with flowing geometric patterns.",
        },
        ["communion"] = new[]
        {
            "A quiet Communion shrine-station, bathed in soft bioluminescent light.",
            "A Communion gathering hub where voices blend into a constant harmonic murmur.",
            "The Communion grand sanctum, a cathedral of shared consciousness spanning kilometers.",
            "A Communion waypoint where the boundary between self and collective grows thin.",
            "A Communion meditation station, its chambers resonating with psychic undertones.",
        },
    };

    /// <summary>
    /// Returns a dock greeting for the given faction, selected deterministically by seed % 5.
    /// </summary>
    public static string GetDockGreeting(string factionId, int seed)
    {
        if (string.IsNullOrEmpty(factionId) || !DockGreetings.TryGetValue(factionId, out var greetings))
            return "Docking protocols engaged.";
        // STRUCTURAL: 5 greetings per faction, deterministic selection
        int index = ((seed % 5) + 5) % 5;
        return greetings[index];
    }

    /// <summary>
    /// Returns a station description for the given faction, selected by station tier.
    /// Tier 0=outpost, 1=hub, 2=capital. Values 3-4 are alternates.
    /// </summary>
    public static string GetStationDescription(string factionId, int stationTier)
    {
        if (string.IsNullOrEmpty(factionId) || !StationDescriptions.TryGetValue(factionId, out var descriptions))
            return "A station of unknown origin.";
        // STRUCTURAL: clamp tier to valid range 0-4
        int index = stationTier < 0 ? 0 : (stationTier > 4 ? 4 : stationTier);
        return descriptions[index];
    }
}
