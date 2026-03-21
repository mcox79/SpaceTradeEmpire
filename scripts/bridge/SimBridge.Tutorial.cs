#nullable enable

using Godot;
using SimCore;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Systems;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpaceTradeEmpire.Bridge;

// Tutorial system bridge queries + commands.
// Exposes tutorial phase, dialogue content, and advancement controls to GDScript.
public partial class SimBridge
{
    // ── Tutorial State ──────────────────────────────────────────────

    // Shared color/name maps for FO candidates (used by multiple methods).
    private static readonly System.Collections.Generic.Dictionary<FirstOfficerCandidate, (float r, float g, float b)> _foColorMap = new()
    {
        [FirstOfficerCandidate.Analyst]    = (0.4f, 0.6f, 1.0f),
        [FirstOfficerCandidate.Veteran]    = (1.0f, 0.8f, 0.3f),
        [FirstOfficerCandidate.Pathfinder] = (0.3f, 0.9f, 0.5f),
    };

    private static readonly System.Collections.Generic.Dictionary<FirstOfficerCandidate, string> _foNameMap = new()
    {
        [FirstOfficerCandidate.Analyst]    = "Maren",
        [FirstOfficerCandidate.Veteran]    = "Dask",
        [FirstOfficerCandidate.Pathfinder] = "Lira",
    };

    /// <summary>
    /// Returns tutorial state: {phase (int), phase_name (string), candidate (string),
    /// dialogue_dismissed (bool), stall_ticks (int), objective (string), pre_selection (bool),
    /// is_ship_computer (bool), is_dask_cameo (bool), is_lira_cameo (bool)}.
    /// Returns empty dict if tutorial state is null (existing saves).
    /// </summary>
    public Godot.Collections.Dictionary GetTutorialStateV0()
    {
        var result = new Godot.Collections.Dictionary();

        TryExecuteSafeRead(state =>
        {
            if (state.TutorialState == null) return;
            var ts = state.TutorialState;

            result["phase"] = (int)ts.Phase;
            result["phase_name"] = ts.Phase.ToString();
            result["candidate"] = ts.SelectedCandidate.ToString();
            result["dialogue_dismissed"] = ts.DialogueDismissed;
            result["dialogue_sequence"] = ts.DialogueSequence;
            result["stall_ticks"] = ts.TicksSincePhaseChange;
            result["objective"] = TutorialContentV0.GetObjectiveText(ts.Phase);
            result["pirate_spawned"] = ts.TutorialPirateSpawned;
            result["module_granted"] = ts.TutorialModuleGranted;

            // Pre-selection: per-act FO speaks during Acts 2-7 (phases Maren_Hail through Automation_React).
            // FO selection moved to after automation so player meets all 3 FOs first.
            result["pre_selection"] = ts.SelectedCandidate == FirstOfficerCandidate.None
                && (int)ts.Phase >= (int)TutorialPhase.Maren_Hail
                && (int)ts.Phase <= (int)TutorialPhase.Automation_React;

            // Ship Computer phases: Act 1, Module_Calibration_Notice, Cruise_Intro, Graduation_Summary.
            result["is_ship_computer"] = ts.Phase == TutorialPhase.Awaken
                || ts.Phase == TutorialPhase.Flight_Intro
                || ts.Phase == TutorialPhase.Module_Calibration_Notice
                || ts.Phase == TutorialPhase.Cruise_Intro
                || ts.Phase == TutorialPhase.Graduation_Summary;

            result["manual_trades"] = ts.ManualTradesCompleted;

            // Cameo phases: Dask speaks during combat (Act 5), Lira during mystery (Act 6).
            result["is_dask_cameo"] = ts.Phase == TutorialPhase.Dask_Hail;
            result["is_lira_cameo"] = ts.Phase == TutorialPhase.Lira_Tease;
        });

        return result;
    }

    /// <summary>
    /// Returns the tutorial dialogue line for the current phase.
    /// Post-selection: uses selected candidate. Pre-selection: uses Maren (Analyst).
    /// Ship Computer phases return Ship Computer lines.
    /// Cameo phases (Dask_Hail, Lira_Tease) return the cameo character's line.
    /// Returns empty string if no line for current phase or tutorial not active.
    /// </summary>
    public string GetTutorialDialogueV0()
    {
        string line = "";

        TryExecuteSafeRead(state =>
        {
            if (state.TutorialState == null) return;
            var ts = state.TutorialState;

            // Ship Computer phases
            if (ts.Phase == TutorialPhase.Awaken
                || ts.Phase == TutorialPhase.Flight_Intro
                || ts.Phase == TutorialPhase.Module_Calibration_Notice
                || ts.Phase == TutorialPhase.Cruise_Intro
                || ts.Phase == TutorialPhase.Graduation_Summary)
            {
                line = TutorialContentV0.GetShipComputerLine(ts.Phase, ts.DialogueSequence);
                return;
            }

            // Dask cameo (always Dask regardless of selection)
            if (ts.Phase == TutorialPhase.Dask_Hail)
            {
                line = TutorialContentV0.GetDaskCameoLine();
                return;
            }

            // Lira cameo (always Lira regardless of selection)
            if (ts.Phase == TutorialPhase.Lira_Tease)
            {
                line = TutorialContentV0.GetLiraCameoLine();
                return;
            }

            // Determine which candidate speaks.
            var speaker = ts.SelectedCandidate;
            if (speaker == FirstOfficerCandidate.None)
            {
                // Pre-selection: Maren speaks all pre-selection phases.
                speaker = TutorialContentV0.GetRotatingCandidate(ts.Phase);
                if (speaker == FirstOfficerCandidate.None) return;
            }

            line = TutorialContentV0.GetLine(ts.Phase, speaker, ts.DialogueSequence);
        });

        return line;
    }

    /// <summary>
    /// Returns the rotating FO dialogue data for the current phase (pre-selection mode).
    /// Returns {type, name, text, color_r, color_g, color_b} or empty dict if not applicable.
    /// Also handles Ship Computer and cameo phases.
    /// </summary>
    public Godot.Collections.Dictionary GetRotatingFODialogueV0()
    {
        var result = new Godot.Collections.Dictionary();

        TryExecuteSafeRead(state =>
        {
            if (state.TutorialState == null) return;
            var ts = state.TutorialState;

            // Ship Computer phases
            if (ts.Phase == TutorialPhase.Awaken
                || ts.Phase == TutorialPhase.Flight_Intro
                || ts.Phase == TutorialPhase.Module_Calibration_Notice
                || ts.Phase == TutorialPhase.Cruise_Intro
                || ts.Phase == TutorialPhase.Graduation_Summary)
            {
                string scText = TutorialContentV0.GetShipComputerLine(ts.Phase, ts.DialogueSequence);
                if (string.IsNullOrEmpty(scText)) return;

                // Graduation_Summary has template variables — substitute with actual stats.
                if (ts.Phase == TutorialPhase.Graduation_Summary)
                {
                    var stats = state.PlayerStats;
                    int modulesEquipped = 0;
                    var fleet = state.Fleets.GetValueOrDefault("fleet_trader_1");
                    if (fleet != null)
                    {
                        foreach (var slot in fleet.Slots)
                        {
                            if (!string.IsNullOrEmpty(slot.InstalledModuleId))
                                modulesEquipped++;
                        }
                    }
                    scText = scText
                        .Replace("{credits_earned}", ((int)stats.TotalCreditsEarned).ToString())
                        .Replace("{nodes_visited}", stats.NodesVisited.ToString())
                        .Replace("{combats_won}", stats.NpcFleetsDestroyed.ToString())
                        .Replace("{modules_equipped}", modulesEquipped.ToString());
                }

                result["type"] = "ShipComputer";
                result["name"] = "SHIP COMPUTER";
                result["text"] = scText;
                result["color_r"] = 0.5f;
                result["color_g"] = 0.5f;
                result["color_b"] = 0.6f;
                return;
            }

            // Dask cameo
            if (ts.Phase == TutorialPhase.Dask_Hail)
            {
                string dText = TutorialContentV0.GetDaskCameoLine();
                if (string.IsNullOrEmpty(dText)) return;
                var col = _foColorMap[FirstOfficerCandidate.Veteran];
                result["type"] = "Veteran";
                result["name"] = "Dask";
                result["text"] = dText;
                result["color_r"] = col.Item1;
                result["color_g"] = col.Item2;
                result["color_b"] = col.Item3;
                return;
            }

            // Lira cameo
            if (ts.Phase == TutorialPhase.Lira_Tease)
            {
                string lText = TutorialContentV0.GetLiraCameoLine();
                if (string.IsNullOrEmpty(lText)) return;
                var col = _foColorMap[FirstOfficerCandidate.Pathfinder];
                result["type"] = "Pathfinder";
                result["name"] = "Lira";
                result["text"] = lText;
                result["color_r"] = col.Item1;
                result["color_g"] = col.Item2;
                result["color_b"] = col.Item3;
                return;
            }

            // Normal: selected FO or rotating Maren
            var speaker = ts.SelectedCandidate != FirstOfficerCandidate.None
                ? ts.SelectedCandidate
                : TutorialContentV0.GetRotatingCandidate(ts.Phase);
            if (speaker == FirstOfficerCandidate.None) return;

            string text = TutorialContentV0.GetLine(ts.Phase, speaker, ts.DialogueSequence);
            if (string.IsNullOrEmpty(text)) return;

            var c = _foColorMap.GetValueOrDefault(speaker, (0.5f, 0.5f, 0.5f));
            result["type"] = speaker.ToString();
            result["name"] = _foNameMap.GetValueOrDefault(speaker, speaker.ToString());
            result["text"] = text;
            result["color_r"] = c.Item1;
            result["color_g"] = c.Item2;
            result["color_b"] = c.Item3;
        });

        return result;
    }

    /// <summary>
    /// Returns true when in pre-selection mode (Maren speaks, no FO selected yet).
    /// </summary>
    public bool IsPreSelectionModeV0()
    {
        bool result = false;
        TryExecuteSafeRead(state =>
        {
            if (state.TutorialState == null) return;
            var ts = state.TutorialState;
            result = ts.SelectedCandidate == FirstOfficerCandidate.None
                && (int)ts.Phase >= (int)TutorialPhase.Maren_Hail
                && (int)ts.Phase <= (int)TutorialPhase.Automation_React;
        });
        return result;
    }

    /// <summary>
    /// Returns the FO candidate selection data for the selection overlay.
    /// Array of {type, name, description, quote, memorable_line}.
    /// </summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetTutorialCandidatesV0()
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();

        foreach (var c in FirstOfficerContentV0.Candidates)
        {
            var card = new Godot.Collections.Dictionary
            {
                ["type"] = c.Type.ToString(),
                ["name"] = c.Name,
                ["description"] = c.Description,
                ["quote"] = TutorialContentV0.GetSelectionIntro(c.Type),
                ["memorable_line"] = TutorialContentV0.GetMemorableLine(c.Type),
            };
            result.Add(card);
        }

        return result;
    }

    /// <summary>
    /// Returns the narrator prompt text for the FO selection screen.
    /// </summary>
    public string GetTutorialNarratorPromptV0()
    {
        return TutorialContentV0.NarratorSelectionPrompt;
    }

    /// <summary>
    /// Returns FO hail lines for the pre-selection dialogue sequence.
    /// Array of {type, name, text, color_r, color_g, color_b}.
    /// </summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetTutorialFOHailsV0()
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();

        foreach (var c in new[] { FirstOfficerCandidate.Analyst, FirstOfficerCandidate.Veteran, FirstOfficerCandidate.Pathfinder })
        {
            var col = _foColorMap[c];
            var entry = new Godot.Collections.Dictionary
            {
                ["type"]    = c.ToString(),
                ["name"]    = _foNameMap[c],
                ["text"]    = TutorialContentV0.GetFoHailText(c),
                ["color_r"] = col.r,
                ["color_g"] = col.g,
                ["color_b"] = col.b,
            };
            result.Add(entry);
        }

        return result;
    }

    // ── Tutorial Commands ──────────────────────────────────────────

    /// <summary>
    /// Initialize tutorial state for a new game. Called by game_manager on new voyage.
    /// Creates the TutorialState entity and sets phase to Awaken.
    /// </summary>
    public void StartTutorialV0()
    {
        _stateLock.EnterWriteLock();
        try
        {
            var state = _kernel.State;
            // Always create fresh tutorial state (handles Continue → Menu → New Voyage).
            state.TutorialState = new TutorialState
            {
                Phase = TutorialPhase.Awaken
            };
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Select FO candidate during tutorial. Promotes the candidate AND
    /// records selection in tutorial state.
    /// </summary>
    public bool SelectTutorialFOV0(string candidateType)
    {
        if (!Enum.TryParse<FirstOfficerCandidate>(candidateType, out var parsed))
            return false;
        if (parsed == FirstOfficerCandidate.None) return false;

        bool success = false;

        _stateLock.EnterWriteLock();
        try
        {
            var state = _kernel.State;
            if (state.TutorialState == null) return false;

            // Promote the FO (bypasses normal tick window — tutorial forces promotion at tick 0).
            success = FirstOfficerSystem.PromoteCandidate(state, parsed);
            if (success)
            {
                state.TutorialState.SelectedCandidate = parsed;
            }
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }

        return success;
    }

    /// <summary>
    /// Dismiss the current phase's dialogue. Called by GDScript when player
    /// clicks/presses to advance past the dialogue box.
    /// </summary>
    public void DismissTutorialDialogueV0()
    {
        _stateLock.EnterWriteLock();
        try
        {
            var state = _kernel.State;
            if (state.TutorialState == null) return;
            var ts = state.TutorialState;

            // For Ship Computer phases, check ShipComputerLines for next sequence.
            if (ts.Phase == TutorialPhase.Awaken
                || ts.Phase == TutorialPhase.Flight_Intro
                || ts.Phase == TutorialPhase.Module_Calibration_Notice
                || ts.Phase == TutorialPhase.Cruise_Intro
                || ts.Phase == TutorialPhase.Graduation_Summary)
            {
                string nextSC = TutorialContentV0.GetShipComputerLine(ts.Phase, ts.DialogueSequence + 1);
                if (!string.IsNullOrEmpty(nextSC))
                {
                    ts.DialogueSequence++;
                    return;
                }
                ts.DialogueDismissed = true;
                return;
            }

            // For cameo phases (Dask_Hail, Lira_Tease), single line — always dismiss.
            if (ts.Phase == TutorialPhase.Dask_Hail || ts.Phase == TutorialPhase.Lira_Tease)
            {
                ts.DialogueDismissed = true;
                return;
            }

            // Determine which candidate's lines to check for next sequence.
            var speaker = ts.SelectedCandidate;
            if (speaker == FirstOfficerCandidate.None)
                speaker = TutorialContentV0.GetRotatingCandidate(ts.Phase);

            // Check if there's a next sequence line for the current phase.
            string nextLine = speaker != FirstOfficerCandidate.None
                ? TutorialContentV0.GetLine(ts.Phase, speaker, ts.DialogueSequence + 1)
                : "";
            if (!string.IsNullOrEmpty(nextLine))
            {
                // More lines in this phase — advance sequence, keep phase.
                ts.DialogueSequence++;
            }
            else
            {
                // No more lines — mark dialogue dismissed so TutorialSystem advances the phase.
                ts.DialogueDismissed = true;
            }
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Notify tutorial that player docked. Called by game_manager on dock event.
    /// Handles multiple dock-gated phases across all acts.
    /// </summary>
    public void NotifyTutorialDockV0()
    {
        _stateLock.EnterWriteLock();
        try
        {
            var state = _kernel.State;
            if (state.TutorialState == null) return;
            var ts = state.TutorialState;

            // Act 1: First_Dock — player docks for the first time → calibration notice.
            if (ts.Phase == TutorialPhase.First_Dock)
            {
                ts.Phase = TutorialPhase.Module_Calibration_Notice;
                ts.DialogueDismissed = false;
                ts.DialogueSequence = 0;
                ts.TicksSincePhaseChange = 0;
            }
            // Act 3: Arrival_Dock — dock at new station after travel.
            // If player has no cargo, go to Buy_Prompt first (they need to buy before they can sell).
            else if (ts.Phase == TutorialPhase.Arrival_Dock)
            {
                bool hasCargo = state.PlayerCargo.Values.Sum() > 0; // STRUCTURAL: empty cargo check
                if (hasCargo)
                {
                    ts.Phase = TutorialPhase.Sell_Prompt;
                    if (state.PlayerStats != null)
                        ts.GoodsTradedAtPhaseEntry = state.PlayerStats.GoodsTraded;
                }
                else
                {
                    ts.Phase = TutorialPhase.Buy_Prompt;
                }
                ts.DialogueDismissed = false;
                ts.DialogueSequence = 0;
                ts.TicksSincePhaseChange = 0;
            }
            // Act 3: Travel_Prompt — also handle dock at new station.
            else if (ts.Phase == TutorialPhase.Travel_Prompt
                && state.PlayerStats != null
                && state.PlayerStats.NodesVisited > ts.NodesVisitedAtPhaseEntry)
            {
                ts.Phase = TutorialPhase.Arrival_Dock;
                ts.DialogueDismissed = false;
                ts.DialogueSequence = 0;
                ts.TicksSincePhaseChange = 0;
            }
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Skip tutorial entirely. Sets phase to Tutorial_Complete.
    /// Used when tutorial toggle is off or for returning saves.
    /// </summary>
    public void SkipTutorialV0()
    {
        _stateLock.EnterWriteLock();
        try
        {
            var state = _kernel.State;
            if (state.TutorialState == null)
                state.TutorialState = new TutorialState();
            state.TutorialState.Phase = TutorialPhase.Tutorial_Complete;
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Check if tutorial is currently active (not complete, not null).
    /// </summary>
    public bool IsTutorialActiveV0()
    {
        bool active = false;
        TryExecuteSafeRead(state =>
        {
            active = TutorialSystem.IsActive(state);
        });
        return active;
    }

    /// <summary>
    /// Returns the number of manual trades completed in the tutorial trade loop.
    /// </summary>
    public int GetTutorialManualTradesV0()
    {
        int trades = 0;
        TryExecuteSafeRead(state =>
        {
            if (state.TutorialState != null)
                trades = state.TutorialState.ManualTradesCompleted;
        });
        return trades;
    }

    // ── Trade Guidance (Tutorial) ────────────────────────────────────

    /// <summary>
    /// Scans adjacent nodes for the best sell price for the player's current cargo.
    /// Returns {node_id, node_name, good_name, sell_price, buy_price} or empty dict.
    /// Used by tutorial_director to set edgedar waypoint and FO dialogue.
    /// </summary>
    public Godot.Collections.Dictionary GetTutorialSellTargetV0()
    {
        var result = new Godot.Collections.Dictionary();

        TryExecuteSafeRead(state =>
        {
            if (state.PlayerCargo.Count == 0) return;
            var currentNodeId = state.PlayerLocationNodeId;
            if (string.IsNullOrEmpty(currentNodeId)) return;

            // Find the primary cargo good (highest quantity).
            string cargoGood = "";
            int cargoQty = 0;
            foreach (var kvp in state.PlayerCargo)
            {
                if (kvp.Value > cargoQty) { cargoGood = kvp.Key; cargoQty = kvp.Value; }
            }
            if (string.IsNullOrEmpty(cargoGood)) return;

            int buyPrice = state.PlayerCargoCostBasis.GetValueOrDefault(cargoGood, 0);

            // Find adjacent nodes via edges — prefer profitable destinations.
            int bestSellPrice = 0;
            string bestNodeId = "";
            string bestNodeName = "";
            // Also track best among 2-hop neighbors if no 1-hop is profitable.
            int bestSellPrice2 = 0;
            string bestNodeId2 = "";
            string bestNodeName2 = "";
            var visitedNeighbors = new HashSet<string>();

            foreach (var edge in state.Edges.Values)
            {
                string neighborId = "";
                if (edge.FromNodeId == currentNodeId) neighborId = edge.ToNodeId;
                else if (edge.ToNodeId == currentNodeId) neighborId = edge.FromNodeId;
                if (string.IsNullOrEmpty(neighborId)) continue;

                visitedNeighbors.Add(neighborId);

                if (!state.Nodes.TryGetValue(neighborId, out var neighborNode)) continue;
                if (string.IsNullOrEmpty(neighborNode.MarketId)) continue;
                if (!state.Markets.TryGetValue(neighborNode.MarketId, out var market)) continue;

                int sellPrice = market.GetSellPrice(cargoGood);
                // Only accept if sell price > buy cost (actual profit).
                if (sellPrice > buyPrice && sellPrice > bestSellPrice)
                {
                    bestSellPrice = sellPrice;
                    bestNodeId = neighborId;
                    bestNodeName = neighborNode.Name;
                }
            }

            // If no 1-hop neighbor is profitable, search 2-hop neighbors.
            if (string.IsNullOrEmpty(bestNodeId))
            {
                foreach (var hop1Id in visitedNeighbors)
                {
                    foreach (var edge2 in state.Edges.Values)
                    {
                        string hop2Id = "";
                        if (edge2.FromNodeId == hop1Id) hop2Id = edge2.ToNodeId;
                        else if (edge2.ToNodeId == hop1Id) hop2Id = edge2.FromNodeId;
                        if (string.IsNullOrEmpty(hop2Id)) continue;
                        if (hop2Id == currentNodeId) continue;
                        if (visitedNeighbors.Contains(hop2Id)) continue;

                        if (!state.Nodes.TryGetValue(hop2Id, out var hop2Node)) continue;
                        if (string.IsNullOrEmpty(hop2Node.MarketId)) continue;
                        if (!state.Markets.TryGetValue(hop2Node.MarketId, out var hop2Market)) continue;

                        int sellPrice2 = hop2Market.GetSellPrice(cargoGood);
                        if (sellPrice2 > buyPrice && sellPrice2 > bestSellPrice2)
                        {
                            bestSellPrice2 = sellPrice2;
                            bestNodeId2 = hop2Id;
                            bestNodeName2 = hop2Node.Name;
                        }
                    }
                }
            }

            // Prefer 1-hop profitable, then 2-hop profitable.
            if (!string.IsNullOrEmpty(bestNodeId))
            {
                result["node_id"] = bestNodeId;
                result["node_name"] = bestNodeName;
                result["good_name"] = cargoGood;
                result["sell_price"] = bestSellPrice;
                result["buy_price"] = buyPrice;
            }
            else if (!string.IsNullOrEmpty(bestNodeId2))
            {
                result["node_id"] = bestNodeId2;
                result["node_name"] = bestNodeName2;
                result["good_name"] = cargoGood;
                result["sell_price"] = bestSellPrice2;
                result["buy_price"] = buyPrice;
                result["two_hop"] = true;
            }
        });

        return result;
    }

    /// <summary>
    /// Checks if the current station is a bad place to sell the player's cargo.
    /// Returns {is_bad (bool), good_name, sell_price, buy_price, better_node_name} or empty dict.
    /// </summary>
    public Godot.Collections.Dictionary IsBadSellStationV0()
    {
        var result = new Godot.Collections.Dictionary();

        TryExecuteSafeRead(state =>
        {
            if (state.PlayerCargo.Count == 0) return;
            var currentNodeId = state.PlayerLocationNodeId;
            if (string.IsNullOrEmpty(currentNodeId)) return;
            if (!state.Nodes.TryGetValue(currentNodeId, out var currentNode)) return;
            if (string.IsNullOrEmpty(currentNode.MarketId)) return;
            if (!state.Markets.TryGetValue(currentNode.MarketId, out var currentMarket)) return;

            // Find the primary cargo good.
            string cargoGood = "";
            int cargoQty = 0;
            foreach (var kvp in state.PlayerCargo)
            {
                if (kvp.Value > cargoQty) { cargoGood = kvp.Key; cargoQty = kvp.Value; }
            }
            if (string.IsNullOrEmpty(cargoGood)) return;

            int buyPrice = state.PlayerCargoCostBasis.GetValueOrDefault(cargoGood, 0);
            int sellPrice = currentMarket.GetSellPrice(cargoGood);
            bool isBad = sellPrice <= buyPrice;

            // Find a better station if this one is bad.
            string betterName = "";
            if (isBad)
            {
                int bestSell = sellPrice;
                foreach (var edge in state.Edges.Values)
                {
                    string neighborId = "";
                    if (edge.FromNodeId == currentNodeId) neighborId = edge.ToNodeId;
                    else if (edge.ToNodeId == currentNodeId) neighborId = edge.FromNodeId;
                    if (string.IsNullOrEmpty(neighborId)) continue;

                    if (!state.Nodes.TryGetValue(neighborId, out var n)) continue;
                    if (string.IsNullOrEmpty(n.MarketId)) continue;
                    if (!state.Markets.TryGetValue(n.MarketId, out var m)) continue;

                    int ns = m.GetSellPrice(cargoGood);
                    if (ns > bestSell) { bestSell = ns; betterName = n.Name; }
                }
            }

            result["is_bad"] = isBad;
            result["good_name"] = cargoGood;
            result["sell_price"] = sellPrice;
            result["buy_price"] = buyPrice;
            result["better_node_name"] = betterName;
        });

        return result;
    }

    // ── Bot Force-Advance Helpers (headless testing only) ──────────

    /// <summary>
    /// Force-discover Haven for tutorial bot. Sets Haven.Discovered = true.
    /// </summary>
    public void ForceDiscoverHavenV0()
    {
        _stateLock.EnterWriteLock();
        try
        {
            var state = _kernel.State;
            state.Haven ??= new HavenStarbase();
            state.Haven.Discovered = true;
            // Set tier to Inhabited so research lab slots are available.
            if (state.Haven.Tier < HavenTier.Inhabited)
                state.Haven.Tier = HavenTier.Inhabited;
            // Ensure Haven has research lab slots for Act 9.
            if (state.Haven.ResearchLabSlots == null || state.Haven.ResearchLabSlots.Count == 0)
            {
                state.Haven.ResearchLabSlots = new List<HavenResearchSlot> { new() };
            }
        }
        finally { _stateLock.ExitWriteLock(); }
    }

    /// <summary>
    /// Force-start research in Haven slot 0 for tutorial bot (Research_Start gate).
    /// Picks the first tier-1 tech with no prerequisites.
    /// </summary>
    public void ForceStartResearchV0()
    {
        _stateLock.EnterWriteLock();
        try
        {
            var state = _kernel.State;
            state.Haven ??= new HavenStarbase();
            if (state.Haven.Tier < HavenTier.Inhabited)
                state.Haven.Tier = HavenTier.Inhabited;
            if (state.Haven.ResearchLabSlots == null || state.Haven.ResearchLabSlots.Count == 0)
                state.Haven.ResearchLabSlots = new List<HavenResearchSlot> { new() };

            var slot = state.Haven.ResearchLabSlots[0];
            if (!slot.IsActive)
            {
                // Find first available tech.
                foreach (var tech in TechContentV0.AllTechs)
                {
                    if (state.Tech.UnlockedTechIds.Contains(tech.TechId)) continue;
                    if (!TechContentV0.PrerequisitesMet(tech.TechId, state.Tech.UnlockedTechIds)) continue;
                    slot.TechId = tech.TechId;
                    slot.ProgressTicks = 0;
                    slot.TotalTicks = tech.ResearchTicks;
                    break;
                }
            }
        }
        finally { _stateLock.ExitWriteLock(); }
    }

    /// <summary>
    /// Force-set player fleet hull to max for tutorial bot (Repair_Prompt gate).
    /// </summary>
    /// <summary>
    /// Force-damage player hull to simulate combat for tutorial bot.
    /// Sets hull to 50% of max.
    /// </summary>
    public void ForceDamagePlayerHullV0()
    {
        _stateLock.EnterWriteLock();
        try
        {
            var state = _kernel.State;
            foreach (var fleet in state.Fleets.Values)
            {
                if (string.Equals(fleet.OwnerId, "player", StringComparison.Ordinal))
                {
                    fleet.HullHp = fleet.HullHpMax / 2;
                    break;
                }
            }
        }
        finally { _stateLock.ExitWriteLock(); }
    }

    public void ForceRepairPlayerHullV0()
    {
        _stateLock.EnterWriteLock();
        try
        {
            var state = _kernel.State;
            foreach (var fleet in state.Fleets.Values)
            {
                if (string.Equals(fleet.OwnerId, "player", StringComparison.Ordinal))
                {
                    fleet.HullHp = fleet.HullHpMax;
                    break;
                }
            }
        }
        finally { _stateLock.ExitWriteLock(); }
    }

    /// <summary>
    /// Force-increment NpcFleetsDestroyed stat for tutorial bot (Combat_Engage gate).
    /// </summary>
    public void ForceIncrementNpcFleetsDestroyedV0()
    {
        _stateLock.EnterWriteLock();
        try
        {
            var state = _kernel.State;
            if (state.PlayerStats != null)
                state.PlayerStats.NpcFleetsDestroyed++;
            // Arm the pirate-spawned guard so future code that checks this flag
            // won't double-spawn the tutorial pirate after save/load.
            if (state.TutorialState != null)
                state.TutorialState.TutorialPirateSpawned = true;
        }
        finally { _stateLock.ExitWriteLock(); }
    }

    /// <summary>
    /// Force-grant a module to player fleet slot 0 for tutorial bot (Module_Equip gate).
    /// </summary>
    public void ForceGrantModuleV0(string moduleId)
    {
        _stateLock.EnterWriteLock();
        try
        {
            var state = _kernel.State;
            foreach (var fleet in state.Fleets.Values)
            {
                if (!string.Equals(fleet.OwnerId, "player", StringComparison.Ordinal)) continue;
                if (fleet.Slots == null || fleet.Slots.Count == 0) continue;
                fleet.Slots[0].InstalledModuleId = moduleId;
                break;
            }
            // Arm the module-granted guard so future code that checks this flag
            // won't re-grant the tutorial module after save/load.
            if (state.TutorialState != null)
                state.TutorialState.TutorialModuleGranted = true;
        }
        finally { _stateLock.ExitWriteLock(); }
    }

    /// <summary>
    /// Get wrong-station warning text for the active FO (rotating or selected).
    /// Replaces {station} placeholder with the actual better station name.
    /// </summary>
    public string GetWrongStationWarningV0(string betterStationName)
    {
        string text = "";
        TryExecuteSafeRead(state =>
        {
            if (state.TutorialState == null) return;
            var ts = state.TutorialState;

            var speaker = ts.SelectedCandidate;
            if (speaker == FirstOfficerCandidate.None)
                speaker = TutorialContentV0.GetRotatingCandidate(ts.Phase);
            if (speaker == FirstOfficerCandidate.None)
                speaker = FirstOfficerCandidate.Analyst; // Fallback to Maren

            text = TutorialContentV0.GetWrongStationText(speaker)
                .Replace("{station}", betterStationName);
        });
        return text;
    }
}
