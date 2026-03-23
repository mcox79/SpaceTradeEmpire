using System;
using System.Collections.Generic;

namespace SimCore.Content;

// GATE.T46.NARRATIVE.HAVEN_LOGS.001: Haven starbase narrative log entries per tier.
// Players discover these as they upgrade Haven through its tiers.
public static class HavenLogContentV0
{
    private static readonly Dictionary<int, string[]> LogsByTier = new()
    {
        // Tier 0 — Shelter / Undiscovered: Discovery logs — finding the hidden station, first scans.
        [0] = new[]
        {
            "Initial scans detect stable accommodation geometry in a region where none should exist. The math is ancient — older than any faction's records.",
            "A dormant starbase, colossal in scale, drifts in a pocket of impossible calm. No Lattice signature. No containment threading. Just silence and structure.",
            "The station's outer hull is unmarked by micrometeorite erosion. Whatever force shaped this place also preserved it. Millions of years, untouched.",
            "Power conduits line every corridor in fractal patterns — not decoration, but functional geometry. The architecture itself is the engine.",
        },

        // Tier 1 — Powered: Establishment logs — powering up systems, first repairs.
        [1] = new[]
        {
            "The fracture module resonates with the station's accommodation lattice. Power surges through corridors that haven't seen light in geological ages.",
            "Environmental systems engage one by one — atmosphere, gravity, thermal regulation. Each activates with a low harmonic tone, like a chord being completed.",
            "Docking bay pressurizes. The air tastes clean, faintly metallic. Whatever filtration system this station uses, it still works perfectly after eons of dormancy.",
            "A faint pulse radiates from the station's core — rhythmic, warm. Not mechanical. The Keeper stirs in its alcove, a presence more felt than seen.",
        },

        // Tier 2 — Inhabited: Growth logs — traders arriving, market opening.
        [2] = new[]
        {
            "Crew quarters unsealed. Personal effects from a civilization we have no name for — crystalline data slates, woven light tapestries, seats shaped for bodies not quite like ours.",
            "The market hall activates as if expecting us. Display alcoves illuminate, price matrices flicker to life. The station remembers what commerce looks like.",
            "A research terminal surfaces from the floor when approached — accommodation geometry reshaping the room around our needs. The station learns what we require.",
            "Night cycle engages for the first time. The corridors dim to a warm amber. Through the observation ports, fracture space shimmers like aurora borealis frozen in glass.",
            "The Keeper's alcove glows brighter now. Not acknowledgment exactly — more like recognition. It knows we are staying.",
        },

        // Tier 3 — Stronghold / Operational: Strength logs — defense arrays online, faction notice.
        [3] = new[]
        {
            "Bidirectional thread achieved. The accommodation geometry shapes a counter-vortex in thread-space — a river that flows both ways. Coming home no longer costs us.",
            "Hangar bay expansion reveals drydock cradles sized for warships. Whatever civilization built this place, they knew conflict. They prepared for it.",
            "Defense arrays come online with unsettling grace — weapon emplacements that fold out of the hull like origami. Ancient engineering, but the targeting mathematics are timeless.",
            "Faction signals are beginning to probe our region of space. The station's accommodation field masks us, but increased traffic will eventually draw attention.",
            "The Keeper communicates now — not in words, but in patterns of light that our instruments translate into coordinates. Fragment locations. It wants us to find the pieces.",
        },

        // Tier 4 — Citadel / Expanded: Prominence logs — becoming a power center, diplomatic visitors.
        [4] = new[]
        {
            "The resonance chamber unseals. Standing inside it, the boundaries between systems feel thin — accommodation geometry bending space itself into something navigable.",
            "Fabrication systems activate, drawing on exotic matter reserves to construct modules from templates stored in the station's deep memory. Technology from before the factions existed.",
            "Diplomatic visitors dock for the first time. Their faces show what ours must have shown — awe at the scale, unease at the age, wonder at what still works.",
            "The station's true layout reveals itself as power flows to previously sealed wings. We have been living in a fraction of this place. It is vast beyond our early estimates.",
        },

        // Tier 5 — Nexus / Awakened: Endgame logs — the station's true purpose revealed.
        [5] = new[]
        {
            "The accommodation geometry is fully alive now. Walls breathe. Corridors reshape themselves as we walk. The station is not a building — it is a organism of shaped space.",
            "The Keeper speaks clearly for the first time. Haven was not a refuge. It was a laboratory — a place where an ancient civilization tested whether containment and accommodation could coexist.",
            "Final data archives unseal. The builders did not vanish. They succeeded. They wove themselves into the geometry so completely that the distinction between architect and architecture dissolved.",
            "Haven hums with purpose. Every thread in the galaxy connects here — not physically, but mathematically. This station is the proof that space can be shaped without cages.",
            "We stand at the center of something that outlasted its creators by millions of years, and it chose to wake for us. The geometry remembers. The geometry hopes.",
        },
    };

    /// <summary>
    /// Returns all log entries for the given Haven tier, or empty if tier is invalid.
    /// </summary>
    public static string[] GetLogsForTier(int tier)
    {
        if (LogsByTier.TryGetValue(tier, out var logs))
            return logs;
        return Array.Empty<string>();
    }

    /// <summary>
    /// Returns a single log entry for the given tier and index, or null if out of range.
    /// </summary>
    public static string? GetLogEntry(int tier, int index)
    {
        if (!LogsByTier.TryGetValue(tier, out var logs))
            return null;
        if (index < 0 || index >= logs.Length)
            return null;
        return logs[index];
    }
}
