using NUnit.Framework;
using SimCore;
using SimCore.Schemas;
using SimCore.World;
using SimCore.Programs;
using SimCore.Systems;

namespace SimCore.Tests.Programs;

[TestFixture]
public sealed class ProgramExecutionIntegrationTests
{
        private static SimKernel KernelWithWorld001()
        {
                var k = new SimKernel(seed: 123);

                var def = new WorldDefinition
                {
                        WorldId = "micro_world_001",
                        Markets =
                        {
                                new WorldMarket { Id = "mkt_a", Inventory = new() { ["ore"] = 10, ["food"] = 3 } },
                                new WorldMarket { Id = "mkt_b", Inventory = new() { ["ore"] = 1,  ["food"] = 12 } }
                        },
                        Nodes =
                        {
                                new WorldNode { Id = "stn_a", Kind = "Station", Name = "Alpha Station", MarketId = "mkt_a", Pos = new float[] { 0f, 0f, 0f } },
                                new WorldNode { Id = "stn_b", Kind = "Station", Name = "Beta Station",  MarketId = "mkt_b", Pos = new float[] { 10f, 0f, 0f } }
                        },
                        Edges =
                        {
                                new WorldEdge { Id = "lane_ab", FromNodeId = "stn_a", ToNodeId = "stn_b", Distance = 1.0f, TotalCapacity = 5 }
                        },
                        Player = new WorldPlayerStart { Credits = 1000, LocationNodeId = "stn_a" }
                };

                WorldLoader.Apply(k.State, def);
                return k;
        }

        [Test]
        public void PROG_EXEC_002_TradeProgram_DrivesBuySell_Intents_AgainstWorld001_And_OnlyAffectsOutcomesViaTick()
        {
                var k = KernelWithWorld001();
                var s = k.State;

                // Seed player cargo so SELL has something to do.
                s.PlayerCargo["food"] = 5;

                // Programs: buy ore, sell food, both against mkt_a
                var buyId = s.CreateAutoBuyProgram("mkt_a", "ore", quantity: 1, cadenceTicks: 1);
                var sellId = s.CreateAutoSellProgram("mkt_a", "food", quantity: 1, cadenceTicks: 1);

                s.Programs.Instances[buyId].Status = ProgramStatus.Running;
                s.Programs.Instances[sellId].Status = ProgramStatus.Running;

                // Ensure both are due now
                s.Programs.Instances[buyId].NextRunTick = s.Tick;
                s.Programs.Instances[sellId].NextRunTick = s.Tick;

                var oreInMarketBefore = s.Markets["mkt_a"].Inventory["ore"];
                var foodInMarketBefore = s.Markets["mkt_a"].Inventory["food"];

                var oreInCargoBefore = s.PlayerCargo.TryGetValue("ore", out var oreBefore) ? oreBefore : 0;
                var foodInCargoBefore = s.PlayerCargo["food"];

                var creditsBefore = s.PlayerCredits;

                // 1) ProgramSystem alone may enqueue intents, but must not mutate market/cargo/credits.
                ProgramSystem.Process(s);

                Assert.That(s.Markets["mkt_a"].Inventory["ore"], Is.EqualTo(oreInMarketBefore));
                Assert.That(s.Markets["mkt_a"].Inventory["food"], Is.EqualTo(foodInMarketBefore));

                var oreInCargoAfterProgram = s.PlayerCargo.TryGetValue("ore", out var oreAfterProg) ? oreAfterProg : 0;
                Assert.That(oreInCargoAfterProgram, Is.EqualTo(oreInCargoBefore));
                Assert.That(s.PlayerCargo["food"], Is.EqualTo(foodInCargoBefore));
                Assert.That(s.PlayerCredits, Is.EqualTo(creditsBefore));

                // ProgramSystem should have emitted 2 intents.
                Assert.That(s.PendingIntents.Count, Is.EqualTo(2));

                // Clear to ensure the only effects come from a normal tick pipeline.
                s.PendingIntents.Clear();

                // Re-arm: ProgramSystem.Process advanced NextRunTick; ensure programs are runnable for the upcoming tick.
                s.Programs.Instances[buyId].NextRunTick = s.Tick;
                s.Programs.Instances[sellId].NextRunTick = s.Tick;

                // 2) Now run one tick: program emission + intent processing should change state via commands.
                k.Step();

                // Intents should be cleared by the pipeline.
                Assert.That(s.PendingIntents.Count, Is.EqualTo(0));

                // BUY: market ore down, cargo ore up
                Assert.That(s.Markets["mkt_a"].Inventory["ore"], Is.LessThan(oreInMarketBefore));
                var oreInCargoAfterTick = s.PlayerCargo.TryGetValue("ore", out var oreAfterTick) ? oreAfterTick : 0;
                Assert.That(oreInCargoAfterTick, Is.GreaterThan(oreInCargoBefore));

                // SELL: market food up, cargo food down
                Assert.That(s.Markets["mkt_a"].Inventory["food"], Is.GreaterThan(foodInMarketBefore));
                Assert.That(s.PlayerCargo["food"], Is.LessThan(foodInCargoBefore));

                // Credits should change as a result of buy and sell commands (direction depends on pricing).
                Assert.That(s.PlayerCredits, Is.Not.EqualTo(creditsBefore));
        }
}
