using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;

namespace SimCore.Tests.Systems;

[TestFixture]
[Category("HavenResearchLabSystem")]
public sealed class HavenResearchLabSystemTests
{
    private static SimState CreateState(int seed = 42)
    {
        var state = new SimState(seed);
        state.Haven.Discovered = true;
        state.Haven.Tier = HavenTier.Operational; // 2 slots
        state.PlayerCredits = 10000;
        return state;
    }

    [Test]
    public void UndiscoveredHaven_IsNoOp()
    {
        var state = new SimState(42);
        state.Haven.Discovered = false;
        state.Haven.Tier = HavenTier.Operational;

        HavenResearchLabSystem.Process(state);

        Assert.That(state.Haven.ResearchLabSlots, Is.Empty);
    }

    [Test]
    public void StartResearch_EmptyTech_Fails()
    {
        var state = CreateState();

        var result = HavenResearchLabSystem.StartSlotResearch(state, "", 0);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Reason, Is.EqualTo("empty_tech_id"));
    }

    [Test]
    public void StartResearch_UnknownTech_Fails()
    {
        var state = CreateState();

        var result = HavenResearchLabSystem.StartSlotResearch(state, "nonexistent_tech_xyz", 0);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Reason, Is.EqualTo("unknown_tech"));
    }

    [Test]
    public void StartResearch_InvalidSlotIndex_Fails()
    {
        var state = CreateState();
        int maxSlots = HavenResearchLabSystem.GetMaxSlots(state.Haven.Tier);

        var result = HavenResearchLabSystem.StartSlotResearch(state, "sensor_suite", maxSlots);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Reason, Is.EqualTo("invalid_slot_index"));
    }

    [Test]
    public void Process_ActiveSlot_DeductsCredits()
    {
        var state = CreateState();
        state.Haven.ResearchLabSlots.Add(new HavenResearchSlot
        {
            SlotIndex = 0,
            TechId = "sensor_suite",
            ProgressTicks = 0,
            TotalTicks = 100
        });
        long creditsBefore = state.PlayerCredits;

        HavenResearchLabSystem.Process(state);

        Assert.That(state.PlayerCredits, Is.LessThan(creditsBefore));
        Assert.That(state.Haven.ResearchLabSlots[0].ProgressTicks, Is.GreaterThan(0));
    }

    [Test]
    public void Process_InsufficientCredits_Stalls()
    {
        var state = CreateState();
        state.PlayerCredits = 0;
        state.Haven.ResearchLabSlots.Add(new HavenResearchSlot
        {
            SlotIndex = 0,
            TechId = "sensor_suite",
            ProgressTicks = 5,
            TotalTicks = 100
        });

        HavenResearchLabSystem.Process(state);

        Assert.That(state.Haven.ResearchLabSlots[0].StallTicks, Is.GreaterThan(0));
        Assert.That(state.Haven.ResearchLabSlots[0].StallReason, Is.EqualTo("insufficient_credits"));
        Assert.That(state.Haven.ResearchLabSlots[0].ProgressTicks, Is.EqualTo(5)); // unchanged
    }

    [Test]
    public void MaxSlots_MatchesTier()
    {
        Assert.That(HavenResearchLabSystem.GetMaxSlots(HavenTier.Undiscovered), Is.EqualTo(0));
        Assert.That(HavenResearchLabSystem.GetMaxSlots(HavenTier.Inhabited), Is.EqualTo(1));
        Assert.That(HavenResearchLabSystem.GetMaxSlots(HavenTier.Operational), Is.EqualTo(2));
        Assert.That(HavenResearchLabSystem.GetMaxSlots(HavenTier.Expanded), Is.EqualTo(3));
    }
}
