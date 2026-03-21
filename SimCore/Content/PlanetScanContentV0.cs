using System.Collections.Generic;
using SimCore.Entities;

namespace SimCore.Content;

// GATE.T42.PLANET_SCAN.CONTENT.001: Flavor text templates + FO hint lines for planet scanning.
public static class PlanetScanContentV0
{
    // ── Flavor text templates per planet type × finding category ──
    // Key: (PlanetType, FindingCategory) → list of templates (picked by hash).
    // Placeholders: {good} = trade good name, {node} = station/node name.

    public sealed class ScanFlavorDef
    {
        public PlanetType PlanetType { get; set; }
        public FindingCategory Category { get; set; }
        public string Text { get; set; } = "";
    }

    public static readonly IReadOnlyList<ScanFlavorDef> AllFlavors = new List<ScanFlavorDef>
    {
        // ── Terrestrial ──
        new() { PlanetType = PlanetType.Terrestrial, Category = FindingCategory.ResourceIntel, Text = "Agricultural output analysis reveals supply surplus in {good}. Nearest buyer at {node}." },
        new() { PlanetType = PlanetType.Terrestrial, Category = FindingCategory.ResourceIntel, Text = "Manufacturing sector data shows demand spike for {good}. Trade route viable." },
        new() { PlanetType = PlanetType.Terrestrial, Category = FindingCategory.SignalLead, Text = "Faction comm intercept detected. Encrypted traffic pattern suggests a hidden relay within 2 hops." },
        new() { PlanetType = PlanetType.Terrestrial, Category = FindingCategory.SignalLead, Text = "Trade anomaly in shipping manifests. Volume discrepancies point to an unreported corridor." },
        new() { PlanetType = PlanetType.Terrestrial, Category = FindingCategory.PhysicalEvidence, Text = "Faction archive fragment recovered. Administrative records from the original settlement charter." },
        new() { PlanetType = PlanetType.Terrestrial, Category = FindingCategory.DataArchive, Text = "Economic topology notes. Someone mapped every dependency arrow in this sector — and drew conclusions." },

        // ── Ice ──
        new() { PlanetType = PlanetType.Ice, Category = FindingCategory.ResourceIntel, Text = "Rare metal concentrations detected in deep ice strata. Purity grade: exceptional." },
        new() { PlanetType = PlanetType.Ice, Category = FindingCategory.ResourceIntel, Text = "Thermal vent cluster identified. Fuel extraction viability rated high." },
        new() { PlanetType = PlanetType.Ice, Category = FindingCategory.SignalLead, Text = "Thread Lattice resonance detected beneath the permafrost. The frequency matches nothing in our database." },
        new() { PlanetType = PlanetType.Ice, Category = FindingCategory.SignalLead, Text = "Faint harmonic signature in the ice cap. Something is vibrating at a frequency too low for natural geology." },
        new() { PlanetType = PlanetType.Ice, Category = FindingCategory.PhysicalEvidence, Text = "Thread Lattice fossil exposed in an ice cliff face. Infrastructure preserved by cold — junction points still intact." },
        new() { PlanetType = PlanetType.Ice, Category = FindingCategory.FragmentCache, Text = "Cryogenic capsule detected in deep ice. Contents show unusual bioelectric activity." },
        new() { PlanetType = PlanetType.Ice, Category = FindingCategory.DataArchive, Text = "Frozen data core recovered. The author's tone is worried: 'Should we cage what we cannot understand?'" },

        // ── Sand ──
        new() { PlanetType = PlanetType.Sand, Category = FindingCategory.ResourceIntel, Text = "Ore deposit density scan complete. This desert is sitting on concentrated mineral veins." },
        new() { PlanetType = PlanetType.Sand, Category = FindingCategory.ResourceIntel, Text = "Subsurface rare metal traces detected. Extraction would yield high-purity {good}." },
        new() { PlanetType = PlanetType.Sand, Category = FindingCategory.SignalLead, Text = "Geological conductance anomaly. Something fossilized in the bedrock is still conducting energy." },
        new() { PlanetType = PlanetType.Sand, Category = FindingCategory.PhysicalEvidence, Text = "Excavation site partially exposed by wind erosion. Ancient mining equipment, half-buried but identifiable." },
        new() { PlanetType = PlanetType.Sand, Category = FindingCategory.FragmentCache, Text = "Structural artifact recovered from a collapsed mineshaft. The compression ratio defies known materials science." },
        new() { PlanetType = PlanetType.Sand, Category = FindingCategory.DataArchive, Text = "Survey notes carved into alloy plates. The author counted resource nodes and drew dependency arrows — economic design." },

        // ── Lava ──
        new() { PlanetType = PlanetType.Lava, Category = FindingCategory.ResourceIntel, Text = "Exotic crystal formations detected in cooled magma chambers. Heat-forged, impossible to replicate." },
        new() { PlanetType = PlanetType.Lava, Category = FindingCategory.SignalLead, Text = "Thread Resonance Bloom detected. Periodic energy burst — a timestamped data point for frequency analysis." },
        new() { PlanetType = PlanetType.Lava, Category = FindingCategory.SignalLead, Text = "Energy signature pushing through the planetary crust. The readings are self-correcting. That shouldn't be possible." },
        new() { PlanetType = PlanetType.Lava, Category = FindingCategory.PhysicalEvidence, Text = "Thread Emergence Point located. Energy actively pushing through rock. Not ruins — a living phenomenon." },
        new() { PlanetType = PlanetType.Lava, Category = FindingCategory.FragmentCache, Text = "Heat-forged energy system recovered from a magma tube. Still warm. Still functional." },
        new() { PlanetType = PlanetType.Lava, Category = FindingCategory.DataArchive, Text = "Accommodation calculations etched into heat-resistant alloy. The math behind the hope: 'If the geometry holds...'" },

        // ── Gaseous ──
        new() { PlanetType = PlanetType.Gaseous, Category = FindingCategory.ResourceIntel, Text = "Atmospheric composition analysis complete. Fuel extraction efficiency rating: above average." },
        new() { PlanetType = PlanetType.Gaseous, Category = FindingCategory.SignalLead, Text = "Ancient transmission trapped in atmospheric layers. The signal has been bouncing between cloud banks for millennia." },
        new() { PlanetType = PlanetType.Gaseous, Category = FindingCategory.SignalLead, Text = "Deep atmosphere probe detected a repeating pattern. It sounds like a warning — still playing, on loop." },
        new() { PlanetType = PlanetType.Gaseous, Category = FindingCategory.PhysicalEvidence, Text = "Resonance Pocket detected in the upper atmosphere. Thread energy in temporary equilibrium — contradicts containment theory." },
        new() { PlanetType = PlanetType.Gaseous, Category = FindingCategory.FragmentCache, Text = "Signal-preserved cognitive artifact recovered from atmospheric sampling. Pattern recognition substrate, still active." },
        new() { PlanetType = PlanetType.Gaseous, Category = FindingCategory.DataArchive, Text = "Warning log decoded from atmospheric signal noise. The tone is urgent, personal, and very old." },

        // ── Barren ──
        new() { PlanetType = PlanetType.Barren, Category = FindingCategory.ResourceIntel, Text = "Surface composition scan complete. No atmospheric distortion — highest purity mineral readings possible." },
        new() { PlanetType = PlanetType.Barren, Category = FindingCategory.SignalLead, Text = "Faint electromagnetic anomaly detected. In this silence, even a whisper carries." },
        new() { PlanetType = PlanetType.Barren, Category = FindingCategory.PhysicalEvidence, Text = "Intact installation located on the surface. No atmosphere means no degradation — pristine after millennia." },
        new() { PlanetType = PlanetType.Barren, Category = FindingCategory.PhysicalEvidence, Text = "Precursor Vault detected. The seal responds to your fracture drive frequency. It was designed for someone who could get here." },
        new() { PlanetType = PlanetType.Barren, Category = FindingCategory.FragmentCache, Text = "Safety deposit cache located in a sealed sub-surface chamber. The ancients chose well — nothing degrades here." },
        new() { PlanetType = PlanetType.Barren, Category = FindingCategory.DataArchive, Text = "Departure records stored in vacuum-sealed archive. Terse entries: dates, coordinates, and one recurring word — 'Timeline.'" },
    };

    // ── FO hint lines per trigger ──
    // 6 triggers × 3 FO types = 18 lines.

    public sealed class FoScanLine
    {
        public string Trigger { get; set; } = "";
        public string FoType { get; set; } = "";  // "Analyst", "Veteran", "Pathfinder"
        public string Text { get; set; } = "";
    }

    public static readonly IReadOnlyList<FoScanLine> FoScanLines = new List<FoScanLine>
    {
        // FIRST_PLANET_SURVEYED
        new() { Trigger = "FIRST_PLANET_SURVEYED", FoType = "Analyst", Text = "Scanner calibrated. You chose the scan mode — the planet determines what we find. Landing would give us deeper data." },
        new() { Trigger = "FIRST_PLANET_SURVEYED", FoType = "Veteran", Text = "First orbital scan logged. Different modes pick up different things. If this world is landable, the surface scan will tell us more." },
        new() { Trigger = "FIRST_PLANET_SURVEYED", FoType = "Pathfinder", Text = "The scanner sees what you point it at. There might be more here if you switch modes — or land and look closer." },

        // SCAN_MODE_MISMATCH
        new() { Trigger = "SCAN_MODE_MISMATCH", FoType = "Analyst", Text = "Low-affinity results. This planet's characteristics don't align well with our current scan mode. Consider switching." },
        new() { Trigger = "SCAN_MODE_MISMATCH", FoType = "Veteran", Text = "Weak readings. Wrong tool for this rock. Try a different mode — the geology here is better suited to other approaches." },
        new() { Trigger = "SCAN_MODE_MISMATCH", FoType = "Pathfinder", Text = "The scanner's struggling here. This world has secrets, but not the kind we're looking for with this mode." },

        // PATTERN_RECOGNIZED
        new() { Trigger = "PATTERN_RECOGNIZED", FoType = "Analyst", Text = "I'm compiling your scan data. A pattern is emerging — certain planet types consistently yield better results with specific modes." },
        new() { Trigger = "PATTERN_RECOGNIZED", FoType = "Veteran", Text = "After enough scans, you start to see it. The planet tells you what it has — you just need the right ears." },
        new() { Trigger = "PATTERN_RECOGNIZED", FoType = "Pathfinder", Text = "You're mapping the patterns. I can see it in your scan choices — you know which worlds favor which modes now." },

        // RARE_FIND
        new() { Trigger = "RARE_FIND", FoType = "Analyst", Text = "Anomalous reading. This finding is outside normal parameters. I recommend thorough documentation." },
        new() { Trigger = "RARE_FIND", FoType = "Veteran", Text = "I've scanned a lot of worlds. This is... not like the others. Whatever this is, someone built it to last." },
        new() { Trigger = "RARE_FIND", FoType = "Pathfinder", Text = "I felt that one through the hull. The scanner didn't find this — it found us." },

        // SIGNAL_TRIANGULATED
        new() { Trigger = "SIGNAL_TRIANGULATED", FoType = "Analyst", Text = "Cross-referencing with the first signal. Triangulation complete — I have precise coordinates." },
        new() { Trigger = "SIGNAL_TRIANGULATED", FoType = "Veteran", Text = "Second signal locked. Two points make a line — and this line points somewhere specific." },
        new() { Trigger = "SIGNAL_TRIANGULATED", FoType = "Pathfinder", Text = "The signals are talking to each other. They've been pointing at the same place this whole time." },

        // LORE_DISCOVERY
        new() { Trigger = "LORE_DISCOVERY", FoType = "Analyst", Text = "Data archive recovered. Cross-referencing with existing knowledge graph entries. The connection implications are significant." },
        new() { Trigger = "LORE_DISCOVERY", FoType = "Veteran", Text = "Someone left this here on purpose. They knew we'd come looking eventually. Read it carefully." },
        new() { Trigger = "LORE_DISCOVERY", FoType = "Pathfinder", Text = "Another voice from the past. They're not explaining — they're confessing. Pay attention to what they don't say." },
    };

    // ── Orbital scan hint lines (what a different mode might find) ──
    // Used in the orbital scan result card to teach the affinity matrix.

    public sealed class ScanHintDef
    {
        public PlanetType PlanetType { get; set; }
        public ScanMode CurrentMode { get; set; }
        public string HintText { get; set; } = "";
    }

    public static readonly IReadOnlyList<ScanHintDef> OrbitalHints = new List<ScanHintDef>
    {
        // Sand world — if using SignalSweep or Archaeological, hint at MineralSurvey
        new() { PlanetType = PlanetType.Sand, CurrentMode = ScanMode.SignalSweep, HintText = "Strong mineral signatures in the substrate. A Mineral Survey would yield richer data here." },
        new() { PlanetType = PlanetType.Sand, CurrentMode = ScanMode.Archaeological, HintText = "Dense ore deposits detected passively. Consider a Mineral Survey for economic intelligence." },

        // Lava world — if using MineralSurvey or Archaeological, hint at SignalSweep
        new() { PlanetType = PlanetType.Lava, CurrentMode = ScanMode.MineralSurvey, HintText = "Faint energy signatures beneath the crust. A Signal Sweep might detect what's generating them." },
        new() { PlanetType = PlanetType.Lava, CurrentMode = ScanMode.Archaeological, HintText = "Periodic energy pulses from the volcanic vents. Signal Sweep recommended." },

        // Gaseous world — hint at SignalSweep
        new() { PlanetType = PlanetType.Gaseous, CurrentMode = ScanMode.MineralSurvey, HintText = "Atmospheric signal anomalies detected. A Signal Sweep could decode the transmissions." },
        new() { PlanetType = PlanetType.Gaseous, CurrentMode = ScanMode.Archaeological, HintText = "Trapped signals in the cloud layers. Signal Sweep would be more effective here." },

        // Barren world — hint at Archaeological
        new() { PlanetType = PlanetType.Barren, CurrentMode = ScanMode.MineralSurvey, HintText = "Surface structures detected — no erosion damage. An Archaeological Scan could reveal more." },
        new() { PlanetType = PlanetType.Barren, CurrentMode = ScanMode.SignalSweep, HintText = "Pristine surface installations visible from orbit. Archaeological Scan recommended for site analysis." },

        // Terrestrial — hint at Archaeological
        new() { PlanetType = PlanetType.Terrestrial, CurrentMode = ScanMode.MineralSurvey, HintText = "Settlement-era structures on the surface. Archaeological Scan might uncover administrative records." },
        new() { PlanetType = PlanetType.Terrestrial, CurrentMode = ScanMode.SignalSweep, HintText = "Faction infrastructure signatures detected. An Archaeological Scan could access the archives." },

        // Ice world — balanced hints
        new() { PlanetType = PlanetType.Ice, CurrentMode = ScanMode.SignalSweep, HintText = "Dense rare metal concentrations in the ice strata. Mineral Survey would map the deposits." },
        new() { PlanetType = PlanetType.Ice, CurrentMode = ScanMode.Archaeological, HintText = "Rare metal signatures under the permafrost. A Mineral Survey could quantify the deposits." },
        new() { PlanetType = PlanetType.Ice, CurrentMode = ScanMode.MineralSurvey, HintText = "Faint harmonic resonance from deep ice. A Signal Sweep might detect preserved infrastructure." },
    };

    // ── Helper: get flavor texts for a specific planet type + category ──
    private static Dictionary<(PlanetType, FindingCategory), List<ScanFlavorDef>>? _flavorIndex;

    public static IReadOnlyList<ScanFlavorDef> GetFlavors(PlanetType planetType, FindingCategory category)
    {
        if (_flavorIndex == null)
        {
            _flavorIndex = new Dictionary<(PlanetType, FindingCategory), List<ScanFlavorDef>>();
            foreach (var f in AllFlavors)
            {
                var key = (f.PlanetType, f.Category);
                if (!_flavorIndex.TryGetValue(key, out var list))
                {
                    list = new List<ScanFlavorDef>();
                    _flavorIndex[key] = list;
                }
                list.Add(f);
            }
        }

        return _flavorIndex.TryGetValue((planetType, category), out var result) ? result : System.Array.Empty<ScanFlavorDef>();
    }

    // ── Helper: get hint text for an orbital scan ──
    private static Dictionary<(PlanetType, ScanMode), List<ScanHintDef>>? _hintIndex;

    public static IReadOnlyList<ScanHintDef> GetHints(PlanetType planetType, ScanMode currentMode)
    {
        if (_hintIndex == null)
        {
            _hintIndex = new Dictionary<(PlanetType, ScanMode), List<ScanHintDef>>();
            foreach (var h in OrbitalHints)
            {
                var key = (h.PlanetType, h.CurrentMode);
                if (!_hintIndex.TryGetValue(key, out var list))
                {
                    list = new List<ScanHintDef>();
                    _hintIndex[key] = list;
                }
                list.Add(h);
            }
        }

        return _hintIndex.TryGetValue((planetType, currentMode), out var result) ? result : System.Array.Empty<ScanHintDef>();
    }

    // ── Helper: get FO line for a trigger + FO type ──
    private static Dictionary<(string, string), FoScanLine>? _foIndex;

    public static FoScanLine? GetFoLine(string trigger, string foType)
    {
        if (_foIndex == null)
        {
            _foIndex = new Dictionary<(string, string), FoScanLine>();
            foreach (var line in FoScanLines)
                _foIndex[(line.Trigger, line.FoType)] = line;
        }

        return _foIndex.TryGetValue((trigger, foType), out var result) ? result : null;
    }
}
