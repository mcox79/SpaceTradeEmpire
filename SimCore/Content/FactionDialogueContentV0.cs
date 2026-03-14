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
}
