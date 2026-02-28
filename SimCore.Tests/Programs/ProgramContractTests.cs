using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SimCore;
using SimCore.Programs;

namespace SimCore.Tests.Programs;

[TestFixture]
public sealed class ProgramContractTests
{
    [Test]
    public void PROG_001_program_schema_file_exists_and_is_json()
    {
        var path = FindRepoFilePath(Path.Combine("SimCore", "Schemas", "ProgramSchema.json"));
        Assert.That(File.Exists(path), Is.True, $"Missing schema file: {path}");

        var json = File.ReadAllText(path);
        Assert.That(string.IsNullOrWhiteSpace(json), Is.False);

        using var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.That(doc.RootElement.ValueKind, Is.EqualTo(System.Text.Json.JsonValueKind.Object));

        Assert.That(doc.RootElement.TryGetProperty("schemaVersion", out var v), Is.True);
        Assert.That(v.ValueKind, Is.EqualTo(System.Text.Json.JsonValueKind.Number));
    }

    [Test]
    public void PROG_EXEC_001_program_system_emits_intents_only()
    {
        var state = new SimState(seed: 123);

        state.Markets["M1"] = new SimCore.Entities.Market { Id = "M1" };
        state.Markets["M1"].Inventory["FOOD"] = 50;

        state.PlayerCredits = 1000;
        state.PlayerCargo["FOOD"] = 0;

        var pid = state.CreateAutoBuyProgram("M1", "FOOD", quantity: 3, cadenceTicks: 10);
        state.Programs.Instances[pid].Status = ProgramStatus.Running;
        state.Programs.Instances[pid].NextRunTick = state.Tick;

        var invBefore = state.Markets["M1"].Inventory["FOOD"];
        var creditsBefore = state.PlayerCredits;
        var cargoBefore = state.PlayerCargo["FOOD"];
        var pendingBefore = state.PendingIntents.Count;

        ProgramSystem.Process(state);

        Assert.That(state.Markets["M1"].Inventory["FOOD"], Is.EqualTo(invBefore));
        Assert.That(state.PlayerCredits, Is.EqualTo(creditsBefore));
        Assert.That(state.PlayerCargo["FOOD"], Is.EqualTo(cargoBefore));

        Assert.That(state.PendingIntents.Count, Is.EqualTo(pendingBefore + 1));
        var env = state.PendingIntents.Last();
        Assert.That(env.Kind, Is.EqualTo("BUY"));
        Assert.That(env.Intent, Is.Not.Null);
    }

    [Test]
    public void EXPLAIN_001_program_explain_payload_is_schema_bound_and_deterministic()
    {
        var state = new SimState(seed: 1);
        var pid = state.CreateAutoBuyProgram("M1", "FOOD", quantity: 2, cadenceTicks: 5);
        var p = state.Programs.Instances[pid];
        p.Status = ProgramStatus.Running;
        p.NextRunTick = 7;
        p.LastRunTick = 2;

        var payload1 = ProgramExplain.Build(state);
        var json1 = ProgramExplain.ToDeterministicJson(payload1);

        ProgramExplain.ValidateJsonIsSchemaBound(json1);

        var payload2 = ProgramExplain.Build(state);
        var json2 = ProgramExplain.ToDeterministicJson(payload2);

        Assert.That(json2, Is.EqualTo(json1));
    }

    [Test]
    public void EXPLOITATION_PACKAGES_001_program_kind_constants_exist()
    {
        Assert.That(ProgramKind.TradeCharterV0, Is.EqualTo("TRADE_CHARTER_V0"));
        Assert.That(ProgramKind.ResourceTapV0, Is.EqualTo("RESOURCE_TAP_V0"));
    }

    [Test]
    public void EXPLOITATION_PACKAGES_001_reason_codes_all_registered()
    {
        Assert.That(ProgramExplain.ReasonCodes.ServiceUnavailable, Is.EqualTo("ServiceUnavailable"));
        Assert.That(ProgramExplain.ReasonCodes.InsufficientCapacity, Is.EqualTo("InsufficientCapacity"));
        Assert.That(ProgramExplain.ReasonCodes.NoExportRoute, Is.EqualTo("NoExportRoute"));
        Assert.That(ProgramExplain.ReasonCodes.BudgetExhausted, Is.EqualTo("BudgetExhausted"));
    }

    [Test]
    public void EXPLOITATION_PACKAGES_001_exploitation_quote_fields_and_ordering_contract()
    {
        // Build a quote with intentionally unsorted risks and mitigations.
        var risks = new System.Collections.Generic.List<ProgramQuote.ExploitationRisk>
                {
                        new() { Token = ProgramQuote.ExploitationRiskToken.LowExpectedThroughput, Magnitude = 40 },
                        new() { Token = ProgramQuote.ExploitationRiskToken.NoRoutingService,       Magnitude = 80 },
                        new() { Token = ProgramQuote.ExploitationRiskToken.HighCapitalLockup,      Magnitude = 80 },
                };

        var verbs = new System.Collections.Generic.List<string>
                {
                        ProgramQuote.ExploitationMitigationVerb.SubstituteInputGood,
                        ProgramQuote.ExploitationMitigationVerb.AssignRoutingFleet,
                        ProgramQuote.ExploitationMitigationVerb.PausePackage,
                };

        var quote = ProgramQuote.BuildExploitationQuote(
                quoteTick: 5,
                programKind: ProgramKind.TradeCharterV0,
                scopeId: "scope_001",
                upfrontCost: 1000,
                ongoingCostPerDay: 200,
                timeToActivateTicks: 120,
                p10: 150,
                p50: 300,
                p90: 500,
                risks: risks,
                mitigationVerbs: verbs);

        // Schema-bound
        var json = ProgramQuote.ToDeterministicJson(quote);
        ProgramQuote.ValidateExploitationJsonIsSchemaBound(json);

        // TopRisks ordering: magnitude desc, then token Ordinal asc on tie.
        // Magnitude 80: HIGH_CAPITAL_LOCKUP < NO_ROUTING_SERVICE (Ordinal), then magnitude 40.
        Assert.That(quote.TopRisks[0].Token, Is.EqualTo(ProgramQuote.ExploitationRiskToken.HighCapitalLockup));
        Assert.That(quote.TopRisks[1].Token, Is.EqualTo(ProgramQuote.ExploitationRiskToken.NoRoutingService));
        Assert.That(quote.TopRisks[2].Token, Is.EqualTo(ProgramQuote.ExploitationRiskToken.LowExpectedThroughput));

        // SuggestedMitigations ordering: Ordinal asc.
        Assert.That(quote.SuggestedMitigations[0], Is.EqualTo(ProgramQuote.ExploitationMitigationVerb.AssignRoutingFleet));
        Assert.That(quote.SuggestedMitigations[1], Is.EqualTo(ProgramQuote.ExploitationMitigationVerb.PausePackage));
        Assert.That(quote.SuggestedMitigations[2], Is.EqualTo(ProgramQuote.ExploitationMitigationVerb.SubstituteInputGood));

        // KPI bands
        Assert.That(quote.ExpectedOutputBands_p10, Is.EqualTo(150));
        Assert.That(quote.ExpectedOutputBands_p50, Is.EqualTo(300));
        Assert.That(quote.ExpectedOutputBands_p90, Is.EqualTo(500));

        // Determinism: same inputs => same JSON
        var quote2 = ProgramQuote.BuildExploitationQuote(5, ProgramKind.TradeCharterV0, "scope_001",
                1000, 200, 120, 150, 300, 500, risks, verbs);
        Assert.That(ProgramQuote.ToDeterministicJson(quote2), Is.EqualTo(json));
    }

    private static string FindRepoFilePath(string relativePathFromRepoRoot)
    {
        // Tests run from bin output; locate repo root by walking upward from base directory.
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        for (int i = 0; i < 12 && dir is not null; i++)
        {
            var candidate = Path.Combine(dir.FullName, relativePathFromRepoRoot);
            if (File.Exists(candidate)) return candidate;

            dir = dir.Parent;
        }

        // Fallback for debuggability
        return Path.Combine(AppContext.BaseDirectory, relativePathFromRepoRoot);
    }
}
