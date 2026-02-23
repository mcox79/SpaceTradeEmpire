using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace SimCore.Tests.Industry
{
    public class IndustryProgramContractTests
    {
        [Test]
        public void SaveLoad_Replay_IndustryEventStream_And_WorldSignature_Match_V0()
        {
            var seed = 42;
            var ticksA = 250;
            var ticksB = 250;

            var k1 = new SimCore.SimKernel(seed);

            // Minimal deterministic setup: one market and one industry site bound to that market id.
            // Assumes Entities are POCOs with public setters, consistent with existing SimCore patterns.
            var marketId = "M1";
            k1.State.Markets[marketId] = new SimCore.Entities.Market
            {
                Id = marketId,
                Inventory = new System.Collections.Generic.Dictionary<string, int>(StringComparer.Ordinal)
                {
                    ["ore"] = 100,
                    ["plates"] = 0,
                    ["cap_module"] = 0
                }
            };

            var siteId = "S1";
            k1.State.IndustrySites[siteId] = new SimCore.Entities.IndustrySite
            {
                Id = siteId,
                NodeId = marketId,
                Active = true,

                // Enable opt-in construction for the contract test only.
                ConstructionEnabled = true,

                BufferDays = 0,
                DegradePerDayBps = 0,
                HealthBps = 10000,
                Inputs = new System.Collections.Generic.Dictionary<string, int>(StringComparer.Ordinal),
                Outputs = new System.Collections.Generic.Dictionary<string, int>(StringComparer.Ordinal)
            };

            for (var i = 0; i < ticksA; i++) k1.Step();

            var save = k1.SaveToString();

            // Continue baseline run for the same post-save duration.
            for (var i = 0; i < ticksB; i++) k1.Step();

            var k2 = new SimCore.SimKernel(seed);
            k2.LoadFromString(save);

            for (var i = 0; i < ticksB; i++) k2.Step();

            var sig1 = k1.State.GetSignature();
            var sig2 = k2.State.GetSignature();
            Assert.That(sig2, Is.EqualTo(sig1));

            var ev1 = (k1.State.IndustryEventLog ?? new System.Collections.Generic.List<string>()).ToArray();
            var ev2 = (k2.State.IndustryEventLog ?? new System.Collections.Generic.List<string>()).ToArray();
            Assert.That(ev2, Is.EqualTo(ev1), "Industry event streams diverged.");

            EmitDeterministicReport(k2, marketId, siteId, sig2, ev2);
        }

        private static void EmitDeterministicReport(SimCore.SimKernel kernel, string marketId, string siteId, string signature, string[] events)
        {
            // Deterministic report format: fixed ordering, LF newlines, UTF-8 no BOM.
            var sb = new StringBuilder();
            sb.Append("industry_min_loop_report_v0").Append('\n');
            sb.Append("tick=").Append(kernel.State.Tick).Append('\n');
            sb.Append("seed=").Append(kernel.State.InitialSeed).Append('\n');
            sb.Append("market_id=").Append(marketId).Append('\n');
            sb.Append("site_id=").Append(siteId).Append('\n');
            sb.Append("world_signature=").Append(signature).Append('\n');

            if (kernel.State.Markets.TryGetValue(marketId, out var m) && m != null && m.Inventory != null)
            {
                sb.Append("inventory:").Append('\n');
                foreach (var kv in m.Inventory.OrderBy(k => k.Key, StringComparer.Ordinal))
                {
                    sb.Append("  ").Append(kv.Key).Append('=').Append(kv.Value).Append('\n');
                }
            }

            sb.Append("industry_events:").Append('\n');
            foreach (var e in events)
            {
                sb.Append("  ").Append(e ?? "").Append('\n');
            }

            var rel = Path.Combine("docs", "generated", "industry_min_loop_report_v0.txt");
            var full = Path.GetFullPath(rel);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);

            // Normalize line endings to LF explicitly.
            var text = sb.ToString().Replace("\r\n", "\n").Replace("\r", "\n");
            var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(text);
            File.WriteAllBytes(full, bytes);
        }
    }
}
