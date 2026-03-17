#nullable enable

using Godot;
using SimCore;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Systems;
using System;
using System.Collections.Generic;

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
    /// dialogue_dismissed (bool), stall_ticks (int), objective (string), pre_selection (bool)}.
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
            result["stall_ticks"] = ts.TicksSincePhaseChange;
            result["objective"] = TutorialContentV0.GetObjectiveText(ts.Phase);
            result["pre_selection"] = ts.SelectedCandidate == FirstOfficerCandidate.None
                && (int)ts.Phase >= (int)TutorialPhase.Flight_Intro
                && (int)ts.Phase <= (int)TutorialPhase.Sell_Complete;
        });

        return result;
    }

    /// <summary>
    /// Returns the tutorial dialogue line for the current phase.
    /// Post-selection: uses selected candidate. Pre-selection: uses rotating candidate.
    /// Returns empty string if no line for current phase or tutorial not active.
    /// </summary>
    public string GetTutorialDialogueV0()
    {
        string line = "";

        TryExecuteSafeRead(state =>
        {
            if (state.TutorialState == null) return;
            var ts = state.TutorialState;

            // Determine which candidate speaks.
            var speaker = ts.SelectedCandidate;
            if (speaker == FirstOfficerCandidate.None)
            {
                // Pre-selection: use rotating candidate for this phase.
                speaker = TutorialContentV0.GetRotatingCandidate(ts.Phase);
                if (speaker == FirstOfficerCandidate.None) return;
            }

            line = TutorialContentV0.GetLine(ts.Phase, speaker, ts.DialogueSequence);
        });

        return line;
    }

    /// <summary>
    /// Returns the rotating FO dialogue data for the current phase (pre-selection mode).
    /// Returns {type, name, text, color_r, color_g, color_b} or empty dict if not in pre-selection.
    /// </summary>
    public Godot.Collections.Dictionary GetRotatingFODialogueV0()
    {
        var result = new Godot.Collections.Dictionary();

        TryExecuteSafeRead(state =>
        {
            if (state.TutorialState == null) return;
            var ts = state.TutorialState;

            var speaker = ts.SelectedCandidate != FirstOfficerCandidate.None
                ? ts.SelectedCandidate
                : TutorialContentV0.GetRotatingCandidate(ts.Phase);
            if (speaker == FirstOfficerCandidate.None) return;

            string text = TutorialContentV0.GetLine(ts.Phase, speaker, ts.DialogueSequence);
            if (string.IsNullOrEmpty(text)) return;

            var col = _foColorMap.GetValueOrDefault(speaker, (0.5f, 0.5f, 0.5f));
            result["type"] = speaker.ToString();
            result["name"] = _foNameMap.GetValueOrDefault(speaker, speaker.ToString());
            result["text"] = text;
            result["color_r"] = col.Item1;
            result["color_g"] = col.Item2;
            result["color_b"] = col.Item3;
        });

        return result;
    }

    /// <summary>
    /// Returns true when in pre-selection mode (rotating FO auditions).
    /// </summary>
    public bool IsPreSelectionModeV0()
    {
        bool result = false;
        TryExecuteSafeRead(state =>
        {
            if (state.TutorialState == null) return;
            var ts = state.TutorialState;
            result = ts.SelectedCandidate == FirstOfficerCandidate.None
                && (int)ts.Phase >= (int)TutorialPhase.Flight_Intro
                && (int)ts.Phase <= (int)TutorialPhase.Sell_Complete;
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
    /// Returns post-trade prompt if in FO_Selection_PostTrade phase.
    /// </summary>
    public string GetTutorialNarratorPromptV0()
    {
        bool postTrade = false;
        TryExecuteSafeRead(state =>
        {
            if (state.TutorialState != null)
                postTrade = state.TutorialState.Phase == TutorialPhase.FO_Selection_PostTrade;
        });
        return postTrade
            ? TutorialContentV0.NarratorSelectionPromptPostTrade
            : TutorialContentV0.NarratorSelectionPrompt;
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
    /// Creates the TutorialState entity and sets phase to FO_Selection.
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
                Phase = TutorialPhase.FO_Selection
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
    /// Advances Dock_Prompt → Docked_First if in that phase.
    /// </summary>
    public void NotifyTutorialDockV0()
    {
        _stateLock.EnterWriteLock();
        try
        {
            var state = _kernel.State;
            if (state.TutorialState == null) return;
            var ts = state.TutorialState;

            if (ts.Phase == TutorialPhase.Dock_Prompt)
            {
                ts.Phase = TutorialPhase.Docked_First;
                ts.DialogueDismissed = false;
                ts.TicksSincePhaseChange = 0;
            }
            // Also handle Travel_Prompt → Sell_Prompt on dock at new station
            else if (ts.Phase == TutorialPhase.Travel_Prompt
                && state.PlayerStats != null
                && state.PlayerStats.NodesVisited > ts.NodesVisitedAtPhaseEntry)
            {
                ts.Phase = TutorialPhase.Sell_Prompt;
                ts.DialogueDismissed = false;
                ts.TicksSincePhaseChange = 0;
                ts.GoodsTradedAtPhaseEntry = state.PlayerStats.GoodsTraded;
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

            // Find adjacent nodes via edges.
            int bestSellPrice = 0;
            string bestNodeId = "";
            string bestNodeName = "";

            foreach (var edge in state.Edges.Values)
            {
                string neighborId = "";
                if (edge.FromNodeId == currentNodeId) neighborId = edge.ToNodeId;
                else if (edge.ToNodeId == currentNodeId) neighborId = edge.FromNodeId;
                if (string.IsNullOrEmpty(neighborId)) continue;

                if (!state.Nodes.TryGetValue(neighborId, out var neighborNode)) continue;
                if (string.IsNullOrEmpty(neighborNode.MarketId)) continue;
                if (!state.Markets.TryGetValue(neighborNode.MarketId, out var market)) continue;

                int sellPrice = market.GetSellPrice(cargoGood);
                if (sellPrice > bestSellPrice)
                {
                    bestSellPrice = sellPrice;
                    bestNodeId = neighborId;
                    bestNodeName = neighborNode.Name;
                }
            }

            if (!string.IsNullOrEmpty(bestNodeId))
            {
                result["node_id"] = bestNodeId;
                result["node_name"] = bestNodeName;
                result["good_name"] = cargoGood;
                result["sell_price"] = bestSellPrice;
                result["buy_price"] = buyPrice;
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
                speaker = FirstOfficerCandidate.Pathfinder; // Fallback

            text = TutorialContentV0.GetWrongStationText(speaker)
                .Replace("{station}", betterStationName);
        });
        return text;
    }
}
