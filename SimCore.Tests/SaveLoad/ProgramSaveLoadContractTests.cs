using NUnit.Framework;
using SimCore;
using SimCore.Programs;

namespace SimCore.Tests.SaveLoad;

[TestFixture]
public sealed class ProgramSaveLoadContractTests
{
        [Test]
        public void SaveLoad_ProgramsPersist_PendingIntentsDiscarded()
        {
                var k = new SimKernel(seed: 5);
                var s = k.State;

                s.Markets["M1"] = new SimCore.Entities.Market { Id = "M1" };
                s.Markets["M1"].Inventory["FOOD"] = 100;

                var pid = s.CreateAutoBuyProgram("M1", "FOOD", quantity: 1, cadenceTicks: 10);
                s.Programs.Instances[pid].Status = ProgramStatus.Running;

                // Create one pending intent (will not persist because Intent is JsonIgnore)
                s.EnqueueIntent(new SimCore.Intents.BuyIntent("M1", "FOOD", 1));
                Assert.That(s.PendingIntents.Count, Is.EqualTo(1));

                var saved = k.SaveToString();

                var k2 = new SimKernel(seed: 0);
                k2.LoadFromString(saved);

                var s2 = k2.State;

                Assert.That(s2.Programs.Instances.ContainsKey(pid), Is.True);
                Assert.That(s2.PendingIntents.Count, Is.EqualTo(0));
        }
}
