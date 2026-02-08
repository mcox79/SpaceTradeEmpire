using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Schemas;
using SimCore.World;

namespace SimCore.Tests.Programs;

[TestFixture]
public sealed class FleetBindingContractTests
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
        public void FLEET_001_single_player_trader_fleet_exists_with_stable_id_and_start_state()
        {
                var k = KernelWithWorld001();
                var s = k.State;

                Assert.That(s.Fleets.Count, Is.EqualTo(1));
                Assert.That(s.Fleets.ContainsKey("fleet_trader_1"), Is.True);

                var f = s.Fleets["fleet_trader_1"];
                Assert.That(f.OwnerId, Is.EqualTo("player"));
                Assert.That(f.CurrentNodeId, Is.EqualTo("stn_a"));
                Assert.That(f.State, Is.EqualTo(FleetState.Docked));
                Assert.That(f.TravelProgress, Is.EqualTo(0f));
                Assert.That(f.DestinationNodeId, Is.EqualTo(""));
                Assert.That(f.CurrentEdgeId, Is.EqualTo(""));
        }

        [Test]
        public void FLEET_001_is_deterministic_by_signature()
        {
                var a = KernelWithWorld001();
                var b = KernelWithWorld001();

                Assert.That(a.State.GetSignature(), Is.EqualTo(b.State.GetSignature()));
        }

        [Test]
        public void FLEET_001_survives_save_load_without_signature_change()
        {
                var k = KernelWithWorld001();
                var before = k.State.GetSignature();

                var saved = k.SaveToString();
                var k2 = new SimKernel(seed: 999);
                k2.LoadFromString(saved);

                var after = k2.State.GetSignature();
                Assert.That(after, Is.EqualTo(before));
                Assert.That(k2.State.Fleets.ContainsKey("fleet_trader_1"), Is.True);
        }
}
