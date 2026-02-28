using System;
using System.IO;
using System.Text.Json;
using NUnit.Framework;
using SimCore;
using SimCore.Programs;
using SimCore.Schemas;
using SimCore.World;

namespace SimCore.Tests.Programs;

[TestFixture]
public sealed class ProgramQuoteContractTests
{
    [Test]
    public void QUOTE_001_request_plus_snapshot_produces_deterministic_schema_bound_quote_against_golden()
    {
        var k = KernelWithWorld001();
        var s = k.State;

        // Create a deterministic program (AUTO_BUY) on World001 ids.
        var pid = s.CreateAutoBuyProgram("mkt_a", "ore", quantity: 3, cadenceTicks: 10);

        // Snapshot + quote
        var snap = ProgramQuoteSnapshot.Capture(s, pid);
        var quote = ProgramQuote.BuildFromSnapshot(snap);
        var json = ProgramQuote.ToDeterministicJson(quote);

        // Schema-bound
        ProgramQuote.ValidateJsonIsSchemaBound(json);

        // Golden compare
        var goldenPath = FindRepoFilePath(Path.Combine("SimCore.Tests", "TestData", "Snapshots", "program_quote_001.json"));
        Assert.That(File.Exists(goldenPath), Is.True, $"Missing golden quote snapshot: {goldenPath}");

        var golden = File.ReadAllText(goldenPath);
        Assert.That(NormalizeJson(json), Is.EqualTo(NormalizeJson(golden)));
    }

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
    public void EXPLOITATION_PACKAGES_001_exploitation_quote_schema_bound_and_deterministic()
    {
        var risks = new System.Collections.Generic.List<ProgramQuote.ExploitationRisk>
                {
                        new() { Token = ProgramQuote.ExploitationRiskToken.NoRoutingService, Magnitude = 75 },
                        new() { Token = ProgramQuote.ExploitationRiskToken.FrontierAccessRequired, Magnitude = 30 },
                };
        var verbs = new System.Collections.Generic.List<string>
                {
                        ProgramQuote.ExploitationMitigationVerb.AssignRoutingFleet,
                        ProgramQuote.ExploitationMitigationVerb.InsureShipment,
                };

        var q1 = ProgramQuote.BuildExploitationQuote(
                quoteTick: 0, programKind: ProgramKind.ResourceTapV0, scopeId: "scope_tap",
                upfrontCost: 500, ongoingCostPerDay: 100, timeToActivateTicks: 60,
                p10: 80, p50: 160, p90: 250,
                risks: risks, mitigationVerbs: verbs);

        var json1 = ProgramQuote.ToDeterministicJson(q1);
        ProgramQuote.ValidateExploitationJsonIsSchemaBound(json1);

        var q2 = ProgramQuote.BuildExploitationQuote(
                quoteTick: 0, programKind: ProgramKind.ResourceTapV0, scopeId: "scope_tap",
                upfrontCost: 500, ongoingCostPerDay: 100, timeToActivateTicks: 60,
                p10: 80, p50: 160, p90: 250,
                risks: risks, mitigationVerbs: verbs);

        Assert.That(ProgramQuote.ToDeterministicJson(q2), Is.EqualTo(json1));
    }

    private static string NormalizeJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string FindRepoFilePath(string relativePathFromRepoRoot)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        for (int i = 0; i < 12 && dir is not null; i++)
        {
            var candidate = Path.Combine(dir.FullName, relativePathFromRepoRoot);
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }

        return Path.Combine(AppContext.BaseDirectory, relativePathFromRepoRoot);
    }
}
