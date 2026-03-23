using SimCore.Entities;
using System.Collections.Generic;

namespace SimCore.Content;

// Tutorial-specific FO dialogue content. 7 acts, ~30 active phases.
// Maren speaks Acts 2-4 (pre-selection). Dask cameos Act 5, Lira cameos Act 6.
// Selected FO speaks graduation (Act 7 end). Ship Computer narrates Act 1 + system notices.
// FOs observe/react — never instruct. HUD objectives handle mechanic instructions.
// Cover-story naming enforced: no "fracture"/"adaptation"/"ancient"/"organism" before Module Revelation.
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

    // Post-automation narrator line (shown on FO selection overlay after all 3 FO auditions).
    public const string NarratorSelectionPrompt =
        "You\u2019ve heard them all. Who do you want by your side?";

    // Ship Computer lines for system notifications (cold, mechanical — no personality).
    public sealed class ShipComputerLine
    {
        public TutorialPhase Phase { get; init; }
        public int Sequence { get; init; }
        public string Text { get; init; } = "";
    }

    public static readonly IReadOnlyList<ShipComputerLine> ShipComputerLines = new List<ShipComputerLine>
    {
        // Act 1: Awaken
        new() { Phase = TutorialPhase.Awaken, Sequence = 0,
            Text = "Systems online. Hull integrity: marginal. Credits: minimal. One station in sensor range." },
        new() { Phase = TutorialPhase.Awaken, Sequence = 1,
            Text = "Three officers responded to your posting. They are en route. For now, it\u2019s just us." },

        // Act 1: Flight_Intro
        new() { Phase = TutorialPhase.Flight_Intro, Sequence = 0,
            Text = "Controls are live. WASD to fly, left-click to set course. The station ahead is your only option. Dock with E when close." },

        // Act 2: Module_Calibration_Notice (mystery seed per NarrativeDesign.md)
        new() { Phase = TutorialPhase.Module_Calibration_Notice, Sequence = 0,
            Text = "NOTICE: Instrument calibration variance detected. Non-critical. Logging." },

        // Act 3: Cruise_Intro (cruise drive notification)
        new() { Phase = TutorialPhase.Cruise_Intro, Sequence = 0,
            Text = "Cruise drive available. Hold C to engage sustained thrust." },

        // Act 7: Graduation_Summary
        new() { Phase = TutorialPhase.Graduation_Summary, Sequence = 0,
            Text = "Tutorial complete. {credits_earned} credits earned. {nodes_visited} systems explored. {combats_won} hostiles eliminated. {modules_equipped} modules installed." },
    };

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
    // Acts 2-7 pre-selection: rotating candidate picks the variant shown.
    // Post-selection (FO_Selection onward): selected FO's variant shown.
    public static readonly IReadOnlyList<TutorialLine> AllLines = new List<TutorialLine>
    {
        // =================================================================
        // ACT 2: THE CREW (Maren introduced — all 3 variants are Maren's
        //        voice since she's the rotating candidate for trading)
        // =================================================================

        // -- Phase: Maren_Hail (Beat 1: first human voice) --
        new() { Phase = TutorialPhase.Maren_Hail, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "Captain, this is Maren Voss. I answered your posting because nobody else would. I\u2019ve been monitoring your feeds since launch \u2014 your situation is worse than advertised." },
        new() { Phase = TutorialPhase.Maren_Hail, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "Captain, this is Maren Voss. I answered your posting because nobody else would. I\u2019ve been monitoring your feeds since launch \u2014 your situation is worse than advertised." },
        new() { Phase = TutorialPhase.Maren_Hail, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "Captain, this is Maren Voss. I answered your posting because nobody else would. I\u2019ve been monitoring your feeds since launch \u2014 your situation is worse than advertised." },
        // Beat 2: stakes
        new() { Phase = TutorialPhase.Maren_Hail, Candidate = FirstOfficerCandidate.Analyst, Sequence = 1,
            Text = "Low credits, empty hold, one station in range. But I see opportunity in the local market data. Dock up and I\u2019ll show you what I mean." },
        new() { Phase = TutorialPhase.Maren_Hail, Candidate = FirstOfficerCandidate.Veteran, Sequence = 1,
            Text = "Low credits, empty hold, one station in range. But I see opportunity in the local market data. Dock up and I\u2019ll show you what I mean." },
        new() { Phase = TutorialPhase.Maren_Hail, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 1,
            Text = "Low credits, empty hold, one station in range. But I see opportunity in the local market data. Dock up and I\u2019ll show you what I mean." },

        // -- Phase: Maren_Settle (warfront context — FO observes, doesn't instruct) --
        new() { Phase = TutorialPhase.Maren_Settle, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "Prices here are distorted \u2014 the warfront\u2019s got the mining runs backed up. But distortion creates margin. I see opportunity." },
        new() { Phase = TutorialPhase.Maren_Settle, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "Prices here are distorted \u2014 the warfront\u2019s got the mining runs backed up. But distortion creates margin. I see opportunity." },
        new() { Phase = TutorialPhase.Maren_Settle, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "Prices here are distorted \u2014 the warfront\u2019s got the mining runs backed up. But distortion creates margin. I see opportunity." },

        // -- Phase: Market_Explain (probability framing — Maren's voice) --
        new() { Phase = TutorialPhase.Market_Explain, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "High stock means surplus \u2014 cheap to buy. Somewhere else, that same good is scarce. 73% chance the margin holds at the next port. I\u2019ve seen worse odds." },
        new() { Phase = TutorialPhase.Market_Explain, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "High stock means surplus \u2014 cheap to buy. Somewhere else, that same good is scarce. 73% chance the margin holds at the next port. I\u2019ve seen worse odds." },
        new() { Phase = TutorialPhase.Market_Explain, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "High stock means surplus \u2014 cheap to buy. Somewhere else, that same good is scarce. 73% chance the margin holds at the next port. I\u2019ve seen worse odds." },

        // -- Phase: Buy_React (Maren validates — no navigation instruction) --
        new() { Phase = TutorialPhase.Buy_React, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "Good call. The margin should hold. 73% confidence." },
        new() { Phase = TutorialPhase.Buy_React, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "Good call. The margin should hold. 73% confidence." },
        new() { Phase = TutorialPhase.Buy_React, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "Good call. The margin should hold. 73% confidence." },

        // =================================================================
        // ACT 3: THE TRADE LOOP (3 manual trades — Maren continues)
        // =================================================================

        // -- Phase: Jump_Anomaly (world-is-watching seed — Maren) --
        new() { Phase = TutorialPhase.Jump_Anomaly, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "Did you see that? Scanner went dark for 0.3 seconds during transit. Probably nothing. Logging it." },
        new() { Phase = TutorialPhase.Jump_Anomaly, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "Did you see that? Scanner went dark for 0.3 seconds during transit. Probably nothing. Logging it." },
        new() { Phase = TutorialPhase.Jump_Anomaly, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "Did you see that? Scanner went dark for 0.3 seconds during transit. Probably nothing. Logging it." },

        // -- Phase: Sell_Prompt (dock at destination — Maren observes) --
        new() { Phase = TutorialPhase.Sell_Prompt, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "New station. Check the sell prices \u2014 if higher than what you paid, that\u2019s pure margin." },
        new() { Phase = TutorialPhase.Sell_Prompt, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "New station. Check the sell prices \u2014 if higher than what you paid, that\u2019s pure margin." },
        new() { Phase = TutorialPhase.Sell_Prompt, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "New station. Check the sell prices \u2014 if higher than what you paid, that\u2019s pure margin." },

        // -- Phase: First_Profit (repeatable — heard up to 3 times in trade loop) --
        new() { Phase = TutorialPhase.First_Profit, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "Profit logged. Margin held to within 2 credits of my estimate. The model works." },
        new() { Phase = TutorialPhase.First_Profit, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "Profit logged. Margin held to within 2 credits of my estimate. The model works." },
        new() { Phase = TutorialPhase.First_Profit, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "Profit logged. Margin held to within 2 credits of my estimate. The model works." },

        // =================================================================
        // ACT 4: THE WORLD (Maren continues — galaxy orientation)
        // =================================================================

        // -- Phase: World_Intro (Beat 1: warfront + factions — Maren observes) --
        new() { Phase = TutorialPhase.World_Intro, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "The warfront between those factions is active \u2014 that\u2019s why prices are distorted. Every faction controls territory and sets tariffs." },
        new() { Phase = TutorialPhase.World_Intro, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "The warfront between those factions is active \u2014 that\u2019s why prices are distorted. Every faction controls territory and sets tariffs." },
        new() { Phase = TutorialPhase.World_Intro, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "The warfront between those factions is active \u2014 that\u2019s why prices are distorted. Every faction controls territory and sets tariffs." },
        // Beat 2: reputation hook
        new() { Phase = TutorialPhase.World_Intro, Candidate = FirstOfficerCandidate.Analyst, Sequence = 1,
            Text = "Reputation determines tariff rates and technology access. Trade with a faction, earn their trust, unlock their modules." },
        new() { Phase = TutorialPhase.World_Intro, Candidate = FirstOfficerCandidate.Veteran, Sequence = 1,
            Text = "Reputation determines tariff rates and technology access. Trade with a faction, earn their trust, unlock their modules." },
        new() { Phase = TutorialPhase.World_Intro, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 1,
            Text = "Reputation determines tariff rates and technology access. Trade with a faction, earn their trust, unlock their modules." },

        // -- Phase: Explore_Prompt (Maren observes) --
        new() { Phase = TutorialPhase.Explore_Prompt, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "More systems means better data. I\u2019d recommend broadening our sensor baseline before committing to a strategy." },
        new() { Phase = TutorialPhase.Explore_Prompt, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "More systems means better data. I\u2019d recommend broadening our sensor baseline before committing to a strategy." },
        new() { Phase = TutorialPhase.Explore_Prompt, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "More systems means better data. I\u2019d recommend broadening our sensor baseline before committing to a strategy." },

        // -- Phase: Galaxy_Map_Prompt (Maren reacts — no keybind instruction) --
        new() { Phase = TutorialPhase.Galaxy_Map_Prompt, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "There it is \u2014 the full picture. Faction territory, trade lanes, contested zones. Every color is a market we could work." },
        new() { Phase = TutorialPhase.Galaxy_Map_Prompt, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "There it is \u2014 the full picture. Faction territory, trade lanes, contested zones. Every color is a market we could work." },
        new() { Phase = TutorialPhase.Galaxy_Map_Prompt, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "There it is \u2014 the full picture. Faction territory, trade lanes, contested zones. Every color is a market we could work." },

        // =================================================================
        // ACT 5: THE THREAT (Dask cameo — Veteran speaks)
        // =================================================================

        // -- Phase: Threat_Warning (rotating=Veteran, Dask's voice) --
        new() { Phase = TutorialPhase.Threat_Warning, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "Scanner contact. Hostile signature. They\u2019re on an intercept course." },
        new() { Phase = TutorialPhase.Threat_Warning, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "Scanner contact. Hostile signature. They\u2019re on an intercept course." },
        new() { Phase = TutorialPhase.Threat_Warning, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "Scanner contact. Hostile signature. They\u2019re on an intercept course." },

        // -- Phase: Dask_Hail (Dask speaks regardless of rotation) --
        new() { Phase = TutorialPhase.Dask_Hail, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "Captain, I\u2019m tracking that contact. Standard pirate \u2014 weak shields, no armor plating. You can take them. Close to weapons range and engage." },

        // -- Phase: Combat_Debrief (Beat 1: debrief — Dask's voice) --
        new() { Phase = TutorialPhase.Combat_Debrief, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "Threat eliminated. Combat data logged. But your hull took hits \u2014 structural integrity is compromised." },
        new() { Phase = TutorialPhase.Combat_Debrief, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "Clean kill, Captain. But look at that hull damage. Next time might not be so easy." },
        new() { Phase = TutorialPhase.Combat_Debrief, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "It\u2019s gone. But so is some of our hull. That was closer than I\u2019d like." },
        // Beat 2: repair + fuel mention
        new() { Phase = TutorialPhase.Combat_Debrief, Candidate = FirstOfficerCandidate.Analyst, Sequence = 1,
            Text = "Dock at any station to repair. Also noted the fuel gauge \u2014 worth topping off when we\u2019re there." },
        new() { Phase = TutorialPhase.Combat_Debrief, Candidate = FirstOfficerCandidate.Veteran, Sequence = 1,
            Text = "We need repairs, Captain. Find a station and dock. And top off the fuel while we\u2019re at it." },
        new() { Phase = TutorialPhase.Combat_Debrief, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 1,
            Text = "We should find a station and patch up. Keep an eye on fuel too \u2014 combat burns through reserves." },

        // =================================================================
        // ACT 6: THE UPGRADE (Lira cameo — Pathfinder speaks)
        // =================================================================

        // -- Phase: Module_Intro (FO observes weakness — no UI instruction) --
        new() { Phase = TutorialPhase.Module_Intro, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "That fight exposed a weakness. Your ship has module slots \u2014 empty ones. The salvaged module from that wreck would fit." },
        new() { Phase = TutorialPhase.Module_Intro, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "That pirate nearly had us because we\u2019re running bare. Module slots are empty. The salvage from the wreckage could change that." },
        new() { Phase = TutorialPhase.Module_Intro, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "We survived, but barely. Your ship has empty module slots. I found something in the wreckage that would fit." },

        // -- Phase: Module_React (FO reacts to upgrade) --
        new() { Phase = TutorialPhase.Module_React, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "Better. But the good modules? Factions guard those. You\u2019ll need to earn their trust \u2014 or find another way." },
        new() { Phase = TutorialPhase.Module_React, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "That\u2019ll help. But the real firepower is faction-locked. Earn reputation or research alternatives." },
        new() { Phase = TutorialPhase.Module_React, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "Feels different already. But this is basic kit. The interesting modules are out there \u2014 behind faction walls and research trees." },

        // -- Phase: Lira_Tease (Lira speaks regardless — sensory observation) --
        new() { Phase = TutorialPhase.Lira_Tease, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "Captain\u2026 your drive\u2019s harmonic signature doesn\u2019t match any registry I\u2019ve cross-referenced. The resonance pattern is\u2026 unusual. Like it\u2019s listening." },

        // =================================================================
        // ACT 7: THE EMPIRE + GRADUATION (Maren for automation, then selected FO)
        // =================================================================

        // -- Phase: Automation_Intro (Beat 1: the reveal — Maren observes) --
        new() { Phase = TutorialPhase.Automation_Intro, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "You\u2019ve been trading manually. Three runs proved the route. But what if it ran itself?" },
        new() { Phase = TutorialPhase.Automation_Intro, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "You\u2019ve been trading manually. Three runs proved the route. But what if it ran itself?" },
        new() { Phase = TutorialPhase.Automation_Intro, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "You\u2019ve been trading manually. Three runs proved the route. But what if it ran itself?" },
        // Beat 2: programs explanation (no UI instruction — FO explains concept)
        new() { Phase = TutorialPhase.Automation_Intro, Candidate = FirstOfficerCandidate.Analyst, Sequence = 1,
            Text = "Programs automate trade routes. One program earns while you focus on something more important." },
        new() { Phase = TutorialPhase.Automation_Intro, Candidate = FirstOfficerCandidate.Veteran, Sequence = 1,
            Text = "Trade programs run routes automatically. Set one up and go find what the galaxy is hiding." },
        new() { Phase = TutorialPhase.Automation_Intro, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 1,
            Text = "A TradeCharter handles the route. You handle the adventure." },

        // -- Phase: Automation_React --
        new() { Phase = TutorialPhase.Automation_React, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "One route running. Revenue accumulating passively. Now imagine ten. That\u2019s an empire." },
        new() { Phase = TutorialPhase.Automation_React, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "Route\u2019s running. Credits rolling in. One route is good. Ten routes is an empire." },
        new() { Phase = TutorialPhase.Automation_React, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "There it goes, working for us. One route running itself. Imagine what we could build." },

        // =================================================================
        // [DEAD PHASES — kept for save compatibility, dialogue preserved but never shown]
        // =================================================================

        // -- Commission_Intro (31): moved to progressive disclosure --
        new() { Phase = TutorialPhase.Commission_Intro, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "Factions post commissions \u2014 bounties, deliveries, escort jobs. They pay well and build your reputation." },
        new() { Phase = TutorialPhase.Commission_Intro, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "There\u2019s work posted on the boards \u2014 commissions from the factions. Bounties, hauling jobs, escorts." },
        new() { Phase = TutorialPhase.Commission_Intro, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "Factions need freelancers. Commissions are posted \u2014 deliveries, bounties, all kinds of work." },

        // -- Frontier_Tease (40): moved to progressive disclosure, cover-story fixed --
        new() { Phase = TutorialPhase.Frontier_Tease, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "There are places the lanes don\u2019t reach. Your drive can go further. Some signals only exist in deep space." },
        new() { Phase = TutorialPhase.Frontier_Tease, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "The lane network ends, but the galaxy doesn\u2019t. Your drive can push beyond \u2014 into uncharted territory." },
        new() { Phase = TutorialPhase.Frontier_Tease, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "The mapped lanes are just the surface. Your drive hums in a way that suggests it knows a deeper path." },

        // =================================================================
        // GRADUATION (post-FO-selection — selected FO speaks)
        // =================================================================

        // -- Phase: Mystery_Reveal (Beat 1: drive mystery deepened) --
        new() { Phase = TutorialPhase.Mystery_Reveal, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "I\u2019ve been analyzing your drive\u2019s resonance patterns. They don\u2019t match anything in the registry. This ship carries something that wasn\u2019t built in this century." },
        new() { Phase = TutorialPhase.Mystery_Reveal, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "Something\u2019s been nagging me. Your drive signature \u2014 it\u2019s not standard issue. I\u2019ve served on a hundred ships and never seen readings like these." },
        new() { Phase = TutorialPhase.Mystery_Reveal, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "I found a match for that harmonic anomaly. One match. In a fragment from a pre-Concord survey station. 340 years old." },
        // Beat 2: cover-story safe (no "fracture" terminology)
        new() { Phase = TutorialPhase.Mystery_Reveal, Candidate = FirstOfficerCandidate.Analyst, Sequence = 1,
            Text = "Whatever built this drive understood something about space that we don\u2019t. The answers are out there." },
        new() { Phase = TutorialPhase.Mystery_Reveal, Candidate = FirstOfficerCandidate.Veteran, Sequence = 1,
            Text = "I don\u2019t think we\u2019re the first to travel these lanes. Someone was here before. Long before." },
        new() { Phase = TutorialPhase.Mystery_Reveal, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 1,
            Text = "Something built this. Something that understood space in ways we\u2019re only beginning to grasp. The edges of the map aren\u2019t the edge of the story." },

        // -- Phase: FO_Farewell --
        new() { Phase = TutorialPhase.FO_Farewell, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "The galaxy is yours, Captain. I\u2019ll keep analyzing." },
        new() { Phase = TutorialPhase.FO_Farewell, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "You\u2019ve got the basics, Captain. From here, it\u2019s your call." },
        new() { Phase = TutorialPhase.FO_Farewell, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "No more hand-holding. The stars are yours. I\u2019ll be here when they surprise you." },

        // -- Phase: Milestone_Award --
        new() { Phase = TutorialPhase.Milestone_Award, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "Captain\u2019s Commission earned. You\u2019ve proven yourself on the frontier." },
        new() { Phase = TutorialPhase.Milestone_Award, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "Captain\u2019s Commission. You\u2019ve earned the trust of your crew." },
        new() { Phase = TutorialPhase.Milestone_Award, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "Captain\u2019s Commission. The frontier is wide open. Let\u2019s see what\u2019s out there." },
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
