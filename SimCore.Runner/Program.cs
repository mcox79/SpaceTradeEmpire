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
