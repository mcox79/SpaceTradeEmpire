using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace SimCore.Tests.Invariants;

[TestFixture]
public sealed class RuntimeFileContractTests
{
    [Test]
    public void SimCore_MustNotUse_SystemIO_ForRuntimeFileAccess()
    {
        // Repo root from test bin folder:
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "SimCore")))
            dir = dir.Parent;

        Assert.That(dir, Is.Not.Null, "Could not locate repo root containing SimCore/");

        var simCoreDir = Path.Combine(dir!.FullName, "SimCore");
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
                    .Select(p => $"{MakeRepoRelative(dir!.FullName, f)} contains '{p}'");
            })
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToArray();

        Assert.That(hits.Length, Is.EqualTo(0),
            "Runtime File Contract violation in SimCore:\n" + string.Join("\n", hits));
    }

    private static string MakeRepoRelative(string root, string fullPath)
    {
        root = root.Replace('\\', '/').TrimEnd('/');
        fullPath = fullPath.Replace('\\', '/');
        if (fullPath.StartsWith(root + "/", StringComparison.Ordinal))
            return fullPath[(root.Length + 1)..];
        return fullPath;
    }
}
