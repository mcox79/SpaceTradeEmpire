using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using NUnit.Framework;
using SimCore.Programs;

namespace SimCore.Tests.Programs;

public sealed class DefaultDoctrineContractTests
{
        [Test]
        public void DOCTRINE_001_DefaultDoctrine_Exists_HasMax2Toggles_AndIsDeterministic()
        {
                var d = DefaultDoctrine.Create();

                // Max 2 toggles: exactly two public bool properties on the payload.
                var boolProps = typeof(DefaultDoctrine.Payload)
                        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => p.PropertyType == typeof(bool))
                        .Select(p => p.Name)
                        .OrderBy(x => x, StringComparer.Ordinal)
                        .ToArray();

                Assert.That(boolProps.Length, Is.EqualTo(2), "DefaultDoctrine.Payload must expose exactly 2 bool toggles.");

                // Deterministic JSON: stable, schema-bound.
                var json = DefaultDoctrine.ToDeterministicJson(d);
                DefaultDoctrine.ValidateJsonIsSchemaBound(json);

                // Stable output string (normalize newlines to avoid platform differences).
                var expected = string.Join("\n", new[]
                {
                        "{",
                        "  \"Version\": 1,",
                        "  \"PreferConservativeCadence\": true,",
                        "  \"RequireConstraintsSatisfied\": true",
                        "}"
                });

                Assert.That(json.Replace("\r\n", "\n"), Is.EqualTo(expected), "DefaultDoctrine JSON must be stable.");

                // Unknown key should fail schema bound validation.
                var mutated = InjectUnknownKeySorted(json, "HackerKey", "true");
                Assert.Throws<InvalidOperationException>(() => DefaultDoctrine.ValidateJsonIsSchemaBound(mutated));
        }

        private static string InjectUnknownKeySorted(string json, string key, string rawValueJson)
        {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var props = root.EnumerateObject()
                        .Select(p => (Name: p.Name, Value: p.Value))
                        .ToList();

                props.Add((key, JsonDocument.Parse(rawValueJson).RootElement));

                props = props.OrderBy(p => p.Name, StringComparer.Ordinal).ToList();

                using var stream = new MemoryStream();
                using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
                {
                        writer.WriteStartObject();
                        foreach (var p in props)
                        {
                                writer.WritePropertyName(p.Name);
                                p.Value.WriteTo(writer);
                        }
                        writer.WriteEndObject();
                }

                return Encoding.UTF8.GetString(stream.ToArray());
        }
}
