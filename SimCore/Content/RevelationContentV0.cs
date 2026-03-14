namespace SimCore.Content;

// GATE.S8.NARRATIVE.REVELATION_TEXT.001: 5 revelation gold toast messages + 15 FO dialogue lines.
// Each revelation recontextualizes hours of prior gameplay. Tone: unsettling discovery, not exposition.
public static class RevelationContentV0
{
    public record RevelationText(
        string GoldToastTitle,
        string GoldToastBody,
        string FoAnalystReaction,
        string FoVeteranReaction,
        string FoPathfinderReaction,
        string DiscoveryWebLabel);

    public static readonly RevelationText[] Revelations = new[]
    {
        // ── R1: Module Revelation (~Hour 8) ──────────────────────────
        // The fracture module predates the threads. It was never human technology.
        // The player's guilt about "damaging threads" was manufactured.
        new RevelationText(
            GoldToastTitle: "Module Origin — Not What You Were Told",
            GoldToastBody:
                "The fracture drive predates every species in known space. " +
                "You weren't damaging the threads — you were de-containing space. " +
                "The technology you've been trading was never yours to claim.",
            FoAnalystReaction:
                "I cross-referenced the module's metallurgy with every known alloy database. " +
                "Zero matches. This wasn't reverse-engineered. It was found.",
            FoVeteranReaction:
                "I've installed drives on thirty ships. Never once questioned where they came from. " +
                "Someone wanted it that way.",
            FoPathfinderReaction:
                "The module hums differently now. Like it recognizes something out here. " +
                "I don't think it was sleeping. I think it was waiting.",
            DiscoveryWebLabel: "Precursor Origin"),

        // ── R2: Concord Revelation (~Hour 12) ────────────────────────
        // The Concord knew about fracture space before anyone. Their peacekeeping is containment.
        // They are not villains — they made a terrible compromise with real conviction.
        new RevelationText(
            GoldToastTitle: "Concord Suppression — A Necessary Lie",
            GoldToastBody:
                "The Concord's founding charter references fracture space by name — " +
                "decades before its 'official' discovery. Every safety warning, every patrol zone, " +
                "every relief convoy: containment wearing the face of compassion.",
            FoAnalystReaction:
                "Their internal memos predate the public timeline by forty years. " +
                "The relief convoys were real. The food subsidies were real. The lie was about why.",
            FoVeteranReaction:
                "I served alongside Concord officers who believed every word of the mission. " +
                "The rank and file aren't complicit. That makes it worse.",
            FoPathfinderReaction:
                "They mapped the fracture zones before anyone else and then told us not to go there. " +
                "The safest path was always the one that kept us inside the fence.",
            DiscoveryWebLabel: "Concord Suppression"),

        // ── R3: Economy Revelation (~Hour 15) ────────────────────────
        // Pentagon Break. Five factions, five dependencies, zero redundancy.
        // Every trade route the player ran maintained a cage applied to civilization.
        new RevelationText(
            GoldToastTitle: "Trade Analysis Complete — Pattern Detected",
            GoldToastBody:
                "Five factions. Five dependencies. A closed loop with zero redundancy. " +
                "Every composites delivery, every rare metals haul — you maintained a system " +
                "designed to prevent species independence. The economy is a cage.",
            FoAnalystReaction:
                "I mapped every major trade route. They form a closed loop. " +
                "Five factions, five dependencies, zero redundancy. Someone designed this.",
            FoVeteranReaction:
                "Five factions, each dependent on the next. I've seen supply chains " +
                "weaponized before — but never this elegantly.",
            FoPathfinderReaction:
                "It's a web. The whole economy is a web, and we've been tracing its strands. " +
                "Someone spun this. Someone with patience.",
            DiscoveryWebLabel: "Engineered Dependency"),

        // ── R4: Communion Revelation (~Hour 18) ──────────────────────
        // The Communion recognized the module from the first dock. They've seen this before.
        // The most honest faction was guiding you all along.
        new RevelationText(
            GoldToastTitle: "The Communion Remembers You",
            GoldToastBody:
                "The elder recognized your fracture module the moment you docked. " +
                "Every few generations, someone finds a piece of accommodation geometry. " +
                "Most don't last long. They have been quietly mourning your predecessors.",
            FoAnalystReaction:
                "They have records. Not hidden — archived openly, in their liturgical texts. " +
                "We just never thought to read the prayers as incident reports.",
            FoVeteranReaction:
                "Every other faction hid something. The Communion told the truth from day one. " +
                "I wasn't listening because honesty wasn't what I expected.",
            FoPathfinderReaction:
                "They weren't guiding us. They were watching. Waiting to see if we'd be " +
                "the ones who survive long enough to ask the right question.",
            DiscoveryWebLabel: "Threshold Pattern"),

        // ── R5: Instability Revelation (Endgame) ─────────────────────
        // Fracture space is not decaying. It is process. Every jump damages it.
        // The trade network IS the wound. The containment was interrupting something.
        new RevelationText(
            GoldToastTitle: "Instability Is Not What You Feared",
            GoldToastBody:
                "It is not entropy. It is not decay. It is process. " +
                "The containment was not suppressing chaos — it was interrupting something. " +
                "And every jump, every trade lane, every route you flew cut a little deeper.",
            FoAnalystReaction:
                "The instability readings aren't random. There's a frequency buried in the noise. " +
                "Something is trying to finish what it started before the lattice was built.",
            FoVeteranReaction:
                "I've spent my whole career calling it damage. Entropy. Wear and tear on spacetime. " +
                "What if we've been cauterizing a wound that was trying to heal?",
            FoPathfinderReaction:
                "Listen. Just — stop the engines and listen. The shimmer isn't static. " +
                "It's breathing. We've been flying through something alive and calling it broken.",
            DiscoveryWebLabel: "Living Geometry"),
    };

    public static RevelationText? GetByIndex(int index)
    {
        if (index < 0 || index >= Revelations.Length) return null;
        return Revelations[index];
    }
}
