using System;
using System.Linq;
using NUnit.Framework;
using SimCore;
using SimCore.Commands;
using SimCore.Entities;
using SimCore.Events;
using SimCore.Programs;

namespace SimCore.Tests.Programs;

[TestFixture]
public sealed class ProgramLifecycleContractTests
{
    [Test]
    public void PROG_LIFE_001_start_pause_cancel_transitions_are_stable()
    {
        var state = new SimState(seed: 1);

        state.Markets["M1"] = new SimCore.Entities.Market { Id = "M1" };
        state.Markets["M1"].Inventory["FOOD"] = 100;

        state.PlayerCredits = 10_000;
        state.PlayerCargo["FOOD"] = 0;

        var pid = state.CreateAutoBuyProgram("M1", "FOOD", quantity: 1, cadenceTicks: 5);
        var p = state.Programs.Instances[pid];

        Assert.That(p.Status, Is.EqualTo(ProgramStatus.Paused));

        // Start
        p.Status = ProgramStatus.Running;
        p.NextRunTick = state.Tick;

        ProgramSystem.Process(state);
        Assert.That(state.PendingIntents.Count, Is.EqualTo(1));

        // Pause prevents new emissions
        state.PendingIntents.Clear();
        p.Status = ProgramStatus.Paused;
        p.NextRunTick = state.Tick;

        ProgramSystem.Process(state);
        Assert.That(state.PendingIntents.Count, Is.EqualTo(0));

        // Cancel prevents new emissions even if due
        p.Status = ProgramStatus.Cancelled;
        p.NextRunTick = state.Tick;

        ProgramSystem.Process(state);
        Assert.That(state.PendingIntents.Count, Is.EqualTo(0));
    }

    [Test]
    public void PROG_UI_001_manual_override_job_cancel_emits_event_and_pauses_programs()
    {
        var state = new SimState(seed: 1);

        // Node required for FleetSetDestinationCommand validation
        state.Nodes["N2"] = new Node { Id = "N2" };

        // Fleet with an active logistics job
        var fleet = new Fleet { Id = "F1", CurrentNodeId = "N1" };
        fleet.CurrentJob = new LogisticsJob
        {
            GoodId = "FOOD",
            Amount = 1,
            SourceNodeId = "N1",
            TargetNodeId = "N2"
        };
        state.Fleets["F1"] = fleet;

        // Program explicitly bound to the fleet (so ProgramSystem can scope the pause correctly)
        state.Markets["M1"] = new SimCore.Entities.Market { Id = "M1" };
        state.Markets["M1"].Inventory["FOOD"] = 100;
        state.PlayerCredits = 10_000;
        state.PlayerCargo["FOOD"] = 0;

        var pid = state.CreateAutoBuyProgram("M1", "FOOD", quantity: 1, cadenceTicks: 5);
        var p = state.Programs.Instances[pid];
        p.FleetId = "F1";
        p.Status = ProgramStatus.Running;
        p.NextRunTick = state.Tick;

        // Issue ManualOverride which must cancel the job and emit schema-bound events
        new FleetSetDestinationCommand("F1", "N2").Execute(state);

        Assert.That(state.Fleets["F1"].CurrentJob, Is.Null);

        Assert.That(
            state.LogisticsEventLog.Any(e =>
                e.Tick == state.Tick &&
                e.Type == LogisticsEvents.LogisticsEventType.ManualOverrideSet &&
                (e.FleetId ?? "") == "F1" &&
                (e.TargetNodeId ?? "") == "N2"),
            Is.True,
            "Expected ManualOverrideSet logistics event.");

        Assert.That(
            state.LogisticsEventLog.Any(e =>
                e.Tick == state.Tick &&
                e.Type == LogisticsEvents.LogisticsEventType.JobCanceled &&
                (e.FleetId ?? "") == "F1"),
            Is.True,
            "Expected JobCanceled logistics event.");

        // ProgramSystem must observe the ManualOverrideSet event deterministically and pause the bound program before emissions
        state.PendingIntents.Clear();
        ProgramSystem.Process(state);

        Assert.That(p.Status, Is.EqualTo(ProgramStatus.Paused));
        Assert.That(state.PendingIntents.Count, Is.EqualTo(0));
    }
}
