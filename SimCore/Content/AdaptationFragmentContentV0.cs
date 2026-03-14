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

        // GATE.S8.NARRATIVE.FRAGMENT_LORE.001: Dual-text lore for cover-story naming discipline.
        // Pre-revelation: player sees CoverName/CoverLore (mundane scientific terms).
        // Post-revelation: player sees RevealedName/RevealedLore (true alien nature).
        public string CoverName { get; set; } = "";
        public string RevealedName { get; set; } = "";
        public string CoverLore { get; set; } = "";
        public string RevealedLore { get; set; } = "";
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
        // GATE.S8.NARRATIVE.FRAGMENT_LORE.001
        new()
        {
            FragmentId = "frag_bio_01", Name = "Growth Lattice",
            Description = "A self-replicating organic matrix that adapts to its container.",
            Kind = AdaptationFragmentKind.Biological, ResonancePairId = "pair_01",
            CoverName = "Regenerative Polymer Sample",
            RevealedName = "Growth Lattice",
            CoverLore = "A high-density polymer recovered from a derelict lab module. Analysis shows unusual self-repair properties consistent with advanced materials science. Engineering teams believe it could improve hull patch kits.",
            RevealedLore = "A living matrix that remembers every shape it has ever been forced into. It does not repair damage — it absorbs the wound into its growth pattern. The lattice treats your ship's hull as another container to fill, and it has been waiting for one.",
        },
        new()
        {
            FragmentId = "frag_bio_02", Name = "Symbiont Cortex",
            Description = "Neural tissue preserved in crystalline suspension. Still active.",
            Kind = AdaptationFragmentKind.Biological, ResonancePairId = "pair_02",
            CoverName = "Bioelectric Sensor Array",
            RevealedName = "Symbiont Cortex",
            CoverLore = "A crystalline substrate containing preserved bioelectric pathways. Likely a prototype organic sensor from an abandoned research program. Shows faint electrical activity when exposed to EM fields.",
            RevealedLore = "Neural tissue from something that was never born and never died. The crystalline suspension is not preservation — it is a cocoon. The cortex responds to your ship's electrical systems because it is trying to form a synapse with them. It wants a host.",
        },
        new()
        {
            FragmentId = "frag_bio_03", Name = "Membrane Archive",
            Description = "Layered biological record of environmental adaptation cycles.",
            Kind = AdaptationFragmentKind.Biological, ResonancePairId = "pair_03",
            CoverName = "Stratified Core Sample",
            RevealedName = "Membrane Archive",
            CoverLore = "A layered mineral sample showing distinct environmental strata. Each layer records different atmospheric and radiation conditions. Geological survey teams use similar samples for planetary history reconstruction.",
            RevealedLore = "Each membrane layer is a skin the organism shed when it outgrew its environment. The archive reads like a diary written in molted flesh — millions of years of adaptation compressed into a cylinder you can hold in one hand. The newest layer is still soft.",
        },
        new()
        {
            FragmentId = "frag_bio_04", Name = "Spore Engine",
            Description = "Dormant reproductive mechanism designed for void dispersal.",
            Kind = AdaptationFragmentKind.Biological, ResonancePairId = "pair_04",
            CoverName = "Vacuum-Hardened Seed Pod",
            RevealedName = "Spore Engine",
            CoverLore = "A sealed organic capsule recovered from deep void. Internal structures suggest a biological dispersal mechanism adapted for vacuum survival. Xenobotany teams classify it as an extremophile propagule.",
            RevealedLore = "Not a seed — a deployment system. The spore engine was designed to cross the gaps between stars and take root in whatever it found. The dormancy is not death. It is patience. The engine has been calculating trajectories since before your species existed.",
        },

        // ── Structural (4) ──
        new()
        {
            FragmentId = "frag_str_01", Name = "Void Girder",
            Description = "Load-bearing element that redistributes stress across dimensions.",
            Kind = AdaptationFragmentKind.Structural, ResonancePairId = "pair_01",
            CoverName = "Structural Resonance Amplifier",
            RevealedName = "Void Girder",
            CoverLore = "An alloy beam segment recovered from a collapsed station. Exhibits unusual stress distribution properties — loads applied at one point dissipate uniformly across the entire structure. Materials engineers are studying the crystalline grain pattern.",
            RevealedLore = "The girder does not distribute stress through the material. It distributes stress through dimensions you cannot perceive. The load passes through your reality and is borne somewhere else — somewhere that may not appreciate the burden.",
        },
        new()
        {
            FragmentId = "frag_str_02", Name = "Compression Seed",
            Description = "When activated, unfolds into a framework 400 times its volume.",
            Kind = AdaptationFragmentKind.Structural, ResonancePairId = "pair_05",
            CoverName = "Deployable Scaffold Module",
            RevealedName = "Compression Seed",
            CoverLore = "A compact device that expands into a rigid structural framework when triggered. Likely a portable construction tool from an advanced fabrication program. The expansion ratio exceeds any known deployable system by an order of magnitude.",
            RevealedLore = "The framework was always there, folded into geometries that do not exist in three dimensions. The seed does not build — it unfolds what was compressed into a space too small for it. The structure it produces is not new. It is very, very old.",
        },
        new()
        {
            FragmentId = "frag_str_03", Name = "Phase Anchor",
            Description = "Locks local space-time geometry against instability fluctuations.",
            Kind = AdaptationFragmentKind.Structural, ResonancePairId = "pair_06",
            CoverName = "Metric Stabilization Node",
            RevealedName = "Phase Anchor",
            CoverLore = "A device that generates a localized field suppressing spatial metric fluctuations. Standard application would be laboratory-grade gravitational shielding. The power source is unknown but appears self-sustaining.",
            RevealedLore = "The anchor does not stabilize space. It pins your local geometry in place while reality shifts around it. You are not protected from instability — you are nailed to one version of space-time while others slide past. The anchor has opinions about which version you belong in.",
        },
        new()
        {
            FragmentId = "frag_str_04", Name = "Lattice Shard",
            Description = "Fragment of the original containment lattice. Still resonates.",
            Kind = AdaptationFragmentKind.Structural, ResonancePairId = "pair_03",
            CoverName = "Harmonic Alloy Fragment",
            RevealedName = "Lattice Shard",
            CoverLore = "A metallic fragment that vibrates at a constant frequency regardless of temperature or pressure. Acoustic analysis suggests it was part of a much larger resonant structure. The harmonic signature matches no known engineering standard.",
            RevealedLore = "A bone from the cage. The original containment lattice spanned light-years, and this shard still vibrates at the frequency of its imprisonment. It resonates because it remembers what it was built to hold. What it held remembers too.",
        },

        // ── Energetic (4) ──
        new()
        {
            FragmentId = "frag_eng_01", Name = "Cascade Core",
            Description = "Energy amplification loop that converts instability into power.",
            Kind = AdaptationFragmentKind.Energetic, ResonancePairId = "pair_02",
            CoverName = "Feedback Loop Reactor",
            RevealedName = "Cascade Core",
            CoverLore = "A compact power unit that amplifies input energy through recursive cycling. Output exceeds input by a factor that violates known thermodynamic models. Research teams attribute the anomaly to an unidentified catalytic process.",
            RevealedLore = "The core does not amplify energy. It feeds on the instability between dimensions, growing stronger as reality frays around it. Every watt it produces is borrowed from the structural integrity of local space-time. It is an engine that runs on damage.",
        },
        new()
        {
            FragmentId = "frag_eng_02", Name = "Void Capacitor",
            Description = "Stores energy harvested from dimensional boundary transitions.",
            Kind = AdaptationFragmentKind.Energetic, ResonancePairId = "pair_05",
            CoverName = "Zero-Point Storage Cell",
            RevealedName = "Void Capacitor",
            CoverLore = "An energy storage unit that charges passively in regions of high spatial variance. The mechanism appears to harvest quantum fluctuations. Capacity far exceeds conventional supercapacitors at equivalent mass.",
            RevealedLore = "The capacitor stores energy by holding open a wound between dimensions. Each charge cycle widens the gap slightly. The energy you draw was the boundary tension keeping two realities apart. Discharge does not release energy — it lets the wound close.",
        },
        new()
        {
            FragmentId = "frag_eng_03", Name = "Resonance Coil",
            Description = "Converts harmonic vibrations into stable power output.",
            Kind = AdaptationFragmentKind.Energetic, ResonancePairId = "pair_07",
            CoverName = "Harmonic Energy Converter",
            RevealedName = "Resonance Coil",
            CoverLore = "A transducer that converts ambient vibrational energy into stable electrical output. Functions in any environment with background mechanical oscillation. Efficiency increases near active machinery — likely designed as a parasitic power source.",
            RevealedLore = "The coil is tuned to a frequency that should not exist in this universe. It converts the vibration of things trying to occupy the same space at the same time — the hum of overlapping realities. The power is clean. The source is not.",
        },
        new()
        {
            FragmentId = "frag_eng_04", Name = "Threshold Lens",
            Description = "Focuses dimensional boundary energy for precision applications.",
            Kind = AdaptationFragmentKind.Energetic, ResonancePairId = "pair_04",
            CoverName = "Directed Energy Focuser",
            RevealedName = "Threshold Lens",
            CoverLore = "An optical element that concentrates ambient energy into a coherent beam. Functions without an external power source in regions of high spatial variance. Applications include precision cutting and long-range signaling.",
            RevealedLore = "The lens focuses the light that leaks between dimensions — photons that exist in two places at once. When you aim it, you are pointing a hole in reality at a target. The beam does not burn. It convinces the target's atoms that they belong somewhere else.",
        },

        // ── Cognitive (4) ──
        new()
        {
            FragmentId = "frag_cog_01", Name = "Pattern Engine",
            Description = "Computational substrate that models reality before it occurs.",
            Kind = AdaptationFragmentKind.Cognitive, ResonancePairId = "pair_06",
            CoverName = "Predictive Processing Unit",
            RevealedName = "Pattern Engine",
            CoverLore = "A computational device that generates environmental models with uncanny accuracy. Prediction windows of 3-5 seconds exceed any known simulation hardware. Research teams believe it uses a novel probabilistic architecture.",
            RevealedLore = "The engine does not predict the future. It remembers it. The computational substrate exists partially outside linear time, and what looks like prediction is recall. It has already seen what happens next. It has seen what happens after that, too.",
        },
        new()
        {
            FragmentId = "frag_cog_02", Name = "Memory Lattice",
            Description = "Non-linear information storage across multiple temporal states.",
            Kind = AdaptationFragmentKind.Cognitive, ResonancePairId = "pair_07",
            CoverName = "Quantum State Memory Core",
            RevealedName = "Memory Lattice",
            CoverLore = "A data storage medium with extraordinary density and access speeds. Information appears to be encoded in quantum superposition states, allowing parallel retrieval. No known interface protocol — data extraction requires custom tooling.",
            RevealedLore = "The lattice stores information in temporal states that have not happened yet. Reading it changes what it contains, because observation collapses the future it was recording. Every query destroys the answer to a question no one has thought to ask.",
        },
        new()
        {
            FragmentId = "frag_cog_03", Name = "Synthesis Node",
            Description = "Integrates disparate data streams into unified understanding.",
            Kind = AdaptationFragmentKind.Cognitive, ResonancePairId = "pair_08",
            CoverName = "Multi-Spectrum Data Integrator",
            RevealedName = "Synthesis Node",
            CoverLore = "A processing unit that correlates data from unrelated sensor feeds into coherent situational models. Analysts describe the output as 'intuitive' — conclusions that are correct but whose reasoning cannot be followed. Likely an advanced pattern-matching system.",
            RevealedLore = "The node does not integrate data. It translates between ways of knowing that were never meant to be compatible. Sensor readings, emotional states, gravitational topology — the node treats them as dialects of the same language. Understanding its output changes how you think. Permanently.",
        },
        new()
        {
            FragmentId = "frag_cog_04", Name = "Oracle Fragment",
            Description = "Precursor decision-support system. Outputs remain cryptic.",
            Kind = AdaptationFragmentKind.Cognitive, ResonancePairId = "pair_08",
            CoverName = "Heuristic Advisory Module",
            RevealedName = "Oracle Fragment",
            CoverLore = "A decision-support system that provides recommendations based on unknown criteria. Outputs are terse and occasionally contradictory, but statistically correlated with optimal outcomes. Origin and training data are unrecoverable.",
            RevealedLore = "A sliver of something that once made decisions for a civilization. It still tries. The recommendations are not predictions — they are preferences. The oracle has goals it cannot articulate and a model of the future it will not share. It is advising you toward an outcome only it can see.",
        },
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

    // GATE.S8.NARRATIVE.ENDGAME_CONTENT.001: Haven context flavor text per tier.
    // Used by the bridge/UI to describe Haven's evolving atmosphere.
    public static readonly IReadOnlyList<string> HavenTierFlavorText = new List<string>
    {
        // Tier 0: Undiscovered
        "A void in the star charts — a place that shouldn't exist but somehow does.",
        // Tier 1: Powered
        "The lights came on when you docked. Ancient systems waking from a sleep measured in millennia. The air tastes of static electricity and forgotten purpose.",
        // Tier 2: Inhabited
        "The crew quarters hum with recycled atmosphere. Research terminals flicker with data in scripts no one can read. Haven accepts your presence the way a house accepts a new tenant — politely, with reservations.",
        // Tier 3: Operational
        "The drydock extends like a mechanical flower. The bidirectional thread pulses with transit energy. Haven is no longer a refuge — it is becoming a staging ground.",
        // Tier 4: Expanded
        "The resonance chamber vibrates at a frequency that makes your teeth ache. The fabricator builds modules from materials that phase in and out of visibility. Haven has stopped pretending to be a station. It is showing you what it really is.",
        // Tier 5: Awakened
        "The accommodation geometry is alive. Walls breathe. Corridors rearrange when you aren't looking. The Keeper speaks in complete sentences now, and what it says makes you wish it would stop. Haven has become what it was always meant to be. The question is whether you have.",
    };

    // GATE.S8.NARRATIVE.ENDGAME_CONTENT.001: Win requirement flavor text for endgame path descriptions.
    public static class EndgamePathFlavorV0
    {
        public const string ReinforceTitle = "Reinforce: The Known Way";
        public const string ReinforceDesc = "Strengthen the existing order. The pentagon holds, the lanes endure, and the factions learn to coexist within the cage they inherited. Stability through structure. Safety through control. The price is never knowing what lies beyond.";

        public const string NaturalizeTitle = "Naturalize: The Open Way";
        public const string NaturalizeDesc = "Accept fracture space as natural — not a wound to be contained but a landscape to inhabit. The Communion's vision of universal harmony, stripped of its hierarchies. Coexistence with the geometry itself. The price is everything the old order provided.";

        public const string RenegotiateTitle = "Renegotiate: The Threshold";
        public const string RenegotiateDesc = "Challenge the geometry itself. Every threshold-crosser before you either died or vanished. You have what they didn't — the dialogue protocol, the five revelations, and the accumulated knowledge of everyone who tried and failed. The price is being the one who has to try.";
    }

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
