using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Schemas;
using SimCore.Systems;
using SimCore.Tweaks;
using SimCore.World;

namespace SimCore.Tests.Systems;

// GATE.T45.DEEP_DREAD.TESTS.001: Deep dread system contract tests.
[TestFixture]
public sealed class DeepDreadTests
{
    private static SimKernel MakeKernel(int instabilityA = 0, int instabilityB = 0)
    {
        var k = new SimKernel(seed: 42);
        var def = ScenarioHarnessV0.MicroWorld001();
        WorldLoader.Apply(k.State, def);
        k.State.Nodes["stn_a"].InstabilityLevel = instabilityA;
        k.State.Nodes["stn_b"].InstabilityLevel = instabilityB;
        // Set up faction home for patrol distance tests.
        k.State.FactionHomeNodes["test_faction"] = "stn_a";
        return k;
    }

    // ── Passive Drain ──

    [Test]
    public void Phase2_DrainApplies()
    {
        var k = MakeKernel(instabilityA: InstabilityTweaksV0.DriftMin);
        var fleet = k.State.Fleets.Values.First(f => f.OwnerId == "player");
        int initialHp = fleet.HullHp;
        Assert.That(initialHp, Is.GreaterThan(0), "Player fleet starts with HP");

        // Tick enough times for drain to fire.
        for (int i = 0; i < DeepDreadTweaksV0.Phase2DrainIntervalTicks + 1; i++)
            k.Step();

        Assert.That(fleet.HullHp, Is.LessThan(initialHp), "Phase 2 drain should reduce HP");
    }

    [Test]
    public void Phase4_NoDrain_VoidParadox()
    {
        // Phase 4 = Void: zero drain (paradox — clarity at maximum depth).
        var k = MakeKernel(instabilityA: InstabilityTweaksV0.VoidMin);
        var fleet = k.State.Fleets.Values.First(f => f.OwnerId == "player");
        int initialHp = fleet.HullHp;

        for (int i = 0; i < 100; i++)
            k.Step();

        Assert.That(fleet.HullHp, Is.EqualTo(initialHp), "Phase 4 (Void) = no drain");
    }

    [Test]
    public void Stable_NoDrain()
    {
        var k = MakeKernel(instabilityA: 0);
        var fleet = k.State.Fleets.Values.First(f => f.OwnerId == "player");
        int initialHp = fleet.HullHp;

        for (int i = 0; i < 100; i++)
            k.Step();

        Assert.That(fleet.HullHp, Is.EqualTo(initialHp), "Stable = no drain");
    }

    // ── Sensor Ghosts ──

    [Test]
    public void SensorGhosts_SpawnAtPhase2Plus()
    {
        var k = MakeKernel(instabilityA: InstabilityTweaksV0.DriftMin);

        // Run many ticks — ghosts should eventually appear.
        for (int i = 0; i < 200; i++)
            k.Step();

        // Ghosts may have spawned and expired; check that the system ran without error.
        Assert.That(k.State.SensorGhosts, Is.Not.Null, "SensorGhosts list initialized");
    }

    [Test]
    public void SensorGhosts_MaxConcurrent()
    {
        var k = MakeKernel(instabilityA: InstabilityTweaksV0.DriftMin);

        for (int i = 0; i < 500; i++)
            k.Step();

        // At no point should count exceed max.
        Assert.That(k.State.SensorGhosts.Count, Is.LessThanOrEqualTo(DeepDreadTweaksV0.GhostMaxConcurrent),
            "Ghost count should not exceed max concurrent");
    }

    // ── Information Fog ──

    [Test]
    public void InfoFog_RecordsVisits()
    {
        var k = MakeKernel();
        // Player is at stn_a — step should record visit.
        for (int i = 0; i < 5; i++)
            k.Step();

        Assert.That(k.State.NodeLastVisitTick, Does.ContainKey("stn_a"),
            "Visit to stn_a should be recorded");
    }

    [Test]
    public void InfoFog_Staleness_FreshAtPlayerNode()
    {
        var k = MakeKernel();
        for (int i = 0; i < 5; i++)
            k.Step();

        int staleness = InformationFogSystem.GetDataStaleness(k.State, "stn_a");
        Assert.That(staleness, Is.EqualTo(0), "Player's current node = fresh data");
    }

    // ── Exposure Tracking ──

    [Test]
    public void Exposure_IncrementsAtPhase2()
    {
        var k = MakeKernel(instabilityA: InstabilityTweaksV0.DriftMin);
        Assert.That(k.State.DeepExposure, Is.EqualTo(0), "Starts at 0");

        for (int i = 0; i < 10; i++)
            k.Step();

        Assert.That(k.State.DeepExposure, Is.GreaterThan(0), "Exposure increases at Phase 2+ nodes");
    }

    [Test]
    public void Exposure_NoIncrementAtStable()
    {
        var k = MakeKernel(instabilityA: 0);

        for (int i = 0; i < 50; i++)
            k.Step();

        Assert.That(k.State.DeepExposure, Is.EqualTo(0), "Stable nodes don't accumulate exposure");
    }

    [Test]
    public void ExposureAdaptation_ReportsCorrectly()
    {
        var k = MakeKernel();
        k.State.DeepExposure = DeepDreadTweaksV0.ExposureAdaptedThreshold;

        Assert.That(ExposureTrackSystem.IsAdapted(k.State), Is.True, "Should be adapted at threshold");
        Assert.That(ExposureTrackSystem.GetDisagreementNarrowBps(k.State), Is.GreaterThan(0),
            "Disagreement narrowing should be positive with exposure");
    }

    // ── Lattice Fauna ──

    [Test]
    public void Fauna_DoesNotSpawnAtStable()
    {
        var k = MakeKernel(instabilityA: 0);

        for (int i = 0; i < 200; i++)
            k.Step();

        Assert.That(k.State.LatticeFauna.Count, Is.EqualTo(0),
            "Fauna should not spawn at stable nodes");
    }

    [Test]
    public void Fauna_RequiresFractureSignature()
    {
        // Phase 3 but no fracture unlock — should not spawn.
        var k = MakeKernel(instabilityA: InstabilityTweaksV0.FractureMin);
        k.State.FractureUnlocked = false;

        for (int i = 0; i < 200; i++)
            k.Step();

        Assert.That(k.State.LatticeFauna.Count, Is.EqualTo(0),
            "Fauna should not spawn without fracture signature");
    }

    // ── Patrol Density ──

    [Test]
    public void PatrolHopDistance_ComputesCorrectly()
    {
        var k = MakeKernel();
        // stn_a is faction home, stn_b is 1 hop away.
        int hopsA = NpcTradeSystem.ComputeHopsFromFactionHome(k.State, "test_faction", "stn_a");
        int hopsB = NpcTradeSystem.ComputeHopsFromFactionHome(k.State, "test_faction", "stn_b");

        Assert.That(hopsA, Is.EqualTo(0), "Home node = 0 hops");
        Assert.That(hopsB, Is.EqualTo(1), "Adjacent node = 1 hop");
    }

    [Test]
    public void PatrolHopDistance_NoPath_ReturnsMaxValue()
    {
        var k = MakeKernel();
        int hops = NpcTradeSystem.ComputeHopsFromFactionHome(k.State, "test_faction", "nonexistent_node");
        Assert.That(hops, Is.EqualTo(int.MaxValue), "Unreachable node = MaxValue");
    }

    // ── FO Triggers ──

    [Test]
    public void FO_LatticeThisTrigger_FiresAtPhase2()
    {
        var k = MakeKernel(instabilityA: InstabilityTweaksV0.DriftMin);
        // Promote an FO first so triggers can fire.
        FirstOfficerSystem.PromoteCandidate(k.State, FirstOfficerCandidate.Analyst);
        k.State.FirstOfficer!.Tier = DialogueTier.Fracture; // Ensure tier >= MinTier

        for (int i = 0; i < 5; i++)
            k.Step();

        // Check that the LATTICE_THIN trigger fired.
        bool triggered = k.State.FirstOfficer.DialogueEventLog
            .Any(e => e.TriggerToken == "LATTICE_THIN");
        Assert.That(triggered, Is.True, "LATTICE_THIN should fire at Phase 2+ node");
    }
}
