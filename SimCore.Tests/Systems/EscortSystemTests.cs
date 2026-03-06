using NUnit.Framework;
using SimCore;
using SimCore.Programs;
using SimCore.Systems;
using SimCore.Tweaks;

namespace SimCore.Tests.Systems;

// GATE.S5.ESCORT_PROG.MODEL.001: Contract tests for EscortSystem.
[TestFixture]
[Category("EscortSystem")]
public sealed class EscortSystemTests
{
    private SimState CreateState()
    {
        var state = new SimState(42);
        state.PlayerCredits = 1000;
        return state;
    }

    private ProgramInstance AddProgram(SimState state, string kind, ProgramStatus status = ProgramStatus.Running)
    {
        var id = $"P{state.NextProgramSeq}";
        state.NextProgramSeq = checked(state.NextProgramSeq + 1);

        var prog = new ProgramInstance
        {
            Id = id,
            Kind = kind,
            Status = status,
            CreatedTick = state.Tick,
            CadenceTicks = 1,
            NextRunTick = 0,
            LastRunTick = -1,
            MarketId = "node_a",
            SourceMarketId = "node_b",
            FleetId = "fleet_escort_1",
            ExpeditionTicksRemaining = 0,
        };

        state.Programs ??= new ProgramBook();
        state.Programs.Instances[id] = prog;
        return prog;
    }

    [Test]
    public void EscortProgram_AdvancesPerTick()
    {
        var state = CreateState();
        var prog = AddProgram(state, ProgramKind.EscortV0);

        EscortSystem.Process(state);

        Assert.That(prog.ExpeditionTicksRemaining, Is.EqualTo(1));
    }

    [Test]
    public void EscortProgram_CompletesLeg()
    {
        var state = CreateState();
        var prog = AddProgram(state, ProgramKind.EscortV0);

        // Run exactly PatrolCycleBaseTicks ticks — should complete on the last tick.
        for (int i = 0; i < EscortTweaksV0.PatrolCycleBaseTicks; i++)
        {
            EscortSystem.Process(state);
            state.AdvanceTick();
        }

        // LastRunTick should have been set; counter should have reset.
        Assert.That(prog.LastRunTick, Is.GreaterThanOrEqualTo(0), "LastRunTick should be set after leg completion");
        Assert.That(prog.ExpeditionTicksRemaining, Is.EqualTo(0), "Counter should reset after leg completion");
    }

    [Test]
    public void PatrolProgram_CyclesContinuously()
    {
        var state = CreateState();
        var prog = AddProgram(state, ProgramKind.PatrolV0);

        // Run 2 full cycles.
        int totalTicks = EscortTweaksV0.PatrolCycleBaseTicks * 2;
        int legCompletions = 0;
        int lastRecordedTick = -1;

        for (int i = 0; i < totalTicks; i++)
        {
            EscortSystem.Process(state);

            if (prog.LastRunTick != lastRecordedTick && prog.LastRunTick >= 0)
            {
                legCompletions++;
                lastRecordedTick = prog.LastRunTick;
            }

            state.AdvanceTick();
        }

        Assert.That(legCompletions, Is.GreaterThanOrEqualTo(2), "Patrol should complete at least 2 legs in 2 cycle intervals");
        Assert.That(prog.ExpeditionTicksRemaining, Is.EqualTo(0), "Counter should be reset after each cycle leg");
    }

    [Test]
    public void EscortProgram_SkipsNonRunning()
    {
        var state = CreateState();
        var prog = AddProgram(state, ProgramKind.EscortV0, ProgramStatus.Paused);

        EscortSystem.Process(state);

        Assert.That(prog.ExpeditionTicksRemaining, Is.EqualTo(0), "Paused program should not advance");
        Assert.That(prog.LastRunTick, Is.EqualTo(-1), "Paused program should not update LastRunTick");
    }

    [Test]
    public void EscortSystem_Deterministic()
    {
        // Run same inputs twice, verify identical outputs.
        SimState BuildAndRun()
        {
            var state = CreateState();
            AddProgram(state, ProgramKind.EscortV0);
            AddProgram(state, ProgramKind.PatrolV0);

            for (int i = 0; i < EscortTweaksV0.PatrolCycleBaseTicks + 5; i++)
            {
                EscortSystem.Process(state);
                state.AdvanceTick();
            }

            return state;
        }

        var a = BuildAndRun();
        var b = BuildAndRun();

        Assert.That(a.GetSignature(), Is.EqualTo(b.GetSignature()), "EscortSystem must be deterministic");
    }
}
