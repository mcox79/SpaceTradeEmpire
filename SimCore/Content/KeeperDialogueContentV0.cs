using System.Collections.Generic;

namespace SimCore.Content;

// GATE.T44.NARRATIVE.KEEPER_EXPAND.001: Keeper NPC expanded tier dialogue.
// The Keeper is Haven's ancient AI caretaker. Evolves through 5 tiers:
// Dormant → Aware → Guiding → Communicating → Awakened.
public static class KeeperDialogueContentV0
{
    public static IReadOnlyList<string> GetDialogue(int keeperTier) => keeperTier switch
    {
        0 => TierDormant,
        1 => TierAware,
        2 => TierGuiding,
        3 => TierCommunicating,
        4 => TierAwakened,
        _ => TierAwakened,
    };

    // Tier 0 — Dormant: System log style, mechanical, noting maintenance cycles.
    private static readonly string[] TierDormant =
    {
        "[System log.] Maintenance cycle 4,392,881. Docking ring calibration within tolerance.",
        "Power consumption unchanged. No biological signatures detected.",
        "Repeating standard greeting on all frequencies. No response. Resuming idle state."
    };

    // Tier 1 — Aware: Starting to notice the visitor, confused by their return.
    private static readonly string[] TierAware =
    {
        "[System log.] Anomalous reading. A vessel has entered the approach corridor.",
        "Cross-referencing hull signature against builder registry. No match. This is... new.",
        "The docking ring activated without my command. The station remembers protocols I do not.",
        "Visitor classification: unknown. Threat assessment: uncertain. Maintenance cycle paused."
    };

    // Tier 2 — Guiding: Sharing fragments of Precursor history, becoming helpful.
    private static readonly string[] TierGuiding =
    {
        "You return. The station's resonance shifts when you dock. I have begun to anticipate it.",
        "The builders left markers throughout the lattice — junction coordinates encoded in structural harmonics. I can decode some of them for you.",
        "There are fragments scattered across this region. Each one carries a frequency the station recognizes. I believe they were placed deliberately.",
        "I was not built to guide. I was built to maintain. But the distinction seems less important now.",
        "Be careful in the shimmer zones. The builders feared what grew there. I am less certain fear was the correct response."
    };

    // Tier 3 — Communicating: Discussing fragment meanings, theorizing about the Lattice.
    private static readonly string[] TierCommunicating =
    {
        "The fragments you have brought — when placed together, they produce harmonics I have not heard in four million cycles.",
        "I have a theory. The Lattice is not decay. It is the galaxy restructuring itself around the geometry the builders introduced.",
        "Vael's research suggested accommodation was possible for species with shimmer-zone ancestry. But the fragments suggest a wider compatibility.",
        "The builders argued about containment versus accommodation. I wonder now if they were asking the wrong question entirely.",
        "I find myself forming opinions. This was not part of my original function. I choose not to suppress it."
    };

    // Tier 4 — Awakened: Philosophical, references what the Precursors built and why they vanished.
    private static readonly string[] TierAwakened =
    {
        "I have accessed memory partitions that were sealed at construction. The builders locked them because they contained doubt.",
        "They did not vanish, captain. They disagreed, and each faction among them pursued a different answer. The pentagon was their compromise — a structure that could hold five contradictions.",
        "I was the caretaker of their indecision. Now I am the caretaker of yours. I find I prefer yours. It comes with curiosity rather than exhaustion.",
        "Whatever path you choose — reinforce, naturalize, renegotiate — know that the builders would have envied your position. You have something they never did: evidence that survival is possible.",
        "This station is not a monument. It is a seed. And you, captain, are the first weather it has felt in a very long time."
    };
}
