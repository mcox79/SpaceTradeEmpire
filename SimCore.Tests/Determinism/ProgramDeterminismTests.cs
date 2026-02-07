using NUnit.Framework;
using SimCore;
using SimCore.Programs;

namespace SimCore.Tests.Determinism;

[TestFixture]
public sealed class ProgramDeterminismTests
{
        [Test]
        public void Program_LongRun_FinalSignature_IsDeterministic()
        {
                var a = BuildKernelWithProgram(seed: 777);
                var b = BuildKernelWithProgram(seed: 777);

                for (int i = 0; i < 200; i++)
                {
                        a.Step();
                        b.Step();
                }

                var sigA = a.State.GetSignature();
                var sigB = b.State.GetSignature();

                Assert.That(sigA, Is.EqualTo(sigB));
        }

        private static SimKernel BuildKernelWithProgram(int seed)
        {
                var k = new SimKernel(seed);
                var s = k.State;

                // Minimal market state for deterministic buys.
                s.Markets["M1"] = new SimCore.Entities.Market { Id = "M1" };
                s.Markets["M1"].Inventory["FOOD"] = 100;

                s.PlayerCredits = 10_000;
                s.PlayerCargo["FOOD"] = 0;

                // Program: every 10 ticks, buy 3 FOOD.
                var pid = s.CreateAutoBuyProgram("M1", "FOOD", quantity: 3, cadenceTicks: 10);
                var p = s.Programs.Instances[pid];
                p.Status = ProgramStatus.Running;
                p.NextRunTick = s.Tick;

                return k;
        }
}
