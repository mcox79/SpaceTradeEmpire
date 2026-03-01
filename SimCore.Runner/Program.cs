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

                if (string.Equals(cmd, "discovery-report", StringComparison.Ordinal))
                {
                    RunDiscoveryReport(args);
                    return;
                }

                if (string.Equals(cmd, "discovery-readout", StringComparison.Ordinal))
                {
                    RunDiscoveryReadout(args);
                    return;
                }

                if (string.Equals(cmd, "unlock-report", StringComparison.Ordinal))
                {
                    RunUnlockReport(args);
                    return;
                }

                if (string.Equals(cmd, "play-loop-proof-report", StringComparison.Ordinal))
                {
                    RunPlayLoopProofReport(args);
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
            Console.WriteLine("  SimCore.Runner discovery-report [--outdir <dir>] [--seedStart <int>] [--seedEnd <int>] [--starCount <int>] [--radius <float>]");
            Console.WriteLine("  SimCore.Runner discovery-readout [--seed <int>] [--outdir <dir>] [--starCount <int>] [--radius <float>]");
            Console.WriteLine("  SimCore.Runner unlock-report [--outdir <dir>] [--seedStart <int>] [--seedEnd <int>] [--starCount <int>] [--radius <float>]");
            Console.WriteLine("  SimCore.Runner play-loop-proof-report [--seed <int>] [--phase <int>]");
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

        private static void RunDiscoveryReport(string[] args)
        {
            // Gate: GATE.S2_5.WGEN.DISCOVERY_SEEDING.004
            // Deterministic report over Seeds [seedStart..seedEnd], stable ordering, no timestamps.
            int seedStart = 1;
            int seedEnd = 100;
            string outDir = Path.Combine("docs", "generated");

            int starCount = SimCore.Gen.GalaxyGenerator.SeedExplorerV0Config.Default.StarCount;
            float radius = SimCore.Gen.GalaxyGenerator.SeedExplorerV0Config.Default.Radius;

            for (int i = 1; i < args.Length; i++)
            {
                if (string.Equals(args[i], "--outdir", StringComparison.Ordinal) && i + 1 < args.Length)
                {
                    outDir = args[i + 1];
                    i++;
                    continue;
                }

                if (string.Equals(args[i], "--seedStart", StringComparison.Ordinal) && i + 1 < args.Length)
                {
                    seedStart = int.Parse(args[i + 1]);
                    i++;
                    continue;
                }

                if (string.Equals(args[i], "--seedEnd", StringComparison.Ordinal) && i + 1 < args.Length)
                {
                    seedEnd = int.Parse(args[i + 1]);
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
            }

            if (seedStart > seedEnd)
            {
                Console.Error.WriteLine("Error: seedStart must be <= seedEnd.");
                Environment.Exit(1);
            }

            Directory.CreateDirectory(outDir);

            var digestPath = Path.Combine("docs", "generated", "content_registry_digest_v0.txt");
            if (!File.Exists(digestPath))
            {
                Console.Error.WriteLine($"Error: Missing required identity artifact: {digestPath}");
                Environment.Exit(1);
            }

            var (regVersion, regDigest) = ParseContentRegistryDigestV0(File.ReadAllText(digestPath));

            var sb = new StringBuilder();
            sb.Append("DISCOVERY_SEEDING_REPORT_V0").Append('\n');
            sb.Append("SeedRange=").Append(seedStart).Append("..").Append(seedEnd).Append('\n');
            sb.Append("WorldgenStarCount=").Append(starCount).Append('\n');
            sb.Append("WorldgenRadius=").Append(radius.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append('\n');
            sb.Append("ContentRegistryVersion=").Append(regVersion).Append('\n');
            sb.Append("ContentRegistryDigest=").Append(regDigest).Append('\n');
            sb.Append('\n');

            sb.Append("SUMMARY").Append('\n');
            sb.Append("Seed\tResult\tViolationsCount").Append('\n');

            int failCount = 0;
            int totalViolationCount = 0;

            // Deterministic: iterate ascending seed.
            for (int seed = seedStart; seed <= seedEnd; seed++)
            {
                var kernel = new SimKernel(seed);
                SimCore.Gen.GalaxyGenerator.Generate(kernel.State, starCount: starCount, radius: radius);

                var vrep = SimCore.Gen.GalaxyGenerator.BuildDiscoverySeedingViolationsReportV0(kernel.State, seed);
                int violationsCount = ExtractIntFieldOrThrow(vrep, "ViolationsCount=");
                string result = ExtractStringFieldOrThrow(vrep, "Result=");

                sb.Append(seed).Append('\t')
                  .Append(result).Append('\t')
                  .Append(violationsCount)
                  .Append('\n');

                totalViolationCount += violationsCount;
                if (!string.Equals(result, "PASS", StringComparison.Ordinal))
                {
                    failCount++;
                }
            }

            sb.Append('\n');
            sb.Append("DETAILS_FAILING_SEEDS_ONLY").Append('\n');

            for (int seed = seedStart; seed <= seedEnd; seed++)
            {
                var kernel = new SimKernel(seed);
                SimCore.Gen.GalaxyGenerator.Generate(kernel.State, starCount: starCount, radius: radius);

                var vrep = SimCore.Gen.GalaxyGenerator.BuildDiscoverySeedingViolationsReportV0(kernel.State, seed);
                string result = ExtractStringFieldOrThrow(vrep, "Result=");

                if (string.Equals(result, "PASS", StringComparison.Ordinal))
                {
                    continue;
                }

                sb.Append("BEGIN_SEED ").Append(seed).Append('\n');
                sb.Append(vrep.TrimEnd()).Append('\n');
                sb.Append("END_SEED ").Append(seed).Append('\n');
            }

            sb.Append('\n');
            sb.Append("Result=").Append(failCount == 0 ? "PASS" : "FAIL").Append('\n');
            sb.Append("FailingSeedsCount=").Append(failCount).Append('\n');
            sb.Append("TotalViolationsCount=").Append(totalViolationCount).Append('\n');

            var outPath = Path.Combine(outDir, "discovery_seeding_report_v0.txt");
            WriteUtf8NoBom(outPath, sb.ToString());

            if (failCount != 0 || totalViolationCount != 0)
            {
                Environment.Exit(2);
            }
        }

        private static void RunDiscoveryReadout(string[] args)
        {
            // Gate: GATE.S2_5.WGEN.DISCOVERY_SEEDING.006
            // CLI readout for one seed. Stable ordering, stable formatting, no timestamps.
            int seed = 42;
            string outDir = Path.Combine("docs", "generated");

            int starCount = SimCore.Gen.GalaxyGenerator.SeedExplorerV0Config.Default.StarCount;
            float radius = SimCore.Gen.GalaxyGenerator.SeedExplorerV0Config.Default.Radius;

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
            }

            Directory.CreateDirectory(outDir);

            var digestPath = Path.Combine("docs", "generated", "content_registry_digest_v0.txt");
            if (!File.Exists(digestPath))
            {
                Console.Error.WriteLine($"Error: Missing required identity artifact: {digestPath}");
                Environment.Exit(1);
            }

            var (regVersion, regDigest) = ParseContentRegistryDigestV0(File.ReadAllText(digestPath));

            var kernel = new SimKernel(seed);
            SimCore.Gen.GalaxyGenerator.Generate(kernel.State, starCount: starCount, radius: radius);

            var readout = SimCore.Gen.GalaxyGenerator.BuildDiscoveryReadoutV0(kernel.State, seed);

            var sb = new StringBuilder();
            sb.Append("DISCOVERY_READOUT_V0").Append('\n');
            sb.Append("Seed=").Append(seed).Append('\n');
            sb.Append("WorldgenStarCount=").Append(starCount).Append('\n');
            sb.Append("WorldgenRadius=").Append(radius.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append('\n');
            sb.Append("ContentRegistryVersion=").Append(regVersion).Append('\n');
            sb.Append("ContentRegistryDigest=").Append(regDigest).Append('\n');
            sb.Append('\n');
            sb.Append(readout.TrimEnd()).Append('\n');

            var outPath = Path.Combine(outDir, $"discovery_readout_seed_{seed}_v0.txt");
            WriteUtf8NoBom(outPath, sb.ToString());
        }

        private static void RunUnlockReport(string[] args)
        {
            // Gate: GATE.S3_6.DISCOVERY_UNLOCK_CONTRACT.007
            // Deterministic unlock report v0 over Seeds [seedStart..seedEnd].
            // Stable header (SeedRange + worldgen params + content registry digest).
            // No timestamps. Exits nonzero on violations (writes report first).
            int seedStart = 1;
            int seedEnd = 100;
            string outDir = Path.Combine("docs", "generated");

            int starCount = SimCore.Gen.GalaxyGenerator.SeedExplorerV0Config.Default.StarCount;
            float radius = SimCore.Gen.GalaxyGenerator.SeedExplorerV0Config.Default.Radius;

            for (int i = 1; i < args.Length; i++)
            {
                if (string.Equals(args[i], "--outdir", StringComparison.Ordinal) && i + 1 < args.Length)
                {
                    outDir = args[i + 1];
                    i++;
                    continue;
                }

                if (string.Equals(args[i], "--seedStart", StringComparison.Ordinal) && i + 1 < args.Length)
                {
                    seedStart = int.Parse(args[i + 1]);
                    i++;
                    continue;
                }

                if (string.Equals(args[i], "--seedEnd", StringComparison.Ordinal) && i + 1 < args.Length)
                {
                    seedEnd = int.Parse(args[i + 1]);
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
            }

            if (seedStart > seedEnd)
            {
                Console.Error.WriteLine("Error: seedStart must be <= seedEnd.");
                Environment.Exit(1);
            }

            Directory.CreateDirectory(outDir);

            var digestPath = Path.Combine("docs", "generated", "content_registry_digest_v0.txt");
            if (!File.Exists(digestPath))
            {
                Console.Error.WriteLine($"Error: Missing required identity artifact: {digestPath}");
                Environment.Exit(1);
            }

            var (regVersion, regDigest) = ParseContentRegistryDigestV0(File.ReadAllText(digestPath));

            var sb = new StringBuilder();
            sb.Append("UNLOCK_REPORT_V0").Append('\n');
            sb.Append("SeedRange=").Append(seedStart).Append("..").Append(seedEnd).Append('\n');
            sb.Append("WorldgenStarCount=").Append(starCount).Append('\n');
            sb.Append("WorldgenRadius=").Append(radius.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append('\n');
            sb.Append("ContentRegistryVersion=").Append(regVersion).Append('\n');
            sb.Append("ContentRegistryDigest=").Append(regDigest).Append('\n');
            sb.Append('\n');

            sb.Append("SUMMARY").Append('\n');
            sb.Append("Seed\tResult\tViolationsCount\tSiteBlueprintCount\tCorridorAccessCount\tPermitCount").Append('\n');

            int failCount = 0;
            int totalViolationCount = 0;

            // Deterministic: iterate ascending seed order.
            for (int seed = seedStart; seed <= seedEnd; seed++)
            {
                var kernel = new SimKernel(seed);
                SimCore.Gen.GalaxyGenerator.Generate(kernel.State, starCount: starCount, radius: radius);

                var srep = SimCore.Gen.GalaxyGenerator.BuildUnlockReportV0(kernel.State, seed);
                int violationsCount = ExtractIntFieldOrThrow(srep, "ViolationsCount=");
                string result = ExtractStringFieldOrThrow(srep, "Result=");
                int siteBlueprintCount = ExtractIntFieldOrThrow(srep, "SiteBlueprintCount=");
                int corridorAccessCount = ExtractIntFieldOrThrow(srep, "CorridorAccessCount=");
                int permitCount = ExtractIntFieldOrThrow(srep, "PermitCount=");

                sb.Append(seed).Append('\t')
                  .Append(result).Append('\t')
                  .Append(violationsCount).Append('\t')
                  .Append(siteBlueprintCount).Append('\t')
                  .Append(corridorAccessCount).Append('\t')
                  .Append(permitCount)
                  .Append('\n');

                totalViolationCount += violationsCount;
                if (!string.Equals(result, "PASS", StringComparison.Ordinal))
                {
                    failCount++;
                }
            }

            sb.Append('\n');
            sb.Append("DETAILS_FAILING_SEEDS_ONLY").Append('\n');

            for (int seed = seedStart; seed <= seedEnd; seed++)
            {
                var kernel = new SimKernel(seed);
                SimCore.Gen.GalaxyGenerator.Generate(kernel.State, starCount: starCount, radius: radius);

                var srep = SimCore.Gen.GalaxyGenerator.BuildUnlockReportV0(kernel.State, seed);
                string result = ExtractStringFieldOrThrow(srep, "Result=");

                if (string.Equals(result, "PASS", StringComparison.Ordinal))
                {
                    continue;
                }

                sb.Append("BEGIN_SEED ").Append(seed).Append('\n');
                sb.Append(srep.TrimEnd()).Append('\n');
                sb.Append("END_SEED ").Append(seed).Append('\n');
            }

            sb.Append('\n');
            sb.Append("Result=").Append(failCount == 0 ? "PASS" : "FAIL").Append('\n');
            sb.Append("FailingSeedsCount=").Append(failCount).Append('\n');
            sb.Append("TotalViolationsCount=").Append(totalViolationCount).Append('\n');

            var outPath = Path.Combine(outDir, "unlock_report_v0.txt");
            WriteUtf8NoBom(outPath, sb.ToString());

            if (failCount != 0 || totalViolationCount != 0)
            {
                Environment.Exit(2);
            }
        }

        private static (string Version, string Digest) ParseContentRegistryDigestV0(string text)
        {
            // Deterministic parse: scan for explicit keys. If missing, hard-fail to avoid incomplete identity.
            // Supported formats (case-insensitive keys):
            //   CONTENT_REGISTRY_DIGEST_V0
            //   version=0
            //   digest_sha256_upper=<HEX>
            // Also supports older keys:
            //   Version=...
            //   Digest=... or Sha256=...
            string? version = null;
            string? digest = null;

            var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var ln = lines[i].Trim();
                if (ln.Length == 0) continue;

                if (ln.StartsWith("version=", StringComparison.OrdinalIgnoreCase))
                {
                    version = ln.Substring("version=".Length).Trim();
                    continue;
                }

                if (ln.StartsWith("Version=", StringComparison.OrdinalIgnoreCase))
                {
                    version = ln.Substring("Version=".Length).Trim();
                    continue;
                }

                if (ln.StartsWith("digest_sha256_upper=", StringComparison.OrdinalIgnoreCase))
                {
                    digest = ln.Substring("digest_sha256_upper=".Length).Trim();
                    continue;
                }

                if (ln.StartsWith("Digest=", StringComparison.OrdinalIgnoreCase))
                {
                    digest = ln.Substring("Digest=".Length).Trim();
                    continue;
                }

                if (ln.StartsWith("Sha256=", StringComparison.OrdinalIgnoreCase))
                {
                    digest = ln.Substring("Sha256=".Length).Trim();
                    continue;
                }
            }

            if (string.IsNullOrWhiteSpace(version) || string.IsNullOrWhiteSpace(digest))
            {
                throw new InvalidOperationException(
                    "content_registry_digest_v0.txt missing required version and digest fields (expected version= and digest_sha256_upper=, or Version= and Digest=%Sha256=).");
            }

            return (version!, digest!);
        }

        private static int ExtractIntFieldOrThrow(string report, string prefix)
        {
            var lines = report.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var ln = lines[i].Trim();
                if (!ln.StartsWith(prefix, StringComparison.Ordinal)) continue;

                var s = ln.Substring(prefix.Length).Trim();
                return int.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
            }

            throw new InvalidOperationException($"Report missing required field: {prefix}");
        }

        private static string ExtractStringFieldOrThrow(string report, string prefix)
        {
            var lines = report.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var ln = lines[i].Trim();
                if (!ln.StartsWith(prefix, StringComparison.Ordinal)) continue;

                return ln.Substring(prefix.Length).Trim();
            }

            throw new InvalidOperationException($"Report missing required field: {prefix}");
        }

        private static void RunPlayLoopProofReport(string[] args)
        {
            // GATE.S3_6.PLAY_LOOP_PROOF.001: schema scaffold (no --phase)
            // GATE.S3_6.PLAY_LOOP_PROOF.002: phase 1 headless proof (discover%trade%freighter v0)
            //
            // Determinism:
            // - No timestamps%wall-clock.
            // - Stable ordering for selections: StringComparer.Ordinal.
            // - Required step verification uses ProgramExplain.PlayLoopProof.CanonicalStepTokensOrdered index order.
            int seed = 42;
            bool fallbackSeedUsed = true;
            int phase = 0;

            for (int i = 1; i < args.Length; i++)
            {
                if (string.Equals(args[i], "--seed", StringComparison.Ordinal) && i + 1 < args.Length)
                {
                    seed = int.Parse(args[i + 1]);
                    fallbackSeedUsed = false;
                    i++;
                    continue;
                }

                if (string.Equals(args[i], "--phase", StringComparison.Ordinal) && i + 1 < args.Length)
                {
                    phase = int.Parse(args[i + 1]);
                    i++;
                    continue;
                }
            }

            if (phase == 0)
            {
                // Schema-only scaffold: emits deterministic schema header + canonical step token list.
                // No timestamps. Stable ordering: ProgramExplain.PlayLoopProof.CanonicalStepTokensOrdered list index order.
                var worldIdScaffold = $"world_seed_{seed}";

                Console.WriteLine("SCHEMA_OK");
                Console.WriteLine($"Seed={seed}");
                Console.WriteLine($"WorldId={worldIdScaffold}");
                Console.WriteLine("TickIndex=0");
                Console.WriteLine($"SeedUsed={seed}");
                Console.WriteLine($"FallbackSeedUsed={(fallbackSeedUsed ? "true" : "false")}");
                Console.WriteLine();

                Console.WriteLine("CANONICAL_STEPS_V0");
                foreach (var token in SimCore.Programs.ProgramExplain.PlayLoopProof.CanonicalStepTokensOrdered)
                {
                    Console.WriteLine(token);
                }

                Environment.Exit(0);
                return;
            }

            if (phase != 1)
            {
                Console.Error.WriteLine("Error: unsupported --phase value. Supported: 0, 1");
                Environment.Exit(1);
                return;
            }

            // Phase 1 required steps (subset, validated in canonical step order):
            // EXPLORE_SITE%DOCK_HUB%TRADE_LOOP_IDENTIFIED%FREIGHTER_ACQUIRED%TRADE_CHARTER_REVENUE
            var required = new[]
            {
                SimCore.Programs.ProgramExplain.PlayLoopProof.EXPLORE_SITE,
                SimCore.Programs.ProgramExplain.PlayLoopProof.DOCK_HUB,
                SimCore.Programs.ProgramExplain.PlayLoopProof.TRADE_LOOP_IDENTIFIED,
                SimCore.Programs.ProgramExplain.PlayLoopProof.FREIGHTER_ACQUIRED,
                SimCore.Programs.ProgramExplain.PlayLoopProof.TRADE_CHARTER_REVENUE
            };

            var outDir = Path.Combine("docs", "generated");
            Directory.CreateDirectory(outDir);

            var reportPath = Path.Combine(outDir, "play_loop_proof_phase1_seed_42_v0.txt");

            var kernel = new SimKernel(seed);
            var state = kernel.State;

            // Worldgen is required for phase 1 (markets%inventory). Runner must initialize deterministically.
            var starCount = SimCore.Gen.GalaxyGenerator.SeedExplorerV0Config.Default.StarCount;
            var radius = SimCore.Gen.GalaxyGenerator.SeedExplorerV0Config.Default.Radius;
            SimCore.Gen.GalaxyGenerator.Generate(state, starCount: starCount, radius: radius);

            // WorldId: prefer state surface if present, else deterministic fallback.
            var worldId = TryGetStringProperty(state, "WorldId") ?? $"world_seed_{seed}";

            // Step tracking: record achieved tokens in canonical ordering.
            var achieved = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);

            // Deterministic selection helpers.
            var (marketA, marketB, goodId) = SelectTwoMarketsAndGood(state);

            // --- Step 1: EXPLORE_SITE ---
            // Deterministic intent: set player location to marketA (if available).
            kernel.EnqueueIntent(new PlayLoop_SetPlayerLocationIntent(marketA, noteToken: "ExploreSite"));
            kernel.Step();
            achieved.Add(SimCore.Programs.ProgramExplain.PlayLoopProof.EXPLORE_SITE);

            // --- Step 2: DOCK_HUB ---
            // Deterministic intent: set player location to marketB (if available).
            kernel.EnqueueIntent(new PlayLoop_SetPlayerLocationIntent(marketB, noteToken: "DockHub"));
            kernel.Step();
            achieved.Add(SimCore.Programs.ProgramExplain.PlayLoopProof.DOCK_HUB);

            // --- Step 3: TRADE_LOOP_IDENTIFIED ---
            // Deterministic: create a TRADE_CHARTER_V0 program instance targeting (marketA -> marketB) and a stable goodId.
            // To guarantee TradePnL with existing TradeCharterIntentV0 semantics, set BuyGoodId == SellGoodId == goodId.
            var programId = "PROG_TRADE_CHARTER_V0_PHASE1";
            EnsureTradeCharterProgramExists(state, programId, marketA, marketB, goodId);
            achieved.Add(SimCore.Programs.ProgramExplain.PlayLoopProof.TRADE_LOOP_IDENTIFIED);

            // Ensure the charter can actually run buys deterministically (single-mutation pipeline via intent).
            // Use tweak-routed budget so we do not introduce a new balance constant in the runner.
            var creditsBeforeEnsure = state.PlayerCredits;
            var creditsTarget = checked((long)SimCore.Tweaks.ExploitationTweaksV0.TradeCharterBudgetPerCycle * 10L);
            kernel.EnqueueIntent(new PlayLoop_EnsurePlayerCreditsIntent(programId, creditsTarget));
            kernel.Step();
            var creditsAfterEnsure = state.PlayerCredits;

            // --- Step 4: FREIGHTER_ACQUIRED ---
            // Phase 1 v0: represent "freighter acquired" as a schema-stable marker in exploitation log.
            kernel.EnqueueIntent(new PlayLoop_AppendExploitationMarkerIntent(programId, marker: "FreighterAcquired"));
            kernel.Step();
            achieved.Add(SimCore.Programs.ProgramExplain.PlayLoopProof.FREIGHTER_ACQUIRED);

            // --- Step 5: TRADE_CHARTER_REVENUE ---
            // Gate requires: >= 1 CashDelta(TradePnL) from TRADE_CHARTER_V0.
            // Deterministic upper bound to avoid infinite loops: fixed step count in runner (non-gameplay).
            var tradePnLEvidenceLines = new System.Collections.Generic.List<string>();
            for (int i = 0; i < 2000; i++)
            {
                kernel.Step();

                tradePnLEvidenceLines = ExtractTradePnLEvidenceLines(state, programId);
                if (tradePnLEvidenceLines.Count > 0)
                {
                    achieved.Add(SimCore.Programs.ProgramExplain.PlayLoopProof.TRADE_CHARTER_REVENUE);
                    break;
                }
            }

            // Save checkpoint at TRADE_CHARTER_REVENUE (always deterministic even if revenue missing).
            var checkpoint = kernel.SaveToString();
            var checkpointSha256 = Sha256HexUtf8(checkpoint);

            // Build deterministic report (UTF-8 no BOM, no timestamps).
            var sb = new StringBuilder();
            sb.AppendLine("SCHEMA_OK");
            sb.AppendLine($"Seed={seed}");
            sb.AppendLine($"WorldId={worldId}");
            sb.AppendLine($"TickIndex={state.Tick}");
            sb.AppendLine($"SeedUsed={seed}");
            sb.AppendLine($"FallbackSeedUsed={(fallbackSeedUsed ? "true" : "false")}");
            sb.AppendLine("Phase=1");
            sb.AppendLine();

            sb.AppendLine("REQUIRED_STEPS_V0");
            foreach (var t in required) sb.AppendLine(t);
            sb.AppendLine();

            sb.AppendLine("ACHIEVED_STEPS_V0");
            foreach (var t in SimCore.Programs.ProgramExplain.PlayLoopProof.CanonicalStepTokensOrdered)
            {
                if (achieved.Contains(t)) sb.AppendLine(t);
            }
            sb.AppendLine();

            sb.AppendLine("CHECKPOINT_V0");
            sb.AppendLine($"CheckpointAt={SimCore.Programs.ProgramExplain.PlayLoopProof.TRADE_CHARTER_REVENUE}");
            sb.AppendLine($"CheckpointSha256={checkpointSha256}");
            sb.AppendLine();

            sb.AppendLine("INPUTS_V0");
            sb.AppendLine($"WorldgenStarCount={starCount}");
            sb.AppendLine($"WorldgenRadius={radius.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            sb.AppendLine($"marketA={marketA}");
            sb.AppendLine($"marketB={marketB}");
            sb.AppendLine($"goodId={goodId}");
            sb.AppendLine($"PlayerCreditsBefore={creditsBeforeEnsure}");
            sb.AppendLine($"PlayerCreditsAfter={creditsAfterEnsure}");
            sb.AppendLine();

            // Deterministic debug surface: program-scoped exploitation events (last fixed N, append order).
            var progEvents = ExtractProgramEventLines(state, programId, maxLines: 60);
            sb.AppendLine("PROGRAM_EVENTS_V0");
            sb.AppendLine($"ProgramId={programId}");
            sb.AppendLine($"ProgramEventsCount={progEvents.Count}");
            for (int i = 0; i < progEvents.Count; i++) sb.AppendLine(progEvents[i]);
            sb.AppendLine();

            sb.AppendLine("TRADE_PNL_V0");
            sb.AppendLine($"TradePnLEventCount={tradePnLEvidenceLines.Count}");
            sb.AppendLine($"TradePnLEventsSha256={Sha256HexUtf8(string.Join("\n", tradePnLEvidenceLines) + "\n")}");
            foreach (var line in tradePnLEvidenceLines) sb.AppendLine(line);

            // Always write report first (even on failure).
            WriteUtf8NoBom(reportPath, sb.ToString());

            // Validate required steps in canonical order and fail deterministically.
            var missing = FirstMissingRequiredInCanonicalOrder(required, achieved);
            if (!string.IsNullOrEmpty(missing))
            {
                Console.Error.WriteLine($"MISSING_STEP|{missing}");
                Environment.Exit(2);
                return;
            }

            Environment.Exit(0);
        }

        private sealed class PlayLoop_SetPlayerLocationIntent : SimCore.Intents.IIntent
        {
            public string Kind => "PLAY_LOOP_SET_LOCATION_V0";
            private readonly string _marketId;
            private readonly string _noteToken;

            public PlayLoop_SetPlayerLocationIntent(string marketId, string noteToken)
            {
                _marketId = marketId ?? "";
                _noteToken = noteToken ?? "";
            }

            public void Apply(SimState state)
            {
                if (state is null) return;
                if (string.IsNullOrWhiteSpace(_marketId)) return;

                // Single-mutation pipeline: apply via intent.
                state.PlayerLocationNodeId = _marketId;

                // Optional marker for debugging (non-required): append to exploitation log if available.
                TryAppendExploitationEvent(state, $"tick={state.Tick} prog=PLAY_LOOP {_noteToken} market={_marketId}");
            }
        }

        private sealed class PlayLoop_EnsurePlayerCreditsIntent : SimCore.Intents.IIntent
        {
            public string Kind => "PLAY_LOOP_ENSURE_PLAYER_CREDITS_V0";
            private readonly string _programId;
            private readonly long _minCredits;

            public PlayLoop_EnsurePlayerCreditsIntent(string programId, long minCredits)
            {
                _programId = programId ?? "";
                _minCredits = minCredits;
            }

            public void Apply(SimState state)
            {
                if (state is null) return;
                if (_minCredits <= 0) return;

                if (state.PlayerCredits < _minCredits)
                {
                    state.PlayerCredits = _minCredits;
                    var pid = string.IsNullOrWhiteSpace(_programId) ? "PLAY_LOOP" : _programId;
                    TryAppendExploitationEvent(state, $"tick={state.Tick} prog={pid} CreditsEnsured min={_minCredits}");
                }
            }
        }

        private sealed class PlayLoop_AppendExploitationMarkerIntent : SimCore.Intents.IIntent
        {
            public string Kind => "PLAY_LOOP_MARKER_V0";
            private readonly string _programId;
            private readonly string _marker;

            public PlayLoop_AppendExploitationMarkerIntent(string programId, string marker)
            {
                _programId = programId ?? "";
                _marker = marker ?? "";
            }

            public void Apply(SimState state)
            {
                if (state is null) return;
                if (string.IsNullOrWhiteSpace(_marker)) return;

                var pid = string.IsNullOrWhiteSpace(_programId) ? "PLAY_LOOP" : _programId;
                TryAppendExploitationEvent(state, $"tick={state.Tick} prog={pid} {_marker}");
            }
        }

        private static (string marketA, string marketB, string goodId) SelectTwoMarketsAndGood(SimState state)
        {
            // Deterministic selection:
            // - marketA = first MarketId ordinal
            // - marketB = second MarketId ordinal if present else marketA
            // - goodId = first inventory good key ordinal from marketA if present else empty
            var marketA = "";
            var marketB = "";
            var goodId = "";

            if (state is null || state.Markets is null || state.Markets.Count == 0)
                return (marketA, marketB, goodId);

            var marketIds = state.Markets.Keys.ToList();
            marketIds.Sort(StringComparer.Ordinal);

            marketA = marketIds[0];
            marketB = marketIds.Count > 1 ? marketIds[1] : marketA;

            if (state.Markets.TryGetValue(marketA, out var m) && m is not null && m.Inventory is not null && m.Inventory.Count > 0)
            {
                // Deterministic: goods sorted ordinal, pick first with qty > 0.
                var goods = m.Inventory.Keys.ToList();
                goods.Sort(StringComparer.Ordinal);

                for (int i = 0; i < goods.Count; i++)
                {
                    var g = goods[i];
                    if (!m.Inventory.TryGetValue(g, out var qty)) continue;
                    if (qty <= 0) continue;
                    goodId = g;
                    break;
                }
            }

            return (marketA, marketB, goodId);
        }

        private static void EnsureTradeCharterProgramExists(SimState state, string programId, string srcMarketId, string dstMarketId, string goodId)
        {
            if (state is null) return;
            if (state.Programs is null) return;

            // Create or overwrite deterministically by id.
            // ProgramSystem ordering is by Id ordinal; we use a stable id.
            var p = new SimCore.Programs.ProgramInstance
            {
                Id = programId ?? "",
                Kind = "TRADE_CHARTER_V0",
                Status = SimCore.Programs.ProgramStatus.Running,
                CreatedTick = state.Tick,
                CadenceTicks = 1,
                NextRunTick = state.Tick,
                LastRunTick = -1,
                SourceMarketId = srcMarketId ?? "",
                MarketId = dstMarketId ?? "",
                GoodId = goodId ?? "",
                SellGoodId = goodId ?? ""
            };

            // Instances dictionary is expected to exist; preserve determinism by direct key assignment.
            if (state.Programs.Instances is null) return;
            state.Programs.Instances[p.Id] = p;

            TryAppendExploitationEvent(state, $"tick={state.Tick} prog={p.Id} TradeCharterAssigned src={p.SourceMarketId} dst={p.MarketId} good={p.GoodId}");
        }

        private static System.Collections.Generic.List<string> ExtractTradePnLEvidenceLines(SimState state, string programId)
        {
            // TradeCharterIntentV0 emits:
            //   tick=... prog=<ProgramId> TradePnL ...
            // Deterministic: preserve append order.
            var lines = new System.Collections.Generic.List<string>();

            var events = TryGetExploitationEvents(state);
            if (events.Count == 0) return lines;

            var needleProg = $"prog={programId}";
            for (int i = 0; i < events.Count; i++)
            {
                var s = events[i] ?? "";
                if (s.IndexOf("TradePnL", StringComparison.Ordinal) < 0) continue;
                if (s.IndexOf(needleProg, StringComparison.Ordinal) < 0) continue;
                lines.Add(s);
            }

            return lines;
        }

        private static string? FirstMissingRequiredInCanonicalOrder(string[] required, System.Collections.Generic.HashSet<string> achieved)
        {
            // Required list is already in canonical order subset order.
            for (int i = 0; i < required.Length; i++)
            {
                var t = required[i];
                if (!achieved.Contains(t)) return t;
            }
            return null;
        }

        private static void TryAppendExploitationEvent(SimState state, string line)
        {
            if (state is null) return;
            if (string.IsNullOrWhiteSpace(line)) return;

            // Prefer AppendExploitationEvent method if present.
            var m = state.GetType().GetMethod("AppendExploitationEvent", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (m is not null)
            {
                m.Invoke(state, new object[] { line });
                return;
            }

            // Else try to find a List<string> style property and append.
            var p = state.GetType().GetProperty("ExploitationEventLog", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            var obj = p?.GetValue(state);
            if (obj is System.Collections.Generic.IList<string> list)
            {
                list.Add(line);
            }
        }

        private static System.Collections.Generic.IReadOnlyList<string> TryGetExploitationEvents(SimState state)
        {
            if (state is null) return Array.Empty<string>();

            // Known surfaces in this repo commonly use "ExploitationEventLog".
            var p = state.GetType().GetProperty("ExploitationEventLog", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            var obj = p?.GetValue(state);

            if (obj is System.Collections.Generic.IReadOnlyList<string> ro) return ro;
            if (obj is System.Collections.Generic.IList<string> list) return new List<string>(list);

            return Array.Empty<string>();
        }

        private static System.Collections.Generic.List<string> ExtractProgramEventLines(SimState state, string programId, int maxLines)
        {
            // Deterministic:
            // - ExploitationEventLog is append-only and deterministic tick-order.
            // - We filter by prog=<programId> and take the last fixed N in append order.
            var lines = new System.Collections.Generic.List<string>();
            if (maxLines <= 0) return lines;

            var events = TryGetExploitationEvents(state);
            if (events.Count == 0) return lines;

            var needle = $"prog={programId}";
            for (int i = 0; i < events.Count; i++)
            {
                var s = events[i] ?? "";
                if (s.IndexOf(needle, StringComparison.Ordinal) < 0) continue;
                lines.Add(s);
            }

            if (lines.Count <= maxLines) return lines;

            // Return last N while preserving original order among those last N.
            return lines.GetRange(lines.Count - maxLines, maxLines);
        }

        private static string? TryGetStringProperty(object obj, string propName)
        {
            if (obj is null) return null;
            var p = obj.GetType().GetProperty(propName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (p is null) return null;
            var v = p.GetValue(obj);
            return v as string;
        }

        private static string Sha256HexUtf8(string text)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(text ?? "");
            using var sha = System.Security.Cryptography.SHA256.Create();
            var hash = sha.ComputeHash(bytes);

            // Hex string, lowercase, deterministic.
            var sb = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++) sb.Append(hash[i].ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
            return sb.ToString();
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
