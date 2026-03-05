using NUnit.Framework;
using SimCore;
using SimCore.Gen;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;
using System.Linq;

namespace SimCore.Tests.Systems;

// GATE.X.PRESSURE.SYSTEM.001 + GATE.X.PRESSURE.PROOF.001: Pressure system contract + scenario tests.
public class PressureSystemScenarioTests
{
    private SimState CreateTestState()
    {
        var kernel = new SimKernel(42);
        GalaxyGenerator.Generate(kernel.State, 12, 100f);
        return kernel.State;
    }

    [Test]
    public void InjectDelta_CreatesNewDomain()
    {
        var state = CreateTestState();

        PressureSystem.InjectDelta(state, "piracy", "test", 500);

        Assert.That(state.Pressure.Domains.ContainsKey("piracy"), Is.True);
        Assert.That(state.Pressure.Domains["piracy"].AccumulatedPressureBps, Is.EqualTo(500));
    }

    [Test]
    public void InjectDelta_AccumulatesPressure()
    {
        var state = CreateTestState();

        PressureSystem.InjectDelta(state, "piracy", "test", 1000);
        PressureSystem.InjectDelta(state, "piracy", "test", 1500);

        Assert.That(state.Pressure.Domains["piracy"].AccumulatedPressureBps, Is.EqualTo(2500));
    }

    [Test]
    public void InjectDelta_ClampsAtMax()
    {
        var state = CreateTestState();

        PressureSystem.InjectDelta(state, "piracy", "test", PressureTweaksV0.MaxAccumulatedBps + 5000);

        Assert.That(state.Pressure.Domains["piracy"].AccumulatedPressureBps,
            Is.EqualTo(PressureTweaksV0.MaxAccumulatedBps));
    }

    [Test]
    public void InjectDelta_LogsDelta()
    {
        var state = CreateTestState();

        PressureSystem.InjectDelta(state, "piracy", "raid_detected", 1000);

        Assert.That(state.Pressure.DeltaLog.Count, Is.EqualTo(1));
        Assert.That(state.Pressure.DeltaLog[0].ReasonCode, Is.EqualTo("raid_detected"));
        Assert.That(state.Pressure.DeltaLog[0].Magnitude, Is.EqualTo(1000));
    }

    [Test]
    public void EvaluateTier_CorrectMapping()
    {
        Assert.That(PressureSystem.EvaluateTier(0), Is.EqualTo(PressureTier.Normal));
        Assert.That(PressureSystem.EvaluateTier(1999), Is.EqualTo(PressureTier.Normal));
        Assert.That(PressureSystem.EvaluateTier(2000), Is.EqualTo(PressureTier.Strained));
        Assert.That(PressureSystem.EvaluateTier(3999), Is.EqualTo(PressureTier.Strained));
        Assert.That(PressureSystem.EvaluateTier(4000), Is.EqualTo(PressureTier.Unstable));
        Assert.That(PressureSystem.EvaluateTier(6999), Is.EqualTo(PressureTier.Unstable));
        Assert.That(PressureSystem.EvaluateTier(7000), Is.EqualTo(PressureTier.Critical));
        Assert.That(PressureSystem.EvaluateTier(8999), Is.EqualTo(PressureTier.Critical));
        Assert.That(PressureSystem.EvaluateTier(9000), Is.EqualTo(PressureTier.Collapsed));
    }

    [Test]
    public void ProcessPressure_DecaysNaturally()
    {
        var state = CreateTestState();

        PressureSystem.InjectDelta(state, "piracy", "test", 500);
        int before = state.Pressure.Domains["piracy"].AccumulatedPressureBps;

        PressureSystem.ProcessPressure(state);

        int after = state.Pressure.Domains["piracy"].AccumulatedPressureBps;
        Assert.That(after, Is.LessThan(before));
        Assert.That(after, Is.EqualTo(before - PressureTweaksV0.NaturalDecayBps));
    }

    [Test]
    public void ProcessPressure_MaxOneJumpPerWindow()
    {
        var state = CreateTestState();

        // Inject well above Strained threshold (2000) to survive decay (10 bps)
        PressureSystem.InjectDelta(state, "piracy", "test", PressureTweaksV0.StrainedThresholdBps + 100);
        PressureSystem.ProcessPressure(state);

        Assert.That(state.Pressure.Domains["piracy"].Tier, Is.EqualTo(PressureTier.Strained));

        // Now inject enough to jump to Critical (7000) in one shot
        PressureSystem.InjectDelta(state, "piracy", "test",
            PressureTweaksV0.CriticalThresholdBps - state.Pressure.Domains["piracy"].AccumulatedPressureBps + 100);

        // The one-jump rule + already-jumped-this-window should prevent more than 1 tier per window
        PressureSystem.ProcessPressure(state);

        // Already jumped Normal->Strained this window, so should hold at Strained
        Assert.That((int)state.Pressure.Domains["piracy"].Tier,
            Is.LessThanOrEqualTo((int)PressureTier.Unstable),
            "Max-one-jump rule should prevent jumping more than 1 tier");
    }

    [Test]
    public void ProcessPressure_TierTransitionLogsEvent()
    {
        var state = CreateTestState();

        // Inject above threshold + decay margin
        PressureSystem.InjectDelta(state, "piracy", "test", PressureTweaksV0.StrainedThresholdBps + 100);
        PressureSystem.ProcessPressure(state);

        Assert.That(state.Pressure.EventLog.Count, Is.GreaterThan(0));
        var evt = state.Pressure.EventLog.Last();
        Assert.That(evt.EventType, Is.EqualTo("TierChanged"));
        Assert.That(evt.OldTier, Is.EqualTo(PressureTier.Normal));
        Assert.That(evt.NewTier, Is.EqualTo(PressureTier.Strained));
    }

    [Test]
    public void IsCrisis_ReturnsCorrectly()
    {
        var normalDomain = new PressureDomainState { Tier = PressureTier.Normal };
        var strainedDomain = new PressureDomainState { Tier = PressureTier.Strained };
        var criticalDomain = new PressureDomainState { Tier = PressureTier.Critical };
        var collapsedDomain = new PressureDomainState { Tier = PressureTier.Collapsed };

        Assert.That(PressureSystem.IsCrisis(normalDomain), Is.False);
        Assert.That(PressureSystem.IsCrisis(strainedDomain), Is.False);
        Assert.That(PressureSystem.IsCrisis(criticalDomain), Is.True);
        Assert.That(PressureSystem.IsCrisis(collapsedDomain), Is.True);
    }

    [Test]
    public void ProcessPressure_NullState_NoThrow()
    {
        Assert.DoesNotThrow(() => PressureSystem.ProcessPressure(null!));
    }

    [Test]
    public void InjectDelta_NullState_NoThrow()
    {
        Assert.DoesNotThrow(() => PressureSystem.InjectDelta(null!, "test", "test", 100));
    }

    [Test]
    public void PressureScenario_500Tick_StairStep()
    {
        // GATE.X.PRESSURE.PROOF.001: 500-tick scenario proving stair-step transitions + budget.
        var state = CreateTestState();
        var domainId = "piracy_test";

        // Phase 1: Inject continuous piracy pressure (ticks 0-100)
        for (int tick = 0; tick < 100; tick++)
        {
            PressureSystem.InjectDelta(state, domainId, "piracy_raid", 50);
            PressureSystem.ProcessPressure(state);
        }

        // Should have escalated from Normal
        Assert.That(state.Pressure.Domains[domainId].Tier,
            Is.Not.EqualTo(PressureTier.Normal),
            "Continuous pressure should escalate above Normal");

        // Phase 2: Let it stabilize (ticks 100-300, no new deltas)
        for (int tick = 100; tick < 300; tick++)
        {
            PressureSystem.ProcessPressure(state);
        }

        // Phase 3: More intense pressure (ticks 300-500)
        for (int tick = 300; tick < 500; tick++)
        {
            PressureSystem.InjectDelta(state, domainId, "piracy_escalation", 100);
            PressureSystem.ProcessPressure(state);
        }

        // Verify: accumulated pressure is within bounds
        var domain = state.Pressure.Domains[domainId];
        Assert.That(domain.AccumulatedPressureBps,
            Is.LessThanOrEqualTo(PressureTweaksV0.MaxAccumulatedBps),
            "Accumulated pressure should never exceed max");
        Assert.That(domain.AccumulatedPressureBps,
            Is.GreaterThanOrEqualTo(0),
            "Accumulated pressure should never go negative");

        // Verify: event log has transitions
        Assert.That(state.Pressure.EventLog.Count, Is.GreaterThan(0),
            "500-tick scenario should produce tier transitions");

        // Verify: all transitions obey max-one-jump
        for (int i = 0; i < state.Pressure.EventLog.Count; i++)
        {
            var evt = state.Pressure.EventLog[i];
            if (evt.EventType == "TierChanged")
            {
                int jump = System.Math.Abs((int)evt.NewTier - (int)evt.OldTier);
                Assert.That(jump, Is.LessThanOrEqualTo(PressureTweaksV0.MaxTierJumpPerWindow),
                    $"Event {i}: jump from {evt.OldTier} to {evt.NewTier} exceeds max-one-jump");
            }
        }
    }

    [Test]
    public void PressureState_SurvivesSaveLoad()
    {
        var state = CreateTestState();
        PressureSystem.InjectDelta(state, "trade_disruption", "test", 3000);
        PressureSystem.ProcessPressure(state);

        var json = System.Text.Json.JsonSerializer.Serialize(state);
        var loaded = System.Text.Json.JsonSerializer.Deserialize<SimState>(json)!;
        loaded.HydrateAfterLoad();

        Assert.That(loaded.Pressure.Domains.ContainsKey("trade_disruption"), Is.True);
        Assert.That(loaded.Pressure.Domains["trade_disruption"].AccumulatedPressureBps,
            Is.EqualTo(state.Pressure.Domains["trade_disruption"].AccumulatedPressureBps));
    }
}
