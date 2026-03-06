using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.S15.FEEL.JUMP_EVENT_SYS.001: Random events when fleets complete lane transit.
public static class JumpEventSystem
{
    // Called after MovementSystem. Checks ArrivalsThisTick for fleets that just landed.
    public static void Process(SimState state)
    {
        if (state.ArrivalsThisTick.Count == 0) return;

        var totalWeight = JumpEventTweaksV0.SalvageWeight
                        + JumpEventTweaksV0.SignalWeight
                        + JumpEventTweaksV0.TurbulenceWeight;
        if (totalWeight <= 0) return;

        foreach (var (fleetId, edgeId, nodeId) in state.ArrivalsThisTick)
        {
            // Deterministic RNG: derive from tick + fleet hash.
            int seed = HashCombine(state.Tick, fleetId);
            var rng = new Random(seed);

            int roll = rng.Next(JumpEventTweaksV0.ProbabilityRange);
            if (roll >= JumpEventTweaksV0.EventChancePct) continue;

            // Pick event type by weighted roll.
            int typeRoll = rng.Next(totalWeight);
            JumpEventKind kind;
            if (typeRoll < JumpEventTweaksV0.SalvageWeight)
                kind = JumpEventKind.Salvage;
            else if (typeRoll < JumpEventTweaksV0.SalvageWeight + JumpEventTweaksV0.SignalWeight)
                kind = JumpEventKind.Signal;
            else
                kind = JumpEventKind.Turbulence;

            var evt = new JumpEvent
            {
                EventId = $"JE{state.NextJumpEventSeq}",
                Kind = kind,
                FleetId = fleetId,
                EdgeId = edgeId,
                NodeId = nodeId,
                Tick = state.Tick
            };
            state.NextJumpEventSeq++;

            switch (kind)
            {
                case JumpEventKind.Salvage:
                    ApplySalvage(state, evt, fleetId, rng);
                    break;
                case JumpEventKind.Signal:
                    // Signal: just a notification for now. Discovery lead system can extend later.
                    break;
                case JumpEventKind.Turbulence:
                    ApplyTurbulence(state, evt, fleetId, rng);
                    break;
            }

            // Cap event history.
            while (state.JumpEvents.Count >= JumpEventTweaksV0.MaxJumpEventHistory)
                state.JumpEvents.RemoveAt(0);
            state.JumpEvents.Add(evt);
        }
    }

    private static void ApplySalvage(SimState state, JumpEvent evt, string fleetId, Random rng)
    {
        int qty = rng.Next(JumpEventTweaksV0.SalvageMinQty, JumpEventTweaksV0.SalvageMaxQty + 1);
        // Give scrap metal from WellKnownGoodIds or first available good.
        string goodId = "Scrap";
        evt.GoodId = goodId;
        evt.Quantity = qty;

        if (state.Fleets.TryGetValue(fleetId, out var fleet))
        {
            fleet.Cargo.TryGetValue(goodId, out var existing);
            fleet.Cargo[goodId] = existing + qty;
        }
    }

    private static void ApplyTurbulence(SimState state, JumpEvent evt, string fleetId, Random rng)
    {
        int damage = rng.Next(JumpEventTweaksV0.TurbulenceMinDamage, JumpEventTweaksV0.TurbulenceMaxDamage + 1);
        evt.HullDamage = damage;

        if (state.Fleets.TryGetValue(fleetId, out var fleet) && fleet.HullHp > 0)
        {
            fleet.HullHp = Math.Max(1, fleet.HullHp - damage); // Never kill from turbulence.
        }
    }

    private static int HashCombine(int a, string s)
    {
        int h = a;
        foreach (char c in s)
            h = h * 31 + c;
        return h;
    }
}
