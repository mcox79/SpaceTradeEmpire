using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimCore.Programs;

/// <summary>
/// Default doctrine for Slice 2 programs.
/// Constraint: max 2 toggles, deterministic, schema-bound JSON.
/// Note: this is intentionally minimal and not yet wired into ProgramSystem.
/// </summary>
public static class DefaultDoctrine
{
        public const int DoctrineVersion = 1;

        public sealed class Payload
        {
                [JsonInclude] public int Version { get; set; } = DoctrineVersion;

                // Toggle 1: if true, programs should favor conservative cadence choices (fewer runs).
                [JsonInclude] public bool PreferConservativeCadence { get; set; } = true;

                // Toggle 2: if true, programs should require credits/cargo constraints to be satisfied before emitting intents.
                [JsonInclude] public bool RequireConstraintsSatisfied { get; set; } = true;
        }

        public static Payload Create()
        {
                return new Payload
                {
                        Version = DoctrineVersion,
                        PreferConservativeCadence = true,
                        RequireConstraintsSatisfied = true
                };
        }

        public static string ToDeterministicJson(Payload payload)
        {
                if (payload is null) throw new ArgumentNullException(nameof(payload));

                var opts = new JsonSerializerOptions
                {
                        WriteIndented = true,
                        IncludeFields = true,
                        DefaultIgnoreCondition = JsonIgnoreCondition.Never
                };

                return JsonSerializer.Serialize(payload, opts);
        }

        public static void ValidateJsonIsSchemaBound(string json)
        {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                        throw new InvalidOperationException("DefaultDoctrine payload must be a JSON object.");

                RequireOnlyKeys(root, new[]
                {
                        "Version",
                        "PreferConservativeCadence",
                        "RequireConstraintsSatisfied"
                });

                RequireKey(root, "Version", JsonValueKind.Number);
                RequireBoolKey(root, "PreferConservativeCadence");
                RequireBoolKey(root, "RequireConstraintsSatisfied");
        }

        private static void RequireKey(JsonElement obj, string name, JsonValueKind kind)
        {
                if (!obj.TryGetProperty(name, out var prop)) throw new InvalidOperationException($"Missing key: {name}");
                if (prop.ValueKind != kind) throw new InvalidOperationException($"Key {name} must be {kind}.");
        }

        private static void RequireBoolKey(JsonElement obj, string name)
        {
                if (!obj.TryGetProperty(name, out var prop)) throw new InvalidOperationException($"Missing key: {name}");
                if (prop.ValueKind != JsonValueKind.True && prop.ValueKind != JsonValueKind.False)
                        throw new InvalidOperationException($"Key {name} must be boolean.");
        }

        private static void RequireOnlyKeys(JsonElement obj, string[] allowed)
        {
                var allowedSet = allowed.ToHashSet(StringComparer.Ordinal);
                foreach (var p in obj.EnumerateObject())
                {
                        if (!allowedSet.Contains(p.Name)) throw new InvalidOperationException($"Unknown key: {p.Name}");
                }
        }
}
