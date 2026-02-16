using System;
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

        // Fallback: if SimState exposes a member named Seed/WorldSeed
        if (TryReadIntMember(state, "Seed", out var seed)) return seed;
        if (TryReadIntMember(state, "WorldSeed", out seed)) return seed;

        return 0;
    }

    private static void InjectSeed(SimState state, int seed)
    {
        if (TryWriteIntMember(state, "Seed", seed)) return;
        TryWriteIntMember(state, "WorldSeed", seed);
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
}
