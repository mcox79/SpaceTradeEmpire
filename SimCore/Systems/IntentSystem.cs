using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SimCore.Intents;

namespace SimCore.Systems;

public static class IntentSystem
{
    // GATE.S3.FLEET.ROLES.001
    // Optional intent surface for "competing route choices" that must be resolved deterministically.
    // Only intents implementing this interface are subject to role-based selection.
    public interface IFleetRouteChoiceIntent : IIntent
    {
        string FleetId { get; }
        string RouteId { get; } // Stable identity for deterministic tie-breaks

        // Scores are v0 integer proxies. Higher is better for Profit%Capacity; lower is better for Risk.
        int ProfitScore { get; }
        int CapacityScore { get; }
        int RiskScore { get; }
    }

    private sealed class Scratch
    {
        public readonly List<IntentEnvelope> Due = new();
        public readonly List<IntentEnvelope> ChoiceEnvelopes = new();
        public readonly HashSet<long> SelectedSeq = new();
        public readonly HashSet<long> DueSeq = new();
        // Group key → list of (envelope, choice intent) pairs
        public readonly Dictionary<(int CreatedTick, string FleetId), List<(IntentEnvelope env, IFleetRouteChoiceIntent ci)>> Groups = new();
        public readonly List<(int CreatedTick, string FleetId)> SortedGroupKeys = new();
        public readonly List<(IntentEnvelope env, IFleetRouteChoiceIntent ci)> CandidateSortBuf = new();
    }

    private static readonly ConditionalWeakTable<SimState, Scratch> s_scratch = new();

    public static void Process(SimState state)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));
        if (state.PendingIntents.Count == 0) return;

        var now = state.Tick;
        var scratch = s_scratch.GetOrCreateValue(state);

        // --- Collect due intents ---
        var due = scratch.Due;
        due.Clear();
        foreach (var env in state.PendingIntents)
        {
            if (env.CreatedTick <= now) due.Add(env);
        }
        if (due.Count == 0) return;

        // Deterministic sort: CreatedTick, Seq, Kind
        due.Sort(static (a, b) =>
        {
            int c = a.CreatedTick.CompareTo(b.CreatedTick);
            if (c != 0) return c;
            c = a.Seq.CompareTo(b.Seq);
            if (c != 0) return c;
            return string.Compare(a.Kind, b.Kind, StringComparison.Ordinal);
        });

        // --- Group choice intents by (CreatedTick, FleetId) ---
        var groups = scratch.Groups;
        groups.Clear();
        var choiceEnvelopes = scratch.ChoiceEnvelopes;
        choiceEnvelopes.Clear();

        foreach (var env in due)
        {
            if (env.Intent is IFleetRouteChoiceIntent ci)
            {
                choiceEnvelopes.Add(env);
                var key = (env.CreatedTick, FleetId: ci.FleetId ?? "");
                if (!groups.TryGetValue(key, out var list))
                {
                    list = new List<(IntentEnvelope, IFleetRouteChoiceIntent)>();
                    groups[key] = list;
                }
                list.Add((env, ci));
            }
        }

        // --- Deterministic winner selection per group ---
        var selectedSeq = scratch.SelectedSeq;
        selectedSeq.Clear();

        if (groups.Count > 0)
        {
            var sortedKeys = scratch.SortedGroupKeys;
            sortedKeys.Clear();
            foreach (var k in groups.Keys) sortedKeys.Add(k);
            sortedKeys.Sort(static (a, b) =>
            {
                int c = a.CreatedTick.CompareTo(b.CreatedTick);
                if (c != 0) return c;
                return string.Compare(a.FleetId, b.FleetId, StringComparison.Ordinal);
            });

            var sortBuf = scratch.CandidateSortBuf;

            foreach (var key in sortedKeys)
            {
                var candidates = groups[key];

                // Fleet role defaults to Trader if missing.
                var role = SimCore.Entities.FleetRole.Trader;
                if (state.Fleets != null &&
                    !string.IsNullOrWhiteSpace(key.FleetId) &&
                    state.Fleets.TryGetValue(key.FleetId, out var fleet) &&
                    fleet != null)
                {
                    role = fleet.Role;
                }

                // Sort candidates deterministically by role priority.
                sortBuf.Clear();
                sortBuf.AddRange(candidates);
                sortBuf.Sort((a, b) => CompareByRole(a, b, role));

                var winner = sortBuf[0];
                var ciw = winner.ci;

                // Emit schema-bound fleet event describing the deterministic choice.
                state.EmitFleetEvent(new SimCore.Events.FleetEvents.Event
                {
                    Type = SimCore.Events.FleetEvents.FleetEventType.RouteChoice,
                    FleetId = ciw.FleetId ?? "",
                    Role = (int)role,
                    ChosenRouteId = ciw.RouteId ?? "",
                    ProfitScore = ciw.ProfitScore,
                    CapacityScore = ciw.CapacityScore,
                    RiskScore = ciw.RiskScore,
                    Note = "route_choice_v0"
                });

                selectedSeq.Add(winner.env.Seq);
            }
        }

        // --- Apply intents ---
        foreach (var env in due)
        {
            if (env.Intent is IFleetRouteChoiceIntent)
            {
                // Skip non-selected competitors deterministically.
                if (!selectedSeq.Contains(env.Seq)) continue;
            }

            env.Intent?.Apply(state);
        }

        // --- Remove processed intents deterministically ---
        var dueSeq = scratch.DueSeq;
        dueSeq.Clear();
        foreach (var env in due) dueSeq.Add(env.Seq);
        state.PendingIntents.RemoveAll(x => dueSeq.Contains(x.Seq));
    }

    private static int CompareByRole(
        (IntentEnvelope env, IFleetRouteChoiceIntent ci) a,
        (IntentEnvelope env, IFleetRouteChoiceIntent ci) b,
        SimCore.Entities.FleetRole role)
    {
        int c;
        if (role == SimCore.Entities.FleetRole.Hauler)
        {
            c = b.ci.CapacityScore.CompareTo(a.ci.CapacityScore); // descending
            if (c != 0) return c;
            c = b.ci.ProfitScore.CompareTo(a.ci.ProfitScore); // descending
            if (c != 0) return c;
            c = a.ci.RiskScore.CompareTo(b.ci.RiskScore); // ascending
            if (c != 0) return c;
        }
        else if (role == SimCore.Entities.FleetRole.Patrol)
        {
            c = a.ci.RiskScore.CompareTo(b.ci.RiskScore); // ascending
            if (c != 0) return c;
            c = b.ci.ProfitScore.CompareTo(a.ci.ProfitScore); // descending
            if (c != 0) return c;
            c = b.ci.CapacityScore.CompareTo(a.ci.CapacityScore); // descending
            if (c != 0) return c;
        }
        else
        {
            // Trader (default)
            c = b.ci.ProfitScore.CompareTo(a.ci.ProfitScore); // descending
            if (c != 0) return c;
            c = b.ci.CapacityScore.CompareTo(a.ci.CapacityScore); // descending
            if (c != 0) return c;
            c = a.ci.RiskScore.CompareTo(b.ci.RiskScore); // ascending
            if (c != 0) return c;
        }
        // Stable tie-breaks: RouteId, then Seq
        c = string.Compare(a.ci.RouteId, b.ci.RouteId, StringComparison.Ordinal);
        if (c != 0) return c;
        return a.env.Seq.CompareTo(b.env.Seq);
    }
}
