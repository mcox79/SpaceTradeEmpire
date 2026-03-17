using SimCore.Entities;
using System.Collections.Generic;

namespace SimCore.Content;

// Tutorial-specific FO dialogue content. 10 acts, 45 phases.
// Maren speaks Acts 1-3 (pre-selection). Selected FO speaks Acts 4-10.
// Dask/Lira cameo hails in Acts 5/6 regardless of selection.
// Ship Computer narrates Act 1 (cold, mechanical — no FO yet).
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

    // Post-trade narrator line (shown on FO selection overlay after Acts 2-3 audition).
    public const string NarratorSelectionPrompt =
        "You\u2019ve heard them all. Who do you want by your side?";

    // Ship Computer lines for Act 1 (cold, mechanical — no FO).
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
            Text = "Controls are live. WASD to fly. The station ahead is your only option. Dock with E when close." },

        // Act 10: Graduation_Summary
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

    // FO hail lines: spoken via dialogue box during Act 1.
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
    // Acts 1-3: Maren speaks (rotating audition). Acts 4+: selected FO speaks.
    public static readonly IReadOnlyList<TutorialLine> AllLines = new List<TutorialLine>
    {
        // ═══════════════════════════════════════════════════════════════
        // ACT 2: THE CREW (Maren introduced — she speaks all 3 variants
        //        since she's the audition voice for trading phases)
        // ═══════════════════════════════════════════════════════════════

        // ── Phase: Maren_Hail (Beat 1: Maren introduces herself — first human voice) ──
        new() { Phase = TutorialPhase.Maren_Hail, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "Captain, this is Maren Voss. I answered your posting because nobody else would. I\u2019ve been monitoring your feeds since launch \u2014 your situation is worse than advertised." },
        new() { Phase = TutorialPhase.Maren_Hail, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "Captain, this is Maren Voss. I answered your posting because nobody else would. I\u2019ve been monitoring your feeds since launch \u2014 your situation is worse than advertised." },
        new() { Phase = TutorialPhase.Maren_Hail, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "Captain, this is Maren Voss. I answered your posting because nobody else would. I\u2019ve been monitoring your feeds since launch \u2014 your situation is worse than advertised." },
        // Beat 2: Maren establishes stakes
        new() { Phase = TutorialPhase.Maren_Hail, Candidate = FirstOfficerCandidate.Analyst, Sequence = 1,
            Text = "Low credits, empty hold, one station in range. But I see opportunity in the local market data. Dock up and I\u2019ll show you what I mean." },
        new() { Phase = TutorialPhase.Maren_Hail, Candidate = FirstOfficerCandidate.Veteran, Sequence = 1,
            Text = "Low credits, empty hold, one station in range. But I see opportunity in the local market data. Dock up and I\u2019ll show you what I mean." },
        new() { Phase = TutorialPhase.Maren_Hail, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 1,
            Text = "Low credits, empty hold, one station in range. But I see opportunity in the local market data. Dock up and I\u2019ll show you what I mean." },

        // ── Phase: Maren_Settle (station docked — Maren reads the market) ──
        new() { Phase = TutorialPhase.Maren_Settle, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "Good, we\u2019re docked. Open the Market tab \u2014 see the stock column? Stations produce and consume different goods. That\u2019s where profit hides." },
        new() { Phase = TutorialPhase.Maren_Settle, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "Good, we\u2019re docked. Open the Market tab \u2014 see the stock column? Stations produce and consume different goods. That\u2019s where profit hides." },
        new() { Phase = TutorialPhase.Maren_Settle, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "Good, we\u2019re docked. Open the Market tab \u2014 see the stock column? Stations produce and consume different goods. That\u2019s where profit hides." },

        // ── Phase: Market_Explain (specific teaching) ──
        new() { Phase = TutorialPhase.Market_Explain, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "High stock means low buy prices \u2014 green in the Stock column. Find a station where that good is scarce and sell it there for more. I\u2019ve tagged the best opportunity." },
        new() { Phase = TutorialPhase.Market_Explain, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "Supply and demand, Captain. Surplus here is someone else\u2019s shortage. Buy what\u2019s green-stocked, haul it where they need it." },
        new() { Phase = TutorialPhase.Market_Explain, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "The stock colors tell the story. Green means surplus \u2014 cheap to buy. Red means scarce \u2014 valuable elsewhere. I\u2019ve tagged the best opportunity." },

        // ── Phase: Buy_React ──
        new() { Phase = TutorialPhase.Buy_React, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "Cargo loaded. I\u2019ve flagged a station where this sells higher. Head for the lane gate \u2014 it connects systems." },
        new() { Phase = TutorialPhase.Buy_React, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "Good pick. Now we haul it somewhere it\u2019s needed. Head for the lane gate \u2014 it\u2019ll take us to the next system." },
        new() { Phase = TutorialPhase.Buy_React, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "Got it. Now the fun part \u2014 finding someone who\u2019ll pay what it\u2019s worth. The lane gates connect us to other stations." },

        // ═══════════════════════════════════════════════════════════════
        // ACT 3: THE TRADE (Maren continues pre-selection)
        // ═══════════════════════════════════════════════════════════════

        // ── Phase: Sell_Prompt (fires on dock at destination) ──
        new() { Phase = TutorialPhase.Sell_Prompt, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "New station. Check the sell prices \u2014 if higher than what you paid, that\u2019s pure margin." },
        new() { Phase = TutorialPhase.Sell_Prompt, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "New port. Open the Market and sell your cargo. If the price beats what we paid, we\u2019re in business." },
        new() { Phase = TutorialPhase.Sell_Prompt, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "New faces, new prices. Sell what you bought \u2014 if the number\u2019s bigger, you\u2019ve just found a trade route." },

        // ── Phase: First_Profit (Beat 1: celebration) ──
        new() { Phase = TutorialPhase.First_Profit, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "Profit logged. One run proves the model works. This frontier isn\u2019t as empty as it looks." },
        new() { Phase = TutorialPhase.First_Profit, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "Clean trade, Captain. Credits in the ledger. One run is good. One run is proof." },
        new() { Phase = TutorialPhase.First_Profit, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "You felt that, right? Richer than when we docked. And we barely tried." },
        // Beat 2: hook for Act 4
        new() { Phase = TutorialPhase.First_Profit, Candidate = FirstOfficerCandidate.Analyst, Sequence = 1,
            Text = "But one run won\u2019t save us. We need volume, and for that we need help. The other officers are still waiting." },
        new() { Phase = TutorialPhase.First_Profit, Candidate = FirstOfficerCandidate.Veteran, Sequence = 1,
            Text = "But one run won\u2019t build an empire. We need crew. The other officers are still on the line." },
        new() { Phase = TutorialPhase.First_Profit, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 1,
            Text = "But there\u2019s so much more out here. We need the right crew. Time to choose." },

        // ═══════════════════════════════════════════════════════════════
        // ACT 4: THE WORLD (Selected FO speaks)
        // ═══════════════════════════════════════════════════════════════

        // ── Phase: World_Intro (Beat 1: factions) ──
        new() { Phase = TutorialPhase.World_Intro, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "Notice the station colors \u2014 each faction controls territory. They set tariffs and guard technology. We\u2019ll need their trust." },
        new() { Phase = TutorialPhase.World_Intro, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "Different flags on this station. Factions own territory, set tariffs, guard their tech. Some are friendly. Some aren\u2019t." },
        new() { Phase = TutorialPhase.World_Intro, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "See the colors? Every faction claims its corner of space. Territory, tariffs, and technology \u2014 all faction-controlled." },
        // Beat 2: hook
        new() { Phase = TutorialPhase.World_Intro, Candidate = FirstOfficerCandidate.Analyst, Sequence = 1,
            Text = "Reputation determines tariff rates and technology access. Trade with a faction, earn their trust, unlock their modules." },
        new() { Phase = TutorialPhase.World_Intro, Candidate = FirstOfficerCandidate.Veteran, Sequence = 1,
            Text = "Earn their trust through trade and they\u2019ll lower tariffs. Cross them and you\u2019ll pay through the nose." },
        new() { Phase = TutorialPhase.World_Intro, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 1,
            Text = "The more you trade with them, the more doors open. But every faction has its own agenda. Choose your allies carefully." },

        // ── Phase: Explore_Prompt ──
        new() { Phase = TutorialPhase.Explore_Prompt, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "Three systems visited unlocks deeper intel. I\u2019d recommend exploring at least one more station." },
        new() { Phase = TutorialPhase.Explore_Prompt, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "More systems means better intelligence. Visit one more port and the Station and Intel tabs unlock." },
        new() { Phase = TutorialPhase.Explore_Prompt, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "The galaxy opens up the more you see. One more system and you\u2019ll unlock the full station intel." },

        // ── Phase: Explore_Complete ──
        new() { Phase = TutorialPhase.Explore_Complete, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "Station and Intel tabs active. More data means better route optimization." },
        new() { Phase = TutorialPhase.Explore_Complete, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "Full intel access. Now you can read the room before you dock." },
        new() { Phase = TutorialPhase.Explore_Complete, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "The picture\u2019s getting clearer. Every new station adds a piece." },

        // ── Phase: Galaxy_Map_Prompt ──
        new() { Phase = TutorialPhase.Galaxy_Map_Prompt, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "Press M for the galaxy map. You\u2019ll see faction territory, trade lanes, and where the opportunities cluster." },
        new() { Phase = TutorialPhase.Galaxy_Map_Prompt, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "Hit M \u2014 the galaxy map shows who owns what. Plan your routes around faction borders." },
        new() { Phase = TutorialPhase.Galaxy_Map_Prompt, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "Press M and zoom out. See the whole picture. Every color is a faction. Every line is a route we could run." },

        // ═══════════════════════════════════════════════════════════════
        // ACT 5: THE THREAT (Dask cameo)
        // ═══════════════════════════════════════════════════════════════

        // ── Phase: Threat_Warning ──
        new() { Phase = TutorialPhase.Threat_Warning, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "Scanner contact. Hostile signature. They\u2019re on an intercept course." },
        new() { Phase = TutorialPhase.Threat_Warning, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "Contact on sensors. Hostile. They\u2019ve seen us and they\u2019re closing." },
        new() { Phase = TutorialPhase.Threat_Warning, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "Something\u2019s out there and it\u2019s not friendly. We\u2019ve been spotted." },

        // ── Phase: Dask_Hail (Dask speaks regardless of who was selected) ──
        // These are Dask-only lines; GetDaskCameoLine handles retrieval.
        new() { Phase = TutorialPhase.Dask_Hail, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "Captain, I\u2019m tracking that contact. Standard pirate \u2014 weak shields, no armor plating. You can take them. Close to weapons range and engage." },

        // ── Phase: Combat_Debrief (Beat 1: debrief) ──
        new() { Phase = TutorialPhase.Combat_Debrief, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "Threat eliminated. Combat data logged. But your hull took hits \u2014 structural integrity is compromised." },
        new() { Phase = TutorialPhase.Combat_Debrief, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "Clean kill, Captain. But look at that hull damage. Next time might not be so easy." },
        new() { Phase = TutorialPhase.Combat_Debrief, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "It\u2019s gone. But so is some of our hull. That was closer than I\u2019d like." },
        // Beat 2: repair motivation
        new() { Phase = TutorialPhase.Combat_Debrief, Candidate = FirstOfficerCandidate.Analyst, Sequence = 1,
            Text = "Dock at any station to repair. Hull damage is real out here \u2014 don\u2019t let it accumulate." },
        new() { Phase = TutorialPhase.Combat_Debrief, Candidate = FirstOfficerCandidate.Veteran, Sequence = 1,
            Text = "We need repairs, Captain. Find a station and dock. Don\u2019t fly damaged if you can help it." },
        new() { Phase = TutorialPhase.Combat_Debrief, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 1,
            Text = "We should find a station and patch up. Flying with hull damage is asking for trouble." },

        // ═══════════════════════════════════════════════════════════════
        // ACT 6: THE UPGRADE (Lira cameo)
        // ═══════════════════════════════════════════════════════════════

        // ── Phase: Module_Intro ──
        new() { Phase = TutorialPhase.Module_Intro, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "That fight exposed a weakness. Your ship has module slots \u2014 empty ones. Open the Ship tab and install the module we salvaged." },
        new() { Phase = TutorialPhase.Module_Intro, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "That pirate nearly had us because we\u2019re running bare. Module slots are empty. Let\u2019s fix that \u2014 open the Ship tab." },
        new() { Phase = TutorialPhase.Module_Intro, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "We survived, but barely. Your ship has empty module slots. I found a salvage module in the wreckage \u2014 let\u2019s install it." },

        // ── Phase: Module_React ──
        new() { Phase = TutorialPhase.Module_React, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "Better. But the good modules? Factions guard those. You\u2019ll need to earn their trust \u2014 or find another way." },
        new() { Phase = TutorialPhase.Module_React, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "That\u2019ll help. But the real firepower is faction-locked. Earn reputation or research alternatives." },
        new() { Phase = TutorialPhase.Module_React, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "Feels different already. But this is basic kit. The interesting modules are out there \u2014 hidden behind faction walls and research trees." },

        // ── Phase: Lira_Tease (Lira speaks regardless of who was selected) ──
        new() { Phase = TutorialPhase.Lira_Tease, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "Captain... I\u2019ve been listening to your drive. It hums differently than anything I\u2019ve heard. Like it\u2019s remembering something. Or waiting for something." },

        // ═══════════════════════════════════════════════════════════════
        // ACT 7: THE EMPIRE (Automation reveal)
        // ═══════════════════════════════════════════════════════════════

        // ── Phase: Automation_Intro (Beat 1: the reveal) ──
        new() { Phase = TutorialPhase.Automation_Intro, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "You\u2019ve been trading manually. That was necessary to learn the routes. But what if this route ran itself?" },
        new() { Phase = TutorialPhase.Automation_Intro, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "Time to delegate, Captain. You\u2019ve proved the route works. Now let a program run it for you." },
        new() { Phase = TutorialPhase.Automation_Intro, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "You\u2019ve been doing this by hand. Imagine it running forever, earning while you explore. That\u2019s what programs do." },
        // Beat 2: programs explanation
        new() { Phase = TutorialPhase.Automation_Intro, Candidate = FirstOfficerCandidate.Analyst, Sequence = 1,
            Text = "Programs automate trade. Open the Jobs tab and create a TradeCharter. It profits while you focus on something more important." },
        new() { Phase = TutorialPhase.Automation_Intro, Candidate = FirstOfficerCandidate.Veteran, Sequence = 1,
            Text = "Trade programs run routes automatically. Open the Jobs tab and set one up. Then go find what the galaxy is hiding." },
        new() { Phase = TutorialPhase.Automation_Intro, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 1,
            Text = "Set up a TradeCharter in the Jobs tab. It handles the route. You handle the adventure." },

        // ── Phase: Automation_React ──
        new() { Phase = TutorialPhase.Automation_React, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "One route running. Revenue accumulating passively. Now imagine ten. That\u2019s an empire." },
        new() { Phase = TutorialPhase.Automation_React, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "Route\u2019s running. Credits rolling in. One route is good. Ten routes is an empire." },
        new() { Phase = TutorialPhase.Automation_React, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "There it goes, working for us. One route running itself. Imagine what we could build." },

        // ── Phase: Commission_Intro ──
        new() { Phase = TutorialPhase.Commission_Intro, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "Factions post commissions \u2014 bounties, deliveries, escort jobs. They pay well and build your reputation. Check the Jobs tab." },
        new() { Phase = TutorialPhase.Commission_Intro, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "There\u2019s work posted on the boards \u2014 commissions from the factions. Bounties, hauling jobs, escorts. Good money and reputation." },
        new() { Phase = TutorialPhase.Commission_Intro, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "Factions need freelancers. Commissions are posted in the Jobs tab \u2014 deliveries, bounties, all kinds of work. Worth checking." },

        // ═══════════════════════════════════════════════════════════════
        // ACT 8: THE HAVEN (Home base)
        // ═══════════════════════════════════════════════════════════════

        // ── Phase: Haven_Tour (3-beat) ──
        new() { Phase = TutorialPhase.Haven_Tour, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "This is Haven. An abandoned starbase on the edge of mapped space. Our sensors say it\u2019s functional. Barely." },
        new() { Phase = TutorialPhase.Haven_Tour, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "Haven. Someone built this and left it behind. Whoever they were, they built it to last." },
        new() { Phase = TutorialPhase.Haven_Tour, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "Look at this place. Someone was here before us. The architecture is\u2026 not human. But the systems are compatible." },
        // Beat 2: facilities
        new() { Phase = TutorialPhase.Haven_Tour, Candidate = FirstOfficerCandidate.Analyst, Sequence = 1,
            Text = "Fabricator, research lab, market terminal. Everything we need to build an empire. Upgrade it and it grows." },
        new() { Phase = TutorialPhase.Haven_Tour, Candidate = FirstOfficerCandidate.Veteran, Sequence = 1,
            Text = "Drydock, research lab, fabrication bay. It\u2019s a fortress waiting to wake up. Invest in it and it\u2019ll invest in you." },
        new() { Phase = TutorialPhase.Haven_Tour, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 1,
            Text = "Research pods, a fabricator, even a market hub. It\u2019s like someone designed it for exactly what we\u2019re doing." },
        // Beat 3: emotional anchor
        new() { Phase = TutorialPhase.Haven_Tour, Candidate = FirstOfficerCandidate.Analyst, Sequence = 2,
            Text = "This is home, Captain. Our base of operations." },
        new() { Phase = TutorialPhase.Haven_Tour, Candidate = FirstOfficerCandidate.Veteran, Sequence = 2,
            Text = "This is home, Captain. Every empire needs a capital." },
        new() { Phase = TutorialPhase.Haven_Tour, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 2,
            Text = "This is home, Captain. The one place in the galaxy that\u2019s ours." },

        // ── Phase: Haven_React ──
        new() { Phase = TutorialPhase.Haven_React, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "Haven will be here when we need it. The galaxy won\u2019t wait \u2014 there\u2019s technology to unlock." },
        new() { Phase = TutorialPhase.Haven_React, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "Haven\u2019s solid. Now let\u2019s get back out there. The galaxy has more to offer." },
        new() { Phase = TutorialPhase.Haven_React, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "Home base established. But the frontier is calling. Let\u2019s see what else is out there." },

        // ═══════════════════════════════════════════════════════════════
        // ACT 9: THE FRONTIER (Research + Knowledge)
        // ═══════════════════════════════════════════════════════════════

        // ── Phase: Research_Intro (2-beat) ──
        new() { Phase = TutorialPhase.Research_Intro, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "Better modules exist, but they\u2019re locked behind research. Haven\u2019s lab can develop new technology." },
        new() { Phase = TutorialPhase.Research_Intro, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "You want the good gear? Research unlocks it. Haven\u2019s lab is ready." },
        new() { Phase = TutorialPhase.Research_Intro, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "The frontier is full of technology we don\u2019t understand yet. Research at Haven\u2019s lab turns curiosity into capability." },
        // Beat 2
        new() { Phase = TutorialPhase.Research_Intro, Candidate = FirstOfficerCandidate.Analyst, Sequence = 1,
            Text = "Start a project in Haven\u2019s research lab. Each unlock opens new module options and faction interactions." },
        new() { Phase = TutorialPhase.Research_Intro, Candidate = FirstOfficerCandidate.Veteran, Sequence = 1,
            Text = "Pick a research project at Haven. The first unlock will change how you fight." },
        new() { Phase = TutorialPhase.Research_Intro, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 1,
            Text = "Choose something that calls to you. Every research project opens a new door." },

        // ── Phase: Research_React ──
        new() { Phase = TutorialPhase.Research_React, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "Research in progress. The probability of breakthrough increases with each cycle. I\u2019ll monitor the data." },
        new() { Phase = TutorialPhase.Research_React, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "Lab\u2019s working. When that research completes, we\u2019ll have options we didn\u2019t have before." },
        new() { Phase = TutorialPhase.Research_React, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "I can feel it already \u2014 something shifting. Research opens paths that weren\u2019t there before." },

        // ── Phase: Knowledge_Intro ──
        new() { Phase = TutorialPhase.Knowledge_Intro, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "Everything connects. The Knowledge Web tracks what we\u2019ve learned \u2014 factions, discoveries, technology. It\u2019s all linked." },
        new() { Phase = TutorialPhase.Knowledge_Intro, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "Intelligence builds on intelligence. The Knowledge Web shows how everything fits together." },
        new() { Phase = TutorialPhase.Knowledge_Intro, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "Look at the Knowledge Web. Every discovery connects to others. The pattern is\u2026 bigger than I expected." },

        // ── Phase: Frontier_Tease ──
        new() { Phase = TutorialPhase.Frontier_Tease, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "There are places the lanes don\u2019t reach. Your fracture drive can. Some signals only exist in deep space." },
        new() { Phase = TutorialPhase.Frontier_Tease, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "The lane network ends, but the galaxy doesn\u2019t. Your drive can push beyond \u2014 into fracture space." },
        new() { Phase = TutorialPhase.Frontier_Tease, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "The mapped lanes are just the surface. Your drive hums in a way that suggests it knows a deeper path." },

        // ═══════════════════════════════════════════════════════════════
        // ACT 10: GRADUATION
        // ═══════════════════════════════════════════════════════════════

        // ── Phase: Mystery_Reveal (2-beat) ──
        new() { Phase = TutorialPhase.Mystery_Reveal, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "I\u2019ve been analyzing your drive\u2019s resonance patterns. They don\u2019t match anything in the registry. This ship carries something that wasn\u2019t built in this century." },
        new() { Phase = TutorialPhase.Mystery_Reveal, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "Something\u2019s been nagging me. Your drive signature \u2014 it\u2019s not standard issue. I\u2019ve served on a hundred ships and never seen readings like these." },
        new() { Phase = TutorialPhase.Mystery_Reveal, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "Captain... I\u2019ve been listening to the ship. The drive hums differently than any I\u2019ve heard. Like it\u2019s remembering something." },
        // Beat 2
        new() { Phase = TutorialPhase.Mystery_Reveal, Candidate = FirstOfficerCandidate.Analyst, Sequence = 1,
            Text = "Whatever built this drive understood the fracture network at a level we don\u2019t. The answers are out there." },
        new() { Phase = TutorialPhase.Mystery_Reveal, Candidate = FirstOfficerCandidate.Veteran, Sequence = 1,
            Text = "I don\u2019t think we\u2019re the first to travel these lanes. Someone was here before. Long before." },
        new() { Phase = TutorialPhase.Mystery_Reveal, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 1,
            Text = "Something built this. Something that understood fracture space. The edges of the map aren\u2019t the edge of the story." },

        // ── Phase: FO_Farewell ──
        new() { Phase = TutorialPhase.FO_Farewell, Candidate = FirstOfficerCandidate.Analyst, Sequence = 0,
            Text = "The galaxy is yours, Captain. I\u2019ll keep analyzing." },
        new() { Phase = TutorialPhase.FO_Farewell, Candidate = FirstOfficerCandidate.Veteran, Sequence = 0,
            Text = "You\u2019ve got the basics, Captain. From here, it\u2019s your call." },
        new() { Phase = TutorialPhase.FO_Farewell, Candidate = FirstOfficerCandidate.Pathfinder, Sequence = 0,
            Text = "No more hand-holding. The stars are yours. I\u2019ll be here when they surprise you." },

        // ── Phase: Milestone_Award ──
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
    /// Returns empty string if no objective for that phase.
    /// </summary>
    public static string GetObjectiveText(TutorialPhase phase)
    {
        return phase switch
        {
            // Act 1
            TutorialPhase.Awaken or TutorialPhase.Flight_Intro => "",
            TutorialPhase.First_Dock => "\u25b8 Dock at the station ahead",
            // Act 2
            TutorialPhase.Maren_Hail or TutorialPhase.Maren_Settle => "",
            TutorialPhase.Market_Explain or TutorialPhase.Buy_Prompt => "\u25b8 Buy a surplus good from the Market",
            TutorialPhase.Buy_React => "\u25b8 Travel to another station and sell for profit",
            // Act 3
            TutorialPhase.Travel_Prompt => "\u25b8 Travel to another station and sell for profit",
            TutorialPhase.Arrival_Dock => "\u25b8 Dock at the destination station",
            TutorialPhase.Sell_Prompt => "\u25b8 Sell your cargo for profit",
            TutorialPhase.First_Profit => "",
            TutorialPhase.FO_Selection => "\u25b8 Choose your First Officer",
            // Act 4
            TutorialPhase.Explore_Prompt => "\u25b8 Explore 3 systems to unlock full station intel",
            TutorialPhase.Galaxy_Map_Prompt => "\u25b8 Press M to open the galaxy map",
            // Act 5
            TutorialPhase.Threat_Warning or TutorialPhase.Dask_Hail => "",
            TutorialPhase.Combat_Engage => "\u25b8 Engage and destroy the hostile",
            TutorialPhase.Repair_Prompt => "\u25b8 Dock at a station to repair hull damage",
            // Act 6
            TutorialPhase.Module_Equip => "\u25b8 Install a module in the Ship tab",
            // Act 7
            TutorialPhase.Automation_Intro => "",
            TutorialPhase.Automation_Create => "\u25b8 Open Jobs tab and create a TradeCharter program",
            TutorialPhase.Automation_Running => "\u25b8 Watch your program earn credits",
            // Act 8
            TutorialPhase.Haven_Discovery => "\u25b8 Travel to Haven and dock",
            TutorialPhase.Haven_Upgrade_Prompt => "\u25b8 Explore Haven\u2019s facilities",
            // Act 9
            TutorialPhase.Research_Start => "\u25b8 Start a research project at Haven",
            _ => ""
        };
    }

    /// <summary>
    /// Get the FO candidate who speaks during pre-selection phases (Acts 2-3).
    /// Maren speaks all pre-selection phases in the new tutorial structure.
    /// Returns None for phases outside the pre-selection window.
    /// </summary>
    public static FirstOfficerCandidate GetRotatingCandidate(TutorialPhase phase)
    {
        return phase switch
        {
            // Acts 2-3: Maren speaks all pre-selection trading phases
            TutorialPhase.Maren_Hail or
            TutorialPhase.Maren_Settle or
            TutorialPhase.Market_Explain or
            TutorialPhase.Buy_Prompt or
            TutorialPhase.Buy_React or
            TutorialPhase.Travel_Prompt or
            TutorialPhase.Sell_Prompt or
            TutorialPhase.First_Profit => FirstOfficerCandidate.Analyst,
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
