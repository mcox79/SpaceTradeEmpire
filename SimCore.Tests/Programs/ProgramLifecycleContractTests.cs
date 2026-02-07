using NUnit.Framework;
using SimCore;
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
}
