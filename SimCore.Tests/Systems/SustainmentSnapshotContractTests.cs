using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;

namespace SimCore.Tests.Systems;

[TestFixture]
public sealed class SustainmentSnapshotContractTests
{
        [Test]
        public void SUS_SNAP_001_snapshot_includes_bands_and_they_match_banded_time()
        {
                var state = new SimState(seed: 1);

                // Market/node id is the same key used by SustainmentReport.BuildForNode
                state.Markets["N1"] = new Market { Id = "N1" };

                // 1 input good, low inventory so coverage is finite and small
                state.Markets["N1"].Inventory["FOOD"] = 10;

                var site = new IndustrySite
                {
                        Id = "SITE1",
                        NodeId = "N1",
                        Active = true,
                        BufferDays = 1,
                        HealthBps = 10000,
                        DegradePerDayBps = 0
                };
                site.Inputs["FOOD"] = 2; // 2 units per tick, coverageTicks = 10/2 = 5
                state.IndustrySites["SITE1"] = site;

                var snap = SustainmentSnapshot.BuildForNode(state, "N1");
                Assert.That(snap.Count, Is.EqualTo(1));

                var s = snap[0];
                Assert.That(s.SiteId, Is.EqualTo("SITE1"));
                Assert.That(s.NodeId, Is.EqualTo("N1"));

                // Bands exist and match BandedTime
                var expStarve = BandedTime.BandTicks(s.TimeToStarveTicks, IndustrySystem.TicksPerDay);
                var expFail = BandedTime.BandTicks(s.TimeToFailureTicks, IndustrySystem.TicksPerDay);

                Assert.That(s.StarveBand, Is.EqualTo(expStarve));
                Assert.That(s.FailBand, Is.EqualTo(expFail));

                Assert.That(s.Inputs.Count, Is.EqualTo(1));
                var inp = s.Inputs[0];

                var expCov = BandedTime.BandTicks(inp.CoverageTicks, IndustrySystem.TicksPerDay);
                Assert.That(inp.CoverageBand, Is.EqualTo(expCov));
        }
}
