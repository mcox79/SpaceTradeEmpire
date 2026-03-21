using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Programs;
using SimCore.Tweaks;
using System.Collections.Generic;
using System.Linq;

namespace SimCore.Tests.Programs;

// GATE.T41.SURVEY_PROG.SYSTEM.001
[TestFixture]
public sealed class SurveyProgramTests
{
    /// <summary>
    /// Build a small graph with discoveries for survey testing:
    ///   A ── B ── C ── D
    /// Player at A. Discoveries seeded at B and C.
    /// </summary>
    private SimState MakeSurveyGraph()
    {
        var state = new SimState(42);
        state.PlayerLocationNodeId = "A";

        string[] ids = { "A", "B", "C", "D" };
        foreach (var id in ids)
        {
            state.Nodes[id] = new Node { Id = id };
            state.Markets[id] = new Market { Id = id };
        }

        void AddEdge(string from, string to)
        {
            string eid = $"e_{from}_{to}";
            state.Edges[eid] = new Edge { Id = eid, FromNodeId = from, ToNodeId = to, Distance = 5f };
        }

        AddEdge("A", "B");
        AddEdge("B", "C");
        AddEdge("C", "D");

        state.Fleets["fleet_player"] = new Fleet { Id = "fleet_player", OwnerId = "player", Speed = 1.0f };

        return state;
    }

    /// Helper: create survey program and set to Running (factory creates as Paused).
    private string CreateAndActivateSurvey(SimState state, string family, string homeNodeId, int rangeHops, int cadence)
    {
        var programId = state.CreateSurveyProgramV0(family, homeNodeId, rangeHops, cadence);
        state.Programs.Instances[programId].Status = ProgramStatus.Running;
        return programId;
    }

    [Test]
    public void CreateSurveyProgramV0_CreatesProgram()
    {
        var state = MakeSurveyGraph();
        var programId = state.CreateSurveyProgramV0("SIGNAL", "A", 3, 10);

        Assert.That(programId, Is.Not.Empty, "Should return a program ID");
        Assert.That(state.Programs, Is.Not.Null);
        Assert.That(state.Programs.Instances.ContainsKey(programId), Is.True);

        var program = state.Programs.Instances[programId];
        Assert.That(program.Kind, Is.EqualTo(ProgramKind.SurveyV0));
        Assert.That(program.SurveyFamily, Is.EqualTo("SIGNAL"));
        Assert.That(program.SiteId, Is.EqualTo("A"));
        Assert.That(program.SurveyRangeHops, Is.EqualTo(3));
        Assert.That(program.CadenceTicks, Is.EqualTo(10));
    }

    [Test]
    public void SurveyProgram_AdvancesSeenToScanned_WithinRange()
    {
        var state = MakeSurveyGraph();

        // Seed a SIGNAL discovery at node B (1 hop from A — within range 3)
        var discId = "disc_v0|SIGNAL|B|ref1|src1";
        state.Intel.Discoveries[discId] = new DiscoveryStateV0
        {
            DiscoveryId = discId,
            Phase = DiscoveryPhase.Seen
        };
        state.Nodes["B"].SeededDiscoveryIds ??= new List<string>();
        state.Nodes["B"].SeededDiscoveryIds.Add(discId);

        // Create survey program at A with range 3, activate it
        CreateAndActivateSurvey(state, "SIGNAL", "A", 3, 1);

        // Advance to trigger the program
        state.AdvanceTick();
        ProgramSystem.Process(state);

        Assert.That(state.Intel.Discoveries[discId].Phase, Is.EqualTo(DiscoveryPhase.Scanned),
            "Survey should advance Seen discovery to Scanned within range");
    }

    [Test]
    public void SurveyProgram_DoesNotAdvance_OutOfRange()
    {
        var state = MakeSurveyGraph();

        // Seed a SIGNAL discovery at node D (3 hops from A — at boundary)
        var discId = "disc_v0|SIGNAL|D|ref1|src1";
        state.Intel.Discoveries[discId] = new DiscoveryStateV0
        {
            DiscoveryId = discId,
            Phase = DiscoveryPhase.Seen
        };
        state.Nodes["D"].SeededDiscoveryIds ??= new List<string>();
        state.Nodes["D"].SeededDiscoveryIds.Add(discId);

        // Create survey program at A with range 2 (D is 3 hops away — out of range)
        CreateAndActivateSurvey(state, "SIGNAL", "A", 2, 1);

        state.AdvanceTick();
        ProgramSystem.Process(state);

        Assert.That(state.Intel.Discoveries[discId].Phase, Is.EqualTo(DiscoveryPhase.Seen),
            "Survey should NOT advance discoveries outside range");
    }

    [Test]
    public void SurveyProgram_DoesNotAdvance_WrongFamily()
    {
        var state = MakeSurveyGraph();

        // Seed a RUIN discovery at node B (within range)
        var discId = "disc_v0|RUIN|B|ref1|src1";
        state.Intel.Discoveries[discId] = new DiscoveryStateV0
        {
            DiscoveryId = discId,
            Phase = DiscoveryPhase.Seen
        };
        state.Nodes["B"].SeededDiscoveryIds ??= new List<string>();
        state.Nodes["B"].SeededDiscoveryIds.Add(discId);

        // Create survey program for SIGNAL family (doesn't match RUIN)
        CreateAndActivateSurvey(state, "SIGNAL", "A", 3, 1);

        state.AdvanceTick();
        ProgramSystem.Process(state);

        Assert.That(state.Intel.Discoveries[discId].Phase, Is.EqualTo(DiscoveryPhase.Seen),
            "Survey should NOT advance discoveries of wrong family");
    }

    [Test]
    public void SurveyProgram_RespectsGateCount()
    {
        var state = MakeSurveyGraph();

        // With 0 manual scans, survey should not be unlocked
        // MapKindToFamily: RESOURCE_POOL_MARKER→RUIN, CORRIDOR_TRACE→SIGNAL, other→OUTCOME
        int count = SimCore.Systems.DiscoveryOutcomeSystem.GetManualScanCountByFamily(state, "RUIN");
        bool unlocked = count >= SurveyProgramTweaksV0.ManualScanGateCount;

        Assert.That(unlocked, Is.False,
            "Survey should not be unlocked with 0 manual scans");

        // Add enough scanned RESOURCE_POOL_MARKER discoveries to unlock RUIN family
        for (int i = 0; i < SurveyProgramTweaksV0.ManualScanGateCount; i++)
        {
            var dId = $"disc_v0|RESOURCE_POOL_MARKER|A|ref{i}|src{i}";
            state.Intel.Discoveries[dId] = new DiscoveryStateV0
            {
                DiscoveryId = dId,
                Phase = DiscoveryPhase.Scanned
            };
        }

        count = SimCore.Systems.DiscoveryOutcomeSystem.GetManualScanCountByFamily(state, "RUIN");
        unlocked = count >= SurveyProgramTweaksV0.ManualScanGateCount;

        Assert.That(unlocked, Is.True,
            $"Survey should be unlocked with {SurveyProgramTweaksV0.ManualScanGateCount} manual scans");
    }

    [Test]
    public void SurveyProgram_OnlyAdvancesSeenPhase_NotScanned()
    {
        var state = MakeSurveyGraph();

        // Seed a discovery already at Scanned phase
        var discId = "disc_v0|SIGNAL|B|ref1|src1";
        state.Intel.Discoveries[discId] = new DiscoveryStateV0
        {
            DiscoveryId = discId,
            Phase = DiscoveryPhase.Scanned
        };
        state.Nodes["B"].SeededDiscoveryIds ??= new List<string>();
        state.Nodes["B"].SeededDiscoveryIds.Add(discId);

        CreateAndActivateSurvey(state, "SIGNAL", "A", 3, 1);

        state.AdvanceTick();
        ProgramSystem.Process(state);

        Assert.That(state.Intel.Discoveries[discId].Phase, Is.EqualTo(DiscoveryPhase.Scanned),
            "Survey should not advance Scanned to Analyzed (manual step required)");
    }

    [Test]
    public void SurveyProgram_CadenceRespected()
    {
        var state = MakeSurveyGraph();

        var discId = "disc_v0|SIGNAL|B|ref1|src1";
        state.Intel.Discoveries[discId] = new DiscoveryStateV0
        {
            DiscoveryId = discId,
            Phase = DiscoveryPhase.Seen
        };
        state.Nodes["B"].SeededDiscoveryIds ??= new List<string>();
        state.Nodes["B"].SeededDiscoveryIds.Add(discId);

        // Create survey with cadence 5, activate it
        CreateAndActivateSurvey(state, "SIGNAL", "A", 3, 5);

        // Tick 1: should run (first tick after creation)
        state.AdvanceTick();
        ProgramSystem.Process(state);

        Assert.That(state.Intel.Discoveries[discId].Phase, Is.EqualTo(DiscoveryPhase.Scanned),
            "First run should process");
    }
}
