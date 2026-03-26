using SimCore.Entities;
using System;
using System.Collections.Generic;

namespace SimCore.Systems;

// GATE.T58.FO.DECISION_DIALOGUE.001: Multi-option FO decision dialogues.
// Per fo_trade_manager_v0.md §Decision Dialogue Design Rules:
// 1. FO recommendation highlighted (personality-driven)
// 2. All options visible (no collapse)
// 3. Context → Stakes → Options structure
// 4. Quantified consequences
// 5. One briefing at a time (queue by severity)
//
// Decision types: Crisis, Fleet, Construction, Route, Warfront.
// The system queues decisions, the bridge consumes them for UI display.
public static class DecisionDialogueSystem
{
    public static void Process(SimState state)
    {
        if (state.FirstOfficer is null || !state.FirstOfficer.IsPromoted) return;

        // Rule 5: One briefing at a time — only process if no active decision pending.
        if (state.FirstOfficer.ActiveDecision is not null
            && state.FirstOfficer.ActiveDecision.Status == DecisionStatus.AwaitingPlayer)
            return;

        // Dequeue next decision if available.
        if (state.FirstOfficer.DecisionQueue.Count > 0) // STRUCTURAL: queue empty check
        {
            // Sort by severity descending (highest severity first).
            state.FirstOfficer.DecisionQueue.Sort((a, b) => b.Severity.CompareTo(a.Severity));
            state.FirstOfficer.ActiveDecision = state.FirstOfficer.DecisionQueue[0]; // STRUCTURAL: index 0
            state.FirstOfficer.DecisionQueue.RemoveAt(0); // STRUCTURAL: dequeue
            state.FirstOfficer.ActiveDecision.Status = DecisionStatus.AwaitingPlayer;
            state.FirstOfficer.ActiveDecision.PresentedTick = state.Tick;
        }
    }

    /// <summary>
    /// Queue a new decision dialogue for the FO to present.
    /// Called by other systems (WarfrontSystem, EmpireHealthSystem, etc.) when a decision is needed.
    /// </summary>
    public static void QueueDecision(SimState state, FODecision decision)
    {
        if (state.FirstOfficer is null) return;
        if (decision is null || decision.Options.Count == 0) return;

        // Apply FO personality to recommendation.
        if (decision.RecommendedOptionIndex < 0)
        {
            decision.RecommendedOptionIndex = SelectRecommendation(
                state.FirstOfficer.CandidateType, decision.Options);
        }

        state.FirstOfficer.DecisionQueue.Add(decision);
    }

    /// <summary>
    /// Player selects an option. Returns true if valid selection.
    /// </summary>
    public static bool ResolveDecision(SimState state, int optionIndex)
    {
        if (state.FirstOfficer?.ActiveDecision is null) return false;

        var decision = state.FirstOfficer.ActiveDecision;
        if (decision.Status != DecisionStatus.AwaitingPlayer) return false;
        if (optionIndex < 0 || optionIndex >= decision.Options.Count) return false; // STRUCTURAL: bounds

        decision.SelectedOptionIndex = optionIndex;
        decision.Status = DecisionStatus.Resolved;
        decision.ResolvedTick = state.Tick;

        // Track in service record.
        var record = state.FirstOfficer.ServiceRecord;
        record.RecommendationsOffered++;
        if (optionIndex == decision.RecommendedOptionIndex)
            record.RecommendationsTaken++;

        // Clear active decision so next can be dequeued.
        state.FirstOfficer.ResolvedDecisions.Add(decision);
        state.FirstOfficer.ActiveDecision = null;

        return true;
    }

    /// <summary>
    /// Personality-driven recommendation selection.
    /// Analyst → margin-optimal, Veteran → defensive, Pathfinder → exploratory.
    /// </summary>
    private static int SelectRecommendation(FirstOfficerCandidate personality, List<DecisionOption> options)
    {
        if (options.Count == 0) return 0; // STRUCTURAL: fallback

        int bestIdx = 0;

        switch (personality)
        {
            case FirstOfficerCandidate.Analyst:
                // Maren: highest CreditImpact option.
                long bestCredit = long.MinValue;
                for (int i = 0; i < options.Count; i++)
                {
                    if (options[i].CreditImpact > bestCredit)
                    {
                        bestCredit = options[i].CreditImpact;
                        bestIdx = i;
                    }
                }
                break;

            case FirstOfficerCandidate.Veteran:
                // Dask: lowest risk (lowest RiskLevel, then highest CreditImpact).
                int bestRisk = int.MaxValue;
                for (int i = 0; i < options.Count; i++)
                {
                    if (options[i].RiskLevel < bestRisk
                        || (options[i].RiskLevel == bestRisk && options[i].CreditImpact > (bestIdx < options.Count ? options[bestIdx].CreditImpact : 0)))
                    {
                        bestRisk = options[i].RiskLevel;
                        bestIdx = i;
                    }
                }
                break;

            case FirstOfficerCandidate.Pathfinder:
                // Lira: highest ExplorationValue option.
                int bestExplore = int.MinValue;
                for (int i = 0; i < options.Count; i++)
                {
                    if (options[i].ExplorationValue > bestExplore)
                    {
                        bestExplore = options[i].ExplorationValue;
                        bestIdx = i;
                    }
                }
                break;
        }

        return bestIdx;
    }
}
