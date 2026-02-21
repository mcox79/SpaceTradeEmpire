using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SimCore.Tests;

public class ContainmentTests
{
    [Test]
    public void Containment_DecaysTrace_OverTime()
    {
        var state = new SimState(111);
        state.Nodes.Add("n1", new Node { Id = "n1", Trace = 1.0f });

        // Act: Process 1 Tick
        ContainmentSystem.Process(state);

        // Assert: Trace reduced by 0.05
        Assert.That(state.Nodes["n1"].Trace, Is.EqualTo(0.95f).Within(0.001f));
    }

    [Test]
    public void Containment_ClampsTrace_ToZero()
    {
        var state = new SimState(222);
        state.Nodes.Add("n1", new Node { Id = "n1", Trace = 0.02f });

        // Act: Process Tick (Decay 0.05 > 0.02)
        ContainmentSystem.Process(state);

        // Assert: Clamped
        Assert.That(state.Nodes["n1"].Trace, Is.EqualTo(0f));
    }

    [Test]
    public void ApiBoundaries_UiMustNotReference_SimCoreEntitiesOrSystems()
    {
        // Deterministic guard:
        // - scans scripts/ui/**/*.cs only
        // - emits stable, sorted violations: file:line:type
        // - no timestamps, no machine paths

        var repoRoot = FindRepoRoot();
        var uiRoot = Path.Combine(repoRoot, "scripts", "ui");

        if (!Directory.Exists(uiRoot))
        {
            Assert.Fail($"UI directory missing: {ToRepoRel(repoRoot, uiRoot)}");
            return;
        }

        var forbidden = new[]
        {
            "SimCore.Entities",
            "SimCore.Systems"
        };

        var files = Directory.EnumerateFiles(uiRoot, "*.cs", SearchOption.AllDirectories)
            .Select(p => NormalizeSep(p))
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToArray();

        var violations = new List<(string relPath, int line, string token)>(64);

        foreach (var abs in files)
        {
            // ReadAllLines preserves line boundaries and gives deterministic line numbering.
            var lines = File.ReadAllLines(abs);

            // Ignore block comments across lines deterministically.
            bool inBlockComment = false;

            for (int i = 0; i < lines.Length; i++)
            {
                var ln = i + 1;
                var raw = lines[i];

                var s = StripCommentsAndStrings(raw, ref inBlockComment);
                if (s.Length == 0) continue;

                for (int t = 0; t < forbidden.Length; t++)
                {
                    var token = forbidden[t];
                    if (s.IndexOf(token, StringComparison.Ordinal) >= 0)
                    {
                        var rel = ToRepoRel(repoRoot, abs);
                        violations.Add((rel, ln, token));
                    }
                }
            }
        }

        static string StripCommentsAndStrings(string line, ref bool inBlockComment)
        {
            // Goal: avoid false positives in // comments, /* */ comments, and string literals.
            // This is a simple deterministic scanner, not a full C# parser.

            if (string.IsNullOrEmpty(line)) return "";

            var sb = new System.Text.StringBuilder(line.Length);

            bool inString = false;
            char stringQuote = '\0';
            bool escape = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                char n = (i + 1 < line.Length) ? line[i + 1] : '\0';

                if (inBlockComment)
                {
                    if (c == '*' && n == '/')
                    {
                        inBlockComment = false;
                        i++; // skip '/'
                    }
                    continue;
                }

                if (inString)
                {
                    if (escape)
                    {
                        escape = false;
                        continue;
                    }

                    if (c == '\\')
                    {
                        escape = true;
                        continue;
                    }

                    if (c == stringQuote)
                    {
                        inString = false;
                        stringQuote = '\0';
                    }
                    continue;
                }

                // Line comment starts: ignore rest
                if (c == '/' && n == '/') break;

                // Block comment starts
                if (c == '/' && n == '*')
                {
                    inBlockComment = true;
                    i++; // skip '*'
                    continue;
                }

                // String starts
                if (c == '"' || c == '\'')
                {
                    inString = true;
                    stringQuote = c;
                    escape = false;
                    continue;
                }

                sb.Append(c);
            }

            return sb.ToString();
        }

        if (violations.Count > 0)
        {
            var report = violations
                .OrderBy(v => v.relPath, StringComparer.Ordinal)
                .ThenBy(v => v.line)
                .ThenBy(v => v.token, StringComparer.Ordinal)
                .Select(v => $"{v.relPath}:{v.line}:{v.token}")
                .ToArray();

            Assert.Fail(
                "UI%SimCore boundary violations (scripts/ui must not reference SimCore.Entities or SimCore.Systems):\n" +
                string.Join("\n", report)
            );
        }
    }

    private static string FindRepoRoot()
    {
        // Start from current directory and walk up until we find a repo-shaped root.
        // Requirements chosen for stability across test runners.
        var cur = new DirectoryInfo(Directory.GetCurrentDirectory());

        for (int i = 0; i < 12 && cur != null; i++)
        {
            var hasScripts = Directory.Exists(Path.Combine(cur.FullName, "scripts"));
            var hasDocs = Directory.Exists(Path.Combine(cur.FullName, "docs"));
            var hasSimCoreTests = Directory.Exists(Path.Combine(cur.FullName, "SimCore.Tests"));

            if (hasScripts && hasDocs && hasSimCoreTests)
            {
                return cur.FullName;
            }

            cur = cur.Parent;
        }

        Assert.Fail("Could not locate repo root (expected scripts/, docs/, SimCore.Tests/).");
        return Directory.GetCurrentDirectory();
    }

    private static string ToRepoRel(string repoRoot, string absPath)
    {
        var rel = Path.GetRelativePath(repoRoot, absPath);
        return NormalizeSep(rel);
    }

    private static string NormalizeSep(string p) => p.Replace('\\', '/');
}
