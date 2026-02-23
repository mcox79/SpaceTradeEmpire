using System;
using System.Collections.Generic;
using System.Globalization;
using SimCore.Entities;
using RM = SimCore.RiskModelV0;

namespace SimCore.Systems
{
    /// <summary>
    /// Slice 3 / GATE.S3.RISK_MODEL.001
    /// Deterministic lane incident generator v0.
    /// </summary>
    public static class RiskSystem
    {
        private const int BpsDenom = RM.BpsDenom;

        public static void Process(SimState state)
        {
            if (state is null) return;
            if (state.Edges is null || state.Edges.Count == default(int)) return;

            // Deterministic edge order.
            var edges = new List<Edge>(state.Edges.Count);
            foreach (var e in state.Edges.Values)
            {
                if (e is null) continue;
                if (string.IsNullOrWhiteSpace(e.Id)) continue;
                edges.Add(e);
            }
            edges.Sort((a, b) => string.CompareOrdinal(a.Id ?? "", b.Id ?? ""));

            // Risk scalar from tweaks (failure-safe).
            var scalar = state.Tweaks != null ? state.Tweaks.RiskScalar : SimCore.RiskModelV0.ScalarDefault;
            if (double.IsNaN(scalar) || double.IsInfinity(scalar)) scalar = SimCore.RiskModelV0.ScalarDefault;
            if (scalar < SimCore.RiskModelV0.ScalarMin) scalar = SimCore.RiskModelV0.ScalarMin;
            if (scalar > SimCore.RiskModelV0.ScalarMax) scalar = SimCore.RiskModelV0.ScalarMax;

            foreach (var e in edges)
            {
                var edgeId = e.Id ?? "";
                if (edgeId.Length == default(int)) continue;

                var from = e.FromNodeId ?? "";
                var to = e.ToNodeId ?? "";

                var band = RoutePlanner.EdgeRiskBandV0(e);

                // Base rates per band (bps). Kept small to avoid spamming v0.
                int delayBps, lossBps, inspBps;
                switch (band)
                {
                    case RM.BandLow:
                        delayBps = SimCore.RiskModelV0.Band0DelayBps;
                        lossBps = SimCore.RiskModelV0.Band0LossBps;
                        inspBps = SimCore.RiskModelV0.Band0InspBps;
                        break;

                    case RM.BandMed:
                        delayBps = SimCore.RiskModelV0.Band1DelayBps;
                        lossBps = SimCore.RiskModelV0.Band1LossBps;
                        inspBps = SimCore.RiskModelV0.Band1InspBps;
                        break;

                    case RM.BandHigh:
                        delayBps = SimCore.RiskModelV0.Band2DelayBps;
                        lossBps = SimCore.RiskModelV0.Band2LossBps;
                        inspBps = SimCore.RiskModelV0.Band2InspBps;
                        break;

                    default:
                        delayBps = SimCore.RiskModelV0.Band3DelayBps;
                        lossBps = SimCore.RiskModelV0.Band3LossBps;
                        inspBps = SimCore.RiskModelV0.Band3InspBps;
                        break;
                }

                delayBps = ScaleBps(delayBps, scalar);
                lossBps = ScaleBps(lossBps, scalar);
                inspBps = ScaleBps(inspBps, scalar);

                var total = delayBps + lossBps + inspBps;
                if (total <= default(int)) continue;

                if (total > SimCore.RiskModelV0.TotalBpsCap)
                {
                    ClampTotal(ref delayBps, ref lossBps, ref inspBps, maxTotal: SimCore.RiskModelV0.TotalBpsCap);
                    total = delayBps + lossBps + inspBps;
                    if (total <= default(int)) continue;
                }

                // Deterministic roll in [0,9999]
                var h = Hash64(state.InitialSeed, state.Tick, edgeId);
                var roll = (int)(h % (ulong)BpsDenom);

                if (roll >= total) continue;

                var t = SimCore.Events.SecurityEvents.SecurityEventType.Unknown;
                if (roll < delayBps) t = SimCore.Events.SecurityEvents.SecurityEventType.Delay;
                else if (roll < delayBps + lossBps) t = SimCore.Events.SecurityEvents.SecurityEventType.Loss;
                else t = SimCore.Events.SecurityEvents.SecurityEventType.Inspection;

                var ho = Hash64(state.InitialSeed ^ SimCore.RiskModelV0.OutcomeHashXor, state.Tick, edgeId);

                int delayTicks = default(int);
                int lossUnits = default(int);
                int inspTicks = default(int);

                if (t == SimCore.Events.SecurityEvents.SecurityEventType.Delay)
                    delayTicks = SimCore.RiskModelV0.DelayMinTicks + (int)(ho % SimCore.RiskModelV0.DelayMod);
                else if (t == SimCore.Events.SecurityEvents.SecurityEventType.Loss)
                    lossUnits = SimCore.RiskModelV0.LossMinUnits + (int)(ho % SimCore.RiskModelV0.LossMod);
                else
                    inspTicks = SimCore.RiskModelV0.InspMinTicks + (int)(ho % SimCore.RiskModelV0.InspMod);

                var scalarText = scalar.ToString("R", CultureInfo.InvariantCulture);
                var cause =
                    "v0 band=" + band.ToString(CultureInfo.InvariantCulture) +
                    " roll=" + roll.ToString(CultureInfo.InvariantCulture) + "/" + BpsDenom.ToString(CultureInfo.InvariantCulture) +
                    " thr_delay=" + delayBps.ToString(CultureInfo.InvariantCulture) +
                    " thr_loss=" + lossBps.ToString(CultureInfo.InvariantCulture) +
                    " thr_insp=" + inspBps.ToString(CultureInfo.InvariantCulture) +
                    " scalar=" + scalarText;

                state.EmitSecurityEvent(new SimCore.Events.SecurityEvents.Event
                {
                    Type = t,
                    EdgeId = edgeId,
                    FromNodeId = from,
                    ToNodeId = to,
                    RiskBand = band,
                    DelayTicks = delayTicks,
                    LossUnits = lossUnits,
                    InspectionTicks = inspTicks,
                    CauseChain = cause,
                    Note = "INCIDENT_V0"
                });
            }
        }

        private static int ScaleBps(int bps, double scalar)
        {
            if (bps <= default(int)) return default(int);
            var scaled = (int)Math.Round(bps * scalar, MidpointRounding.AwayFromZero);
            if (scaled < default(int)) scaled = default(int);
            if (scaled > BpsDenom) scaled = BpsDenom;
            return scaled;
        }

        private static void ClampTotal(ref int a, ref int b, ref int c, int maxTotal)
        {
            var total = a + b + c;
            if (total <= maxTotal) return;
            if (total <= default(int)) { a = b = c = default(int); return; }

            a = (a * maxTotal) / total;
            b = (b * maxTotal) / total;
            c = (c * maxTotal) / total;

            var rem = maxTotal - (a + b + c);
            if (rem <= default(int)) return;

            while (rem-- > default(int))
            {
                a++;
                if (rem-- <= default(int)) break;
                c++;
                if (rem-- <= default(int)) break;
                b++;
            }
        }

        private static ulong Hash64(int seed, int tick, string edgeId)
        {
            var key = seed.ToString(CultureInfo.InvariantCulture)
                      + "|"
                      + tick.ToString(CultureInfo.InvariantCulture)
                      + "|"
                      + (edgeId ?? "");

            const ulong offset = SimCore.RiskModelV0.FnvOffset;
            const ulong prime = SimCore.RiskModelV0.FnvPrime;

            ulong h = offset;
            for (int i = default(int); i < key.Length; i++)
            {
                h ^= (byte)key[i];
                h *= prime;
            }
            return h;
        }
    }
}
