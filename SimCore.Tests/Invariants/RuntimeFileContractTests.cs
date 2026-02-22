using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using NUnit.Framework;

namespace SimCore.Tests.Invariants;

[TestFixture]
public sealed class RuntimeFileContractTests
{
    [Test]
    public void RepoHealth_Scan_MustPass_V0()
    {
        var repoRoot = LocateRepoRootContainingSimCore();
        var reportPath = Path.Combine(repoRoot, "docs", "generated", "repo_health_report_v0.txt");

        var result = RunRepoHealthScan(repoRoot, reportPath);

        Assert.That(result.Violations.Count, Is.EqualTo(0),
            "Repo health violations:\n" + string.Join("\n", result.Violations));
    }


    [Test]
    public void TweakRoutingGuard_NewNumericLiterals_MustBeTweaksOrAllowlisted_V0()
    {
        var repoRoot = LocateRepoRootContainingSimCore();

        var baselineAbs = Path.Combine(repoRoot, "docs", "tweaks", "baseline_numeric_literals_v0.txt");
        var allowlistAbs = Path.Combine(repoRoot, "docs", "tweaks", "allowlist_numeric_literals_v0.txt");

        var violationsReportAbs = Path.Combine(repoRoot, "docs", "generated", "tweak_routing_guard_violations_v0.txt");
        var dumpReportAbs = Path.Combine(repoRoot, "docs", "generated", "tweak_routing_guard_dump_v0.txt");

        var scanDirs = new[]
        {
            Path.Combine(repoRoot, "SimCore", "Systems"),
            Path.Combine(repoRoot, "SimCore", "Gen"),
        };

        var scan = ScanNumericLiterals(repoRoot, scanDirs);

        if (IsFlagEnabledGlobal("STE_TWEAK_GUARD_DUMP"))
            WriteTextFileDeterministic(dumpReportAbs, BuildLiteralReportLines("tweak_routing_guard_dump_v0", scan.AllLiterals));

        if (IsFlagEnabledGlobal("STE_TWEAK_GUARD_MINT_BASELINE"))
        {
            MintNumericLiteralBaseline(baselineAbs, scan.ByFileTokensOnly);
            Assert.Pass("Minted numeric literal baseline: " + MakeRepoRelative(repoRoot, baselineAbs));
        }

        if (!File.Exists(baselineAbs))
        {
            WriteTextFileDeterministic(violationsReportAbs, new[]
            {
                "# tweak_routing_guard_violations_v0",
                "# format: file:line:token",
                "# ERROR: missing baseline file",
                MakeRepoRelative(repoRoot, baselineAbs),
                "# To mint: set STE_TWEAK_GUARD_MINT_BASELINE=1 and rerun this test",
            });

            Assert.Fail("Tweak routing guard missing baseline: " + MakeRepoRelative(repoRoot, baselineAbs));
        }

        var baseline = ReadNumericLiteralBaseline(baselineAbs);
        var allowlist = ReadNumericLiteralAllowlist(allowlistAbs);

        var newLiterals = FindNewNumericLiterals(baseline, scan.ByFileLiterals);

        var violations = newLiterals
            .Where(v => !IsAllowedByTweaksPath(v.PathRel))
            .Where(v => !allowlist.IsAllowlisted(v.PathRel, v.Token))
            .OrderBy(v => v.PathRel, StringComparer.Ordinal)
            .ThenBy(v => v.Line, Comparer<int>.Default)
            .ThenBy(v => v.Token, StringComparer.Ordinal)
            .Select(v => $"{v.PathRel}:{v.Line}:{v.Token}")
            .ToArray();

        WriteTextFileDeterministic(violationsReportAbs, BuildViolationsReportLines("tweak_routing_guard_violations_v0", violations));

        Assert.That(violations.Length, Is.EqualTo(0),
            "Tweak routing guard violations:\n" + string.Join("\n", violations)
            + "\nReport: " + MakeRepoRelative(repoRoot, violationsReportAbs));
    }

    [Test]
    public void RunnerHostTweaksFile_IfPresent_MustBeUtf8NoBom_AndJsonObject_V0()
    {
        var repoRoot = LocateRepoRootContainingSimCore();
        var path = Path.Combine(repoRoot, "Data", "Tweaks", "tweaks_v0.json");

        if (!File.Exists(path))
            Assert.Pass("No host tweak file present (allowed): " + MakeRepoRelative(repoRoot, path));

        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(path);
        }
        catch (Exception ex)
        {
            Assert.Fail("Failed to read host tweak file: " + MakeRepoRelative(repoRoot, path) + "\n" + ex);
            return;
        }

        // Contract: UTF-8 no BOM.
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            Assert.Fail("Host tweak file must be UTF-8 no BOM: " + MakeRepoRelative(repoRoot, path));

        string text;
        try
        {
            var utf8Strict = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            text = utf8Strict.GetString(bytes);
        }
        catch (Exception ex)
        {
            Assert.Fail("Host tweak file must be strict UTF-8: " + MakeRepoRelative(repoRoot, path) + "\n" + ex);
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(text);
            Assert.That(doc.RootElement.ValueKind, Is.EqualTo(JsonValueKind.Object),
                "Host tweak file must be a JSON object: " + MakeRepoRelative(repoRoot, path));
        }
        catch (Exception ex)
        {
            Assert.Fail("Host tweak file must parse as JSON: " + MakeRepoRelative(repoRoot, path) + "\n" + ex);
        }
    }

    [Test]
    public void SimCore_MustNotUse_SystemIO_ForRuntimeFileAccess()
    {
        var repoRoot = LocateRepoRootContainingSimCore();

        var simCoreDir = Path.Combine(repoRoot, "SimCore");
        var files = Directory.GetFiles(simCoreDir, "*.cs", SearchOption.AllDirectories)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToArray();

        // Heuristic patterns that indicate direct disk IO in SimCore.
        var badPatterns = new[]
        {
            "using System.IO",
            "System.IO.",
            "File.",
            "Directory.",
            "FileStream",
            "StreamReader",
            "StreamWriter",
            "Path.Combine(",
        };

        var hits = files
            .SelectMany(f =>
            {
                var text = File.ReadAllText(f);
                return badPatterns
                    .Where(p => text.Contains(p, StringComparison.Ordinal))
                    .Select(p => $"{MakeRepoRelative(repoRoot, f)} contains '{p}'");
            })
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToArray();

        Assert.That(hits.Length, Is.EqualTo(0),
            "Runtime File Contract violation in SimCore:\n" + string.Join("\n", hits));
    }

    private sealed record RepoHealthResult(
        int ScannedFileCount,
        List<string> Violations,
        string ReportPathRepoRelative
    );

    private static RepoHealthResult RunRepoHealthScan(string repoRoot, string reportPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);

        // Exclusions must be conservative to avoid scanning build outputs and editor caches.
        var excludedDirSegments = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".git",
            ".vs",
            ".idea",
            ".godot",
            ".import",
            "bin",
            "obj",
            "node_modules",
            "TestResults",
        };

        // Do not treat generated reports as violations (we intentionally write to docs/generated/).
        var excludedPathPrefixes = new[]
        {
            "docs/generated/",
        };

        // Hard limits (repo-wide).
        const long maxBytes = 25L * 1024L * 1024L;

        // LLM workflow budgets (visibility + guardrails).
        const long contextPacketWarnBytes = 250L * 1024L;
        const long contextPacketFailBytes = 750L * 1024L;

        const long docsWarnBytes = 150L * 1024L;
        const long docsFailBytes = 400L * 1024L;

        const long llmSurfaceWarnBytes = 250L * 1024L;
        const long llmSurfaceFailBytes = 750L * 1024L;

        // Forbidden file types unless allowlisted (prevent accidental bloat).
        var forbiddenExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".zip",
            ".rar",
            ".7z",
            ".exe",
            ".dll",
            ".pdb",
            ".mp4",
            ".mov",
            ".avi",
            ".mkv",
            ".psd",
            ".blend",
        };

        // Repo-relative allowlist for forbidden extensions.
        // Keep minimal and explicit. Empty by default.
        var allowlistedForbiddenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Example:
            // "third_party/tools/some_allowed_tool.exe",
        };

        // Backup%junk patterns (should not creep into normal working tree).
        // FAIL outside archive folders; WARN inside archive folders.
        static bool IsSuspectBackupOrJunk(string rel)
        {
            var name = Path.GetFileName(rel);
            if (string.IsNullOrEmpty(name))
                return false;

            if (name.EndsWith("~", StringComparison.Ordinal))
                return true;

            if (name.Contains(".bak", StringComparison.OrdinalIgnoreCase))
                return true;

            return name.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)
                   || name.EndsWith(".orig", StringComparison.OrdinalIgnoreCase)
                   || name.EndsWith(".old", StringComparison.OrdinalIgnoreCase)
                   || name.EndsWith(".rej", StringComparison.OrdinalIgnoreCase);
        }

        static bool IsInArchiveFolder(string rel)
        {
            rel = rel.Replace('\\', '/');
            return rel.StartsWith("docs/archive/", StringComparison.OrdinalIgnoreCase)
                   || rel.StartsWith("_archive/", StringComparison.OrdinalIgnoreCase);
        }

        static bool IsLlmSurfacePath(string rel)
        {
            rel = rel.Replace('\\', '/');

            if (rel.StartsWith("docs/", StringComparison.OrdinalIgnoreCase))
                return true;

            if (rel.StartsWith("scripts/", StringComparison.OrdinalIgnoreCase))
                return true;

            if (rel.StartsWith("SimCore/", StringComparison.OrdinalIgnoreCase))
                return true;

            if (rel.StartsWith("SimCore.Tests/", StringComparison.OrdinalIgnoreCase))
                return true;

            if (rel.Equals("_PROJECT_CONTEXT.md", StringComparison.OrdinalIgnoreCase))
                return true;

            if (rel.Equals("docs/gates/gates.json", StringComparison.OrdinalIgnoreCase))
                return true;

            var fileName = Path.GetFileName(rel);
            if (fileName.StartsWith("DevTool", StringComparison.OrdinalIgnoreCase) && fileName.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        var allFiles = EnumerateRepoFiles(repoRoot, excludedDirSegments, excludedPathPrefixes)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToArray();

        var violations = new List<string>();
        var warnings = new List<string>();

        // Deterministic visibility sections.
        const int largestFilesTopN = 50;
        const int extensionsTopN = 30;

        var sizeByFile = new List<(string Rel, long Bytes)>(allFiles.Length);
        var extAgg = new Dictionary<string, (int Count, long Bytes)>(StringComparer.OrdinalIgnoreCase);

        foreach (var rel in allFiles)
        {
            var full = Path.Combine(repoRoot, rel.Replace('/', Path.DirectorySeparatorChar));
            long bytes = 0;

            try
            {
                var fi = new FileInfo(full);
                bytes = fi.Exists ? fi.Length : 0;

                if (fi.Exists && fi.Length > maxBytes)
                    violations.Add($"FILE_TOO_LARGE|{rel}|bytes={fi.Length}");
            }
            catch (Exception ex)
            {
                violations.Add($"SCAN_ERROR|{rel}|{ex.GetType().Name}");
                bytes = 0;
            }

            sizeByFile.Add((rel, bytes));

            var ext = Path.GetExtension(rel);
            if (!string.IsNullOrEmpty(ext))
            {
                if (forbiddenExtensions.Contains(ext))
                {
                    if (!allowlistedForbiddenPaths.Contains(rel))
                        violations.Add($"FORBIDDEN_EXTENSION|{rel}|ext={ext}");
                }
            }

            // Backup%junk policy.
            if (IsSuspectBackupOrJunk(rel))
            {
                if (IsInArchiveFolder(rel))
                    warnings.Add($"SUSPECT_BACKUP_FILE|{rel}|scope=ARCHIVE_WARN");
                else
                    violations.Add($"SUSPECT_BACKUP_FILE|{rel}|scope=FAIL_OUTSIDE_ARCHIVE");
            }

            // LLM budgets.
            var norm = rel.Replace('\\', '/');

            if (norm.Equals("docs/generated/01_CONTEXT_PACKET.md", StringComparison.OrdinalIgnoreCase))
            {
                if (bytes > contextPacketFailBytes)
                    violations.Add($"CONTEXT_PACKET_TOO_LARGE|{rel}|bytes={bytes}|limit={contextPacketFailBytes}");
                else if (bytes > contextPacketWarnBytes)
                    warnings.Add($"CONTEXT_PACKET_LARGE_WARN|{rel}|bytes={bytes}|warn={contextPacketWarnBytes}");
            }

            if (norm.StartsWith("docs/", StringComparison.OrdinalIgnoreCase))
            {
                if (bytes > docsFailBytes)
                    violations.Add($"DOC_FILE_TOO_LARGE|{rel}|bytes={bytes}|limit={docsFailBytes}");
                else if (bytes > docsWarnBytes)
                    warnings.Add($"DOC_FILE_LARGE_WARN|{rel}|bytes={bytes}|warn={docsWarnBytes}");
            }

            if (IsLlmSurfacePath(norm))
            {
                if (bytes > llmSurfaceFailBytes)
                    violations.Add($"LLM_SURFACE_FILE_TOO_LARGE|{rel}|bytes={bytes}|limit={llmSurfaceFailBytes}");
                else if (bytes > llmSurfaceWarnBytes)
                    warnings.Add($"LLM_SURFACE_FILE_LARGE_WARN|{rel}|bytes={bytes}|warn={llmSurfaceWarnBytes}");
            }

            // Extension aggregation for report.
            var extKey = string.IsNullOrEmpty(ext) ? "(none)" : ext.ToLowerInvariant();
            if (!extAgg.TryGetValue(extKey, out var cur))
                cur = (0, 0);
            extAgg[extKey] = (cur.Count + 1, cur.Bytes + bytes);
        }

        // Generated artifacts only under docs/generated/.
        // Flag any directory named 'generated' outside docs/generated/.
        var generatedDirsOutsideDocs = EnumerateRepoDirectories(repoRoot, excludedDirSegments)
            .Where(d =>
            {
                var rel = d.Replace('\\', '/');
                if (rel.Equals("docs/generated", StringComparison.OrdinalIgnoreCase))
                    return false;

                return rel.Equals("generated", StringComparison.OrdinalIgnoreCase)
                       || rel.EndsWith("/generated", StringComparison.OrdinalIgnoreCase);
            })
            .OrderBy(d => d, StringComparer.Ordinal)
            .ToArray();

        foreach (var d in generatedDirsOutsideDocs)
            violations.Add($"GENERATED_DIR_OUTSIDE_DOCS|{d.Replace('\\', '/')}|");

        // Also flag known generated artifact filenames if they appear outside docs/generated/.
        var knownGeneratedFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "01_CONTEXT_PACKET.md",
            "connectivity_manifest.json",
            "connectivity_graph.json",
            "connectivity_violations.json",
            "repo_health_report_v0.txt",
        };

        var misplacedGeneratedFiles = allFiles
            .Where(p =>
            {
                var name = Path.GetFileName(p);
                return knownGeneratedFileNames.Contains(name)
                       && !p.StartsWith("docs/generated/", StringComparison.OrdinalIgnoreCase);
            })
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToArray();

        foreach (var p in misplacedGeneratedFiles)
            violations.Add($"GENERATED_ARTIFACT_MISPLACED|{p}|");

        violations.Sort(StringComparer.Ordinal);
        warnings.Sort(StringComparer.Ordinal);

        var largestFiles = sizeByFile
            .OrderByDescending(t => t.Bytes)
            .ThenBy(t => t.Rel, StringComparer.Ordinal)
            .Take(largestFilesTopN)
            .ToArray();

        var extTop = extAgg
            .Select(kv => (Ext: kv.Key.ToLowerInvariant(), kv.Value.Count, kv.Value.Bytes))
            .OrderByDescending(t => t.Count)
            .ThenByDescending(t => t.Bytes)
            .ThenBy(t => t.Ext, StringComparer.Ordinal)
            .Take(extensionsTopN)
            .ToArray();

        // Allowlist visibility (to prevent allowlist becoming a trash chute).
        var allowlistedForbiddenFound = sizeByFile
            .Where(t => allowlistedForbiddenPaths.Contains(t.Rel))
            .OrderBy(t => t.Rel, StringComparer.Ordinal)
            .Select(t => $"{t.Rel}|bytes={t.Bytes}")
            .ToArray();

        var contextPacketBytes = sizeByFile
            .Where(t => t.Rel.Replace('\\', '/').Equals("docs/generated/01_CONTEXT_PACKET.md", StringComparison.OrdinalIgnoreCase))
            .Select(t => t.Bytes)
            .FirstOrDefault();

        // Connectivity delta enforcement (fail only on NEW cross-layer edges not allowlisted).
        // Enabled only when STE_REPO_HEALTH_REQUIRE_CONNECTIVITY_DELTA=1.
        // One-time baseline mint mode when STE_REPO_HEALTH_MINT_CONNECTIVITY_BASELINE=1.
        var connectivityNewEdgeViolations = new List<string>();
        var connectivityCurrentCrossLayerCount = 0;

        static string GetEnvFlag(string name)
        {
            var v = Environment.GetEnvironmentVariable(name);
            return (v ?? "").Trim();
        }

        static bool IsFlagEnabled(string name) =>
            string.Equals(GetEnvFlag(name), "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(GetEnvFlag(name), "true", StringComparison.OrdinalIgnoreCase);

        static IEnumerable<string> ReadEdgeSetFile(string absPath)
        {
            if (!File.Exists(absPath))
                return Array.Empty<string>();

            var lines = File.ReadAllLines(absPath);
            var outLines = new List<string>();
            foreach (var raw in lines)
            {
                var s = (raw ?? "").Trim();
                if (s.Length == 0) continue;
                if (s.StartsWith("#", StringComparison.Ordinal)) continue;
                outLines.Add(s);
            }
            return outLines;
        }

        static void WriteEdgeSetFile(string absPath, IEnumerable<string> lines)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(absPath)!);
            var text = string.Join("\n", lines) + "\n";
            File.WriteAllText(absPath, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        static string LayerOf(string repoRel)
        {
            var p = (repoRel ?? "").Replace('\\', '/');

            if (p.StartsWith("SimCore/", StringComparison.OrdinalIgnoreCase)) return "SimCore";
            if (p.StartsWith("SimCore.Tests/", StringComparison.OrdinalIgnoreCase)) return "SimCore.Tests";

            if (p.StartsWith("scripts/tools/", StringComparison.OrdinalIgnoreCase)) return "Tooling";
            if (p.StartsWith("docs/", StringComparison.OrdinalIgnoreCase)) return "Docs";
            if (p.StartsWith("scripts/bridge/", StringComparison.OrdinalIgnoreCase)) return "Bridge";
            if (p.StartsWith("scripts/", StringComparison.OrdinalIgnoreCase)) return "GameShell";

            var name = Path.GetFileName(p);
            if (name.StartsWith("DevTool", StringComparison.OrdinalIgnoreCase) && name.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase))
                return "Tooling";

            return "Other";
        }

        static bool IsConnectivityRelevantLayer(string layer) =>
            !string.Equals(layer, "Docs", StringComparison.Ordinal)
            && !string.Equals(layer, "Tooling", StringComparison.Ordinal);

        static bool IsCrossLayerRelevant(string fromPath, string toPath, out string fromLayer, out string toLayer)
        {
            fromLayer = LayerOf(fromPath);
            toLayer = LayerOf(toPath);

            if (string.Equals(fromLayer, toLayer, StringComparison.Ordinal))
                return false;

            if (!IsConnectivityRelevantLayer(fromLayer) || !IsConnectivityRelevantLayer(toLayer))
                return false;

            return true;
        }

        if (IsFlagEnabled("STE_REPO_HEALTH_REQUIRE_CONNECTIVITY_DELTA") || IsFlagEnabled("STE_REPO_HEALTH_MINT_CONNECTIVITY_BASELINE"))
        {
            var graphAbs = Path.Combine(repoRoot, "docs", "generated", "connectivity_graph.json");
            var baselineAbs = Path.Combine(repoRoot, "docs", "connectivity", "baseline_cross_layer_edges_v0.txt");
            var allowAbs = Path.Combine(repoRoot, "docs", "connectivity", "allowlist_cross_layer_edges_v0.txt");

            if (!File.Exists(graphAbs))
            {
                violations.Add("CONNECTIVITY_MISSING_GRAPH|docs/generated/connectivity_graph.json|");
            }
            else
            {
                // Minimal JSON parse: {"nodes":[...], "edges":[{"from_id":0,"to_id":1,"type":"..."}]}
                // Use JsonDocument to avoid schema drift.
                using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(graphAbs));
                var root = doc.RootElement;

                var nodesEl = root.TryGetProperty("nodes", out var tmpNodes) ? tmpNodes : default;
                var edgesEl = root.TryGetProperty("edges", out var tmpEdges) ? tmpEdges : default;

                if (nodesEl.ValueKind != System.Text.Json.JsonValueKind.Array || edgesEl.ValueKind != System.Text.Json.JsonValueKind.Array)
                {
                    violations.Add("CONNECTIVITY_GRAPH_SCHEMA_UNEXPECTED|docs/generated/connectivity_graph.json|");
                }
                else
                {
                    var nodes = nodesEl.EnumerateArray().Select(x => x.GetString() ?? "").ToArray();

                    var currentCrossLayer = new List<(string Key, string Detail)>();

                    foreach (var e in edgesEl.EnumerateArray())
                    {
                        if (!e.TryGetProperty("from_id", out var fromIdEl)) continue;
                        if (!e.TryGetProperty("to_id", out var toIdEl)) continue;
                        if (!e.TryGetProperty("type", out var typeEl)) continue;

                        var fromId = fromIdEl.GetInt32();
                        var toId = toIdEl.GetInt32();
                        var type = typeEl.GetString() ?? "";

                        if (fromId < 0 || toId < 0 || fromId >= nodes.Length || toId >= nodes.Length)
                            continue;

                        var fromPath = nodes[fromId];
                        var toPath = nodes[toId];

                        if (!IsCrossLayerRelevant(fromPath, toPath, out var fromLayer, out var toLayer))
                            continue;

                        var key = $"{fromPath}|{toPath}|{type}";
                        var detail = $"CONNECTIVITY_CROSS_LAYER_EDGE|from={fromPath}|to={toPath}|type={type}|layers={fromLayer}->{toLayer}";
                        currentCrossLayer.Add((key, detail));
                    }

                    // Deterministic normalize.
                    currentCrossLayer = currentCrossLayer
                        .OrderBy(t => t.Key, StringComparer.Ordinal)
                        .ToList();

                    connectivityCurrentCrossLayerCount = currentCrossLayer.Count;

                    if (IsFlagEnabled("STE_REPO_HEALTH_MINT_CONNECTIVITY_BASELINE"))
                    {
                        // Mint baseline as keys only.
                        var keys = currentCrossLayer.Select(t => t.Key).Distinct(StringComparer.Ordinal).OrderBy(s => s, StringComparer.Ordinal).ToArray();
                        WriteEdgeSetFile(baselineAbs, new[] { "# baseline_cross_layer_edges_v0 (auto-minted, deterministic)", "# format: from|to|type" }.Concat(keys));
                    }
                    else if (IsFlagEnabled("STE_REPO_HEALTH_REQUIRE_CONNECTIVITY_DELTA"))
                    {
                        if (!File.Exists(baselineAbs))
                        {
                            violations.Add("CONNECTIVITY_MISSING_BASELINE|docs/connectivity/baseline_cross_layer_edges_v0.txt|");
                        }
                        else
                        {
                            var baseline = new HashSet<string>(ReadEdgeSetFile(baselineAbs), StringComparer.Ordinal);
                            var allow = new HashSet<string>(ReadEdgeSetFile(allowAbs), StringComparer.Ordinal);

                            foreach (var t in currentCrossLayer)
                            {
                                if (baseline.Contains(t.Key)) continue;
                                if (allow.Contains(t.Key)) continue;

                                connectivityNewEdgeViolations.Add(t.Detail.Replace("CONNECTIVITY_CROSS_LAYER_EDGE|", "CONNECTIVITY_NEW_CROSS_LAYER_EDGE|"));
                            }

                            connectivityNewEdgeViolations.Sort(StringComparer.Ordinal);
                            foreach (var v in connectivityNewEdgeViolations)
                                violations.Add(v);
                        }
                    }
                }
            }
        }

        var reportLines = new List<string>
        {
            "REPO_HEALTH_REPORT_V0",
            "rules:",
            $"- FILE_TOO_LARGE: bytes>{maxBytes}",
            $"- FORBIDDEN_EXTENSION: {string.Join(",", forbiddenExtensions.OrderBy(s => s, StringComparer.Ordinal))} (unless allowlisted)",
            "- GENERATED_DIR_OUTSIDE_DOCS: any directory named 'generated' outside docs/generated/",
            "- GENERATED_ARTIFACT_MISPLACED: known tool outputs found outside docs/generated/",
            "- SUSPECT_BACKUP_FILE: *.bak* *~ *.tmp *.orig *.old *.rej (FAIL outside docs/archive/ and _archive/)",
            $"- CONTEXT_PACKET_BUDGET: warn>{contextPacketWarnBytes} fail>{contextPacketFailBytes} (docs/generated/01_CONTEXT_PACKET.md)",
            $"- DOC_BUDGET: warn>{docsWarnBytes} fail>{docsFailBytes} (docs/**)",
            $"- LLM_SURFACE_BUDGET: warn>{llmSurfaceWarnBytes} fail>{llmSurfaceFailBytes} (docs/scripts/SimCore/SimCore.Tests/DevTool*)",
            "- CONNECTIVITY_DELTA: FAIL only on NEW cross-layer edges not allowlisted when STE_REPO_HEALTH_REQUIRE_CONNECTIVITY_DELTA=1",
            "excludes:",
            $"- dir_segments: {string.Join(",", excludedDirSegments.OrderBy(s => s, StringComparer.Ordinal))}",
            $"- path_prefixes: {string.Join(",", excludedPathPrefixes.OrderBy(s => s, StringComparer.Ordinal))}",
            $"scanned_files: {allFiles.Length}",
            $"context_packet_bytes: {contextPacketBytes}",
            $"connectivity_cross_layer_edges: {connectivityCurrentCrossLayerCount}",
            $"warnings_count: {warnings.Count}",
            $"violations_count: {violations.Count}",
            $"largest_files_top: {largestFilesTopN}",
            "largest_files:",
        };

        foreach (var t in largestFiles)
            reportLines.Add($"{t.Rel}|bytes={t.Bytes}");

        reportLines.Add($"extensions_top: {extensionsTopN}");
        reportLines.Add("extensions:");
        foreach (var t in extTop)
            reportLines.Add($"{t.Ext}|count={t.Count}|bytes={t.Bytes}");

        reportLines.Add($"allowlisted_forbidden_paths: {allowlistedForbiddenPaths.Count}");
        reportLines.Add("allowlisted_forbidden_found:");
        reportLines.AddRange(allowlistedForbiddenFound);

        reportLines.Add("warnings:");
        reportLines.AddRange(warnings);

        reportLines.Add("violations:");
        reportLines.AddRange(violations);

        var reportText = string.Join("\n", reportLines) + "\n";
        File.WriteAllText(reportPath, reportText, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        return new RepoHealthResult(allFiles.Length, violations, MakeRepoRelative(repoRoot, reportPath));
    }

    private static string LocateRepoRootContainingSimCore()
    {
        // Repo root from test bin folder:
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "SimCore")))
            dir = dir.Parent;

        Assert.That(dir, Is.Not.Null, "Could not locate repo root containing SimCore/");
        return dir!.FullName;
    }

    private static IEnumerable<string> EnumerateRepoFiles(
        string repoRoot,
        HashSet<string> excludedDirSegments,
        string[] excludedPathPrefixes
    )
    {
        var stack = new Stack<string>();
        stack.Push(repoRoot);

        while (stack.Count > 0)
        {
            var fullDir = stack.Pop();
            var relDir = MakeRepoRelative(repoRoot, fullDir).Replace('\\', '/');

            if (relDir.Length != 0)
            {
                var dirSegments = relDir.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (dirSegments.Any(s => excludedDirSegments.Contains(s)))
                    continue;
            }

            if (relDir.Equals("docs/generated", StringComparison.OrdinalIgnoreCase))
                continue;

            IEnumerable<string> subdirs;
            try
            {
                subdirs = Directory.EnumerateDirectories(fullDir, "*", SearchOption.TopDirectoryOnly);
            }
            catch
            {
                subdirs = Array.Empty<string>();
            }

            foreach (var sd in subdirs.OrderByDescending(p => p, StringComparer.Ordinal))
                stack.Push(sd);

            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(fullDir, "*", SearchOption.TopDirectoryOnly);
            }
            catch
            {
                continue;
            }

            foreach (var fullFile in files.OrderBy(p => p, StringComparer.Ordinal))
            {
                var rel = MakeRepoRelative(repoRoot, fullFile).Replace('\\', '/');

                if (excludedPathPrefixes.Any(prefix => rel.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                    continue;

                var segments = rel.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Any(s => excludedDirSegments.Contains(s)))
                    continue;

                yield return rel;
            }
        }
    }

    private static IEnumerable<string> EnumerateRepoDirectories(string repoRoot, HashSet<string> excludedDirSegments)
    {
        var stack = new Stack<string>();
        stack.Push(repoRoot);

        while (stack.Count > 0)
        {
            var fullDir = stack.Pop();
            var relDir = MakeRepoRelative(repoRoot, fullDir).Replace('\\', '/');

            if (relDir.Length != 0)
            {
                var segments = relDir.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Any(s => excludedDirSegments.Contains(s)))
                    continue;

                yield return relDir;
            }

            if (relDir.Equals("docs/generated", StringComparison.OrdinalIgnoreCase))
                continue;

            IEnumerable<string> subdirs;
            try
            {
                subdirs = Directory.EnumerateDirectories(fullDir, "*", SearchOption.TopDirectoryOnly);
            }
            catch
            {
                continue;
            }

            foreach (var sd in subdirs.OrderByDescending(p => p, StringComparer.Ordinal))
                stack.Push(sd);
        }
    }


    private sealed record NumericLiteralOccurrence(string PathRel, int Line, string Token);

    private sealed class NumericLiteralAllowlist
    {
        private readonly HashSet<string> _globalTokens;
        private readonly HashSet<string> _pathToken;

        public NumericLiteralAllowlist(HashSet<string> globalTokens, HashSet<string> pathToken)
        {
            _globalTokens = globalTokens;
            _pathToken = pathToken;
        }

        public bool IsAllowlisted(string pathRel, string token)
        {
            if (_globalTokens.Contains(token)) return true;
            return _pathToken.Contains(pathRel + ":" + token);
        }
    }

    private sealed record NumericLiteralScanResult(
        IReadOnlyList<NumericLiteralOccurrence> AllLiterals,
        IReadOnlyDictionary<string, IReadOnlyList<NumericLiteralOccurrence>> ByFileLiterals,
        IReadOnlyDictionary<string, IReadOnlyList<string>> ByFileTokensOnly);

    private static bool IsFlagEnabledGlobal(string name)
    {
        var v = (Environment.GetEnvironmentVariable(name) ?? "").Trim();
        return string.Equals(v, "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(v, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAllowedByTweaksPath(string pathRel)
    {
        // "Sourced from Tweaks" is interpreted as: literals in files that define tweaks are allowed.
        // We allow any file with a path segment or filename that includes "Tweaks".
        var p = pathRel.Replace('\\', '/');
        if (p.IndexOf("/Tweaks/", StringComparison.OrdinalIgnoreCase) >= 0) return true;
        var file = Path.GetFileName(p);
        return file.IndexOf("Tweaks", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static void EnsureDirForFile(string absPath)
    {
        var dir = Path.GetDirectoryName(absPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }

    private static void WriteTextFileDeterministic(string absPath, IEnumerable<string> lines)
    {
        EnsureDirForFile(absPath);
        File.WriteAllText(absPath, string.Join("\n", lines) + "\n", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static IEnumerable<string> BuildViolationsReportLines(string header, IReadOnlyList<string> violations)
    {
        yield return "# " + header;
        yield return "# format: file:line:token";
        if (violations.Count == 0)
        {
            yield return "OK";
            yield break;
        }

        foreach (var v in violations)
            yield return v;
    }

    private static IEnumerable<string> BuildLiteralReportLines(string header, IReadOnlyList<NumericLiteralOccurrence> literals)
    {
        yield return "# " + header;
        yield return "# format: file:line:token";

        foreach (var o in literals
            .OrderBy(x => x.PathRel, StringComparer.Ordinal)
            .ThenBy(x => x.Line, Comparer<int>.Default)
            .ThenBy(x => x.Token, StringComparer.Ordinal))
        {
            yield return $"{o.PathRel}:{o.Line}:{o.Token}";
        }
    }

    private static NumericLiteralAllowlist ReadNumericLiteralAllowlist(string allowlistAbs)
    {
        var globalTokens = new HashSet<string>(StringComparer.Ordinal);
        var pathToken = new HashSet<string>(StringComparer.Ordinal);

        if (!File.Exists(allowlistAbs))
            return new NumericLiteralAllowlist(globalTokens, pathToken);

        foreach (var raw in File.ReadAllLines(allowlistAbs))
        {
            var s = (raw ?? "").Trim();
            if (s.Length == 0) continue;
            if (s.StartsWith("#", StringComparison.Ordinal)) continue;

            var idx = s.IndexOf(':');
            if (idx > 0 && idx < s.Length - 1)
                pathToken.Add(s);
            else
                globalTokens.Add(s);
        }

        return new NumericLiteralAllowlist(globalTokens, pathToken);
    }

    private static Dictionary<string, List<string>> ReadNumericLiteralBaseline(string baselineAbs)
    {
        var map = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        string? currentFile = null;

        foreach (var raw in File.ReadAllLines(baselineAbs))
        {
            var s = (raw ?? "").TrimEnd();
            if (s.Length == 0) continue;
            if (s.StartsWith("#", StringComparison.Ordinal))
            {
                const string prefix = "# FILE ";
                if (s.StartsWith(prefix, StringComparison.Ordinal))
                {
                    currentFile = s.Substring(prefix.Length).Trim();
                    if (!map.ContainsKey(currentFile))
                        map[currentFile] = new List<string>();
                }
                continue;
            }

            if (currentFile == null)
                continue;

            map[currentFile].Add(s.Trim());
        }

        return map;
    }

    private static void MintNumericLiteralBaseline(string baselineAbs, IReadOnlyDictionary<string, IReadOnlyList<string>> byFileTokensOnly)
    {
        EnsureDirForFile(baselineAbs);

        var outLines = new List<string>
        {
            "# baseline_numeric_literals_v0 (auto-minted, deterministic)",
            "# format:",
            "#   # FILE <repo-relative-path>",
            "#   <token> (one per line, order of appearance in file)",
        };

        foreach (var kvp in byFileTokensOnly.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            outLines.Add("# FILE " + kvp.Key);
            foreach (var t in kvp.Value)
                outLines.Add(t);
        }

        WriteTextFileDeterministic(baselineAbs, outLines);
    }

    private static IReadOnlyList<NumericLiteralOccurrence> FindNewNumericLiterals(
        Dictionary<string, List<string>> baseline,
        IReadOnlyDictionary<string, IReadOnlyList<NumericLiteralOccurrence>> current)
    {
        var outList = new List<NumericLiteralOccurrence>();

        foreach (var kvp in current.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            var pathRel = kvp.Key;
            var occurrences = kvp.Value;

            baseline.TryGetValue(pathRel, out var baseTokensMaybeNull);
            var baseTokens = baseTokensMaybeNull ?? new List<string>();

            var matched = new bool[occurrences.Count];

            // Deterministic subsequence match: baseline tokens are expected to appear in current in the same order.
            var cursor = 0;
            for (var bi = 0; bi < baseTokens.Count; bi++)
            {
                var bt = baseTokens[bi];
                for (var ci = cursor; ci < occurrences.Count; ci++)
                {
                    if (matched[ci]) continue;
                    if (!string.Equals(occurrences[ci].Token, bt, StringComparison.Ordinal)) continue;

                    matched[ci] = true;
                    cursor = ci + 1;
                    break;
                }
            }

            for (var i = 0; i < occurrences.Count; i++)
            {
                if (!matched[i])
                    outList.Add(occurrences[i]);
            }
        }

        return outList;
    }

    private static NumericLiteralScanResult ScanNumericLiterals(string repoRoot, IEnumerable<string> scanDirsAbs)
    {
        var all = new List<NumericLiteralOccurrence>();
        var byFile = new Dictionary<string, List<NumericLiteralOccurrence>>(StringComparer.Ordinal);
        var byFileTokensOnly = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        var files = scanDirsAbs
            .Where(Directory.Exists)
            .SelectMany(d => Directory.EnumerateFiles(d, "*.cs", SearchOption.AllDirectories))
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToArray();

        foreach (var abs in files)
        {
            var rel = MakeRepoRelative(repoRoot, abs);
            var text = File.ReadAllText(abs);

            var occ = ExtractNumericLiterals(rel, text);
            byFile[rel] = occ;

            byFileTokensOnly[rel] = occ.Select(o => o.Token).ToList();

            all.AddRange(occ);
        }

        return new NumericLiteralScanResult(
            AllLiterals: all,
            ByFileLiterals: byFile.ToDictionary(k => k.Key, v => (IReadOnlyList<NumericLiteralOccurrence>)v.Value, StringComparer.Ordinal),
            ByFileTokensOnly: byFileTokensOnly.ToDictionary(k => k.Key, v => (IReadOnlyList<string>)v.Value, StringComparer.Ordinal));
    }

    private static List<NumericLiteralOccurrence> ExtractNumericLiterals(string pathRel, string text)
    {
        var outList = new List<NumericLiteralOccurrence>();

        var line = 1;
        var i = 0;

        var inLineComment = false;
        var inBlockComment = false;
        var inString = false;
        var inVerbatimString = false;
        var inChar = false;

        while (i < text.Length)
        {
            var c = text[i];

            if (c == '\n')
                line++;

            if (inLineComment)
            {
                if (c == '\n')
                    inLineComment = false;
                i++;
                continue;
            }

            if (inBlockComment)
            {
                if (c == '*' && i + 1 < text.Length && text[i + 1] == '/')
                {
                    inBlockComment = false;
                    i += 2;
                    continue;
                }
                i++;
                continue;
            }

            if (inString)
            {
                if (inVerbatimString)
                {
                    if (c == '"')
                    {
                        // escaped "" inside verbatim string
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            i += 2;
                            continue;
                        }

                        inString = false;
                        inVerbatimString = false;
                        i++;
                        continue;
                    }
                    i++;
                    continue;
                }

                if (c == '\\')
                {
                    i += 2;
                    continue;
                }

                if (c == '"')
                {
                    inString = false;
                    i++;
                    continue;
                }

                i++;
                continue;
            }

            if (inChar)
            {
                if (c == '\\')
                {
                    i += 2;
                    continue;
                }

                if (c == '\'')
                {
                    inChar = false;
                    i++;
                    continue;
                }

                i++;
                continue;
            }

            // entering comments or strings
            if (c == '/' && i + 1 < text.Length)
            {
                var n = text[i + 1];
                if (n == '/')
                {
                    inLineComment = true;
                    i += 2;
                    continue;
                }
                if (n == '*')
                {
                    inBlockComment = true;
                    i += 2;
                    continue;
                }
            }

            if (c == '@' && i + 1 < text.Length && text[i + 1] == '"')
            {
                inString = true;
                inVerbatimString = true;
                i += 2;
                continue;
            }

            if (c == '"')
            {
                inString = true;
                inVerbatimString = false;
                i++;
                continue;
            }

            if (c == '\'')
            {
                inChar = true;
                i++;
                continue;
            }

            // numeric literal detection (in code)
            if (IsNumericLiteralStart(text, i))
            {
                var token = ConsumeNumericLiteral(text, ref i);
                if (token.Length > 0)
                    outList.Add(new NumericLiteralOccurrence(pathRel, line, token));
                continue;
            }

            i++;
        }

        return outList;
    }

    private static bool IsNumericLiteralStart(string s, int i)
    {
        var c = s[i];

        if (char.IsDigit(c))
        {
            // avoid identifiers like Foo2
            if (i > 0 && IsIdentChar(s[i - 1]))
                return false;
            return true;
        }

        if (c == '.' && i + 1 < s.Length && char.IsDigit(s[i + 1]))
        {
            if (i > 0 && IsIdentChar(s[i - 1]))
                return false;
            return true;
        }

        return false;
    }

    private static bool IsIdentChar(char c) =>
        char.IsLetterOrDigit(c) || c == '_';

    private static string ConsumeNumericLiteral(string s, ref int i)
    {
        var start = i;

        // leading .
        if (s[i] == '.')
        {
            i++; // consume '.'
            while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '_')) i++;
            ConsumeExponentAndSuffix(s, ref i);
            return s.Substring(start, i - start);
        }

        // hex
        if (s[i] == '0' && i + 1 < s.Length && (s[i + 1] == 'x' || s[i + 1] == 'X'))
        {
            i += 2;
            while (i < s.Length && (IsHexDigit(s[i]) || s[i] == '_')) i++;
            ConsumeSuffix(s, ref i);
            return s.Substring(start, i - start);
        }

        // decimal / float
        while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '_')) i++;

        if (i < s.Length && s[i] == '.' && !(i + 1 < s.Length && s[i + 1] == '.'))
        {
            i++; // '.'
            while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '_')) i++;
        }

        ConsumeExponentAndSuffix(s, ref i);

        return s.Substring(start, i - start);
    }

    private static void ConsumeExponentAndSuffix(string s, ref int i)
    {
        if (i < s.Length && (s[i] == 'e' || s[i] == 'E'))
        {
            var j = i + 1;
            if (j < s.Length && (s[j] == '+' || s[j] == '-')) j++;

            var hasDigits = false;
            while (j < s.Length && (char.IsDigit(s[j]) || s[j] == '_'))
            {
                hasDigits = true;
                j++;
            }

            if (hasDigits)
                i = j;
        }

        ConsumeSuffix(s, ref i);
    }

    private static void ConsumeSuffix(string s, ref int i)
    {
        // common C# numeric suffixes (case-insensitive):
        // f, d, m, u, l (including combinations like ul, lu)
        var j = i;
        while (j < s.Length && char.IsLetter(s[j]))
            j++;

        if (j > i)
        {
            var suf = s.Substring(i, j - i);
            // Keep suffix only if it looks like a numeric suffix, to avoid swallowing identifiers.
            if (LooksLikeNumericSuffix(suf))
                i = j;
        }
    }

    private static bool LooksLikeNumericSuffix(string suf)
    {
        suf = suf.ToLowerInvariant();

        if (suf == "f" || suf == "d" || suf == "m") return true;
        if (suf == "u" || suf == "l" || suf == "ul" || suf == "lu") return true;

        // allow u/l combos
        if (suf.All(ch => ch == 'u' || ch == 'l') && suf.Length <= 2) return true;

        return false;
    }

    private static bool IsHexDigit(char c) =>
        (c >= '0' && c <= '9')
        || (c >= 'a' && c <= 'f')
        || (c >= 'A' && c <= 'F');

    private static string MakeRepoRelative(string root, string fullPath)
    {
        root = root.Replace('\\', '/').TrimEnd('/');
        fullPath = fullPath.Replace('\\', '/').TrimEnd('/');

        if (string.Equals(fullPath, root, StringComparison.Ordinal))
            return string.Empty;

        if (fullPath.StartsWith(root + "/", StringComparison.Ordinal))
            return fullPath[(root.Length + 1)..];

        return fullPath;
    }
}
