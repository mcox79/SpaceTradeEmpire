using System;
using System.Collections.Generic;

namespace SimCore.Systems;

public sealed class SustainmentSnapshotInput
{
        public string GoodId { get; set; } = "";

        public int HaveUnits { get; set; }
        public int PerTickRequired { get; set; }
        public int BufferTargetUnits { get; set; }

        public int CoverageTicks { get; set; }
        public float CoverageDays { get; set; }
        public float BufferMargin { get; set; }

        // Contracted UI band
        public string CoverageBand { get; set; } = "?";
}

public sealed class SustainmentSnapshotSite
{
        public string SiteId { get; set; } = "";
        public string NodeId { get; set; } = "";

        public int HealthBps { get; set; }
        public int EffBpsNow { get; set; }
        public int DegradePerDayBps { get; set; }

        public float WorstBufferMargin { get; set; }

        public int TimeToStarveTicks { get; set; }
        public float TimeToStarveDays { get; set; }

        public int TimeToFailureTicks { get; set; }
        public float TimeToFailureDays { get; set; }

        // Contracted UI bands
        public string StarveBand { get; set; } = "?";
        public string FailBand { get; set; } = "?";

        public List<SustainmentSnapshotInput> Inputs { get; set; } = new();
}

public static class SustainmentSnapshot
{
        public static List<SustainmentSnapshotSite> BuildForNode(SimState state, string nodeId)
        {
                var sites = SustainmentReport.BuildForNode(state, nodeId);
                var tpd = IndustrySystem.TicksPerDay;

                var result = new List<SustainmentSnapshotSite>(sites.Count);

                foreach (var s in sites)
                {
                        var site = new SustainmentSnapshotSite
                        {
                                SiteId = s.SiteId,
                                NodeId = s.NodeId,

                                HealthBps = s.HealthBps,
                                EffBpsNow = s.EffBpsNow,
                                DegradePerDayBps = s.DegradePerDayBps,

                                WorstBufferMargin = s.WorstBufferMargin,

                                TimeToStarveTicks = s.TimeToStarveTicks,
                                TimeToStarveDays = s.TimeToStarveDays,

                                TimeToFailureTicks = s.TimeToFailureTicks,
                                TimeToFailureDays = s.TimeToFailureDays,

                                StarveBand = BandedTime.BandTicks(s.TimeToStarveTicks, tpd),
                                FailBand = BandedTime.BandTicks(s.TimeToFailureTicks, tpd),
                        };

                        foreach (var inp in s.Inputs)
                        {
                                site.Inputs.Add(new SustainmentSnapshotInput
                                {
                                        GoodId = inp.GoodId,
                                        HaveUnits = inp.HaveUnits,
                                        PerTickRequired = inp.PerTickRequired,
                                        BufferTargetUnits = inp.BufferTargetUnits,
                                        CoverageTicks = inp.CoverageTicks,
                                        CoverageDays = inp.CoverageDays,
                                        BufferMargin = inp.BufferMargin,
                                        CoverageBand = BandedTime.BandTicks(inp.CoverageTicks, tpd)
                                });
                        }

                        result.Add(site);
                }

                return result;
        }
}
