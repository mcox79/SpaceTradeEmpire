using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Text.Json;
using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Gen;
using SimCore.Systems;

namespace SimCore.Tests.SaveLoad;

public class SaveLoadWorldHashTests
{
    [Test]
    public void SaveLoad_RoundTrip_Preserves_WorldHash_And_Seed()
    {
        const int seed = 123;
        const int ticks = 500;

        var sim = new SimKernel(seed);
        GalaxyGenerator.Generate(sim.State, 20, 100f);

        for (int i = 0; i < ticks; i++)
        {
            sim.Step();
        }

        // GATE.S3_5.CONTENT_SUBSTRATE.003
        // Prove content pack identity is persisted through save%load by stamping a non-empty value pre-save.
        sim.State.ContentPackIdV0 = "test_pack_v0";
        sim.State.ContentPackVersionV0 = 7;

        var expectedPackId = sim.State.ContentPackIdV0;
        var expectedPackVersion = sim.State.ContentPackVersionV0;

        var beforeHash = sim.State.GetSignature();

        var json = sim.SaveToString();
        var savedSeed = ReadEnvelopeSeed(json);

        // Prove load restores identity independent of constructor seed by using a different value.
        var sim2 = new SimKernel(seed: 999);
        sim2.LoadFromString(json);

        var afterHash = sim2.State.GetSignature();

        var json2 = sim2.SaveToString();
        var resavedSeed = ReadEnvelopeSeed(json2);

        TestContext.Out.WriteLine($"SaveLoad Seed (expected): {seed}");
        TestContext.Out.WriteLine($"SaveLoad Seed (saved): {savedSeed}");
        TestContext.Out.WriteLine($"SaveLoad Seed (resaved): {resavedSeed}");
        TestContext.Out.WriteLine($"SaveLoad Before Hash: {beforeHash}");
        TestContext.Out.WriteLine($"SaveLoad After  Hash: {afterHash}");

        Assert.That(afterHash, Is.EqualTo(beforeHash), $"Save/load roundtrip changed world hash (Seed={seed}).");
        Assert.That(savedSeed, Is.EqualTo(seed), $"Save payload did not include expected seed (Seed={seed}).");
        Assert.That(resavedSeed, Is.EqualTo(seed), $"Loaded world did not preserve seed identity on re-save (Seed={seed}).");

        Assert.That(sim2.State.ContentPackIdV0, Is.EqualTo(expectedPackId),
            $"Save/load roundtrip did not preserve ContentPackIdV0 (Seed={seed} pack_id={expectedPackId} pack_version={expectedPackVersion}).");
        Assert.That(sim2.State.ContentPackVersionV0, Is.EqualTo(expectedPackVersion),
            $"Save/load roundtrip did not preserve ContentPackVersionV0 (Seed={seed} pack_id={expectedPackId} pack_version={expectedPackVersion}).");
    }

    [Test]
    public void SaveLoad_RoundTrip_Preserves_DiscoveryStateDigest_NoDrift()
    {
        const int seed = 456;

        var sim = new SimKernel(seed);
        GalaxyGenerator.Generate(sim.State, 20, 100f);

        // Prefer the real discovery set. If none exist yet, add a minimal deterministic fixture without clearing.
        if (sim.State.Intel.Discoveries.Count == 0)
        {
            sim.State.Intel.Discoveries["disc_fixture_001"] = new DiscoveryStateV0 { DiscoveryId = "disc_fixture_001", Phase = DiscoveryPhase.Seen };
        }

        var beforeDigest = ComputeDiscoveryDigestV0(sim.State);

        var json = sim.SaveToString();
        var sim2 = new SimKernel(seed: 999); // prove identity restored independent of ctor seed
        sim2.LoadFromString(json);

        var afterDigest = ComputeDiscoveryDigestV0(sim2.State);

        if (!string.Equals(beforeDigest, afterDigest, StringComparison.Ordinal))
        {
            var first = FindFirstDiscoveryDigestMismatchV0(beforeDigest, afterDigest);

            TestContext.Out.WriteLine($"DiscoveryDigest Seed: {seed}");
            TestContext.Out.WriteLine($"DiscoveryDigest FirstDiff DiscoveryId: {first.discoveryId}");
            TestContext.Out.WriteLine($"DiscoveryDigest FirstDiff Field: {first.fieldName}");
            TestContext.Out.WriteLine($"DiscoveryDigest Before: {first.beforeValue}");
            TestContext.Out.WriteLine($"DiscoveryDigest After : {first.afterValue}");
            TestContext.Out.WriteLine($"DiscoveryDigest Before PhaseBits: {first.beforePhaseBits}");
            TestContext.Out.WriteLine($"DiscoveryDigest After  PhaseBits: {first.afterPhaseBits}");

            Assert.Fail(
                $"DiscoveryState digest drift after save%load (Seed={seed} DiscoveryId={first.discoveryId} Field={first.fieldName} Before={first.beforeValue} After={first.afterValue} BeforePhaseBits={first.beforePhaseBits} AfterPhaseBits={first.afterPhaseBits}).");
        }

        Assert.That(afterDigest, Is.EqualTo(beforeDigest),
            $"DiscoveryState digest drift after save%load (Seed={seed}).");
    }

    [Test]
    public void SaveLoad_RoundTrip_Preserves_UnlockDigest_NoDrift()
    {
        const int seed = 456;

        var sim = new SimKernel(seed);
        GalaxyGenerator.Generate(sim.State, 20, 100f);

        // Prefer the real unlock set. If none exist yet, add a minimal deterministic fixture without clearing.
        if (sim.State.Intel.Unlocks.Count == 0)
        {
            sim.State.Intel.Unlocks["unlock_fixture_001"] = new UnlockContractV0
            {
                UnlockId = "unlock_fixture_001",
                Kind = UnlockKind.Permit,
                IsAcquired = false,
                IsBlocked = false
            };
        }

        var beforeDigest = ComputeUnlockDigestV0(sim.State);

        var json = sim.SaveToString();
        var sim2 = new SimKernel(seed: 999); // prove identity restored independent of ctor seed
        sim2.LoadFromString(json);

        var afterDigest = ComputeUnlockDigestV0(sim2.State);

        if (!string.Equals(beforeDigest, afterDigest, StringComparison.Ordinal))
        {
            var first = FindFirstUnlockDigestMismatchV0(beforeDigest, afterDigest);

            TestContext.Out.WriteLine($"UnlockDigest Seed: {seed}");
            TestContext.Out.WriteLine($"UnlockDigest FirstDiff UnlockId: {first.unlockId}");
            TestContext.Out.WriteLine($"UnlockDigest FirstDiff Field: {first.fieldName}");
            TestContext.Out.WriteLine($"UnlockDigest Before: {first.beforeValue}");
            TestContext.Out.WriteLine($"UnlockDigest After : {first.afterValue}");

            Assert.Fail(
                $"Unlock digest drift after save%load (Seed={seed} UnlockId={first.unlockId} Field={first.fieldName} Before={first.beforeValue} After={first.afterValue}).");
        }

        Assert.That(afterDigest, Is.EqualTo(beforeDigest),
            $"Unlock digest drift after save%load (Seed={seed}).");
    }

    [Test]
    public void DiscoveryState_ScenarioProof_Seed42_EmitsReportV0_NoTimestamps_StableOrdering()
    {
        const int seed = 42;
        const string fleetId = "f1";
        const string discoverNodeId = "node_001";
        const string hubNodeId = "hub";
        const string discoveryId = "disc_seed_42_001";

        var sim = new SimKernel(seed);

        // Minimal deterministic fixture for the scenario (avoid worldgen dependency in this proof).
        sim.State.Nodes[discoverNodeId] = new Node { Id = discoverNodeId, Name = "DiscoverNode" };
        sim.State.Nodes[hubNodeId] = new Node { Id = hubNodeId, Name = "Hub" };

        sim.State.PlayerLocationNodeId = hubNodeId;

        sim.State.Fleets[fleetId] = new Fleet
        {
            Id = fleetId,
            CurrentNodeId = discoverNodeId,
            State = FleetState.Idle
        };

        var report = new StringBuilder();
        report.AppendLine("DiscoveryStateScenarioProofV0");
        report.AppendLine($"Seed: {seed}");
        report.AppendLine($"FleetId: {fleetId}");
        report.AppendLine($"DiscoveryId: {discoveryId}");
        report.AppendLine("");
        report.AppendLine("Step%Action%ReasonCode%Phase");

        // 1) discover (Seen)
        IntelSystem.ApplySeenFromNodeEntry(sim.State, fleetId, discoverNodeId, new List<string> { discoveryId });

        var phase1 = sim.State.Intel.Discoveries.TryGetValue(discoveryId, out var d1) ? d1.Phase : DiscoveryPhase.Seen;
        report.AppendLine($"1%discover_seen%{DiscoveryReasonCode.Ok}%{phase1}");

        // 2) scan (Seen -> Scanned)
        var rcScan = IntelSystem.ApplyScan(sim.State, fleetId, discoveryId);
        var phase2 = sim.State.Intel.Discoveries.TryGetValue(discoveryId, out var d2) ? d2.Phase : DiscoveryPhase.Seen;
        report.AppendLine($"2%scan%{rcScan}%{phase2}");

        // 3) dock (move to hub for analyze eligibility)
        var f = sim.State.Fleets[fleetId];
        f.CurrentNodeId = hubNodeId;
        sim.State.Fleets[fleetId] = f;

        var phase3 = sim.State.Intel.Discoveries.TryGetValue(discoveryId, out var d3) ? d3.Phase : DiscoveryPhase.Seen;
        report.AppendLine($"3%dock%{DiscoveryReasonCode.Ok}%{phase3}");

        // 4) analyze (Scanned -> Analyzed) at hub
        var rcAnalyze = IntelSystem.ApplyAnalyze(sim.State, fleetId, discoveryId);
        var phase4 = sim.State.Intel.Discoveries.TryGetValue(discoveryId, out var d4) ? d4.Phase : DiscoveryPhase.Seen;
        report.AppendLine($"4%analyze%{rcAnalyze}%{phase4}");

        // Snapshot before save%load (deterministic ordering: DiscoveryId ordinal asc)
        var beforeSnapshot = SnapshotDiscoveriesV0(sim.State);

        // 5) save%load%verify
        var json = sim.SaveToString();
        var savedSeed = ReadEnvelopeSeed(json);

        var sim2 = new SimKernel(seed: 999);
        sim2.LoadFromString(json);

        var afterSnapshot = SnapshotDiscoveriesV0(sim2.State);

        var phaseAfter = sim2.State.Intel.Discoveries.TryGetValue(discoveryId, out var d5) ? d5.Phase : DiscoveryPhase.Seen;
        var rcSaveLoad = string.Equals(beforeSnapshot, afterSnapshot, StringComparison.Ordinal) ? "Ok" : "SnapshotMismatch";
        report.AppendLine($"5%save_load%{rcSaveLoad}%{phaseAfter}");

        report.AppendLine("");
        report.AppendLine("Discoveries (DiscoveryId asc):");
        report.Append(SnapshotDiscoveriesV0(sim2.State));

        var reportText = report.ToString().Replace("\r\n", "\n", StringComparison.Ordinal);

        var outPath = ResolveDiscoveryStateScenarioReportPathV0();
        WriteDeterministicTextFileV0(outPath, reportText);

        if (savedSeed != seed)
        {
            Assert.Fail($"DiscoveryState scenario proof save payload seed mismatch (ExpectedSeed={seed} SavedSeed={savedSeed}).");
        }

        if (!string.Equals(beforeSnapshot, afterSnapshot, StringComparison.Ordinal))
        {
            Assert.Fail($"DiscoveryState scenario proof snapshot mismatch after save%load (Seed={seed}).");
        }

        Assert.That(phaseAfter, Is.EqualTo(DiscoveryPhase.Analyzed), $"DiscoveryState scenario proof did not end in Analyzed after load (Seed={seed} DiscoveryId={discoveryId}).");

        Assert.That(reportText, Is.EqualTo(ExpectedDiscoveryStateScenarioReportV0()), "DiscoveryState scenario proof report content drifted.");
    }

    private static string SnapshotDiscoveriesV0(SimState state)
    {
        var ids = state.Intel.Discoveries.Keys.OrderBy(x => x, StringComparer.Ordinal).ToList();
        var lines = new List<string>(ids.Count);

        for (int i = 0; i < ids.Count; i++)
        {
            var id = ids[i];
            var phase = state.Intel.Discoveries.TryGetValue(id, out var d) ? d.Phase : DiscoveryPhase.Seen;
            lines.Add($"{id}\tPhase={phase}");
        }

        return string.Join("\n", lines) + (lines.Count == 0 ? "" : "\n");
    }

    private static string ResolveDiscoveryStateScenarioReportPathV0()
    {
        // Deterministic repo-root probing: walk upward from NUnit work directory until "docs" exists.
        var start = new DirectoryInfo(TestContext.CurrentContext.WorkDirectory);
        var dir = start;

        for (int i = 0; i < 10 && dir is not null; i++)
        {
            var docsDir = Path.Combine(dir.FullName, "docs");
            if (Directory.Exists(docsDir))
            {
                return Path.Combine(docsDir, "generated", "discovery_state_scenario_seed_42_v0.txt");
            }

            dir = dir.Parent;
        }

        // Fallback: relative path from current directory.
        return Path.Combine(Environment.CurrentDirectory, "docs", "generated", "discovery_state_scenario_seed_42_v0.txt");
    }

    private static void WriteDeterministicTextFileV0(string path, string contents)
    {
        var dir = Path.GetDirectoryName(path) ?? "";
        if (dir.Length != 0)
        {
            Directory.CreateDirectory(dir);
        }

        // Deterministic encoding: UTF-8 without BOM.
        File.WriteAllText(path, contents, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string ExpectedDiscoveryStateScenarioReportV0()
    {
        // Canonical v0 report (no timestamps; stable ordering).
        var lines = new[]
        {
            "DiscoveryStateScenarioProofV0",
            "Seed: 42",
            "FleetId: f1",
            "DiscoveryId: disc_seed_42_001",
            "",
            "Step%Action%ReasonCode%Phase",
            "1%discover_seen%Ok%Seen",
            "2%scan%Ok%Scanned",
            "3%dock%Ok%Scanned",
            "4%analyze%Ok%Analyzed",
            "5%save_load%Ok%Analyzed",
            "",
            "Discoveries (DiscoveryId asc):",
            "disc_seed_42_001\tPhase=Analyzed",
            ""
        };

        return string.Join("\n", lines);
    }

    // Digest format (v0): one line per discovery, sorted by DiscoveryId (Ordinal asc)
    // <DiscoveryId>\tPhaseBits=<int>\t<member>=<token>...
    // Members include all public instance fields%properties on DiscoveryStateV0, sorted by member name (Ordinal asc).
    private static string ComputeDiscoveryDigestV0(SimState state)
    {
        var ids = state.Intel.Discoveries.Keys.OrderBy(x => x, StringComparer.Ordinal).ToList();

        return string.Join("\n", ids.Select(id =>
        {
            var d = state.Intel.Discoveries[id];

            var phaseBits = PhaseToBits(TryGetPhase(d));
            var tokens = new List<string> { $"PhaseBits={phaseBits}" };

            foreach (var acc in DiscoveryStateAccessorsV0())
            {
                var v = acc.getter(d);
                tokens.Add($"{acc.name}={ValueToTokenV0(v)}");
            }

            return $"{id}\t{string.Join("\t", tokens)}";
        }));
    }

    private static (string discoveryId, string fieldName, string beforeValue, string afterValue, int beforePhaseBits, int afterPhaseBits)
        FindFirstDiscoveryDigestMismatchV0(string before, string after)
    {
        var a = ParseDigest(before);
        var b = ParseDigest(after);

        var allIds = a.Keys.Concat(b.Keys).Distinct().OrderBy(x => x, StringComparer.Ordinal);
        foreach (var id in allIds)
        {
            var hasA = a.TryGetValue(id, out var av);
            var hasB = b.TryGetValue(id, out var bv);

            if (!hasA || !hasB)
            {
                var beforeBits = hasA ? av.phaseBits : 0;
                var afterBits = hasB ? bv.phaseBits : 0;

                return (
                    id,
                    "MISSING_ROW",
                    hasA ? "present" : "null",
                    hasB ? "present" : "null",
                    beforeBits,
                    afterBits
                );
            }

            if (av.phaseBits != bv.phaseBits)
            {
                return (
                    id,
                    "PhaseBits",
                    av.phaseBits.ToString(CultureInfo.InvariantCulture),
                    bv.phaseBits.ToString(CultureInfo.InvariantCulture),
                    av.phaseBits,
                    bv.phaseBits
                );
            }

            var allFields = av.fields.Keys.Concat(bv.fields.Keys).Distinct().OrderBy(x => x, StringComparer.Ordinal);
            foreach (var f in allFields)
            {
                av.fields.TryGetValue(f, out var fvA);
                bv.fields.TryGetValue(f, out var fvB);

                if (!string.Equals(fvA, fvB, StringComparison.Ordinal))
                {
                    return (
                        id,
                        f,
                        fvA ?? "null",
                        fvB ?? "null",
                        av.phaseBits,
                        bv.phaseBits
                    );
                }
            }
        }

        // Should be unreachable if caller only invokes on mismatch, but keep deterministic.
        return ("", "", "", "", 0, 0);

        static Dictionary<string, (int phaseBits, Dictionary<string, string> fields)> ParseDigest(string digest)
        {
            var map = new Dictionary<string, (int phaseBits, Dictionary<string, string> fields)>(StringComparer.Ordinal);
            if (string.IsNullOrEmpty(digest)) return map;

            foreach (var line in digest.Split('\n'))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split('\t');
                if (parts.Length < 2) continue;

                var id = parts[0];
                var fields = new Dictionary<string, string>(StringComparer.Ordinal);
                var phaseBits = 0;

                for (int i = 1; i < parts.Length; i++)
                {
                    var kv = parts[i];
                    var eq = kv.IndexOf('=');
                    if (eq <= 0) continue;

                    var k = kv.Substring(0, eq);
                    var v = kv.Substring(eq + 1);

                    if (string.Equals(k, "PhaseBits", StringComparison.Ordinal) &&
                        int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pb))
                    {
                        phaseBits = pb;
                    }
                    else
                    {
                        fields[k] = v;
                    }
                }

                map[id] = (phaseBits, fields);
            }

            return map;
        }
    }

    // Digest format (v0): one line per unlock, sorted by UnlockId (Ordinal asc)
    // <UnlockId>\t<member>=<token>...
    // Members include all public instance fields%properties on UnlockContractV0, sorted by member name (Ordinal asc).
    private static string ComputeUnlockDigestV0(SimState state)
    {
        var ids = state.Intel.Unlocks.Keys.OrderBy(x => x, StringComparer.Ordinal).ToList();

        return string.Join("\n", ids.Select(id =>
        {
            var u = state.Intel.Unlocks[id];

            var tokens = new List<string>();
            foreach (var acc in UnlockAccessorsV0())
            {
                var v = acc.getter(u);
                tokens.Add($"{acc.name}={ValueToTokenV0(v)}");
            }

            return $"{id}\t{string.Join("\t", tokens)}";
        }));
    }

    private static (string unlockId, string fieldName, string beforeValue, string afterValue)
        FindFirstUnlockDigestMismatchV0(string before, string after)
    {
        var a = ParseDigest(before);
        var b = ParseDigest(after);

        var allIds = a.Keys.Concat(b.Keys).Distinct().OrderBy(x => x, StringComparer.Ordinal);
        foreach (var id in allIds)
        {
            var hasA = a.TryGetValue(id, out var av);
            var hasB = b.TryGetValue(id, out var bv);

            if (!hasA || !hasB)
            {
                return (
                    id,
                    "MISSING_ROW",
                    hasA ? "present" : "null",
                    hasB ? "present" : "null"
                );
            }

            var allFields = av.Keys.Concat(bv.Keys).Distinct().OrderBy(x => x, StringComparer.Ordinal);
            foreach (var f in allFields)
            {
                av.TryGetValue(f, out var fvA);
                bv.TryGetValue(f, out var fvB);

                if (!string.Equals(fvA, fvB, StringComparison.Ordinal))
                {
                    return (
                        id,
                        f,
                        fvA ?? "null",
                        fvB ?? "null"
                    );
                }
            }
        }

        // Should be unreachable if caller only invokes on mismatch, but keep deterministic.
        return ("", "", "", "");

        static Dictionary<string, Dictionary<string, string>> ParseDigest(string digest)
        {
            var map = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
            if (string.IsNullOrEmpty(digest)) return map;

            foreach (var line in digest.Split('\n'))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split('\t');
                if (parts.Length < 2) continue;

                var id = parts[0];
                var fields = new Dictionary<string, string>(StringComparer.Ordinal);

                for (int i = 1; i < parts.Length; i++)
                {
                    var kv = parts[i];
                    var eq = kv.IndexOf('=');
                    if (eq <= 0) continue;

                    var k = kv.Substring(0, eq);
                    var v = kv.Substring(eq + 1);

                    fields[k] = v;
                }

                map[id] = fields;
            }

            return map;
        }
    }

    private static IEnumerable<(string name, Func<UnlockContractV0, object?> getter)> UnlockAccessorsV0()
    {
        // Deterministic: member names sorted Ordinal asc; excludes indexers.
        var t = typeof(UnlockContractV0);

        var props = t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.GetIndexParameters().Length == 0 && p.GetMethod != null)
            .Select(p => (name: p.Name, getter: (Func<UnlockContractV0, object?>)(u => p.GetValue(u))))
            .ToList();

        var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public)
            .Select(f => (name: f.Name, getter: (Func<UnlockContractV0, object?>)(u => f.GetValue(u))))
            .ToList();

        // If both a field and property share the same name, prefer property (stable rule).
        var merged = new Dictionary<string, Func<UnlockContractV0, object?>>(StringComparer.Ordinal);
        foreach (var f in fields.OrderBy(x => x.name, StringComparer.Ordinal))
        {
            merged[f.name] = f.getter;
        }
        foreach (var p in props.OrderBy(x => x.name, StringComparer.Ordinal))
        {
            merged[p.name] = p.getter;
        }

        return merged.Keys.OrderBy(x => x, StringComparer.Ordinal).Select(k => (k, merged[k]));
    }

    private static DiscoveryPhase TryGetPhase(DiscoveryStateV0 d)
    {
        // Prefer a member actually named Phase when present. Deterministic fallback is Seen.
        var t = typeof(DiscoveryStateV0);

        var p = t.GetProperty("Phase", BindingFlags.Instance | BindingFlags.Public);
        if (p != null && p.PropertyType == typeof(DiscoveryPhase))
        {
            return (DiscoveryPhase)(p.GetValue(d) ?? DiscoveryPhase.Seen);
        }

        var f = t.GetField("Phase", BindingFlags.Instance | BindingFlags.Public);
        if (f != null && f.FieldType == typeof(DiscoveryPhase))
        {
            return (DiscoveryPhase)(f.GetValue(d) ?? DiscoveryPhase.Seen);
        }

        return DiscoveryPhase.Seen;
    }

    private static IEnumerable<(string name, Func<DiscoveryStateV0, object?> getter)> DiscoveryStateAccessorsV0()
    {
        // Deterministic: member names sorted Ordinal asc; excludes indexers.
        var t = typeof(DiscoveryStateV0);

        var props = t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.GetIndexParameters().Length == 0 && p.GetMethod != null)
            .Select(p => (name: p.Name, getter: (Func<DiscoveryStateV0, object?>)(d => p.GetValue(d))))
            .ToList();

        var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public)
            .Select(f => (name: f.Name, getter: (Func<DiscoveryStateV0, object?>)(d => f.GetValue(d))))
            .ToList();

        // If both a field and property share the same name, prefer property (stable rule).
        var merged = new Dictionary<string, Func<DiscoveryStateV0, object?>>(StringComparer.Ordinal);
        foreach (var f in fields.OrderBy(x => x.name, StringComparer.Ordinal))
        {
            merged[f.name] = f.getter;
        }
        foreach (var p in props.OrderBy(x => x.name, StringComparer.Ordinal))
        {
            merged[p.name] = p.getter;
        }

        return merged.Keys.OrderBy(x => x, StringComparer.Ordinal).Select(k => (k, merged[k]));
    }

    private static string ValueToTokenV0(object? v)
    {
        if (v == null) return "null";

        switch (v)
        {
            case string s:
                return EscapeTokenV0(s);
            case bool b:
                return b ? "true" : "false";
            case Enum e:
                return Convert.ToInt64(e, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
            case int i:
                return i.ToString(CultureInfo.InvariantCulture);
            case long l:
                return l.ToString(CultureInfo.InvariantCulture);
            case float f:
                return f.ToString("R", CultureInfo.InvariantCulture);
            case double d:
                return d.ToString("R", CultureInfo.InvariantCulture);
        }

        if (v is System.Collections.IDictionary dict)
        {
            var entries = new List<string>();
            foreach (System.Collections.DictionaryEntry de in dict)
            {
                var k = ValueToTokenV0(de.Key);
                var val = ValueToTokenV0(de.Value);
                entries.Add($"{k}:{val}");
            }

            entries.Sort(StringComparer.Ordinal);
            return "{" + string.Join(",", entries) + "}";
        }

        if (v is System.Collections.IEnumerable en && v is not string)
        {
            var items = new List<string>();
            foreach (var item in en)
            {
                items.Add(ValueToTokenV0(item));
            }

            // If it looks like a set, canonicalize by sorting tokens.
            if (IsSetLikeV0(v))
            {
                items.Sort(StringComparer.Ordinal);
            }

            return "[" + string.Join(",", items) + "]";
        }

        // Deterministic fallback: type name + ToString token (escaped)
        return EscapeTokenV0(v.ToString() ?? "");
    }

    private static bool IsSetLikeV0(object v)
    {
        var t = v.GetType();
        foreach (var i in t.GetInterfaces())
        {
            if (i.IsGenericType && i.GetGenericTypeDefinition().FullName == "System.Collections.Generic.ISet`1")
            {
                return true;
            }
        }
        return false;
    }

    private static string EscapeTokenV0(string s)
    {
        return s.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\t", "\\t", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal);
    }

    // GATE.S3_6.DISCOVERY_UNLOCK_CONTRACT.008
    // Unlock scenario proof v0: discover -> unlock -> effect realized -> save/load -> verify
    // Seed 42; no timestamps; stable ordering; failures write report first then exit nonzero.
    [Test]
    public void UnlockScenarioProof_Seed42_EmitsReportV0_NoTimestamps_StableOrdering()
    {
        const int seed = 42;
        const string fleetId = "f1";
        const string discoverNodeId = "node_001";
        const string hubNodeId = "hub";
        const string discoveryId = "disc_seed_42_001";
        const string unlockId = "unlock_broker_seed_42_001";
        const int grossCredits = 1000;

        var sim = new SimKernel(seed);

        // Minimal deterministic fixture.
        sim.State.Nodes[discoverNodeId] = new Node { Id = discoverNodeId, Name = "DiscoverNode" };
        sim.State.Nodes[hubNodeId] = new Node { Id = hubNodeId, Name = "Hub" };
        sim.State.PlayerLocationNodeId = hubNodeId;
        sim.State.Fleets[fleetId] = new Fleet { Id = fleetId, CurrentNodeId = discoverNodeId, State = FleetState.Idle };

        var report = new StringBuilder();
        report.AppendLine("UnlockScenarioProofV0");
        report.AppendLine($"Seed: {seed}");
        report.AppendLine($"FleetId: {fleetId}");
        report.AppendLine($"DiscoveryId: {discoveryId}");
        report.AppendLine($"UnlockId: {unlockId}");
        report.AppendLine("");
        report.AppendLine("Step%Action%ReasonCode%Effect");

        // 1) discover (Seen)
        IntelSystem.ApplySeenFromNodeEntry(sim.State, fleetId, discoverNodeId, new List<string> { discoveryId });
        var phase1 = sim.State.Intel.Discoveries.TryGetValue(discoveryId, out var dv1) ? dv1.Phase : DiscoveryPhase.Seen;
        report.AppendLine($"1%discover_seen%{DiscoveryReasonCode.Ok}%Phase={phase1}");

        // 2) scan (Seen -> Scanned)
        var rcScan = IntelSystem.ApplyScan(sim.State, fleetId, discoveryId);
        var phase2 = sim.State.Intel.Discoveries.TryGetValue(discoveryId, out var dv2) ? dv2.Phase : DiscoveryPhase.Seen;
        report.AppendLine($"2%scan%{rcScan}%Phase={phase2}");

        // 3) dock (move fleet to hub)
        var fleet = sim.State.Fleets[fleetId];
        fleet.CurrentNodeId = hubNodeId;
        sim.State.Fleets[fleetId] = fleet;
        var phase3 = sim.State.Intel.Discoveries.TryGetValue(discoveryId, out var dv3) ? dv3.Phase : DiscoveryPhase.Seen;
        report.AppendLine($"3%dock%{DiscoveryReasonCode.Ok}%Phase={phase3}");

        // 4) analyze (Scanned -> Analyzed) at hub
        var rcAnalyze = IntelSystem.ApplyAnalyze(sim.State, fleetId, discoveryId);
        var phase4 = sim.State.Intel.Discoveries.TryGetValue(discoveryId, out var dv4) ? dv4.Phase : DiscoveryPhase.Seen;
        report.AppendLine($"4%analyze%{rcAnalyze}%Phase={phase4}");

        // 5) seed unlock in book (Broker kind; acquisition verb is 'analyze at hub' in this scenario)
        sim.State.Intel.Unlocks[unlockId] = new UnlockContractV0
        {
            UnlockId = unlockId,
            Kind = UnlockKind.Broker,
            IsAcquired = false,
            IsBlocked = false
        };

        // 6) acquire unlock
        var rcAcquire = IntelSystem.GetAcquireUnlockReasonCode(sim.State, unlockId);
        if (rcAcquire == UnlockReasonCode.Ok)
        {
            var u = sim.State.Intel.Unlocks[unlockId];
            u.IsAcquired = true;
            sim.State.Intel.Unlocks[unlockId] = u;
        }
        var rcAcquireAfter = IntelSystem.GetAcquireUnlockReasonCode(sim.State, unlockId);
        report.AppendLine($"5%seed_unlock%{UnlockReasonCode.Ok}%IsAcquired=false");
        report.AppendLine($"6%acquire%{rcAcquire}%IsAcquired={sim.State.Intel.Unlocks[unlockId].IsAcquired.ToString().ToLowerInvariant()}");

        // 7) verify economic effect: Broker unlock -> fee waived (bps=0)
        var feeBpsBeforeSave = MarketSystem.GetEffectiveTransactionFeeBps(sim.State);
        var feeCreditsBeforeSave = MarketSystem.ComputeTransactionFeeCredits(sim.State, grossCredits);
        report.AppendLine($"7%broker_effect%{UnlockReasonCode.Ok}%FeeBps={feeBpsBeforeSave},FeeCredits={feeCreditsBeforeSave}");

        // Snapshot unlocks pre-save (filtered to proof unlock only; verb unlocks are internal mechanism)
        var beforeSnapshot = SnapshotUnlocksV0(sim.State, unlockId);

        // 8) save/load/verify
        var json = sim.SaveToString();
        var savedSeed = ReadEnvelopeSeed(json);

        var sim2 = new SimKernel(seed: 999); // prove identity restored independent of ctor seed
        sim2.LoadFromString(json);

        var afterSnapshot = SnapshotUnlocksV0(sim2.State, unlockId);
        var feeBpsAfterLoad = MarketSystem.GetEffectiveTransactionFeeBps(sim2.State);
        var feeCreditsAfterLoad = MarketSystem.ComputeTransactionFeeCredits(sim2.State, grossCredits);
        var rcAcquireAfterLoad = IntelSystem.GetAcquireUnlockReasonCode(sim2.State, unlockId);
        var rcSaveLoad = string.Equals(beforeSnapshot, afterSnapshot, StringComparison.Ordinal) ? "Ok" : "SnapshotMismatch";
        report.AppendLine($"8%save_load%{rcSaveLoad}%FeeBps={feeBpsAfterLoad},FeeCredits={feeCreditsAfterLoad},AcquireRc={rcAcquireAfterLoad}");

        report.AppendLine("");
        report.AppendLine("Unlocks (UnlockId asc):");
        report.Append(SnapshotUnlocksV0(sim2.State, unlockId));

        var reportText = report.ToString().Replace("\r\n", "\n", StringComparison.Ordinal);

        var outPath = ResolveUnlockScenarioReportPathV0();
        WriteDeterministicTextFileV0(outPath, reportText);

        if (savedSeed != seed)
        {
            Assert.Fail($"Unlock scenario proof save payload seed mismatch (ExpectedSeed={seed} SavedSeed={savedSeed}).");
        }

        if (!string.Equals(beforeSnapshot, afterSnapshot, StringComparison.Ordinal))
        {
            Assert.Fail($"Unlock scenario proof snapshot mismatch after save%load (Seed={seed}).");
        }

        Assert.That(feeBpsBeforeSave, Is.EqualTo(0),
            $"Unlock scenario proof: Broker unlock must waive fees pre-save (Seed={seed} FeeBps={feeBpsBeforeSave}).");
        Assert.That(feeCreditsBeforeSave, Is.EqualTo(0),
            $"Unlock scenario proof: Broker unlock must yield zero fee credits pre-save (Seed={seed}).");
        Assert.That(feeBpsAfterLoad, Is.EqualTo(0),
            $"Unlock scenario proof: Broker unlock must persist fee waiver after save%load (Seed={seed} FeeBps={feeBpsAfterLoad}).");
        Assert.That(feeCreditsAfterLoad, Is.EqualTo(0),
            $"Unlock scenario proof: Broker unlock must persist zero fee credits after save%load (Seed={seed}).");
        Assert.That(rcAcquireAfterLoad, Is.EqualTo(UnlockReasonCode.AlreadyAcquired),
            $"Unlock scenario proof: unlock must remain acquired after save%load (Seed={seed} Rc={rcAcquireAfterLoad}).");

        Assert.That(reportText, Is.EqualTo(ExpectedUnlockScenarioReportV0()), "Unlock scenario proof report content drifted.");
    }

    [Test]
    public void ExpeditionProgram_MidExpedition_SaveLoad_Stable()
    {
        // GATE.S3_6.EXPEDITION_PROGRAMS.002
        // Proof: save/load mid-expedition preserves world hash and ExpeditionTicksRemaining.
        const int seed = 42;
        const string leadId = "lead_exped_saveload_42";
        const string fleetId = "fleet_exped_saveload_42";

        var sim = new SimKernel(seed);

        sim.State.Intel.Discoveries[leadId] = new SimCore.Entities.DiscoveryStateV0
        {
            DiscoveryId = leadId,
            Phase = SimCore.Entities.DiscoveryPhase.Seen
        };

        sim.State.Fleets[fleetId] = new SimCore.Entities.Fleet
        {
            Id = fleetId,
            State = SimCore.Entities.FleetState.Idle
        };

        var pid = sim.State.CreateExpeditionProgramV0(leadId, fleetId, cadenceTicks: 1);
        sim.State.Programs.Instances[pid].Status = SimCore.Programs.ProgramStatus.Running;
        sim.State.Programs.Instances[pid].NextRunTick = sim.State.Tick;

        // Advance partway through an expedition but not to completion.
        var midPoint = SimCore.Tweaks.IntelTweaksV0.ExpeditionDurationTicks / 2;
        for (int i = 0; i < midPoint; i++)
        {
            sim.Step();
        }

        var beforeHash = sim.State.GetSignature();
        var beforeRemaining = sim.State.Programs.Instances[pid].ExpeditionTicksRemaining;

        var json = sim.SaveToString();
        var savedSeed = ReadEnvelopeSeed(json);

        var sim2 = new SimKernel(seed: 999);
        sim2.LoadFromString(json);

        var afterHash = sim2.State.GetSignature();
        var afterRemaining = sim2.State.Programs.Instances[pid].ExpeditionTicksRemaining;

        TestContext.Out.WriteLine($"ExpeditionMidSaveLoad Seed: {seed}");
        TestContext.Out.WriteLine($"ExpeditionMidSaveLoad SavedSeed: {savedSeed}");
        TestContext.Out.WriteLine($"ExpeditionMidSaveLoad BeforeHash: {beforeHash}");
        TestContext.Out.WriteLine($"ExpeditionMidSaveLoad AfterHash:  {afterHash}");
        TestContext.Out.WriteLine($"ExpeditionMidSaveLoad TicksRemaining Before: {beforeRemaining}");
        TestContext.Out.WriteLine($"ExpeditionMidSaveLoad TicksRemaining After:  {afterRemaining}");

        Assert.That(savedSeed, Is.EqualTo(seed),
            $"ExpeditionProgram_MidExpedition_SaveLoad_Stable: save payload seed mismatch (Seed={seed}).");
        Assert.That(afterHash, Is.EqualTo(beforeHash),
            $"ExpeditionProgram_MidExpedition_SaveLoad_Stable: world hash changed after save/load (Seed={seed}).");
        Assert.That(afterRemaining, Is.EqualTo(beforeRemaining),
            $"ExpeditionProgram_MidExpedition_SaveLoad_Stable: ExpeditionTicksRemaining drifted after save/load (Seed={seed} Before={beforeRemaining} After={afterRemaining}).");
    }

    private static string SnapshotUnlocksV0(SimState state, string? filterUnlockId = null)
    {
        if (state.Intel?.Unlocks is null) return "";
        var ids = state.Intel.Unlocks.Keys
            .Where(k => filterUnlockId is null || string.Equals(k, filterUnlockId, StringComparison.Ordinal))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();
        var lines = new List<string>(ids.Count);

        foreach (var id in ids)
        {
            if (!state.Intel.Unlocks.TryGetValue(id, out var u)) continue;
            lines.Add($"{id}\tKind={u.Kind}\tIsAcquired={u.IsAcquired.ToString().ToLowerInvariant()}\tIsBlocked={u.IsBlocked.ToString().ToLowerInvariant()}");
        }

        return string.Join("\n", lines) + (lines.Count == 0 ? "" : "\n");
    }

    private static string ResolveUnlockScenarioReportPathV0()
    {
        var start = new DirectoryInfo(TestContext.CurrentContext.WorkDirectory);
        var dir = start;

        for (int i = 0; i < 10 && dir is not null; i++)
        {
            var docsDir = Path.Combine(dir.FullName, "docs");
            if (Directory.Exists(docsDir))
            {
                return Path.Combine(docsDir, "generated", "unlock_scenario_seed_42_v0.txt");
            }

            dir = dir.Parent;
        }

        return Path.Combine(Environment.CurrentDirectory, "docs", "generated", "unlock_scenario_seed_42_v0.txt");
    }

    private static string ExpectedUnlockScenarioReportV0()
    {
        // Canonical v0 report (no timestamps; stable ordering).
        var lines = new[]
        {
            "UnlockScenarioProofV0",
            "Seed: 42",
            "FleetId: f1",
            "DiscoveryId: disc_seed_42_001",
            "UnlockId: unlock_broker_seed_42_001",
            "",
            "Step%Action%ReasonCode%Effect",
            "1%discover_seen%Ok%Phase=Seen",
            "2%scan%Ok%Phase=Scanned",
            "3%dock%Ok%Phase=Scanned",
            "4%analyze%Ok%Phase=Analyzed",
            "5%seed_unlock%Ok%IsAcquired=false",
            "6%acquire%Ok%IsAcquired=true",
            "7%broker_effect%Ok%FeeBps=0,FeeCredits=0",
            "8%save_load%Ok%FeeBps=0,FeeCredits=0,AcquireRc=AlreadyAcquired",
            "",
            "Unlocks (UnlockId asc):",
            "unlock_broker_seed_42_001\tKind=Broker\tIsAcquired=true\tIsBlocked=false",
            ""
        };

        return string.Join("\n", lines);
    }

    // GATE.S3_6.RUMOR_INTEL_MIN.002
    [Test]
    public void SaveLoad_RoundTrip_Preserves_RumorLeadDigest_NoDrift()
    {
        const int seed = 42;

        var sim = new SimKernel(seed);
        GalaxyGenerator.Generate(sim.State, 20, 100f);

        var minExpected = SimCore.Tweaks.IntelTweaksV0.MinRumorLeadsPerSeed;
        Assert.That(sim.State.Intel.RumorLeads.Count, Is.GreaterThanOrEqualTo(minExpected),
            $"RumorLead count after Generate (Seed={seed}) must be >= {minExpected}. Got {sim.State.Intel.RumorLeads.Count}.");

        var beforeDigest = ComputeRumorLeadDigestV0(sim.State);

        var json = sim.SaveToString();
        var savedSeed = ReadEnvelopeSeed(json);

        var sim2 = new SimKernel(seed: 999);
        sim2.LoadFromString(json);

        var afterDigest = ComputeRumorLeadDigestV0(sim2.State);

        if (!string.Equals(beforeDigest, afterDigest, StringComparison.Ordinal))
        {
            var first = FindFirstRumorLeadDigestMismatchV0(beforeDigest, afterDigest);
            TestContext.Out.WriteLine($"RumorLeadDigest Seed: {seed}");
            TestContext.Out.WriteLine($"RumorLeadDigest FirstDiff LeadId: {first.leadId}");
            TestContext.Out.WriteLine($"RumorLeadDigest FirstDiff Field: {first.fieldName}");
            TestContext.Out.WriteLine($"RumorLeadDigest Before: {first.beforeValue}");
            TestContext.Out.WriteLine($"RumorLeadDigest After : {first.afterValue}");
            Assert.Fail($"RumorLead digest drift after save%load (Seed={seed} LeadId={first.leadId} Field={first.fieldName} Before={first.beforeValue} After={first.afterValue}).");
        }

        Assert.That(savedSeed, Is.EqualTo(seed),
            $"RumorLead digest test: seed mismatch (Seed={seed} SavedSeed={savedSeed}).");
        Assert.That(afterDigest, Is.EqualTo(beforeDigest),
            $"RumorLead digest drift after save%load (Seed={seed}).");
    }

    private static string ComputeRumorLeadDigestV0(SimState state)
    {
        if (state.Intel?.RumorLeads is null) return "";

        var ids = state.Intel.RumorLeads.Keys.OrderBy(x => x, StringComparer.Ordinal).ToList();

        return string.Join("\n", ids.Select(id =>
        {
            var lead = state.Intel.RumorLeads[id];
            var tokens = new List<string>();
            foreach (var acc in RumorLeadAccessorsV0())
                tokens.Add($"{acc.name}={ValueToTokenV0(acc.getter(lead))}");
            return $"{id}\t{string.Join("\t", tokens)}";
        }));
    }

    private static (string leadId, string fieldName, string beforeValue, string afterValue)
        FindFirstRumorLeadDigestMismatchV0(string before, string after)
    {
        var a = ParseLeadDigest(before);
        var b = ParseLeadDigest(after);

        var allIds = a.Keys.Concat(b.Keys).Distinct().OrderBy(x => x, StringComparer.Ordinal);
        foreach (var id in allIds)
        {
            var hasA = a.TryGetValue(id, out var av);
            var hasB = b.TryGetValue(id, out var bv);

            if (!hasA || !hasB)
                return (id, "MISSING_ROW", hasA ? "present" : "null", hasB ? "present" : "null");

            var allFields = av!.Keys.Concat(bv!.Keys).Distinct().OrderBy(x => x, StringComparer.Ordinal);
            foreach (var f in allFields)
            {
                av.TryGetValue(f, out var fvA);
                bv.TryGetValue(f, out var fvB);
                if (!string.Equals(fvA, fvB, StringComparison.Ordinal))
                    return (id, f, fvA ?? "null", fvB ?? "null");
            }
        }

        return ("", "", "", "");

        static Dictionary<string, Dictionary<string, string>> ParseLeadDigest(string digest)
        {
            var map = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
            if (string.IsNullOrEmpty(digest)) return map;
            foreach (var line in digest.Split('\n'))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = line.Split('\t');
                if (parts.Length < 2) continue;
                var id = parts[0];
                var fields = new Dictionary<string, string>(StringComparer.Ordinal);
                for (int i = 1; i < parts.Length; i++)
                {
                    var eq = parts[i].IndexOf('=');
                    if (eq <= 0) continue;
                    fields[parts[i].Substring(0, eq)] = parts[i].Substring(eq + 1);
                }
                map[id] = fields;
            }
            return map;
        }
    }

    private static IEnumerable<(string name, Func<RumorLead, object?> getter)> RumorLeadAccessorsV0()
    {
        var t = typeof(RumorLead);

        var merged = new Dictionary<string, Func<RumorLead, object?>>(StringComparer.Ordinal);

        foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public)
                           .OrderBy(x => x.Name, StringComparer.Ordinal))
            merged[f.Name] = r => f.GetValue(r);

        foreach (var p in t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                           .Where(p => p.GetIndexParameters().Length == 0 && p.GetMethod != null)
                           .OrderBy(x => x.Name, StringComparer.Ordinal))
            merged[p.Name] = r => p.GetValue(r);

        return merged.Keys.OrderBy(x => x, StringComparer.Ordinal).Select(k => (k, merged[k]));
    }

    private static int PhaseToBits(DiscoveryPhase phase)
    {
        // Deterministic derived bitfield from phase ladder:
        // Seen implies Seen; Scanned implies Seen+Scanned; Analyzed implies Seen+Scanned+Analyzed.
        return phase switch
        {
            DiscoveryPhase.Seen => 1,
            DiscoveryPhase.Scanned => 1 | 2,
            DiscoveryPhase.Analyzed => 1 | 2 | 4,
            _ => 0
        };
    }

    private static int ReadEnvelopeSeed(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                Assert.Fail("Save JSON root is not an object. Seed identity envelope is required.");
                return 0;
            }

            if (!root.TryGetProperty("Seed", out var seedEl))
            {
                Assert.Fail("Save JSON missing Seed property. Seed identity envelope is required.");
                return 0;
            }

            if (!root.TryGetProperty("State", out _))
            {
                Assert.Fail("Save JSON missing State property. Seed identity envelope is required.");
                return 0;
            }

            if (seedEl.ValueKind == JsonValueKind.Number && seedEl.TryGetInt32(out var seed))
            {
                return seed;
            }

            Assert.Fail("Save JSON Seed property is not an int32 number.");
            return 0;
        }
        catch (JsonException je)
        {
            Assert.Fail($"Save JSON was not valid JSON: {je.Message}");
            return 0;
        }
    }
}
