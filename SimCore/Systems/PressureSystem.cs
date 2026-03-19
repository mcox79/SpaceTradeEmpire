using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.X.PRESSURE.SYSTEM.001: Pressure system — process deltas, enforce one-jump, track budget.
public static class PressureSystem
{
    private sealed class Scratch
    {
        public readonly List<string> DomainIds = new();
    }
    private static readonly ConditionalWeakTable<SimState, Scratch> s_scratch = new();
    /// <summary>
    /// Injects a pressure delta into the specified domain.
    /// </summary>
    public static void InjectDelta(SimState state, string domainId, string reasonCode, int magnitude,
        string targetRef = "", string sourceRef = "")
    {
        if (state?.Pressure == null) return;
        if (string.IsNullOrEmpty(domainId)) return;

        var delta = new PressureDelta
        {
            DomainId = domainId,
            ReasonCode = reasonCode ?? "",
            Magnitude = magnitude,
            TargetRef = targetRef ?? "",
            SourceRef = sourceRef ?? "",
            Tick = state.Tick,
        };

        state.Pressure.DeltaLog.Add(delta);

        // Ensure domain state exists
        if (!state.Pressure.Domains.TryGetValue(domainId, out var domain))
        {
            domain = new PressureDomainState
            {
                DomainId = domainId,
                WindowStartTick = state.Tick,
            };
            state.Pressure.Domains[domainId] = domain;
        }

        // Apply magnitude to accumulated pressure
        domain.AccumulatedPressureBps = Math.Clamp(
            domain.AccumulatedPressureBps + magnitude,
            0,
            PressureTweaksV0.MaxAccumulatedBps);
    }

    /// <summary>
    /// Process pressure per tick: decay, evaluate tier transitions, enforce one-jump rule.
    /// </summary>
    public static void ProcessPressure(SimState state)
    {
        if (state?.Pressure == null) return;

        var scratch = s_scratch.GetOrCreateValue(state);
        var domainIds = scratch.DomainIds;
        domainIds.Clear();
        foreach (var k in state.Pressure.Domains.Keys) domainIds.Add(k);
        domainIds.Sort(StringComparer.Ordinal);

        foreach (var domainId in domainIds)
        {
            var domain = state.Pressure.Domains[domainId];

            // Check window rollover
            if (state.Tick - domain.WindowStartTick >= PressureTweaksV0.EnforcementWindowTicks)
            {
                domain.WindowStartTick = state.Tick;
                domain.AlertCount = 0;
            }

            // Natural decay
            if (domain.AccumulatedPressureBps > 0)
            {
                domain.AccumulatedPressureBps = Math.Max(
                    0,
                    domain.AccumulatedPressureBps - PressureTweaksV0.NaturalDecayBps);
            }

            // Evaluate target tier from accumulated pressure
            var targetTier = EvaluateTier(domain.AccumulatedPressureBps);

            // Enforce max-one-jump rule
            if (targetTier != domain.Tier)
            {
                int jump = (int)targetTier - (int)domain.Tier;
                if (Math.Abs(jump) > PressureTweaksV0.MaxTierJumpPerWindow)
                {
                    // Clamp to one jump
                    targetTier = jump > 0
                        ? (PressureTier)((int)domain.Tier + PressureTweaksV0.MaxTierJumpPerWindow)
                        : (PressureTier)((int)domain.Tier - PressureTweaksV0.MaxTierJumpPerWindow);
                }

                // Check if already jumped this window
                if (domain.LastTransitionTick >= domain.WindowStartTick &&
                    domain.LastTransitionTick < state.Tick)
                {
                    // Already transitioned this window — hold
                    targetTier = domain.Tier;
                }

                if (targetTier != domain.Tier)
                {
                    var oldTier = domain.Tier;
                    domain.Tier = targetTier;
                    domain.LastTransitionTick = state.Tick;

                    state.Pressure.EventLog.Add(new PressureEvent
                    {
                        Seq = state.Pressure.NextEventSeq++,
                        Tick = state.Tick,
                        DomainId = domainId,
                        EventType = "TierChanged",
                        OldTier = oldTier,
                        NewTier = targetTier,
                    });

                    // Count as an alert
                    domain.AlertCount++;
                }
            }

            // Update direction
            domain.Direction = domain.AccumulatedPressureBps > GetTierThreshold(domain.Tier)
                ? PressureDirection.Worsening
                : domain.AccumulatedPressureBps < GetTierLowerBound(domain.Tier)
                    ? PressureDirection.Improving
                    : PressureDirection.Stable;
        }
    }

    public static PressureTier EvaluateTier(int accumulatedBps)
    {
        if (accumulatedBps >= PressureTweaksV0.CollapsedThresholdBps) return PressureTier.Collapsed;
        if (accumulatedBps >= PressureTweaksV0.CriticalThresholdBps) return PressureTier.Critical;
        if (accumulatedBps >= PressureTweaksV0.UnstableThresholdBps) return PressureTier.Unstable;
        if (accumulatedBps >= PressureTweaksV0.StrainedThresholdBps) return PressureTier.Strained;
        return PressureTier.Normal;
    }

    private static int GetTierThreshold(PressureTier tier)
    {
        return tier switch
        {
            PressureTier.Normal => PressureTweaksV0.StrainedThresholdBps,
            PressureTier.Strained => PressureTweaksV0.UnstableThresholdBps,
            PressureTier.Unstable => PressureTweaksV0.CriticalThresholdBps,
            PressureTier.Critical => PressureTweaksV0.CollapsedThresholdBps,
            _ => PressureTweaksV0.MaxAccumulatedBps,
        };
    }

    private static int GetTierLowerBound(PressureTier tier)
    {
        return tier switch
        {
            PressureTier.Strained => 0,
            PressureTier.Unstable => PressureTweaksV0.StrainedThresholdBps,
            PressureTier.Critical => PressureTweaksV0.UnstableThresholdBps,
            PressureTier.Collapsed => PressureTweaksV0.CriticalThresholdBps,
            _ => 0,
        };
    }

    /// <summary>
    /// Check if domain is in crisis (Critical or above).
    /// </summary>
    public static bool IsCrisis(PressureDomainState domain)
    {
        return (int)domain.Tier >= PressureTweaksV0.CrisisTierMin;
    }

    /// <summary>
    /// Get max alerts allowed for current domain state.
    /// </summary>
    public static int GetMaxAlerts(PressureDomainState domain)
    {
        return IsCrisis(domain)
            ? PressureTweaksV0.MaxAlertsPerWindowCrisis
            : PressureTweaksV0.MaxAlertsPerWindowNormal;
    }

    // GATE.X.PRESSURE.ENFORCE.001: Apply consequences based on current pressure tiers.
    public static void EnforceConsequences(SimState state)
    {
        if (state?.Pressure == null) return;

        // Snapshot domain keys to avoid collection-modified-during-enumeration
        // (InjectDelta may add new domains like "piracy").
        var scratch2 = s_scratch.GetOrCreateValue(state);
        var domainIds = scratch2.DomainIds;
        domainIds.Clear();
        foreach (var k in state.Pressure.Domains.Keys) domainIds.Add(k);
        domainIds.Sort(StringComparer.Ordinal);

        foreach (var domainId in domainIds)
        {
            var domain = state.Pressure.Domains[domainId];

            // Crisis: increase market fees
            if (domain.Tier == PressureTier.Critical)
            {
                // Apply fee increase as a per-domain effect
                // Market fee is global — track via event rather than mutating global state each tick
                if (domain.LastConsequenceTick != state.Tick)
                {
                    state.Pressure.EventLog.Add(new PressureEvent
                    {
                        Seq = state.Pressure.NextEventSeq++,
                        Tick = state.Tick,
                        DomainId = domain.DomainId,
                        EventType = "CrisisFeeSurcharge",
                        OldTier = domain.Tier,
                        NewTier = domain.Tier,
                    });
                    domain.LastConsequenceTick = state.Tick;
                }
            }
            // Collapse: trigger piracy escalation
            else if (domain.Tier == PressureTier.Collapsed)
            {
                if (domain.LastConsequenceTick != state.Tick)
                {
                    // Inject piracy pressure into the piracy domain
                    InjectDelta(state, "piracy", "collapse_escalation",
                        PressureTweaksV0.CollapsePiracyEscalationMagnitude,
                        sourceRef: domain.DomainId);

                    state.Pressure.EventLog.Add(new PressureEvent
                    {
                        Seq = state.Pressure.NextEventSeq++,
                        Tick = state.Tick,
                        DomainId = domain.DomainId,
                        EventType = "CollapseEscalation",
                        OldTier = domain.Tier,
                        NewTier = domain.Tier,
                    });
                    domain.LastConsequenceTick = state.Tick;
                }
            }
        }
    }
}
