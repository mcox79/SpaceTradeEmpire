using NUnit.Framework;
using SimCore.Entities;
using SimCore.Systems;

namespace SimCore.Tests.Systems;

// GATE.S7.AUTOMATION_MGMT: Contract tests for automation management systems.
[TestFixture]
[Category("AutomationSystem")]
public sealed class AutomationSystemTests
{
    private Fleet CreateFleet(int hullHp = 100, int hullHpMax = 100)
    {
        return new Fleet
        {
            Id = "fleet_test_1",
            OwnerId = "player",
            HullHp = hullHp,
            HullHpMax = hullHpMax,
        };
    }

    // --- GATE 1: DoctrineSystem ---

    [Test]
    public void DoctrineSystem_DefaultStance_IsDefensive()
    {
        var fleet = CreateFleet();
        var stance = DoctrineSystem.GetEffectiveStance(fleet);
        Assert.That(stance, Is.EqualTo(EngagementStance.Defensive));
    }

    [Test]
    public void DoctrineSystem_RetreatThreshold_TriggersAtLowHull()
    {
        var fleet = CreateFleet(hullHp: 20, hullHpMax: 100);
        // Default threshold is 25%, hull is at 20% => should retreat.
        Assert.That(DoctrineSystem.EvaluateRetreat(fleet), Is.True);
    }

    [Test]
    public void DoctrineSystem_RetreatThreshold_DoesNotTriggerAboveThreshold()
    {
        var fleet = CreateFleet(hullHp: 80, hullHpMax: 100);
        // Default threshold is 25%, hull is at 80% => should NOT retreat.
        Assert.That(DoctrineSystem.EvaluateRetreat(fleet), Is.False);
    }

    [Test]
    public void DoctrineSystem_CustomStance_ReturnsCorrectly()
    {
        var fleet = CreateFleet();
        fleet.Doctrine.Stance = EngagementStance.Aggressive;
        Assert.That(DoctrineSystem.GetEffectiveStance(fleet), Is.EqualTo(EngagementStance.Aggressive));
    }

    // --- GATE 2: ProgramMetricsSystem ---

    [Test]
    public void ProgramMetrics_RecordSuccess_IncrementsCounters()
    {
        var fleet = CreateFleet();
        ProgramMetricsSystem.RecordCycleSuccess(fleet, goodsMoved: 10, credits: 500, tick: 5);

        Assert.That(fleet.Metrics.CyclesRun, Is.EqualTo(1));
        Assert.That(fleet.Metrics.GoodsMoved, Is.EqualTo(10));
        Assert.That(fleet.Metrics.CreditsEarned, Is.EqualTo(500));
        Assert.That(fleet.Metrics.LastActiveTick, Is.EqualTo(5));
    }

    [Test]
    public void ProgramMetrics_RecordFailure_IncrementsFailures()
    {
        var fleet = CreateFleet();
        ProgramMetricsSystem.RecordCycleFailure(fleet, tick: 3);
        ProgramMetricsSystem.RecordCycleFailure(fleet, tick: 4);

        Assert.That(fleet.Metrics.Failures, Is.EqualTo(2));
        Assert.That(fleet.Metrics.LastActiveTick, Is.EqualTo(4));
    }

    [Test]
    public void ProgramMetrics_SuccessResetsConsecutiveFailures()
    {
        var fleet = CreateFleet();
        fleet.Metrics.ConsecutiveFailures = 2;
        fleet.Metrics.LastFailureReason = ProgramFailureReason.NoRoute;

        ProgramMetricsSystem.RecordCycleSuccess(fleet, goodsMoved: 5, credits: 100, tick: 10);

        Assert.That(fleet.Metrics.ConsecutiveFailures, Is.EqualTo(0));
        Assert.That(fleet.Metrics.LastFailureReason, Is.EqualTo(ProgramFailureReason.None));
    }

    // --- GATE 3: FailureRecoverySystem ---

    [Test]
    public void FailureRecovery_RecordFailure_StoresReason()
    {
        var fleet = CreateFleet();
        FailureRecoverySystem.RecordFailure(fleet, ProgramFailureReason.NoRoute, tick: 5);

        Assert.That(FailureRecoverySystem.GetLastFailureReason(fleet), Is.EqualTo(ProgramFailureReason.NoRoute));
        Assert.That(fleet.Metrics.ConsecutiveFailures, Is.EqualTo(1));
    }

    [Test]
    public void FailureRecovery_ThreeConsecutiveFailures_StopsRetry()
    {
        var fleet = CreateFleet();

        // First two failures: retry still allowed.
        FailureRecoverySystem.RecordFailure(fleet, ProgramFailureReason.InsufficientFunds, tick: 1);
        Assert.That(FailureRecoverySystem.ShouldRetry(fleet), Is.True);

        FailureRecoverySystem.RecordFailure(fleet, ProgramFailureReason.InsufficientFunds, tick: 2);
        Assert.That(FailureRecoverySystem.ShouldRetry(fleet), Is.True);

        // Third failure: retry should be denied.
        FailureRecoverySystem.RecordFailure(fleet, ProgramFailureReason.InsufficientFunds, tick: 3);
        Assert.That(FailureRecoverySystem.ShouldRetry(fleet), Is.False);
    }

    [Test]
    public void FailureRecovery_SuccessResetsFailureCount()
    {
        var fleet = CreateFleet();

        // Accumulate 2 failures.
        FailureRecoverySystem.RecordFailure(fleet, ProgramFailureReason.Timeout, tick: 1);
        FailureRecoverySystem.RecordFailure(fleet, ProgramFailureReason.Timeout, tick: 2);
        Assert.That(fleet.Metrics.ConsecutiveFailures, Is.EqualTo(2));

        // Success resets consecutive failures.
        ProgramMetricsSystem.RecordCycleSuccess(fleet, goodsMoved: 1, credits: 10, tick: 3);
        Assert.That(fleet.Metrics.ConsecutiveFailures, Is.EqualTo(0));
        Assert.That(FailureRecoverySystem.ShouldRetry(fleet), Is.True);
    }

    // --- GATE 4: BudgetEnforcementSystem ---

    [Test]
    public void BudgetEnforcement_WithinBudget_Allows()
    {
        var fleet = CreateFleet();
        BudgetEnforcementSystem.SetBudget(fleet, creditCap: 1000, goodsCap: 50);

        // No spending yet, so a 500 credit / 20 goods operation should be allowed.
        Assert.That(BudgetEnforcementSystem.CheckBudget(fleet, creditCost: 500, goodsCost: 20), Is.True);
    }

    [Test]
    public void BudgetEnforcement_OverBudget_Denies()
    {
        var fleet = CreateFleet();
        BudgetEnforcementSystem.SetBudget(fleet, creditCap: 1000, goodsCap: 50);

        // Simulate some spending.
        fleet.Metrics.SpentCreditsThisCycle = 800;
        fleet.Metrics.SpentGoodsThisCycle = 40;

        // 300 credits would push over the 1000 cap.
        Assert.That(BudgetEnforcementSystem.CheckBudget(fleet, creditCost: 300, goodsCost: 5), Is.False);
    }

    [Test]
    public void BudgetEnforcement_UnlimitedCap_AlwaysAllows()
    {
        var fleet = CreateFleet();
        // Cap of 0 = unlimited.
        BudgetEnforcementSystem.SetBudget(fleet, creditCap: 0, goodsCap: 0);

        fleet.Metrics.SpentCreditsThisCycle = 999999;
        fleet.Metrics.SpentGoodsThisCycle = 999999;

        Assert.That(BudgetEnforcementSystem.CheckBudget(fleet, creditCost: 1000, goodsCost: 100), Is.True);
    }

    [Test]
    public void BudgetEnforcement_GetRemainingBudget_CalculatesCorrectly()
    {
        var fleet = CreateFleet();
        BudgetEnforcementSystem.SetBudget(fleet, creditCap: 1000, goodsCap: 50);
        fleet.Metrics.SpentCreditsThisCycle = 300;
        fleet.Metrics.SpentGoodsThisCycle = 10;

        var (credits, goods) = BudgetEnforcementSystem.GetRemainingBudget(fleet);
        Assert.That(credits, Is.EqualTo(700));
        Assert.That(goods, Is.EqualTo(40));
    }

    // --- GATE 5: ProgramHistorySystem ---

    [Test]
    public void ProgramHistory_RecordOutcome_AddsEntry()
    {
        var fleet = CreateFleet();
        var entry = new ProgramHistoryEntry
        {
            Tick = 10,
            Success = true,
            GoodsMoved = 5,
            CreditsEarned = 200,
        };

        ProgramHistorySystem.RecordOutcome(fleet, entry);

        var history = ProgramHistorySystem.GetHistory(fleet);
        Assert.That(history.Count, Is.EqualTo(1));
        Assert.That(history[0].Tick, Is.EqualTo(10));
        Assert.That(history[0].Success, Is.True);
    }

    [Test]
    public void ProgramHistory_RingBuffer_CapsAt20()
    {
        var fleet = CreateFleet();

        // Add 25 entries.
        for (int i = 0; i < 25; i++)
        {
            ProgramHistorySystem.RecordOutcome(fleet, new ProgramHistoryEntry
            {
                Tick = i,
                Success = true,
                GoodsMoved = 1,
                CreditsEarned = 10,
            });
        }

        var history = ProgramHistorySystem.GetHistory(fleet);
        Assert.That(history.Count, Is.EqualTo(ProgramHistorySystem.MaxEntries));
        // Newest first: most recent tick should be 24.
        Assert.That(history[0].Tick, Is.EqualTo(24));
        // Oldest surviving entry should be tick 5 (ticks 0-4 were evicted).
        Assert.That(history[history.Count - 1].Tick, Is.EqualTo(5));
    }

    [Test]
    public void ProgramHistory_SuccessRate_CalculatesCorrectly()
    {
        var fleet = CreateFleet();

        // 3 successes, 2 failures => rate = 3/5 = 0.6
        for (int i = 0; i < 3; i++)
        {
            ProgramHistorySystem.RecordOutcome(fleet, new ProgramHistoryEntry
            {
                Tick = i, Success = true, GoodsMoved = 1, CreditsEarned = 10,
            });
        }
        for (int i = 0; i < 2; i++)
        {
            ProgramHistorySystem.RecordOutcome(fleet, new ProgramHistoryEntry
            {
                Tick = 10 + i, Success = false, FailureReason = ProgramFailureReason.NoRoute,
            });
        }

        float rate = ProgramHistorySystem.GetSuccessRate(fleet);
        Assert.That(rate, Is.EqualTo(0.6f).Within(0.001f));
    }

    [Test]
    public void ProgramHistory_EmptyHistory_ReturnsZeroRate()
    {
        var fleet = CreateFleet();
        Assert.That(ProgramHistorySystem.GetSuccessRate(fleet), Is.EqualTo(0f));
    }
}
