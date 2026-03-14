using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace SimCore.Systems;

public static class SerializationSystem
{
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        IncludeFields = true
    };

    // Seed identity is not necessarily part of SimState's public surface.
    // We persist it in the save payload and attach it to the SimState instance at runtime.
    private sealed class SeedBox
    {
        public int Seed;
        public SeedBox(int seed) => Seed = seed;
    }

    private static readonly ConditionalWeakTable<SimState, SeedBox> _seedIdentity = new();

    // GATE.S9.SAVE.MIGRATION.001: Current save format version.
    public const int CurrentVersion = 2;

    private sealed class SaveEnvelope
    {
        public int Version { get; set; } = 1;
        public int Seed { get; set; }
        public SimState State { get; set; } = null!;
    }

    public static void AttachSeed(SimState state, int seed)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));

        _seedIdentity.Remove(state);
        _seedIdentity.Add(state, new SeedBox(seed));
    }

    public static bool TryGetAttachedSeed(SimState state, out int seed)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));

        if (_seedIdentity.TryGetValue(state, out var box))
        {
            seed = box.Seed;
            return true;
        }

        seed = 0;
        return false;
    }

    public static string Serialize(SimState state)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));

        var envelope = new SaveEnvelope
        {
            Version = CurrentVersion,
            Seed = ExtractSeed(state),
            State = state
        };

        return JsonSerializer.Serialize(envelope, _options);
    }

    public static SimState Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("JSON must be non-empty.", nameof(json));

        // Backwards compatible:
        // - New format: { "Version": 1, "Seed": <int>, "State": { ...SimState... } }
        // - Old format: { ...SimState... }
        if (LooksLikeEnvelope(json))
        {
            // GATE.S9.SAVE.MIGRATION.001: Apply migration chain before deserializing state.
            json = ApplyMigrations(json);

            var env = JsonSerializer.Deserialize<SaveEnvelope>(json, _options)
                ?? throw new InvalidOperationException("Failed to deserialize save envelope.");

            var state = env.State ?? throw new InvalidOperationException("Save envelope missing State.");

            // Restore RNG + derived caches first.
            state.HydrateAfterLoad();

            // Attach and (best-effort) inject seed identity for consumers that do expose it.
            AttachSeed(state, env.Seed);
            InjectSeed(state, env.Seed);

            return state;
        }

        var legacyState = JsonSerializer.Deserialize<SimState>(json, _options)
            ?? throw new InvalidOperationException("Failed to deserialize SimState.");

        legacyState.HydrateAfterLoad();
        return legacyState;
    }

    // GATE.S9.SAVE.MIGRATION.001: Version-routed migration chain.
    // Each step transforms the JSON from version N to N+1.
    private static string ApplyMigrations(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        int version = 1; // STRUCTURAL: default version for envelopes without Version field
        if (root.TryGetProperty("Version", out var vProp) && vProp.ValueKind == JsonValueKind.Number)
            version = vProp.GetInt32();

        if (version >= CurrentVersion)
            return json;

        // Parse into mutable DOM for transforms.
        var mutableDoc = System.Text.Json.Nodes.JsonNode.Parse(json)!.AsObject();

        if (version < 2) // STRUCTURAL: version threshold
            MigrateV1ToV2(mutableDoc);

        mutableDoc["Version"] = CurrentVersion;

        return mutableDoc.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    // v1→v2: Ensure Haven, Megaprojects, and SensorPylonNodes exist in State.
    private static void MigrateV1ToV2(System.Text.Json.Nodes.JsonObject envelope)
    {
        if (!envelope.TryGetPropertyValue("State", out var stateNode) || stateNode is not System.Text.Json.Nodes.JsonObject state)
            return;

        // Ensure Haven object exists with defaults.
        if (!state.ContainsKey("Haven"))
            state["Haven"] = new System.Text.Json.Nodes.JsonObject();

        // Ensure Haven.ResearchLabSlots array exists.
        if (state["Haven"] is System.Text.Json.Nodes.JsonObject haven)
        {
            if (!haven.ContainsKey("ResearchLabSlots"))
                haven["ResearchLabSlots"] = new System.Text.Json.Nodes.JsonArray();
        }

        // Ensure Megaprojects dict exists.
        if (!state.ContainsKey("Megaprojects"))
            state["Megaprojects"] = new System.Text.Json.Nodes.JsonObject();

        // Ensure SensorPylonNodes array exists.
        if (!state.ContainsKey("SensorPylonNodes"))
            state["SensorPylonNodes"] = new System.Text.Json.Nodes.JsonArray();
    }

    private static bool LooksLikeEnvelope(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return false;
            return root.TryGetProperty("State", out _) && root.TryGetProperty("Seed", out _);
        }
        catch
        {
            return false;
        }
    }

    private static int ExtractSeed(SimState state)
    {
        // Preferred: runtime-attached identity
        if (TryGetAttachedSeed(state, out var attached)) return attached;

        // Deterministic fallback: probe a fixed ordered set of common seed member names.
        // Do NOT enumerate members via reflection (ordering can vary).
        // Prefer any non-zero value found; if only zeros are found, return 0.
        var foundZero = false;

        if (TryReadCandidate("Seed", out var seed)) return seed;
        if (TryReadCandidate("WorldSeed", out seed)) return seed;
        if (TryReadCandidate("KernelSeed", out seed)) return seed;
        if (TryReadCandidate("SimSeed", out seed)) return seed;
        if (TryReadCandidate("InitialSeed", out seed)) return seed;
        if (TryReadCandidate("RngSeed", out seed)) return seed;
        if (TryReadCandidate("_seed", out seed)) return seed;
        if (TryReadCandidate("_worldSeed", out seed)) return seed;
        if (TryReadCandidate("_kernelSeed", out seed)) return seed;

        return foundZero ? 0 : 0;

        bool TryReadCandidate(string name, out int value)
        {
            if (TryReadIntMember(state, name, out value))
            {
                if (value != 0) return true;
                foundZero = true;
            }

            value = 0;
            return false;
        }
    }

    private static void InjectSeed(SimState state, int seed)
    {
        // Deterministic injection: same ordered candidate list as ExtractSeed.
        if (TryWriteIntMember(state, "Seed", seed)) return;
        if (TryWriteIntMember(state, "WorldSeed", seed)) return;
        if (TryWriteIntMember(state, "KernelSeed", seed)) return;
        if (TryWriteIntMember(state, "SimSeed", seed)) return;
        if (TryWriteIntMember(state, "InitialSeed", seed)) return;
        if (TryWriteIntMember(state, "RngSeed", seed)) return;
        if (TryWriteIntMember(state, "_seed", seed)) return;
        if (TryWriteIntMember(state, "_worldSeed", seed)) return;
        TryWriteIntMember(state, "_kernelSeed", seed);
    }

    private static bool TryReadIntMember(object instance, string name, out int value)
    {
        var t = instance.GetType();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        var p = t.GetProperty(name, flags);
        if (p is not null && p.GetIndexParameters().Length == 0)
        {
            var v = p.GetValue(instance);
            if (TryCoerceToInt(v, out value)) return true;
        }

        var f = t.GetField(name, flags);
        if (f is not null)
        {
            var v = f.GetValue(instance);
            if (TryCoerceToInt(v, out value)) return true;
        }

        value = 0;
        return false;
    }

    private static bool TryWriteIntMember(object instance, string name, int value)
    {
        var t = instance.GetType();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        var p = t.GetProperty(name, flags);
        if (p is not null && p.GetIndexParameters().Length == 0 && p.CanWrite)
        {
            if (p.PropertyType == typeof(int))
            {
                p.SetValue(instance, value);
                return true;
            }

            if (p.PropertyType == typeof(long))
            {
                p.SetValue(instance, (long)value);
                return true;
            }
        }

        var f = t.GetField(name, flags);
        if (f is not null && !f.IsInitOnly)
        {
            if (f.FieldType == typeof(int))
            {
                f.SetValue(instance, value);
                return true;
            }

            if (f.FieldType == typeof(long))
            {
                f.SetValue(instance, (long)value);
                return true;
            }
        }

        return false;
    }

    private static bool TryCoerceToInt(object? v, out int value)
    {
        switch (v)
        {
            case int i:
                value = i;
                return true;
            case long l when l <= int.MaxValue && l >= int.MinValue:
                value = (int)l;
                return true;
            case null:
                value = 0;
                return false;
            default:
                value = 0;
                return false;
        }
    }

    // GATE.S9.SAVE.INTEGRITY.001: Corruption-safe deserialization with validation.
    public sealed class DeserializeResult
    {
        public bool Success { get; init; }
        public SimState? State { get; init; }
        public string Error { get; init; } = "";
        public List<string> Warnings { get; init; } = new();
    }

    public static DeserializeResult TryDeserializeSafe(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new DeserializeResult { Success = false, Error = "save_empty" };

        try
        {
            // Quick JSON validity check.
            using var checkDoc = JsonDocument.Parse(json);
            if (checkDoc.RootElement.ValueKind != JsonValueKind.Object)
                return new DeserializeResult { Success = false, Error = "save_not_object" };
        }
        catch (JsonException)
        {
            return new DeserializeResult { Success = false, Error = "save_malformed_json" };
        }

        SimState state;
        try
        {
            state = Deserialize(json);
        }
        catch (Exception ex)
        {
            return new DeserializeResult { Success = false, Error = $"save_deserialize_failed: {ex.Message}" };
        }

        var warnings = ValidateState(state);
        return new DeserializeResult { Success = true, State = state, Warnings = warnings };
    }

    // GATE.S9.SAVE.INTEGRITY.001: Post-load validation. Returns list of warnings (non-fatal).
    public static List<string> ValidateState(SimState state)
    {
        var warnings = new List<string>();

        if (state.Nodes.Count == 0)
            warnings.Add("no_nodes");

        if (state.PlayerCredits < 0)
            warnings.Add("negative_credits");

        // Validate fleet references.
        foreach (var (fleetId, fleet) in state.Fleets)
        {
            if (!string.IsNullOrEmpty(fleet.CurrentNodeId) && !state.Nodes.ContainsKey(fleet.CurrentNodeId))
                warnings.Add($"fleet_invalid_node:{fleetId}");
        }

        // Validate edge references.
        foreach (var (edgeId, edge) in state.Edges)
        {
            if (!state.Nodes.ContainsKey(edge.FromNodeId))
                warnings.Add($"edge_invalid_from:{edgeId}");
            if (!state.Nodes.ContainsKey(edge.ToNodeId))
                warnings.Add($"edge_invalid_to:{edgeId}");
        }

        // Validate megaproject node references.
        foreach (var (mpId, mp) in state.Megaprojects)
        {
            if (!state.Nodes.ContainsKey(mp.NodeId))
                warnings.Add($"megaproject_invalid_node:{mpId}");
        }

        return warnings;
    }
}
