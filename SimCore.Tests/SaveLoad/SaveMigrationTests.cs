using System.Text.Json;
using NUnit.Framework;
using SimCore;
using SimCore.Gen;
using SimCore.Systems;

namespace SimCore.Tests.SaveLoad;

// GATE.S9.SAVE.MIGRATION.001: Migration pipeline contract tests.
[TestFixture]
public class SaveMigrationTests
{
    [Test]
    public void Deserialize_V1Envelope_MigratesAndLoads()
    {
        // Build a v1 envelope (no Haven, Megaprojects, SensorPylonNodes in State).
        var kernel = new SimKernel(42);
        GalaxyGenerator.Generate(kernel.State, 8, 60f);
        for (int i = 0; i < 10; i++) kernel.Step();

        // Serialize normally (produces v2).
        var json = SerializationSystem.Serialize(kernel.State);

        // Downgrade to v1: remove new fields and set Version=1.
        var doc = System.Text.Json.Nodes.JsonNode.Parse(json)!.AsObject();
        doc["Version"] = 1;
        var state = doc["State"]!.AsObject();
        state.Remove("Haven");
        state.Remove("Megaprojects");
        state.Remove("SensorPylonNodes");
        var v1Json = doc.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

        // Deserialize should succeed via migration.
        var loaded = SerializationSystem.Deserialize(v1Json);
        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded.Haven, Is.Not.Null);
        Assert.That(loaded.Megaprojects, Is.Not.Null);
        Assert.That(loaded.SensorPylonNodes, Is.Not.Null);
    }

    [Test]
    public void Deserialize_V2Envelope_NoMigrationNeeded()
    {
        var kernel = new SimKernel(99);
        GalaxyGenerator.Generate(kernel.State, 8, 60f);
        for (int i = 0; i < 5; i++) kernel.Step();

        var json = SerializationSystem.Serialize(kernel.State);

        // V2 envelope should load without issues.
        var loaded = SerializationSystem.Deserialize(json);
        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded.Haven, Is.Not.Null);
    }

    [Test]
    public void Serialize_EmitsCurrentVersion()
    {
        var kernel = new SimKernel(42);
        var json = SerializationSystem.Serialize(kernel.State);

        using var doc = JsonDocument.Parse(json);
        var version = doc.RootElement.GetProperty("Version").GetInt32();
        Assert.That(version, Is.EqualTo(SerializationSystem.CurrentVersion));
    }

    [Test]
    public void Deserialize_LegacyNoEnvelope_StillWorks()
    {
        // Old format: just a bare SimState JSON, no envelope.
        var kernel = new SimKernel(42);
        GalaxyGenerator.Generate(kernel.State, 6, 50f);
        for (int i = 0; i < 3; i++) kernel.Step();

        var options = new JsonSerializerOptions { WriteIndented = true, IncludeFields = true };
        var bareJson = JsonSerializer.Serialize(kernel.State, options);

        var loaded = SerializationSystem.Deserialize(bareJson);
        Assert.That(loaded, Is.Not.Null);
    }

    [Test]
    public void Migration_V1ToV2_PreservesExistingData()
    {
        var kernel = new SimKernel(42);
        GalaxyGenerator.Generate(kernel.State, 8, 60f);
        for (int i = 0; i < 10; i++) kernel.Step();

        var json = SerializationSystem.Serialize(kernel.State);
        // Downgrade to v1 but keep Haven (partially — missing ResearchLabSlots).
        var doc = System.Text.Json.Nodes.JsonNode.Parse(json)!.AsObject();
        doc["Version"] = 1;
        var state = doc["State"]!.AsObject();
        // Remove only the new fields, keep Haven base.
        if (state["Haven"] is System.Text.Json.Nodes.JsonObject haven)
            haven.Remove("ResearchLabSlots");
        state.Remove("Megaprojects");
        state.Remove("SensorPylonNodes");
        var v1Json = doc.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

        var loaded = SerializationSystem.Deserialize(v1Json);
        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded.Haven, Is.Not.Null);
        Assert.That(loaded.Haven.ResearchLabSlots, Is.Not.Null);
    }

    // GATE.S9.SAVE.INTEGRITY.001: Corruption detection tests.
    [Test]
    public void TryDeserializeSafe_MalformedJson_ReturnsError()
    {
        var result = SerializationSystem.TryDeserializeSafe("{ broken json!!!");
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("save_malformed_json"));
    }

    [Test]
    public void TryDeserializeSafe_EmptyString_ReturnsError()
    {
        var result = SerializationSystem.TryDeserializeSafe("");
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("save_empty"));
    }

    [Test]
    public void TryDeserializeSafe_Null_ReturnsError()
    {
        var result = SerializationSystem.TryDeserializeSafe(null);
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("save_empty"));
    }

    [Test]
    public void TryDeserializeSafe_ValidSave_Succeeds()
    {
        var kernel = new SimKernel(42);
        GalaxyGenerator.Generate(kernel.State, 8, 60f);
        for (int i = 0; i < 5; i++) kernel.Step();

        var json = SerializationSystem.Serialize(kernel.State);
        var result = SerializationSystem.TryDeserializeSafe(json);
        Assert.That(result.Success, Is.True);
        Assert.That(result.State, Is.Not.Null);
        Assert.That(result.Warnings.Count, Is.EqualTo(0));
    }

    [Test]
    public void ValidateState_NegativeCredits_ReturnsWarning()
    {
        var kernel = new SimKernel(42);
        GalaxyGenerator.Generate(kernel.State, 8, 60f);
        kernel.State.PlayerCredits = -500;

        var warnings = SerializationSystem.ValidateState(kernel.State);
        Assert.That(warnings, Does.Contain("negative_credits"));
    }

    [Test]
    public void ValidateState_HealthyState_NoWarnings()
    {
        var kernel = new SimKernel(42);
        GalaxyGenerator.Generate(kernel.State, 8, 60f);
        for (int i = 0; i < 5; i++) kernel.Step();

        var warnings = SerializationSystem.ValidateState(kernel.State);
        Assert.That(warnings.Count, Is.EqualTo(0));
    }
}
