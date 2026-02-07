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
