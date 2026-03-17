using SimCore.Entities;
using System.Collections.Generic;

namespace SimCore.Content;

// Tutorial-specific FO dialogue content. ~46 lines: 3 per phase per candidate + narrator + selection intros.
// Structure mirrors FirstOfficerContentV0.
// These lines are SEPARATE from the 78 reactive FO trigger lines which activate post-tutorial.
public static class TutorialContentV0
{
    public sealed class TutorialLine
    {
        public TutorialPhase Phase { get; init; }
        public FirstOfficerCandidate Candidate { get; init; }
        public int Sequence { get; init; } // 0-based for multi-line sequences within a phase
        public string Text { get; init; } = "";
    }

    public sealed class CandidateIntro
    {
        public FirstOfficerCandidate Candidate { get; init; }
        public string Quote { get; init; } = "";
    }

    // Pre-selection narrator line (shown above the candidate cards).
    public const string NarratorSelectionPrompt = "Choose your First Officer.";

    // Post-trade narrator line (shown after rotating auditions, player has experienced all 3).
    public const string NarratorSelectionPromptPostTrade =
        "You\u2019ve heard them all. Who do you want by your side?";

    // FO hail lines: spoken via dialogue box before the selection overlay appears.
    // These are CHARACTER THROUGH REACTION — they respond to the captain's situation (broke, frontier, desperate).
    public sealed class FoHailLine
    {
        public FirstOfficerCandidate Candidate { get; init; }
        public string Text { get; init; } = "";
    }

    public static readonly IReadOnlyList<FoHailLine> FoHailLines = new List<FoHailLine>
    {
        new() { Candidate = FirstOfficerCandidate.Analyst,
            Text = "Captain, I've reviewed your route history. Whoever was running your numbers before got you into this. I can get you out." },
        new() { Candidate = FirstOfficerCandidate.Veteran,
            Text = "I've seen contractors lose their shirts on the frontier. The lanes here are rough but honest. You need someone who knows when to hold course." },
        new() { Candidate = FirstOfficerCandidate.Pathfinder,
            Text = "Your posting said 'frontier, open-ended.' That's the only kind I answer. Everyone else is afraid of the edges." },
    };

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
    public static readonly IReadOnlyList<CandidateIntro> SelectionIntros = new List<CandidateIntro>
    {
        new() { Candidate = FirstOfficerCandidate.Analyst,
            Quote = "I deal in probabilities, Captain. Every route has a number. Every risk has a margin. Let me run yours." },
        new() { Candidate = FirstOfficerCandidate.Veteran,
            Quote = "Twenty years in the Concord fleet. I know these lanes, these stations, these factions. Need someone steady." },
        new() { Candidate = FirstOfficerCandidate.Pathfinder,
            Quote = "I've been everywhere they said not to go. The maps are wrong in the best ways. I notice things most people walk past." },
    };

    // Phase-specific tutorial dialogue lines. 3 variants per phase (one per candidate).
    public static readonly IReadOnlyList<TutorialLine> AllLines = new List<TutorialLine>
    {
        // ── Phase: FO_Selected_Settle (Beat 1: FO acknowledges being CHOSEN) ──
        new() { Phase = TutorialPhase.FO_Selected_Settle, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "You picked the analyst over the soldier and the wanderer. That means you want answers, not comfort. Good. So do I." },
        new() { Phase = TutorialPhase.FO_Selected_Settle, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "The other two will find other ships. But you need someone who won't flinch when the numbers stop adding up. That's me." },
        new() { Phase = TutorialPhase.FO_Selected_Settle, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "The others would have kept you safe. I'll keep you curious. I think that matters more out here." },

        // ── Phase: FO_Selected_Settle (Beat 2: FO reads your SITUATION) ──
        new() { Phase = TutorialPhase.FO_Selected_Settle, Candidate = FirstOfficerCandidate.Analyst, Sequence = 1,
            Text = "I looked at your balance sheet. The margins on these frontier runs are thin, but they exist. I've already found three." },
        new() { Phase = TutorialPhase.FO_Selected_Settle, Candidate = FirstOfficerCandidate.Veteran, Sequence = 1,
            Text = "I've seen fleets start with less. The frontier stations out here run honest markets \u2014 no guild markups. We can build on that." },
        new() { Phase = TutorialPhase.FO_Selected_Settle, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 1,
            Text = "Your posting said 'desperate.' You're not desperate, Captain. Desperate people hide. You came looking." },

        // ── Phase: Flight_Intro ──
        new() { Phase = TutorialPhase.Flight_Intro, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "Controls are live. WASD for thrust, mouse to orient. That station ahead — dock with E when you're close." },
        new() { Phase = TutorialPhase.Flight_Intro, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "Ship's yours, Captain. Standard formation: WASD thrust, mouse heading. See that station? Close range, press E." },
        new() { Phase = TutorialPhase.Flight_Intro, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "There she goes. WASD to move, mouse to look around. See the station? Fly close and press E to dock." },

        // ── Phase: Docked_First ──
        new() { Phase = TutorialPhase.Docked_First, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "Docked. See the BEST BUY tag? That's the cheapest good here. Load up and sell it at the next station for a profit." },
        new() { Phase = TutorialPhase.Docked_First, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "Good approach, Captain. Market's open. The BEST BUY is the cheapest good — buy it here, sell it at the next port where it's worth more." },
        new() { Phase = TutorialPhase.Docked_First, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "We're in. Check the market — BEST BUY marks the cheapest stock. Buy it cheap here, haul it to a station where they'll pay more." },

        // ── Phase: Market_Explain ──
        new() { Phase = TutorialPhase.Market_Explain, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "Look at the quantities. High stock pushes prices down. Find a station where this good is scarce — the margin is your profit." },
        new() { Phase = TutorialPhase.Market_Explain, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "Supply and demand, Captain. Every surplus here is someone else's shortage. Buy what's green, haul it where it's red." },
        new() { Phase = TutorialPhase.Market_Explain, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "The colors tell the story. Surplus stations sell cheap, scarce stations buy dear. Pick a green good and let's go shopping." },

        // ── Phase: Buy_Complete ──
        new() { Phase = TutorialPhase.Buy_Complete, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "Cargo loaded. I've flagged stations where this sells higher. Fly to a lane gate — they connect systems." },
        new() { Phase = TutorialPhase.Buy_Complete, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "Good pick. Now we haul it somewhere it's needed. Head for the lane gate — it'll take us to the next system." },
        new() { Phase = TutorialPhase.Buy_Complete, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "Got it. Now the fun part — finding someone who'll pay what it's worth. The lane gates connect us to other stations." },

        // ── Phase: Sell_Prompt (fires on dock at destination) ──
        new() { Phase = TutorialPhase.Sell_Prompt, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "New station. Check the sell prices — if higher than what you paid, that's pure margin." },
        new() { Phase = TutorialPhase.Sell_Prompt, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "New port. Open the Market and sell your cargo. If the price beats what we paid, we're in business." },
        new() { Phase = TutorialPhase.Sell_Prompt, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "New faces, new prices. Sell what you bought — if the number's bigger, you've just found a trade route." },

        // ── Phase: Sell_Complete (EMOTIONAL PEAK) ──
        new() { Phase = TutorialPhase.Sell_Complete, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "Profit logged. One run proves the model. Now imagine that running automatically while you explore. That's how empires start." },
        new() { Phase = TutorialPhase.Sell_Complete, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "Clean trade, Captain. Credits in the ledger. One run is good. A hundred runs, automated? That's an empire." },
        new() { Phase = TutorialPhase.Sell_Complete, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "You felt that, right? Richer, and we barely tried. Now picture this route running itself. That's what programs do." },

        // ── Phase: Faction_Explain ──
        new() { Phase = TutorialPhase.Faction_Explain, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "Notice the station colors — each faction controls territory. Press M for the galaxy map. They set tariffs and guard technology." },
        new() { Phase = TutorialPhase.Faction_Explain, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "Different flags on this station. Factions own territory, set tariffs, guard their tech. Press M — the galaxy map shows who owns what." },
        new() { Phase = TutorialPhase.Faction_Explain, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "See the colors? Every faction claims its corner of space. Press M for the big picture. Territory, tariffs, and technology — all faction-controlled." },

        // ── Phase: Explore_Prompt ──
        new() { Phase = TutorialPhase.Explore_Prompt, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "Three systems visited unlocks deeper intel. I'd recommend exploring at least one more station." },
        new() { Phase = TutorialPhase.Explore_Prompt, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "More systems means better intelligence. Visit one more port and the Station and Intel tabs unlock." },
        new() { Phase = TutorialPhase.Explore_Prompt, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "The galaxy opens up the more you see. One more system and you'll unlock the full station intel." },

        // ── Phase: Explore_Complete ──
        new() { Phase = TutorialPhase.Explore_Complete, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "Station and Intel tabs active. More data means better route optimization." },
        new() { Phase = TutorialPhase.Explore_Complete, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "Full intel access. Now you can read the room before you dock." },
        new() { Phase = TutorialPhase.Explore_Complete, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "The picture's getting clearer. Every new station adds a piece." },

        // ── Phase: Automation_Explain (THE REVEAL) ──
        new() { Phase = TutorialPhase.Automation_Explain, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "Captain, you've been trading manually. Programs automate that. A TradeCharter runs your route indefinitely. You profit while exploring the unknown." },
        new() { Phase = TutorialPhase.Automation_Explain, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "Time to delegate, Captain. Trade programs run routes automatically. Set one up, then go find what the galaxy is hiding." },
        new() { Phase = TutorialPhase.Automation_Explain, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "You've been doing this by hand. But programs do it for you — forever. Set up a TradeCharter and then? Go see what's really out there." },

        // ── Phase: Automation_Complete ──
        new() { Phase = TutorialPhase.Automation_Complete, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "Program active. Revenue will accumulate passively. Now we can focus on what I've been wanting to investigate..." },
        new() { Phase = TutorialPhase.Automation_Complete, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "Route's running. Credits rolling in. Now we've got time to look at the bigger picture." },
        new() { Phase = TutorialPhase.Automation_Complete, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "There it goes, working for us. And now — don't you feel it? The galaxy pulling at you. There's something out there." },

        // ── Phase: Mystery_Tease ──
        new() { Phase = TutorialPhase.Mystery_Tease, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "Captain, I've been analyzing your drive's resonance patterns. They don't match anything in the registry. I think your ship carries something that wasn't built in this century." },
        new() { Phase = TutorialPhase.Mystery_Tease, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "Captain, something's been nagging me. Your drive signature — it's not standard issue. I've served on a hundred ships and I've never seen readings like these." },
        new() { Phase = TutorialPhase.Mystery_Tease, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "Captain... I've been listening to the ship. The drive hums differently than any I've heard. Like it's remembering something. Or waiting for something." },

        // ── Phase: Tutorial_Complete ──
        new() { Phase = TutorialPhase.Tutorial_Complete, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "The galaxy is yours, Captain. I'll keep analyzing." },
        new() { Phase = TutorialPhase.Tutorial_Complete, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "You've got the basics, Captain. From here, it's your call." },
        new() { Phase = TutorialPhase.Tutorial_Complete, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "No more hand-holding. The stars are yours. I'll be here when they surprise you." },
    };

    /// <summary>
    /// Get the tutorial dialogue line for a given phase and candidate.
    /// Returns empty string if no line exists for that combination.
    /// </summary>
    public static string GetLine(TutorialPhase phase, FirstOfficerCandidate candidate, int sequence = 0)
    {
        foreach (var line in AllLines)
        {
            if (line.Phase == phase && line.Candidate == candidate && line.Sequence == sequence)
                return line.Text;
        }
        return "";
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
    /// Returns empty string if no objective for that phase.
    /// </summary>
    public static string GetObjectiveText(TutorialPhase phase)
    {
        return phase switch
        {
            TutorialPhase.FO_Selection => "",  // Hails playing — no objective text yet
            TutorialPhase.FO_Selection_PostTrade => "Choose your First Officer",
            TutorialPhase.Dock_Prompt => "\u25b8 Dock at the station ahead",
            TutorialPhase.Buy_Prompt or TutorialPhase.Market_Explain => "\u25b8 Buy a surplus good from the Market",
            TutorialPhase.Travel_Prompt or TutorialPhase.Buy_Complete => "\u25b8 Travel to another station and sell for profit",
            TutorialPhase.Sell_Prompt => "\u25b8 Sell your cargo for profit",
            TutorialPhase.Explore_Prompt => "\u25b8 Explore 3 systems to unlock full station intel",
            TutorialPhase.Automation_Prompt or TutorialPhase.Automation_Explain => "\u25b8 Open Jobs tab and create a TradeCharter program",
            _ => ""
        };
    }

    /// <summary>
    /// Rotating auditions: which FO candidate speaks during each pre-selection tutorial phase.
    /// Maren (Analyst) = flight/navigation, Dask (Veteran) = market/trading, Lira (Pathfinder) = exploration/selling.
    /// Returns None for phases outside the rotation window or post-selection phases.
    /// </summary>
    public static FirstOfficerCandidate GetRotatingCandidate(TutorialPhase phase)
    {
        return phase switch
        {
            TutorialPhase.Flight_Intro => FirstOfficerCandidate.Analyst,    // Maren: precise controls
            TutorialPhase.Dock_Prompt => FirstOfficerCandidate.Analyst,     // Maren: navigation
            TutorialPhase.Docked_First => FirstOfficerCandidate.Veteran,    // Dask: market knowledge
            TutorialPhase.Market_Explain => FirstOfficerCandidate.Veteran,  // Dask: supply/demand
            TutorialPhase.Buy_Prompt => FirstOfficerCandidate.Veteran,      // Dask: buying advice
            TutorialPhase.Buy_Complete => FirstOfficerCandidate.Pathfinder,  // Lira: "my turn"
            TutorialPhase.Travel_Prompt => FirstOfficerCandidate.Pathfinder, // Lira: exploration
            TutorialPhase.Sell_Prompt => FirstOfficerCandidate.Pathfinder,   // Lira: selling
            TutorialPhase.Sell_Complete => FirstOfficerCandidate.Pathfinder,  // Lira: emotional peak
            _ => FirstOfficerCandidate.None
        };
    }

    /// <summary>
    /// Get the "memorable line" for a candidate — shown on the post-trade selection overlay
    /// as a reminder of what this FO said during their audition stretch.
    /// </summary>
    public static string GetMemorableLine(FirstOfficerCandidate candidate)
    {
        return candidate switch
        {
            // Each FO's emotional peak or most distinctive line from their audition phases
            FirstOfficerCandidate.Analyst => GetLine(TutorialPhase.Flight_Intro, FirstOfficerCandidate.Analyst),
            FirstOfficerCandidate.Veteran => GetLine(TutorialPhase.Docked_First, FirstOfficerCandidate.Veteran),
            FirstOfficerCandidate.Pathfinder => GetLine(TutorialPhase.Sell_Complete, FirstOfficerCandidate.Pathfinder),
            _ => ""
        };
    }

    // Wrong-station warning lines: FO warns player when they dock at a station
    // where their cargo sells for less than they paid.
    public sealed class WrongStationLine
    {
        public FirstOfficerCandidate Candidate { get; init; }
        public string Text { get; init; } = "";
    }

    public static readonly IReadOnlyList<WrongStationLine> WrongStationLines = new List<WrongStationLine>
    {
        new() { Candidate = FirstOfficerCandidate.Analyst,
            Text = "The numbers don\u2019t work here. We\u2019d lose credits selling at this station. I\u2019d suggest heading to {station} instead." },
        new() { Candidate = FirstOfficerCandidate.Veteran,
            Text = "Wrong port for this cargo, Captain. We need to keep moving \u2014 {station} has better demand." },
        new() { Candidate = FirstOfficerCandidate.Pathfinder,
            Text = "Not the right place for what we\u2019re carrying. Let\u2019s try {station} \u2014 I\u2019ve got a feeling." },
    };

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
