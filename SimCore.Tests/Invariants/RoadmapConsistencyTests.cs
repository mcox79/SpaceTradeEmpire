using System;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;

namespace SimCore.Tests.Invariants
{
    public sealed class RoadmapConsistencyTests
    {
        [Test]
        public void RoadmapConsistency_Scan_HardFailOnly()
        {
            var repoRoot = FindRepoRoot();
            var scriptPath = Path.Combine(repoRoot, "scripts", "tools", "Scan-RoadmapConsistency.ps1");
            Assert.That(File.Exists(scriptPath), Is.True, $"Missing scan script at: {scriptPath}");

            var exe = FindPowerShellExe();
            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"",
                WorkingDirectory = repoRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var p = Process.Start(psi);
            if (p == null) throw new InvalidOperationException("Failed to start PowerShell process.");

            var stdout = p.StandardOutput.ReadToEnd();
            var stderr = p.StandardError.ReadToEnd();
            p.WaitForExit();

            if (p.ExitCode != 0)
            {
                // The scan tool is responsible for writing a deterministic report; tests just surface it.
                var reportPath = Path.Combine(repoRoot, "docs", "generated", "roadmap_mismatches_v0.txt");
                var report = File.Exists(reportPath) ? File.ReadAllText(reportPath) : "<missing report>";
                var msg =
                    $"Roadmap consistency scan failed (exit={p.ExitCode}).\n" +
                    $"stdout:\n{stdout}\n" +
                    $"stderr:\n{stderr}\n" +
                    $"report ({reportPath}):\n{report}";
                Assert.Fail(msg);
            }
        }

        private static string FindPowerShellExe()
        {
            // Prefer pwsh when available, otherwise fallback to Windows PowerShell.
            var pwsh = "pwsh";
            if (CanStart(pwsh)) return pwsh;

            var powershell = "powershell";
            return powershell;
        }

        private static bool CanStart(string exe)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = "-NoProfile -Command \"$PSVersionTable.PSVersion.ToString()\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi);
                if (p == null) return false;
                p.WaitForExit(3000);
                return p.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private static string FindRepoRoot()
        {
            // Walk upward from test base directory until we find docs/55_GATES.md.
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            for (var i = 0; i < 12 && dir != null; i++)
            {
                var candidate = Path.Combine(dir.FullName, "docs", "55_GATES.md");
                if (File.Exists(candidate))
                {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate repo root (expected to find docs/55_GATES.md within 12 parent directories).");
        }
    }
}
