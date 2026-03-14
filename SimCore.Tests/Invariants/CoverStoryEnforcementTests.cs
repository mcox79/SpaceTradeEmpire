using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace SimCore.Tests.Invariants;

// GATE.X.COVER_STORY.CI.001: Ensure "fracture" is not used in player-facing
// strings before revelation. SimBridge display names and GDScript UI text must
// use cover-story naming (e.g., "Structural Resonance" not "Fracture Drive")
// unless guarded by a revelation state check.
public class CoverStoryEnforcementTests
{
    // Allowlisted patterns that may contain "fracture" without revelation guards.
    // These are code comments, system-internal references, gate markers, or
    // post-revelation content that is appropriately gated.
    private static readonly string[] AllowlistPatterns = new[]
    {
        "// ",           // Code comments
        "/// ",          // XML doc comments
        "GATE.",         // Gate markers
        "fracture_",     // Snake_case identifiers (not display text)
        "Fracture",      // PascalCase type/class names
        "\"fracture\"",  // String literal identifiers (not display text)
        "FractureExposure", // State field names
        "FractureTraveling", // Enum values
        "FractureWeight",    // System names
        "FractureSystem",    // System names
        "FractureTarget",    // State field names
        "FractureUnlocked",  // State flag
        "FractureDiscovery",  // Discovery flag
        "fracture_jump",     // Event identifiers
        "fracture_drive",    // Module identifiers
        "fracture_travel",   // Command identifiers
        "offlane_fracture",  // Route identifiers
        "cover_story",       // Meta-references to the cover story system itself
        "pre-revelation",    // Documentation about the cover story
        "post-revelation",   // Documentation about the cover story
        "has_r",             // Revelation check guards
        "HasRevelation",     // Revelation check guards
        "revelation_count",  // Progress tracking
        "RevealedFlags",     // Story state access
        "CurrentAct",        // Story act check
        "REVELATION",        // Trigger token names
    };

    [Test]
    public void SimBridge_NoUnguardedFractureInDisplayText()
    {
        var repoRoot = FindRepoRoot();
        var bridgeDir = Path.Combine(repoRoot, "scripts", "bridge");
        if (!Directory.Exists(bridgeDir))
        {
            Assert.Fail($"Bridge directory not found: {bridgeDir}");
            return;
        }

        var violations = new List<string>();
        var fracturePattern = new Regex(@"\bfracture\b", RegexOptions.IgnoreCase);

        foreach (var file in Directory.GetFiles(bridgeDir, "SimBridge.*.cs"))
        {
            var lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (!fracturePattern.IsMatch(line)) continue;

                // Check if line is allowlisted
                bool allowed = AllowlistPatterns.Any(p =>
                    line.Contains(p, StringComparison.OrdinalIgnoreCase));
                if (allowed) continue;

                // Check if this is inside a display string (quoted text that would be shown to player)
                // Look for patterns like: ["label"] = "...fracture..."
                if (IsPlayerFacingString(line))
                {
                    violations.Add($"{Path.GetFileName(file)}:{i + 1}: {line.Trim()}");
                }
            }
        }

        if (violations.Count > 0)
        {
            Assert.Fail($"Found {violations.Count} unguarded 'fracture' in player-facing strings:\n" +
                string.Join("\n", violations));
        }
    }

    // Files that are inherently post-revelation or post-game (fracture naming is appropriate).
    private static readonly string[] AllowlistedGdFiles = new[]
    {
        "epilogue_data.gd",          // Post-game epilogue text — always post-revelation
        "fracture_travel_panel.gd",  // Only accessible after FractureUnlocked
        "DiscoverySitePanel.gd",     // Fracture status gated behind FractureUnlocked check
        "victory_screen.gd",         // Post-game victory screen
        "loss_screen.gd",            // Post-game loss screen
    };

    [Test]
    public void GDScript_NoUnguardedFractureInUIText()
    {
        var repoRoot = FindRepoRoot();
        var uiDir = Path.Combine(repoRoot, "scripts", "ui");
        if (!Directory.Exists(uiDir))
        {
            return; // UI dir may not exist yet in early development
        }

        var violations = new List<string>();
        // Match "fracture" in quoted strings in GDScript (not in comments or identifiers)
        var quotedFracturePattern = new Regex(@"""[^""]*\bfracture\b[^""]*""", RegexOptions.IgnoreCase);

        foreach (var file in Directory.GetFiles(uiDir, "*.gd", SearchOption.AllDirectories))
        {
            // Skip allowlisted files (post-revelation / post-game content).
            var fileName = Path.GetFileName(file);
            if (AllowlistedGdFiles.Any(a =>
                string.Equals(a, fileName, StringComparison.OrdinalIgnoreCase)))
                continue;

            var lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var trimmed = line.TrimStart();

                // Skip comments
                if (trimmed.StartsWith("#")) continue;

                if (!quotedFracturePattern.IsMatch(line)) continue;

                // Check if line has a revelation guard nearby (within 10 lines before)
                bool guarded = false;
                for (int j = Math.Max(0, i - 10); j < i; j++)
                {
                    if (lines[j].Contains("has_r", StringComparison.OrdinalIgnoreCase) ||
                        lines[j].Contains("revelation", StringComparison.OrdinalIgnoreCase) ||
                        lines[j].Contains("current_act", StringComparison.OrdinalIgnoreCase))
                    {
                        guarded = true;
                        break;
                    }
                }
                if (guarded) continue;

                violations.Add($"{Path.GetFileName(file)}:{i + 1}: {line.Trim()}");
            }
        }

        if (violations.Count > 0)
        {
            Assert.Fail($"Found {violations.Count} unguarded 'fracture' in GDScript UI text:\n" +
                string.Join("\n", violations));
        }
    }

    private static bool IsPlayerFacingString(string line)
    {
        // Match dictionary assignments with quoted strings: ["key"] = "...fracture..."
        // Or simple string assignments: var x = "...fracture..."
        var trimmed = line.Trim();
        return Regex.IsMatch(trimmed, @"\[""[^""]*""\]\s*=\s*""[^""]*fracture[^""]*""", RegexOptions.IgnoreCase) ||
               Regex.IsMatch(trimmed, @"=\s*""[^""]*fracture[^""]*""", RegexOptions.IgnoreCase);
    }

    private static string FindRepoRoot()
    {
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "CLAUDE.md")))
                return dir;
            dir = Path.GetDirectoryName(dir);
        }
        return AppDomain.CurrentDomain.BaseDirectory;
    }
}
