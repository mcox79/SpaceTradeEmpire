using System.Collections.Generic;

namespace SimCore.Content;

// GATE.T44.NARRATIVE.COMMUNION_DIALOGUE.001: Communion Representative 3-arc dialogue content.
// The Communion emissary appears at Haven when tier >= Operational and Communion rep >= 0.
// Three arcs indexed by DialogueTier (0, 1, 2).
public static class CommunionRepDialogueContentV0
{
    public static IReadOnlyList<string> GetDialogue(int dialogueTier) => dialogueTier switch
    {
        0 => Tier0,
        1 => Tier1,
        2 => Tier2,
        _ => Tier2,
    };

    // Tier 0 — Introduction: The Communion believes in coexistence with the Lattice.
    // Precursor technology is a bridge, not a weapon.
    private static readonly string[] Tier0 =
    {
        "I am called Syrel, and the Communion has sent me because this place remembers us.",
        "You have found what the Precursors left behind. Most would see a weapon. We see a conversation that was never finished.",
        "The Lattice is not an enemy, captain. It is a question posed in geometry. The Precursors understood this before the end.",
        "We ask only that you listen before you choose. The galaxy has had enough answers built from fear."
    };

    // Tier 1 — Path Guidance: Counsels on three endgame paths. Neutral but subtly favors Naturalize.
    private static readonly string[] Tier1 =
    {
        "Three paths lie before you now, each carved by a different understanding of what the Precursors began.",
        "Reinforce: fortify the existing order. The Hegemony and Dominion believe strength alone holds the fractures closed. There is courage in this, and blindness.",
        "Naturalize: accept the Lattice as part of the galaxy's living geometry. This is what the Communion has always taught. The instability is not a wound — it is growth.",
        "Renegotiate: seek a diplomatic equilibrium among all factions. The Sovereignty favors this balance. It is careful, and it is slow.",
        "I will not pretend neutrality. The Communion has walked inside the shimmer zones for generations. We know what coexistence feels like. But the choice must be yours."
    };

    // Tier 2 — Endgame Counsel: Final wisdom. References the Precursors' choice and its consequences.
    private static readonly string[] Tier2 =
    {
        "The Precursors faced this same crossroads. Five voices argued — cage it, adapt to it, leave it, exploit it, or endure it. They could not agree.",
        "Their disagreement was not failure, captain. It was honesty. Every path had a cost they were unwilling to name.",
        "You stand where they stood, but you carry something they did not: the proof that the galaxy survived their absence. The pentagon held. The factions endured.",
        "Whatever you choose, let it be because you understood the question — not because you feared the silence that follows."
    };
}
