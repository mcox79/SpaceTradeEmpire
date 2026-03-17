using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using NUnit.Framework;

namespace SimCore.Tests.Invariants;

// GATE.S9.BALANCE.LOCK.001: Detect unintentional tweak value drift.
// Compares all *TweaksV0 const fields against a committed baseline.
[TestFixture]
public class BalanceLockTests
{
    private static readonly string BaselinePath = Path.Combine(
        TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "docs", "tweaks", "balance_baseline_v0.json");

    [Test]
    public void BalanceLock_AllTweakConsts_MatchBaseline()
    {
        var currentValues = SnapshotAllTweakConsts();

        var resolvedPath = Path.GetFullPath(BaselinePath);
        if (!File.Exists(resolvedPath))
        {
            // First run: generate baseline.
            var json = JsonSerializer.Serialize(currentValues, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(resolvedPath, json);
            Assert.Pass($"Baseline generated at {resolvedPath} with {currentValues.Count} values. Commit this file.");
            return;
        }

        var baselineJson = File.ReadAllText(resolvedPath);
        var baseline = JsonSerializer.Deserialize<SortedDictionary<string, JsonElement>>(baselineJson)
            ?? new SortedDictionary<string, JsonElement>();

        var drifts = new List<string>();

        // Check for changed or removed values.
        foreach (var (key, baseVal) in baseline)
        {
            if (!currentValues.ContainsKey(key))
            {
                drifts.Add($"REMOVED: {key} (was {baseVal})");
                continue;
            }

            var curStr = currentValues[key].ToString();
            var baseStr = baseVal.ToString();
            if (curStr != baseStr)
                drifts.Add($"CHANGED: {key} was {baseStr} now {curStr}");
        }

        // Check for new values (informational — new tweaks are expected).
        foreach (var key in currentValues.Keys)
        {
            if (!baseline.ContainsKey(key))
                drifts.Add($"ADDED: {key} = {currentValues[key]}");
        }

        if (drifts.Count > 0)
        {
            var msg = $"Balance drift detected ({drifts.Count} changes):\n" + string.Join("\n", drifts);
            msg += "\n\nIf intentional, regenerate baseline: delete docs/tweaks/balance_baseline_v0.json and re-run this test.";
            Assert.Fail(msg);
        }
    }

    [Test]
    public void BalanceLock_SnapshotIsNotEmpty()
    {
        var values = SnapshotAllTweakConsts();
        Assert.That(values.Count, Is.GreaterThan(100), "Expected > 100 tweak constants across all TweaksV0 classes");
    }


    private static SortedDictionary<string, object> SnapshotAllTweakConsts()
    {
        var result = new SortedDictionary<string, object>(StringComparer.Ordinal);
        var assembly = typeof(SimState).Assembly;

        foreach (var type in assembly.GetTypes())
        {
            if (!type.Name.EndsWith("TweaksV0", StringComparison.Ordinal)) continue;
            if (!type.IsAbstract || !type.IsSealed) continue; // static classes only

            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (!field.IsLiteral) continue; // const only

                var key = $"{type.Name}.{field.Name}";
                var value = field.GetRawConstantValue();
                if (value != null)
                    result[key] = value;
            }
        }

        return result;
    }
}
