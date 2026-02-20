using System;
using System.Linq;
using SimCore.Intents;

namespace SimCore.Systems;

public static class IntentSystem
{
    // GATE.S3.FLEET.ROLES.001
    // Optional intent surface for “competing route choices” that must be resolved deterministically.
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

    public static void Process(SimState state)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));
        if (state.PendingIntents.Count == 0) return;

        var now = state.Tick;

        var due = state.PendingIntents
            .Where(x => x.CreatedTick <= now)
            .OrderBy(x => x.CreatedTick)
            .ThenBy(x => x.Seq)
            .ThenBy(x => x.Kind, StringComparer.Ordinal)
            .ToList();

        if (due.Count == 0) return;

        // Role-based deterministic selection for competing route-choice intents.
        // Semantics: for a given (CreatedTick, FleetId), apply exactly one choice intent (best per role),
        // then drop the remaining competing choice intents in that group. All non-choice intents are applied as-is.
        // Determine winners per (CreatedTick, FleetId) and emit a schema-bound event for each winner.
        var choiceGroups = due
            .Where(env => env.Intent is IFleetRouteChoiceIntent)
            .GroupBy(env =>
            {
                var ci = (IFleetRouteChoiceIntent)env.Intent!;
                return (env.CreatedTick, FleetId: ci.FleetId);
            })
            .OrderBy(g => g.Key.CreatedTick)
            .ThenBy(g => g.Key.FleetId ?? "", StringComparer.Ordinal)
            .ToList();

        var selectedSeq = choiceGroups
            .Select(g =>
            {
                // Fleet role defaults to Trader if missing.
                var role = SimCore.Entities.FleetRole.Trader;
                if (state.Fleets != null &&
                    !string.IsNullOrWhiteSpace(g.Key.FleetId) &&
                    state.Fleets.TryGetValue(g.Key.FleetId, out var fleet) &&
                    fleet != null)
                {
                    role = fleet.Role;
                }

                var candidates = g.Select(env => (env, ci: (IFleetRouteChoiceIntent)env.Intent!));

                IOrderedEnumerable<(IntentEnvelope env, IFleetRouteChoiceIntent ci)> ordered;

                // Order within group deterministically based on role, then stable tie-breaks.
                if (role == SimCore.Entities.FleetRole.Hauler)
                {
                    ordered = candidates
                        .OrderByDescending(x => x.ci.CapacityScore)
                        .ThenByDescending(x => x.ci.ProfitScore)
                        .ThenBy(x => x.ci.RiskScore)
                        .ThenBy(x => x.ci.RouteId, StringComparer.Ordinal)
                        .ThenBy(x => x.env.Seq);
                }
                else if (role == SimCore.Entities.FleetRole.Patrol)
                {
                    ordered = candidates
                        .OrderBy(x => x.ci.RiskScore)
                        .ThenByDescending(x => x.ci.ProfitScore)
                        .ThenByDescending(x => x.ci.CapacityScore)
                        .ThenBy(x => x.ci.RouteId, StringComparer.Ordinal)
                        .ThenBy(x => x.env.Seq);
                }
                else
                {
                    // Trader (default)
                    ordered = candidates
                        .OrderByDescending(x => x.ci.ProfitScore)
                        .ThenByDescending(x => x.ci.CapacityScore)
                        .ThenBy(x => x.ci.RiskScore)
                        .ThenBy(x => x.ci.RouteId, StringComparer.Ordinal)
                        .ThenBy(x => x.env.Seq);
                }

                var winner = ordered.First();
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

                return winner.env.Seq;
            })
            .ToHashSet();

        foreach (var env in due)
        {
            if (env.Intent is IFleetRouteChoiceIntent)
            {
                // Skip non-selected competitors deterministically.
                if (!selectedSeq.Contains(env.Seq)) continue;
            }

            env.Intent?.Apply(state);
        }

        // Remove processed intents deterministically
        var dueSeq = due.Select(x => x.Seq).ToHashSet();
        state.PendingIntents.RemoveAll(x => dueSeq.Contains(x.Seq));
    }
}
