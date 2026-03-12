using NUnit.Framework;
using SimCore;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;

namespace SimCore.Tests.Systems;

// GATE.T18.NARRATIVE.FO_SYSTEM.001
[TestFixture]
public sealed class FirstOfficerTests
{
    private static void AdvanceTo(SimState state, int targetTick)
    {
        while (state.Tick < targetTick)
            state.AdvanceTick();
    }

    [Test]
    public void PromoteCandidate_CreatesFirstOfficer()
    {
        var state = new SimState(42);
        bool ok = FirstOfficerSystem.PromoteCandidate(state, FirstOfficerCandidate.Analyst);

        Assert.That(ok, Is.True);
        Assert.That(state.FirstOfficer, Is.Not.Null);
        Assert.That(state.FirstOfficer!.IsPromoted, Is.True);
        Assert.That(state.FirstOfficer.CandidateType, Is.EqualTo(FirstOfficerCandidate.Analyst));
        Assert.That(state.FirstOfficer.Tier, Is.EqualTo(DialogueTier.Early));
    }

    [Test]
    public void PromoteCandidate_FailsIfAlreadyPromoted()
    {
        var state = new SimState(42);
        FirstOfficerSystem.PromoteCandidate(state, FirstOfficerCandidate.Analyst);
        bool second = FirstOfficerSystem.PromoteCandidate(state, FirstOfficerCandidate.Veteran);

        Assert.That(second, Is.False);
        Assert.That(state.FirstOfficer!.CandidateType, Is.EqualTo(FirstOfficerCandidate.Analyst));
    }

    [Test]
    public void PromoteCandidate_NoneIsRejected()
    {
        var state = new SimState(42);
        bool ok = FirstOfficerSystem.PromoteCandidate(state, FirstOfficerCandidate.None);
        Assert.That(ok, Is.False);
        Assert.That(state.FirstOfficer, Is.Null);
    }

    [Test]
    public void TierProgression_AdvancesAtThresholds()
    {
        var state = new SimState(42);
        FirstOfficerSystem.PromoteCandidate(state, FirstOfficerCandidate.Veteran);

        // Tick 0 → Early
        FirstOfficerSystem.Process(state);
        Assert.That(state.FirstOfficer!.Tier, Is.EqualTo(DialogueTier.Early));

        // Advance to Mid threshold
        AdvanceTo(state, NarrativeTweaksV0.TierMidTick);
        FirstOfficerSystem.Process(state);
        Assert.That(state.FirstOfficer.Tier, Is.EqualTo(DialogueTier.Mid));

        // Advance to Fracture
        AdvanceTo(state, NarrativeTweaksV0.TierFractureTick);
        FirstOfficerSystem.Process(state);
        Assert.That(state.FirstOfficer.Tier, Is.EqualTo(DialogueTier.Fracture));

        // Advance to Revelation
        AdvanceTo(state, NarrativeTweaksV0.TierRevelationTick);
        FirstOfficerSystem.Process(state);
        Assert.That(state.FirstOfficer.Tier, Is.EqualTo(DialogueTier.Revelation));

        // Advance to Endgame
        AdvanceTo(state, NarrativeTweaksV0.TierEndgameTick);
        FirstOfficerSystem.Process(state);
        Assert.That(state.FirstOfficer.Tier, Is.EqualTo(DialogueTier.Endgame));
    }

    [Test]
    public void TierNeverRegresses()
    {
        var state = new SimState(42);
        FirstOfficerSystem.PromoteCandidate(state, FirstOfficerCandidate.Pathfinder);

        AdvanceTo(state, NarrativeTweaksV0.TierFractureTick);
        FirstOfficerSystem.Process(state);
        Assert.That(state.FirstOfficer!.Tier, Is.EqualTo(DialogueTier.Fracture));

        // Tier should not regress even if we don't advance further
        // (Process called again at same tick should not lower tier)
        FirstOfficerSystem.Process(state);
        Assert.That(state.FirstOfficer.Tier, Is.EqualTo(DialogueTier.Fracture));
    }

    [Test]
    public void TryFireTrigger_FiresOnceAndLogs()
    {
        var state = new SimState(42);
        FirstOfficerSystem.PromoteCandidate(state, FirstOfficerCandidate.Analyst);

        string line1 = FirstOfficerSystem.TryFireTrigger(state, "FIRST_TRADE_LOSS");
        Assert.That(line1, Is.Not.Empty);
        Assert.That(state.FirstOfficer!.DialogueEventLog, Has.Count.EqualTo(1));

        // Second fire of same trigger returns empty
        string line2 = FirstOfficerSystem.TryFireTrigger(state, "FIRST_TRADE_LOSS");
        Assert.That(line2, Is.Empty);
        Assert.That(state.FirstOfficer.DialogueEventLog, Has.Count.EqualTo(1));
    }

    [Test]
    public void TryFireTrigger_RespectsMinTier()
    {
        var state = new SimState(42);
        FirstOfficerSystem.PromoteCandidate(state, FirstOfficerCandidate.Analyst);

        // FIRST_WAR_GOODS_SALE requires Mid tier — should fail at Early
        string line = FirstOfficerSystem.TryFireTrigger(state, "FIRST_WAR_GOODS_SALE");
        Assert.That(line, Is.Empty);

        // Advance to Mid and try again
        AdvanceTo(state, NarrativeTweaksV0.TierMidTick);
        FirstOfficerSystem.Process(state);
        line = FirstOfficerSystem.TryFireTrigger(state, "FIRST_WAR_GOODS_SALE");
        Assert.That(line, Is.Not.Empty);
    }

    [Test]
    public void TryFireTrigger_UpdatesRelationshipScore()
    {
        var state = new SimState(42);
        FirstOfficerSystem.PromoteCandidate(state, FirstOfficerCandidate.Analyst);

        int scoreBefore = state.FirstOfficer!.RelationshipScore;
        FirstOfficerSystem.TryFireTrigger(state, "FIRST_PROFITABLE_TRADE");
        int scoreAfter = state.FirstOfficer.RelationshipScore;

        // Should have changed (specific delta depends on content)
        Assert.That(scoreAfter, Is.Not.EqualTo(scoreBefore));
    }

    [Test]
    public void BlindSpotExposed_SetsByTrigger()
    {
        var state = new SimState(42);
        FirstOfficerSystem.PromoteCandidate(state, FirstOfficerCandidate.Veteran);

        Assert.That(state.FirstOfficer!.BlindSpotExposed, Is.False);

        // Need Revelation tier for BLINDSPOT_EXPOSED
        AdvanceTo(state, NarrativeTweaksV0.TierRevelationTick);
        FirstOfficerSystem.Process(state);

        FirstOfficerSystem.TryFireTrigger(state, "BLINDSPOT_EXPOSED");
        Assert.That(state.FirstOfficer.BlindSpotExposed, Is.True);
    }

    [Test]
    public void ConsumePendingDialogue_ClearsAfterRead()
    {
        var state = new SimState(42);
        FirstOfficerSystem.PromoteCandidate(state, FirstOfficerCandidate.Pathfinder);

        FirstOfficerSystem.TryFireTrigger(state, "FIRST_TRADE_LOSS");
        Assert.That(state.FirstOfficer!.PendingDialogueLine, Is.Not.Empty);

        string consumed = FirstOfficerSystem.ConsumePendingDialogue(state);
        Assert.That(consumed, Is.Not.Empty);
        Assert.That(state.FirstOfficer.PendingDialogueLine, Is.Empty);
    }

    [Test]
    public void IsInPromotionWindow_CorrectRange()
    {
        var state = new SimState(42);

        // Before window
        Assert.That(FirstOfficerSystem.IsInPromotionWindow(state), Is.False);

        // At window start
        AdvanceTo(state, NarrativeTweaksV0.FOPromotionMinTick);
        Assert.That(FirstOfficerSystem.IsInPromotionWindow(state), Is.True);

        // At window end
        AdvanceTo(state, NarrativeTweaksV0.FOPromotionMaxTick);
        Assert.That(FirstOfficerSystem.IsInPromotionWindow(state), Is.True);

        // Past window
        AdvanceTo(state, NarrativeTweaksV0.FOPromotionMaxTick + 1);
        Assert.That(FirstOfficerSystem.IsInPromotionWindow(state), Is.False);
    }

    [Test]
    public void IsInPromotionWindow_FalseAfterPromotion()
    {
        var state = new SimState(42);
        AdvanceTo(state, NarrativeTweaksV0.FOPromotionMinTick);
        FirstOfficerSystem.PromoteCandidate(state, FirstOfficerCandidate.Analyst);
        Assert.That(FirstOfficerSystem.IsInPromotionWindow(state), Is.False);
    }

    [Test]
    public void Process_NoOpWithoutPromotion()
    {
        var state = new SimState(42);
        AdvanceTo(state, NarrativeTweaksV0.TierEndgameTick);
        FirstOfficerSystem.Process(state);
        Assert.That(state.FirstOfficer, Is.Null);
    }

    [Test]
    public void AllThreeCandidates_HaveContent()
    {
        var candidates = FirstOfficerContentV0.Candidates;
        Assert.That(candidates, Has.Count.EqualTo(3));

        foreach (var c in candidates)
        {
            Assert.That(c.Name, Is.Not.Empty, $"Candidate {c.Type} has no name");
            Assert.That(c.Description, Is.Not.Empty, $"Candidate {c.Type} has no description");
            Assert.That(c.BlindSpot, Is.Not.Empty, $"Candidate {c.Type} has no blind spot");
            Assert.That(c.EndgameLean, Is.Not.Empty, $"Candidate {c.Type} has no endgame lean");
        }
    }

    [Test]
    public void EachCandidate_HasEarlyTierLines()
    {
        foreach (var candidate in new[] { FirstOfficerCandidate.Analyst, FirstOfficerCandidate.Veteran, FirstOfficerCandidate.Pathfinder })
        {
            var line = FirstOfficerContentV0.GetLine("FIRST_TRADE_LOSS", candidate);
            Assert.That(line, Is.Not.Null, $"No FIRST_TRADE_LOSS line for {candidate}");
            Assert.That(line!.Text, Is.Not.Empty, $"Empty text for {candidate}");
        }
    }

    [Test]
    public void TryFireTrigger_NonexistentTrigger_ReturnsEmpty()
    {
        var state = new SimState(42);
        FirstOfficerSystem.PromoteCandidate(state, FirstOfficerCandidate.Analyst);

        string line = FirstOfficerSystem.TryFireTrigger(state, "TOTALLY_FAKE_TRIGGER");
        Assert.That(line, Is.Empty);
        Assert.That(state.FirstOfficer!.DialogueEventLog, Has.Count.EqualTo(0));
    }

    [Test]
    public void TryFireTrigger_NoPromotion_ReturnsEmpty()
    {
        var state = new SimState(42);
        string line = FirstOfficerSystem.TryFireTrigger(state, "FIRST_TRADE_LOSS");
        Assert.That(line, Is.Empty);
    }

    [Test]
    public void ConsumePendingDialogue_WhenNothingPending_ReturnsEmpty()
    {
        var state = new SimState(42);
        FirstOfficerSystem.PromoteCandidate(state, FirstOfficerCandidate.Analyst);

        string consumed = FirstOfficerSystem.ConsumePendingDialogue(state);
        Assert.That(consumed, Is.Empty);
    }

    [Test]
    public void ConsumePendingDialogue_WhenNoFO_ReturnsEmpty()
    {
        var state = new SimState(42);
        string consumed = FirstOfficerSystem.ConsumePendingDialogue(state);
        Assert.That(consumed, Is.Empty);
    }

    [Test]
    public void LatePromotion_TierJumpsDirectly()
    {
        var state = new SimState(42);
        AdvanceTo(state, NarrativeTweaksV0.TierFractureTick + 1);
        FirstOfficerSystem.PromoteCandidate(state, FirstOfficerCandidate.Pathfinder);

        // Tier starts at Early even at late tick
        Assert.That(state.FirstOfficer!.Tier, Is.EqualTo(DialogueTier.Early));

        // But Process should jump directly to Fracture
        FirstOfficerSystem.Process(state);
        Assert.That(state.FirstOfficer.Tier, Is.EqualTo(DialogueTier.Fracture));
    }

    [Test]
    public void TryFireTrigger_FailedTrigger_DoesNotMutateState()
    {
        var state = new SimState(42);
        FirstOfficerSystem.PromoteCandidate(state, FirstOfficerCandidate.Analyst);

        // FIRST_WAR_GOODS_SALE requires Mid tier — should fail at Early
        int scoreBefore = state.FirstOfficer!.RelationshipScore;
        string pending = state.FirstOfficer.PendingDialogueLine;

        FirstOfficerSystem.TryFireTrigger(state, "FIRST_WAR_GOODS_SALE");

        Assert.That(state.FirstOfficer.RelationshipScore, Is.EqualTo(scoreBefore));
        Assert.That(state.FirstOfficer.PendingDialogueLine, Is.EqualTo(pending));
        Assert.That(state.FirstOfficer.DialogueEventLog, Has.Count.EqualTo(0));
    }
}
