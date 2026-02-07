using NUnit.Framework;
using SimCore;
using SimCore.Commands;
using SimCore.Programs;

namespace SimCore.Tests.Programs;

[TestFixture]
public sealed class ProgramStatusCommandContractTests
{
        [Test]
        public void PROG_CMD_001_set_status_command_updates_program_and_clamps_next_run_tick_on_start()
        {
                var state = new SimState(seed: 1);

                state.Markets["M1"] = new SimCore.Entities.Market { Id = "M1" };
                state.Markets["M1"].Inventory["FOOD"] = 100;

                var pid = state.CreateAutoBuyProgram("M1", "FOOD", quantity: 1, cadenceTicks: 5);
                var p = state.Programs.Instances[pid];

                // Put next run in the past relative to current tick.
                state.AdvanceTick();
                state.AdvanceTick();
                Assert.That(state.Tick, Is.EqualTo(2));

                p.NextRunTick = 0;
                p.Status = ProgramStatus.Paused;

                // Start should clamp NextRunTick to now if it is behind.
                var cmdStart = new SetProgramStatusCommand(pid, ProgramStatus.Running);
                cmdStart.Execute(state);

                Assert.That(p.Status, Is.EqualTo(ProgramStatus.Running));
                Assert.That(p.NextRunTick, Is.EqualTo(state.Tick));

                // Pause should not change NextRunTick.
                p.NextRunTick = state.Tick + 10;
                var cmdPause = new SetProgramStatusCommand(pid, ProgramStatus.Paused);
                cmdPause.Execute(state);

                Assert.That(p.Status, Is.EqualTo(ProgramStatus.Paused));
                Assert.That(p.NextRunTick, Is.EqualTo(state.Tick + 10));

                // Cancel should set cancelled.
                var cmdCancel = new SetProgramStatusCommand(pid, ProgramStatus.Cancelled);
                cmdCancel.Execute(state);

                Assert.That(p.Status, Is.EqualTo(ProgramStatus.Cancelled));
        }
}
