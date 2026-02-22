using System;
using System.IO;
using System.Text;
using System.Text.Json;
using SimCore;
using SimCore.Schemas;

namespace SimCore.Runner
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintUsage();
                Environment.Exit(1);
            }

            try
            {
                var cmd = args[0];

                if (string.Equals(cmd, "seed-explore", StringComparison.Ordinal))
                {
                    RunSeedExplore(args);
                    return;
                }

                if (string.Equals(cmd, "seed-diff", StringComparison.Ordinal))
                {
                    RunSeedDiff(args);
                    return;
                }

                // Back-compat: original runner mode expects a scenario.json path as the first arg.
                var scenarioPath = args[0];
                if (!File.Exists(scenarioPath))
                {
                    Console.Error.WriteLine($"Error: Scenario file not found at {scenarioPath}");
                    PrintUsage();
                    Environment.Exit(1);
                }

                string json = File.ReadAllText(scenarioPath);
                var scenario = JsonSerializer.Deserialize<ScenarioDefinition>(json);

                if (scenario == null)
                {
                    Console.Error.WriteLine("Error: Failed to deserialize scenario definition.");
                    Environment.Exit(1);
                }

                RunScenario(scenario);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"CRITICAL FAILURE: {ex.Message}\n{ex.StackTrace}");
                Environment.Exit(1);
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  SimCore.Runner <scenario.json>");
            Console.WriteLine("  SimCore.Runner seed-explore --seed <int> [--outdir <dir>] [--starCount <int>] [--radius <float>] [--maxHops <int>] [--chokeCapLe <int>] [--maxChokepoints <int>]");
            Console.WriteLine("  SimCore.Runner seed-diff --seedA <int> --seedB <int> [--outdir <dir>] [--starCount <int>] [--radius <float>] [--maxHops <int>] [--chokeCapLe <int>] [--maxChokepoints <int>]");
        }

        private static void RunSeedExplore(string[] args)
        {
            int seed = 0;
            string outDir = Path.Combine("docs", "generated");

            int starCount = SimCore.Gen.GalaxyGenerator.SeedExplorerV0Config.Default.StarCount;
            float radius = SimCore.Gen.GalaxyGenerator.SeedExplorerV0Config.Default.Radius;
            int maxHops = SimCore.Gen.GalaxyGenerator.SeedExplorerV0Config.Default.MaxHops;
            int chokeCapLe = SimCore.Gen.GalaxyGenerator.SeedExplorerV0Config.Default.ChokepointCapLe;
            int maxChokepoints = SimCore.Gen.GalaxyGenerator.SeedExplorerV0Config.Default.MaxChokepoints;

            for (int i = 1; i < args.Length; i++)
            {
                if (string.Equals(args[i], "--seed", StringComparison.Ordinal) && i + 1 < args.Length)
                {
                    seed = int.Parse(args[i + 1]);
                    i++;
                    continue;
                }

                if (string.Equals(args[i], "--outdir", StringComparison.Ordinal) && i + 1 < args.Length)
                {
                    outDir = args[i + 1];
                    i++;
                    continue;
                }

                if (string.Equals(args[i], "--starCount", StringComparison.Ordinal) && i + 1 < args.Length)
                {
                    starCount = int.Parse(args[i + 1]);
                    i++;
                    continue;
                }

                if (string.Equals(args[i], "--radius", StringComparison.Ordinal) && i + 1 < args.Length)
                {
                    radius = float.Parse(args[i + 1], System.Globalization.CultureInfo.InvariantCulture);
                    i++;
                    continue;
                }

                if (string.Equals(args[i], "--maxHops", StringComparison.Ordinal) && i + 1 < args.Length)
                {
                    maxHops = int.Parse(args[i + 1]);
                    i++;
                    continue;
                }

                if (string.Equals(args[i], "--chokeCapLe", StringComparison.Ordinal) && i + 1 < args.Length)
                {
                    chokeCapLe = int.Parse(args[i + 1]);
                    i++;
                    continue;
                }

                if (string.Equals(args[i], "--maxChokepoints", StringComparison.Ordinal) && i + 1 < args.Length)
                {
                    maxChokepoints = int.Parse(args[i + 1]);
                    i++;
                    continue;
                }
            }

            Directory.CreateDirectory(outDir);

            var cfg = SimCore.Gen.GalaxyGenerator.SeedExplorerV0Config.Default with
            {
                StarCount = starCount,
                Radius = radius,
                MaxHops = maxHops,
                ChokepointCapLe = chokeCapLe,
                MaxChokepoints = maxChokepoints
            };

            var kernel = new SimKernel(seed);
            SimCore.Gen.GalaxyGenerator.Generate(kernel.State, starCount: cfg.StarCount, radius: cfg.Radius);

            var topology = SimCore.Gen.GalaxyGenerator.BuildTopologyDump(kernel.State);
            var loops = SimCore.Gen.GalaxyGenerator.BuildEconLoopsReport(kernel.State, seed, cfg);
            var inv = SimCore.Gen.GalaxyGenerator.BuildInvariantsReport(kernel.State, seed, cfg);

            WriteUtf8NoBom(Path.Combine(outDir, "topology_summary.txt"), topology);
            WriteUtf8NoBom(Path.Combine(outDir, "econ_loops.txt"), loops);
            WriteUtf8NoBom(Path.Combine(outDir, "invariants.txt"), inv);
        }

        private static void RunSeedDiff(string[] args)
        {
            int seedA = 0;
            int seedB = 0;
            string outDir = Path.Combine("docs", "generated");

            int starCount = SimCore.Gen.GalaxyGenerator.SeedExplorerV0Config.Default.StarCount;
            float radius = SimCore.Gen.GalaxyGenerator.SeedExplorerV0Config.Default.Radius;
            int maxHops = SimCore.Gen.GalaxyGenerator.SeedExplorerV0Config.Default.MaxHops;
            int chokeCapLe = SimCore.Gen.GalaxyGenerator.SeedExplorerV0Config.Default.ChokepointCapLe;
            int maxChokepoints = SimCore.Gen.GalaxyGenerator.SeedExplorerV0Config.Default.MaxChokepoints;

            for (int i = 1; i < args.Length; i++)
            {
                if (string.Equals(args[i], "--seedA", StringComparison.Ordinal) && i + 1 < args.Length)
                {
                    seedA = int.Parse(args[i + 1]);
                    i++;
                    continue;
                }

                if (string.Equals(args[i], "--seedB", StringComparison.Ordinal) && i + 1 < args.Length)
                {
                    seedB = int.Parse(args[i + 1]);
                    i++;
                    continue;
                }

                if (string.Equals(args[i], "--outdir", StringComparison.Ordinal) && i + 1 < args.Length)
                {
                    outDir = args[i + 1];
                    i++;
                    continue;
                }

                if (string.Equals(args[i], "--starCount", StringComparison.Ordinal) && i + 1 < args.Length)
                {
                    starCount = int.Parse(args[i + 1]);
                    i++;
                    continue;
                }

                if (string.Equals(args[i], "--radius", StringComparison.Ordinal) && i + 1 < args.Length)
                {
                    radius = float.Parse(args[i + 1], System.Globalization.CultureInfo.InvariantCulture);
                    i++;
                    continue;
                }

                if (string.Equals(args[i], "--maxHops", StringComparison.Ordinal) && i + 1 < args.Length)
                {
                    maxHops = int.Parse(args[i + 1]);
                    i++;
                    continue;
                }

                if (string.Equals(args[i], "--chokeCapLe", StringComparison.Ordinal) && i + 1 < args.Length)
                {
                    chokeCapLe = int.Parse(args[i + 1]);
                    i++;
                    continue;
                }

                if (string.Equals(args[i], "--maxChokepoints", StringComparison.Ordinal) && i + 1 < args.Length)
                {
                    maxChokepoints = int.Parse(args[i + 1]);
                    i++;
                    continue;
                }
            }

            Directory.CreateDirectory(outDir);

            var cfg = SimCore.Gen.GalaxyGenerator.SeedExplorerV0Config.Default with
            {
                StarCount = starCount,
                Radius = radius,
                MaxHops = maxHops,
                ChokepointCapLe = chokeCapLe,
                MaxChokepoints = maxChokepoints
            };

            var a = new SimKernel(seedA);
            SimCore.Gen.GalaxyGenerator.Generate(a.State, starCount: cfg.StarCount, radius: cfg.Radius);

            var b = new SimKernel(seedB);
            SimCore.Gen.GalaxyGenerator.Generate(b.State, starCount: cfg.StarCount, radius: cfg.Radius);

            var topoDiff = SimCore.Gen.GalaxyGenerator.BuildTopologyDiffReport(a.State, seedA, b.State, seedB);
            var loopsDiff = SimCore.Gen.GalaxyGenerator.BuildLoopsDiffReport(a.State, seedA, b.State, seedB, cfg);

            WriteUtf8NoBom(Path.Combine(outDir, "diff_topology.txt"), topoDiff);
            WriteUtf8NoBom(Path.Combine(outDir, "diff_loops.txt"), loopsDiff);
        }

        private static void WriteUtf8NoBom(string path, string contents)
        {
            // Deterministic: UTF-8 without BOM, no timestamps, caller provides stable content.
            var enc = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            File.WriteAllText(path, contents, enc);
        }

        private static string? TryLoadHostTweaksJsonV0()
        {
            // Host-provided tweaks (runner surface):
            // - Path is fixed and repo-relative for determinism.
            // - Missing or invalid falls back to defaults by returning null.
            // - Enforces UTF-8 no BOM.
            var path = Path.Combine("Data", "Tweaks", "tweaks_v0.json");
            if (!File.Exists(path)) return null;

            try
            {
                var bytes = File.ReadAllBytes(path);

                // Reject UTF-8 BOM explicitly (contract: UTF-8 no BOM).
                if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                    return null;

                // Strict UTF-8 decode (throws on invalid byte sequences).
                var utf8Strict = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
                var text = utf8Strict.GetString(bytes);

                // Deterministic parse gate: must be a JSON object (otherwise treat as invalid).
                using var doc = JsonDocument.Parse(text);
                if (doc.RootElement.ValueKind != JsonValueKind.Object) return null;

                return text;
            }
            catch
            {
                // Failure-safe determinism: invalid file falls back to defaults.
                return null;
            }
        }

        private static void RunScenario(ScenarioDefinition scenario)
        {
            Console.WriteLine($"--- RUNNING SCENARIO: {scenario.ScenarioId} ---");
            Console.WriteLine($"Seed: {scenario.InitialSeed} | Duration: {scenario.StopAtDay} Days");

            var hostTweaksJson = TryLoadHostTweaksJsonV0();
            var kernel = new SimKernel(scenario.InitialSeed, tweakConfigJsonOverride: hostTweaksJson);

            // Transcript surface: record effective tweaks hash deterministically.
            // Missing/invalid host file results in stable defaults and stable hash.
            Console.WriteLine($"TweaksHash: {kernel.State.TweaksHash}");

            var startTime = DateTime.UtcNow;

            for (int day = 0; day < scenario.StopAtDay; day++)
            {
                // Future: Inject Player Commands from scenario.CommandScript here
                kernel.Step();
            }

            var duration = DateTime.UtcNow - startTime;
            Console.WriteLine($"--- COMPLETED in {duration.TotalMilliseconds:F2} ms ---");
            Console.WriteLine($"Final Tick: {kernel.State.Tick}");
        }
    }
}
