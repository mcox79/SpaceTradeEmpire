using SimCore.Entities;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimCore.Content;

// Tutorial-specific FO dialogue content. 7 acts, ~30 active phases.
// Maren speaks Acts 2-4 (pre-selection). Dask cameos Act 5, Lira cameos Act 6.
// Selected FO speaks graduation (Act 7 end). Ship Computer narrates Act 1 + system notices.
// FOs observe/react — never instruct. HUD objectives handle mechanic instructions.
// Cover-story naming enforced: no "fracture"/"adaptation"/"ancient"/"organism" before Module Revelation.
//
// GATE.T52.NARR.CONTENT_EXTRACT.001: All dialogue text externalized to
// SimCore/Content/Data/tutorial_dialogue_v0.json (embedded resource).
public static class TutorialContentV0
{
    public sealed class TutorialLine
    {
        public TutorialPhase Phase { get; init; }
        public FirstOfficerCandidate Candidate { get; init; }
        public int Sequence { get; init; } // 0-based for multi-line sequences within a phase
        public int Variant { get; init; }  // 0 = default, 1+ = alternative phrasings (same info, different wording)
        public string Text { get; init; } = "";
    }

    public sealed class CandidateIntro
    {
        public FirstOfficerCandidate Candidate { get; init; }
        public string Quote { get; init; } = "";
    }

    // Post-automation narrator line (shown on FO selection overlay after all 3 FO auditions).
    public static readonly string NarratorSelectionPrompt;

    // Ship Computer lines for system notifications (cold, mechanical — no personality).
    public sealed class ShipComputerLine
    {
        public TutorialPhase Phase { get; init; }
        public int Sequence { get; init; }
        public string Text { get; init; } = "";
    }

    public static readonly IReadOnlyList<ShipComputerLine> ShipComputerLines;

    /// <summary>Get Ship Computer line for a phase and sequence.</summary>
    public static string GetShipComputerLine(TutorialPhase phase, int sequence = 0)
    {
        foreach (var line in ShipComputerLines)
        {
            if (line.Phase == phase && line.Sequence == sequence)
                return line.Text;
        }
        return "";
    }

    // FO hail lines: spoken via dialogue box during Act 2 introduction.
    public sealed class FoHailLine
    {
        public FirstOfficerCandidate Candidate { get; init; }
        public string Text { get; init; } = "";
    }

    public static readonly IReadOnlyList<FoHailLine> FoHailLines;

    /// <summary>Get the hail text for a given candidate.</summary>
    public static string GetFoHailText(FirstOfficerCandidate candidate)
    {
        foreach (var line in FoHailLines)
        {
            if (line.Candidate == candidate) return line.Text;
        }
        return "";
    }

    // Self-introduction quotes shown on the FO selection overlay.
    public static readonly IReadOnlyList<CandidateIntro> SelectionIntros;

    // Phase-specific tutorial dialogue lines. 3 variants per phase (one per candidate).
    // Acts 2-7 pre-selection: rotating candidate picks the variant shown.
    // Post-selection (FO_Selection onward): selected FO's variant shown.
    public static readonly IReadOnlyList<TutorialLine> AllLines;

    // Wrong-station warning lines: FO warns player when they dock at a station
    // where their cargo sells for less than they paid.
    public sealed class WrongStationLine
    {
        public FirstOfficerCandidate Candidate { get; init; }
        public string Text { get; init; } = "";
    }

    public static readonly IReadOnlyList<WrongStationLine> WrongStationLines;

    // ── JSON DTO types for deserialization ──

    private sealed class JsonRoot
    {
        [JsonPropertyName("version")] public int Version { get; set; }
        [JsonPropertyName("narrator_selection_prompt")] public string NarratorSelectionPrompt { get; set; } = "";
        [JsonPropertyName("ship_computer_lines")] public List<JsonShipComputerLine> ShipComputerLines { get; set; } = new();
        [JsonPropertyName("selection_intros")] public List<JsonCandidateIntro> SelectionIntros { get; set; } = new();
        [JsonPropertyName("fo_hail_lines")] public List<JsonFoHailLine> FoHailLines { get; set; } = new();
        [JsonPropertyName("wrong_station_lines")] public List<JsonWrongStationLine> WrongStationLines { get; set; } = new();
        [JsonPropertyName("tutorial_lines")] public List<JsonTutorialLine> TutorialLines { get; set; } = new();
    }

    private sealed class JsonShipComputerLine
    {
        [JsonPropertyName("phase")] public string Phase { get; set; } = "";
        [JsonPropertyName("sequence")] public int Sequence { get; set; }
        [JsonPropertyName("text")] public string Text { get; set; } = "";
    }

    private sealed class JsonCandidateIntro
    {
        [JsonPropertyName("candidate")] public string Candidate { get; set; } = "";
        [JsonPropertyName("quote")] public string Quote { get; set; } = "";
    }

    private sealed class JsonFoHailLine
    {
        [JsonPropertyName("candidate")] public string Candidate { get; set; } = "";
        [JsonPropertyName("text")] public string Text { get; set; } = "";
    }

    private sealed class JsonWrongStationLine
    {
        [JsonPropertyName("candidate")] public string Candidate { get; set; } = "";
        [JsonPropertyName("text")] public string Text { get; set; } = "";
    }

    private sealed class JsonTutorialLine
    {
        [JsonPropertyName("phase")] public string Phase { get; set; } = "";
        [JsonPropertyName("candidate")] public string Candidate { get; set; } = "";
        [JsonPropertyName("sequence")] public int Sequence { get; set; }
        [JsonPropertyName("variant")] public int Variant { get; set; }
        [JsonPropertyName("text")] public string Text { get; set; } = "";
    }

    // ── Static initializer: load from embedded JSON ──

    static TutorialContentV0()
    {
        var root = DialogueJsonLoader.Load<JsonRoot>("tutorial_dialogue_v0.json");

        NarratorSelectionPrompt = root.NarratorSelectionPrompt;

        var scLines = new List<ShipComputerLine>();
        foreach (var j in root.ShipComputerLines)
        {
            scLines.Add(new ShipComputerLine
            {
                Phase = Enum.Parse<TutorialPhase>(j.Phase),
                Sequence = j.Sequence,
                Text = j.Text,
            });
        }
        ShipComputerLines = scLines;

        var intros = new List<CandidateIntro>();
        foreach (var j in root.SelectionIntros)
        {
            intros.Add(new CandidateIntro
            {
                Candidate = Enum.Parse<FirstOfficerCandidate>(j.Candidate),
                Quote = j.Quote,
            });
        }
        SelectionIntros = intros;

        var hails = new List<FoHailLine>();
        foreach (var j in root.FoHailLines)
        {
            hails.Add(new FoHailLine
            {
                Candidate = Enum.Parse<FirstOfficerCandidate>(j.Candidate),
                Text = j.Text,
            });
        }
        FoHailLines = hails;

        var wrong = new List<WrongStationLine>();
        foreach (var j in root.WrongStationLines)
        {
            wrong.Add(new WrongStationLine
            {
                Candidate = Enum.Parse<FirstOfficerCandidate>(j.Candidate),
                Text = j.Text,
            });
        }
        WrongStationLines = wrong;

        var lines = new List<TutorialLine>();
        foreach (var j in root.TutorialLines)
        {
            lines.Add(new TutorialLine
            {
                Phase = Enum.Parse<TutorialPhase>(j.Phase),
                Candidate = Enum.Parse<FirstOfficerCandidate>(j.Candidate),
                Sequence = j.Sequence,
                Variant = j.Variant,
                Text = j.Text,
            });
        }
        AllLines = lines;
    }

    /// <summary>
    /// Get the tutorial dialogue line for a given phase and candidate (always variant 0).
    /// Returns empty string if no line exists for that combination.
    /// </summary>
    public static string GetLine(TutorialPhase phase, FirstOfficerCandidate candidate, int sequence = 0)
    {
        foreach (var line in AllLines)
        {
            if (line.Phase == phase && line.Candidate == candidate && line.Sequence == sequence && line.Variant == 0)
                return line.Text;
        }
        return "";
    }

    /// <summary>
    /// Get a tutorial dialogue line with deterministic variant selection.
    /// The seed selects among available variants for the (phase, candidate, sequence) tuple.
    /// Same seed always returns the same variant (deterministic for replay).
    /// Falls back to variant 0 if no variants exist or seed selects it.
    /// </summary>
    public static string GetLineForSeed(TutorialPhase phase, FirstOfficerCandidate candidate, int sequence, int seed)
    {
        // Count available variants for this (phase, candidate, sequence)
        int variantCount = 0;
        foreach (var line in AllLines)
        {
            if (line.Phase == phase && line.Candidate == candidate && line.Sequence == sequence)
                variantCount++;
        }

        if (variantCount == 0) return "";

        // Deterministic selection: abs(seed) % variantCount
        int selected = Math.Abs(seed) % variantCount;

        // Find the line with the selected variant index
        int seen = 0;
        foreach (var line in AllLines)
        {
            if (line.Phase == phase && line.Candidate == candidate && line.Sequence == sequence)
            {
                if (seen == selected) return line.Text;
                seen++;
            }
        }

        return "";
    }

    /// <summary>
    /// Get the Dask cameo line for Act 5. Dask always speaks during combat tutorial.
    /// </summary>
    public static string GetDaskCameoLine()
    {
        return GetLine(TutorialPhase.Dask_Hail, FirstOfficerCandidate.Veteran);
    }

    /// <summary>
    /// Get the Lira cameo line for Act 6. Lira always speaks during mystery tease.
    /// </summary>
    public static string GetLiraCameoLine()
    {
        return GetLine(TutorialPhase.Lira_Tease, FirstOfficerCandidate.Pathfinder);
    }

    /// <summary>
    /// Get the selection intro quote for a given candidate.
    /// </summary>
    public static string GetSelectionIntro(FirstOfficerCandidate candidate)
    {
        foreach (var intro in SelectionIntros)
        {
            if (intro.Candidate == candidate)
                return intro.Quote;
        }
        return "";
    }

    /// <summary>
    /// Get the HUD objective text for a given tutorial phase.
    /// Keybind instructions live here — FO dialogue never contains key prompts.
    /// Returns empty string if no objective for that phase.
    /// </summary>
    public static string GetObjectiveText(TutorialPhase phase)
    {
        return phase switch
        {
            // Act 1
            TutorialPhase.Awaken or TutorialPhase.Flight_Intro => "",
            TutorialPhase.First_Dock => "\u25b8 Dock at the station ahead (E)",
            // Act 2
            TutorialPhase.Module_Calibration_Notice => "",
            TutorialPhase.Maren_Hail or TutorialPhase.Maren_Settle => "",
            TutorialPhase.Market_Explain or TutorialPhase.Buy_Prompt => "\u25b8 Buy a surplus good from the Market tab",
            TutorialPhase.Buy_React => "",
            // Act 3
            TutorialPhase.Cruise_Intro => "",
            TutorialPhase.Travel_Prompt => "\u25b8 Travel to another station via lane gate",
            TutorialPhase.Jump_Anomaly => "",
            TutorialPhase.Arrival_Dock => "\u25b8 Dock at the destination station (E)",
            TutorialPhase.Sell_Prompt => "\u25b8 Sell your cargo for profit",
            TutorialPhase.First_Profit => "",
            TutorialPhase.FO_Selection => "\u25b8 Choose your First Officer",
            // Act 4
            TutorialPhase.Explore_Prompt => "\u25b8 Explore more systems",
            TutorialPhase.Galaxy_Map_Prompt => "\u25b8 Open the galaxy map (M)",
            // Act 5
            TutorialPhase.Threat_Warning or TutorialPhase.Dask_Hail => "",
            TutorialPhase.Combat_Engage => "\u25b8 Engage and destroy the hostile",
            TutorialPhase.Repair_Prompt => "\u25b8 Dock at a station to repair hull damage",
            // Act 6
            TutorialPhase.Module_Equip => "\u25b8 Install a module in the Ship tab",
            // Act 7
            TutorialPhase.Automation_Intro => "",
            TutorialPhase.Automation_Create => "\u25b8 Create a TradeCharter program in the Jobs tab",
            TutorialPhase.Automation_Running => "\u25b8 Watch your program earn credits",
            _ => ""
        };
    }

    /// <summary>
    /// Get the FO candidate who speaks during pre-selection phases (Acts 2-7).
    /// Each act has a designated speaker so the player meets all 3 FOs before choosing.
    /// Returns None for phases outside the pre-selection window (Act 1, Ship Computer, post-selection).
    /// </summary>
    public static FirstOfficerCandidate GetRotatingCandidate(TutorialPhase phase)
    {
        return phase switch
        {
            // Acts 2-3: Maren (Analyst) — trade intro + trade loop
            TutorialPhase.Maren_Hail or
            TutorialPhase.Maren_Settle or
            TutorialPhase.Market_Explain or
            TutorialPhase.Buy_Prompt or
            TutorialPhase.Buy_React or
            TutorialPhase.Jump_Anomaly or
            TutorialPhase.Travel_Prompt or
            TutorialPhase.Sell_Prompt or
            TutorialPhase.First_Profit => FirstOfficerCandidate.Analyst,

            // Act 4: Maren (Analyst) — galaxy orientation
            TutorialPhase.World_Intro or
            TutorialPhase.Explore_Prompt or
            TutorialPhase.Galaxy_Map_Prompt => FirstOfficerCandidate.Analyst,

            // Act 5: Dask (Veteran) — combat is his domain
            TutorialPhase.Threat_Warning or
            TutorialPhase.Dask_Hail or
            TutorialPhase.Combat_Engage or
            TutorialPhase.Combat_Debrief or
            TutorialPhase.Repair_Prompt => FirstOfficerCandidate.Veteran,

            // Act 6: Lira (Pathfinder) — modules and exploration
            TutorialPhase.Module_Intro or
            TutorialPhase.Module_Equip or
            TutorialPhase.Module_React or
            TutorialPhase.Lira_Tease => FirstOfficerCandidate.Pathfinder,

            // Act 7: Maren (Analyst) — trade automation
            TutorialPhase.Automation_Intro or
            TutorialPhase.Automation_Create or
            TutorialPhase.Automation_Running or
            TutorialPhase.Automation_React => FirstOfficerCandidate.Analyst,

            _ => FirstOfficerCandidate.None
        };
    }

    /// <summary>
    /// Get the "memorable line" for a candidate — shown on the selection overlay
    /// as a reminder of what this FO said during their hail.
    /// </summary>
    public static string GetMemorableLine(FirstOfficerCandidate candidate)
    {
        return candidate switch
        {
            FirstOfficerCandidate.Analyst => GetFoHailText(FirstOfficerCandidate.Analyst),
            FirstOfficerCandidate.Veteran => GetFoHailText(FirstOfficerCandidate.Veteran),
            FirstOfficerCandidate.Pathfinder => GetFoHailText(FirstOfficerCandidate.Pathfinder),
            _ => ""
        };
    }

    // GATE.T51.VO.BRIDGE_KEY.001: Map tutorial phases to vo_key strings for VO file lookup.
    // vo_key is the filename stem used by vo_lookup.gd: res://assets/audio/vo/{speaker}/{vo_key}_{seq}.mp3
    public static string GetVoKey(TutorialPhase phase)
    {
        return phase switch
        {
            TutorialPhase.Awaken => "awaken",
            TutorialPhase.Flight_Intro => "flight_intro",
            TutorialPhase.First_Dock => "first_dock",
            TutorialPhase.Module_Calibration_Notice => "module_calibration",
            TutorialPhase.Maren_Hail => "maren_hail",
            TutorialPhase.Maren_Settle => "maren_settle",
            TutorialPhase.Market_Explain => "market_explain",
            TutorialPhase.Buy_Prompt => "buy_prompt",
            TutorialPhase.Buy_React => "buy_react",
            TutorialPhase.Cruise_Intro => "cruise_intro",
            TutorialPhase.Travel_Prompt => "travel_prompt",
            TutorialPhase.Jump_Anomaly => "jump_anomaly",
            TutorialPhase.Arrival_Dock => "arrival_dock",
            TutorialPhase.Sell_Prompt => "sell_prompt",
            TutorialPhase.First_Profit => "first_profit",
            TutorialPhase.FO_Selection => "fo_selection",
            TutorialPhase.World_Intro => "world_intro",
            TutorialPhase.Explore_Prompt => "explore_prompt",
            TutorialPhase.Galaxy_Map_Prompt => "galaxy_map_prompt",
            TutorialPhase.Threat_Warning => "threat_warning",
            TutorialPhase.Dask_Hail => "dask_hail",
            TutorialPhase.Combat_Engage => "combat_engage",
            TutorialPhase.Combat_Debrief => "combat_debrief",
            TutorialPhase.Repair_Prompt => "repair_prompt",
            TutorialPhase.Module_Intro => "module_intro",
            TutorialPhase.Module_Equip => "module_equip",
            TutorialPhase.Module_React => "module_react",
            TutorialPhase.Lira_Tease => "lira_tease",
            TutorialPhase.Automation_Intro => "automation_intro",
            TutorialPhase.Automation_Create => "automation_create",
            TutorialPhase.Automation_Running => "automation_running",
            TutorialPhase.Automation_React => "automation_react",
            TutorialPhase.Mystery_Reveal => "mystery_reveal",
            TutorialPhase.FO_Farewell => "fo_farewell",
            TutorialPhase.Milestone_Award => "milestone_award",
            TutorialPhase.Graduation_Summary => "graduation_summary",
            _ => ""
        };
    }

    /// <summary>Get wrong-station warning text for a candidate. Replace {station} with actual name.</summary>
    public static string GetWrongStationText(FirstOfficerCandidate candidate)
    {
        foreach (var line in WrongStationLines)
        {
            if (line.Candidate == candidate) return line.Text;
        }
        return "";
    }
}
