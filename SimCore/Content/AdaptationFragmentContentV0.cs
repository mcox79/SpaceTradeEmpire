using System;
using System.Collections.Generic;
using SimCore.Entities;

namespace SimCore.Content;

// GATE.S8.ADAPTATION.ENTITY.001: 16 adaptation fragments + 8 resonance pairs.
public static class AdaptationFragmentContentV0
{
    public sealed class FragmentDef
    {
        public string FragmentId { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public AdaptationFragmentKind Kind { get; set; }
        public string ResonancePairId { get; set; } = "";
    }

    public sealed class ResonancePairDef
    {
        public string PairId { get; set; } = "";
        public string FragmentA { get; set; } = "";
        public string FragmentB { get; set; } = "";
        public string BonusDescription { get; set; } = "";
    }

    public static readonly IReadOnlyList<FragmentDef> AllFragments = new List<FragmentDef>
    {
        // ── Biological (4) ──
        new() { FragmentId = "frag_bio_01", Name = "Growth Lattice", Description = "A self-replicating organic matrix that adapts to its container.", Kind = AdaptationFragmentKind.Biological, ResonancePairId = "pair_01" },
        new() { FragmentId = "frag_bio_02", Name = "Symbiont Cortex", Description = "Neural tissue preserved in crystalline suspension. Still active.", Kind = AdaptationFragmentKind.Biological, ResonancePairId = "pair_02" },
        new() { FragmentId = "frag_bio_03", Name = "Membrane Archive", Description = "Layered biological record of environmental adaptation cycles.", Kind = AdaptationFragmentKind.Biological, ResonancePairId = "pair_03" },
        new() { FragmentId = "frag_bio_04", Name = "Spore Engine", Description = "Dormant reproductive mechanism designed for void dispersal.", Kind = AdaptationFragmentKind.Biological, ResonancePairId = "pair_04" },

        // ── Structural (4) ──
        new() { FragmentId = "frag_str_01", Name = "Void Girder", Description = "Load-bearing element that redistributes stress across dimensions.", Kind = AdaptationFragmentKind.Structural, ResonancePairId = "pair_01" },
        new() { FragmentId = "frag_str_02", Name = "Compression Seed", Description = "When activated, unfolds into a framework 400 times its volume.", Kind = AdaptationFragmentKind.Structural, ResonancePairId = "pair_05" },
        new() { FragmentId = "frag_str_03", Name = "Phase Anchor", Description = "Locks local space-time geometry against instability fluctuations.", Kind = AdaptationFragmentKind.Structural, ResonancePairId = "pair_06" },
        new() { FragmentId = "frag_str_04", Name = "Lattice Shard", Description = "Fragment of the original containment lattice. Still resonates.", Kind = AdaptationFragmentKind.Structural, ResonancePairId = "pair_03" },

        // ── Energetic (4) ──
        new() { FragmentId = "frag_eng_01", Name = "Cascade Core", Description = "Energy amplification loop that converts instability into power.", Kind = AdaptationFragmentKind.Energetic, ResonancePairId = "pair_02" },
        new() { FragmentId = "frag_eng_02", Name = "Void Capacitor", Description = "Stores energy harvested from dimensional boundary transitions.", Kind = AdaptationFragmentKind.Energetic, ResonancePairId = "pair_05" },
        new() { FragmentId = "frag_eng_03", Name = "Resonance Coil", Description = "Converts harmonic vibrations into stable power output.", Kind = AdaptationFragmentKind.Energetic, ResonancePairId = "pair_07" },
        new() { FragmentId = "frag_eng_04", Name = "Threshold Lens", Description = "Focuses dimensional boundary energy for precision applications.", Kind = AdaptationFragmentKind.Energetic, ResonancePairId = "pair_04" },

        // ── Cognitive (4) ──
        new() { FragmentId = "frag_cog_01", Name = "Pattern Engine", Description = "Computational substrate that models reality before it occurs.", Kind = AdaptationFragmentKind.Cognitive, ResonancePairId = "pair_06" },
        new() { FragmentId = "frag_cog_02", Name = "Memory Lattice", Description = "Non-linear information storage across multiple temporal states.", Kind = AdaptationFragmentKind.Cognitive, ResonancePairId = "pair_07" },
        new() { FragmentId = "frag_cog_03", Name = "Synthesis Node", Description = "Integrates disparate data streams into unified understanding.", Kind = AdaptationFragmentKind.Cognitive, ResonancePairId = "pair_08" },
        new() { FragmentId = "frag_cog_04", Name = "Oracle Fragment", Description = "Precursor decision-support system. Outputs remain cryptic.", Kind = AdaptationFragmentKind.Cognitive, ResonancePairId = "pair_08" },
    };

    public static readonly IReadOnlyList<ResonancePairDef> AllResonancePairs = new List<ResonancePairDef>
    {
        new() { PairId = "pair_01", FragmentA = "frag_bio_01", FragmentB = "frag_str_01", BonusDescription = "+5% trade margin (organic-structural synergy)" },
        new() { PairId = "pair_02", FragmentA = "frag_bio_02", FragmentB = "frag_eng_01", BonusDescription = "+10% scan range (neural-energy amplification)" },
        new() { PairId = "pair_03", FragmentA = "frag_bio_03", FragmentB = "frag_str_04", BonusDescription = "+1 Haven hangar bay (membrane-lattice housing)" },
        new() { PairId = "pair_04", FragmentA = "frag_bio_04", FragmentB = "frag_eng_04", BonusDescription = "-10% fracture travel cost (spore-lens navigation)" },
        new() { PairId = "pair_05", FragmentA = "frag_str_02", FragmentB = "frag_eng_02", BonusDescription = "+15% shield capacity (compressed energy storage)" },
        new() { PairId = "pair_06", FragmentA = "frag_str_03", FragmentB = "frag_cog_01", BonusDescription = "+10% module power budget (anchor-pattern optimization)" },
        new() { PairId = "pair_07", FragmentA = "frag_eng_03", FragmentB = "frag_cog_02", BonusDescription = "+5% research speed (resonance-memory feedback)" },
        new() { PairId = "pair_08", FragmentA = "frag_cog_03", FragmentB = "frag_cog_04", BonusDescription = "Unlock Precursor Core module (synthesis-oracle convergence)" },
    };

    private static readonly Dictionary<string, FragmentDef> _byId;
    private static readonly Dictionary<string, ResonancePairDef> _pairById;

    static AdaptationFragmentContentV0()
    {
        _byId = new Dictionary<string, FragmentDef>(StringComparer.Ordinal);
        foreach (var f in AllFragments)
            _byId[f.FragmentId] = f;

        _pairById = new Dictionary<string, ResonancePairDef>(StringComparer.Ordinal);
        foreach (var p in AllResonancePairs)
            _pairById[p.PairId] = p;
    }

    public static FragmentDef? GetById(string fragmentId)
    {
        if (string.IsNullOrEmpty(fragmentId)) return null;
        return _byId.TryGetValue(fragmentId, out var def) ? def : null;
    }

    public static ResonancePairDef? GetPairById(string pairId)
    {
        if (string.IsNullOrEmpty(pairId)) return null;
        return _pairById.TryGetValue(pairId, out var def) ? def : null;
    }
}
