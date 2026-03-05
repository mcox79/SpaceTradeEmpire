using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimCore.Tests.ExperienceProof;

/// <summary>
/// EPIC.X.EXPERIENCE_PROOF.V0 — Layer 3: Exploration Bot Tests
///
/// Runs the ExplorationBot across multiple seeds and asserts on the FLAGS
/// it produces. These tests don't check specific game paths — they check
/// that the bot can PLAY the game and doesn't find critical issues.
///
/// When a test fails, the flag's Detail + Diagnostic fields tell you
/// exactly what's broken and where to look.
/// </summary>
[TestFixture]
public class ExplorationBotTests
{
    private static readonly int[] BotSeeds = { 42, 99, 1000, 31337, 77777 };

    // ── Core: bot can trade profitably ──

    [Test]
    public void Bot_MakesProfit_Across5Seeds()
    {
        foreach (var seed in BotSeeds)
        {
            var bot = new ExplorationBot { TickBudget = 2000 };
            var report = bot.Run(seed);

            // Bot should have traded
            Assert.That(report.TotalBuys, Is.GreaterThan(0),
                $"Seed {seed}: bot never bought anything.\n{report.GetSummary()}");
            Assert.That(report.TotalSells, Is.GreaterThan(0),
                $"Seed {seed}: bot never sold anything.\n{report.GetSummary()}");

            // Bot should have made money
            Assert.That(report.NetProfit, Is.GreaterThan(0),
                $"Seed {seed}: bot lost money ({report.NetProfit}cr).\n{report.GetSummary()}");
        }
    }

    // ── Exploration: bot visits most of the galaxy ──

    [Test]
    public void Bot_VisitsMostNodes_Across5Seeds()
    {
        foreach (var seed in BotSeeds)
        {
            var bot = new ExplorationBot { TickBudget = 2000 };
            var report = bot.Run(seed);

            float visitPct = (float)report.NodesVisited / Math.Max(1, report.TotalNodes);
            Assert.That(visitPct, Is.GreaterThanOrEqualTo(0.5f),
                $"Seed {seed}: only visited {report.NodesVisited}/{report.TotalNodes} ({visitPct:P0}).\n{report.GetSummary()}");
        }
    }

    // ── No critical flags ──

    [Test]
    public void Bot_NoCriticalFlags_Across5Seeds()
    {
        foreach (var seed in BotSeeds)
        {
            var bot = new ExplorationBot { TickBudget = 2000 };
            var report = bot.Run(seed);

            var criticals = report.Flags.Where(f => f.Severity == "CRITICAL").ToList();
            Assert.That(criticals, Is.Empty,
                $"Seed {seed}: {criticals.Count} CRITICAL flag(s):\n" +
                string.Join("\n", criticals.Select(f => $"  [{f.Id}] {f.Detail}")));
        }
    }

    // ── Trade variety: bot uses multiple goods ──

    [Test]
    public void Bot_TradesMultipleGoods()
    {
        // Across all seeds combined, the bot should trade at least 2 distinct goods
        var allGoodsBought = new HashSet<string>(StringComparer.Ordinal);
        var allGoodsSold = new HashSet<string>(StringComparer.Ordinal);

        foreach (var seed in BotSeeds)
        {
            var bot = new ExplorationBot { TickBudget = 2000 };
            var report = bot.Run(seed);
            foreach (var g in report.GoodsBought) allGoodsBought.Add(g);
            foreach (var g in report.GoodsSold) allGoodsSold.Add(g);
        }

        Assert.That(allGoodsBought.Count, Is.GreaterThanOrEqualTo(2),
            $"Bot only bought: [{string.Join(", ", allGoodsBought)}]");
        Assert.That(allGoodsSold.Count, Is.GreaterThanOrEqualTo(2),
            $"Bot only sold: [{string.Join(", ", allGoodsSold)}]");
    }

    // ── Credits trajectory is not flat ──

    [Test]
    public void Bot_CreditsChangeOverTime()
    {
        foreach (var seed in BotSeeds)
        {
            var bot = new ExplorationBot { TickBudget = 1000 };
            var report = bot.Run(seed);

            // Credits should change at least once (buy or sell)
            bool changed = false;
            for (int i = 1; i < report.CreditTrajectory.Count; i++)
            {
                if (report.CreditTrajectory[i] != report.CreditTrajectory[i - 1])
                {
                    changed = true;
                    break;
                }
            }

            Assert.That(changed, Is.True,
                $"Seed {seed}: credits stayed at {report.CreditTrajectory[0]} for entire run.\n{report.GetSummary()}");
        }
    }

    // ── Diagnostic output: print full report for one seed ──

    [Test]
    public void Bot_DiagnosticReport_Seed42()
    {
        var bot = new ExplorationBot { TickBudget = 2000 };
        var report = bot.Run(42);

        // Print the full report for human/Claude review
        TestContext.WriteLine(report.GetSummary());

        // This test always passes — it's for diagnostic output
        Assert.Pass($"Report generated. {report.Flags.Count} flag(s), " +
            $"net profit {report.NetProfit}cr, " +
            $"{report.NodesVisited}/{report.TotalNodes} nodes visited.");
    }

    // ── Multi-seed aggregate: flag frequency analysis ──

    [Test]
    public void Bot_FlagFrequency_10Seeds()
    {
        var seeds = new[] { 1, 7, 42, 99, 256, 1000, 31337, 55555, 77777, 99999 };
        var flagCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var reports = new List<BotReport>();

        foreach (var seed in seeds)
        {
            var bot = new ExplorationBot { TickBudget = 1000 };
            var report = bot.Run(seed);
            reports.Add(report);

            foreach (var flag in report.Flags)
            {
                flagCounts.TryGetValue(flag.Id, out var count);
                flagCounts[flag.Id] = count + 1;
            }
        }

        // Print aggregate
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("═══ Aggregate Flag Frequency (10 seeds) ═══");
        foreach (var kv in flagCounts.OrderByDescending(k => k.Value))
        {
            sb.AppendLine($"  {kv.Key}: {kv.Value}/{seeds.Length} seeds");
        }

        var avgProfit = reports.Average(r => r.NetProfit);
        var avgVisitPct = reports.Average(r => (float)r.NodesVisited / Math.Max(1, r.TotalNodes));
        sb.AppendLine($"\nAvg net profit: {avgProfit:F0}cr");
        sb.AppendLine($"Avg node coverage: {avgVisitPct:P0}");
        sb.AppendLine($"Avg buys: {reports.Average(r => r.TotalBuys):F0}");
        sb.AppendLine($"Avg sells: {reports.Average(r => r.TotalSells):F0}");

        TestContext.WriteLine(sb.ToString());

        // Flags that appear in >50% of seeds are systemic issues
        var systemic = flagCounts.Where(kv => kv.Value > seeds.Length / 2).ToList();
        if (systemic.Count > 0)
        {
            var details = string.Join(", ", systemic.Select(kv => $"{kv.Key}({kv.Value}/{seeds.Length})"));
            // Don't hard-fail — just report. These are development insights.
            TestContext.WriteLine($"\nSystemic flags (>50% seeds): {details}");
        }

        Assert.Pass($"Analyzed {seeds.Length} seeds. {flagCounts.Count} distinct flag types. " +
            $"Avg profit: {avgProfit:F0}cr.");
    }

    // ── Determinism: same seed produces same report ──

    [Test]
    public void Bot_Deterministic_SameSeedSameResult()
    {
        var bot1 = new ExplorationBot { TickBudget = 500 };
        var report1 = bot1.Run(42);

        var bot2 = new ExplorationBot { TickBudget = 500 };
        var report2 = bot2.Run(42);

        Assert.That(report2.EndCredits, Is.EqualTo(report1.EndCredits),
            "Same seed should produce identical end credits");
        Assert.That(report2.TotalBuys, Is.EqualTo(report1.TotalBuys),
            "Same seed should produce identical buy count");
        Assert.That(report2.TotalSells, Is.EqualTo(report1.TotalSells),
            "Same seed should produce identical sell count");
        Assert.That(report2.NodesVisited, Is.EqualTo(report1.NodesVisited),
            "Same seed should produce identical node coverage");
    }
}
