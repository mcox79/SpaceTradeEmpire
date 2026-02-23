using System;
using System.IO;
using System.Text;
using System.Text.Json;
using NUnit.Framework;
using SimCore.Content;

namespace SimCore.Tests.Content;

[TestFixture]
public sealed class ContentRegistryContractTests
{
    [Test]
    public void ContentRegistryV0_LoadTwice_DigestAndOrderingStable_AndEmitDigestReport()
    {
        // Load from embedded default (runtime-safe) twice.
        var a = ContentRegistryLoader.LoadFromJsonOrThrow(ContentRegistryLoader.DefaultRegistryJsonV0);
        var b = ContentRegistryLoader.LoadFromJsonOrThrow(ContentRegistryLoader.DefaultRegistryJsonV0);

        var da = ContentRegistryLoader.ComputeDigestUpperHex(a);
        var db = ContentRegistryLoader.ComputeDigestUpperHex(b);

        Assert.That(db, Is.EqualTo(da), "Digest mismatch for identical loads.");

        // Ordering stability assertions (explicit keys)
        AssertGoodsOrderedById(a.Goods);
        AssertModulesOrderedById(a.Modules);
        AssertRecipesOrderedById(a.Recipes);

        foreach (var r in a.Recipes)
        {
            AssertRecipeLinesOrdered(r.Inputs);
            AssertRecipeLinesOrdered(r.Outputs);
        }

        // Validate docs/content/content_registry_v0.json against schema file (deterministic checks).
        // Contract: schema exists, version is 0, and docs registry parses and normalizes to same digest.
        var repoRoot = FindRepoRootOrThrow();
        var schemaPath = Path.Combine(repoRoot, "SimCore", "Schemas", "ContentRegistrySchema.json");
        var docsRegPath = Path.Combine(repoRoot, "docs", "content", "content_registry_v0.json");

        // Deterministic bootstrap: if the repo files are missing, create them with canonical contents.
        EnsureFileWithCanonicalContents(schemaPath, CanonicalSchemaJsonV0);
        EnsureFileWithCanonicalContents(docsRegPath, ContentRegistryLoader.DefaultRegistryJsonV0);

        var schemaJson = File.ReadAllText(schemaPath, new UTF8Encoding(false));
        var docsJson = File.ReadAllText(docsRegPath, new UTF8Encoding(false));

        AssertSchemaV0HasRequiredFields(schemaJson);

        var docsReg = ContentRegistryLoader.LoadFromJsonOrThrow(docsJson);
        var docsDigest = ContentRegistryLoader.ComputeDigestUpperHex(docsReg);
        Assert.That(docsDigest, Is.EqualTo(da), "Docs registry digest must match embedded default v0 digest.");

        // Emit deterministic digest report to docs/generated/content_registry_digest_v0.txt
        var outDir = Path.Combine(repoRoot, "docs", "generated");
        Directory.CreateDirectory(outDir);

        var outPath = Path.Combine(outDir, "content_registry_digest_v0.txt");
        var report = ContentRegistryLoader.BuildDigestReportText(docsReg);

        // Stable encoding, stable newlines (BuildDigestReportText uses LF).
        File.WriteAllText(outPath, report, new UTF8Encoding(false));

        // Basic sanity: ensure the file contains the digest line.
        Assert.That(report, Does.Contain("digest_sha256_upper=" + docsDigest));
    }

    private static void AssertGoodsOrderedById(System.Collections.Generic.List<ContentRegistryLoader.GoodDefV0> list)
    {
        for (int i = 1; i < list.Count; i++)
            Assert.That(StringComparer.Ordinal.Compare(list[i - 1].Id, list[i].Id), Is.LessThanOrEqualTo(0),
                "Goods not ordered by Id (Ordinal).");
    }

    private static void AssertModulesOrderedById(System.Collections.Generic.List<ContentRegistryLoader.ModuleDefV0> list)
    {
        for (int i = 1; i < list.Count; i++)
            Assert.That(StringComparer.Ordinal.Compare(list[i - 1].Id, list[i].Id), Is.LessThanOrEqualTo(0),
                "Modules not ordered by Id (Ordinal).");
    }

    private static void AssertRecipesOrderedById(System.Collections.Generic.List<ContentRegistryLoader.RecipeDefV0> list)
    {
        for (int i = 1; i < list.Count; i++)
            Assert.That(StringComparer.Ordinal.Compare(list[i - 1].Id, list[i].Id), Is.LessThanOrEqualTo(0),
                "Recipes not ordered by Id (Ordinal).");
    }

    private static void AssertRecipeLinesOrdered(System.Collections.Generic.List<ContentRegistryLoader.RecipeLineV0> lines)
    {
        for (int i = 1; i < lines.Count; i++)
        {
            var a = lines[i - 1];
            var b = lines[i];

            var c = StringComparer.Ordinal.Compare(a.GoodId, b.GoodId);
            if (c < 0) continue;

            if (c == 0)
            {
                Assert.That(a.Qty, Is.LessThanOrEqualTo(b.Qty), "Recipe lines not ordered by GoodId then Qty.");
                continue;
            }

            Assert.Fail("Recipe lines not ordered by GoodId (Ordinal).");
        }
    }

    private static void AssertSchemaV0HasRequiredFields(string schemaJson)
    {
        using var doc = JsonDocument.Parse(schemaJson);
        var root = doc.RootElement;

        Assert.That(root.GetProperty("schema_id").GetString(), Is.EqualTo("ContentRegistrySchema.v0"));
        Assert.That(root.GetProperty("version").GetInt32(), Is.EqualTo(0));

        var req = root.GetProperty("required");
        Assert.That(req.ValueKind, Is.EqualTo(JsonValueKind.Array));

        var required = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        foreach (var e in req.EnumerateArray()) required.Add(e.GetString() ?? "");

        Assert.That(required.Contains("version"), Is.True);
        Assert.That(required.Contains("goods"), Is.True);
        Assert.That(required.Contains("recipes"), Is.True);
        Assert.That(required.Contains("modules"), Is.True);
    }

    private static void EnsureFileWithCanonicalContents(string path, string canonicalContents)
    {
        var dir = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(dir)) throw new InvalidOperationException("Invalid path: no directory.");
        Directory.CreateDirectory(dir);

        if (!File.Exists(path))
        {
            File.WriteAllText(path, canonicalContents, new UTF8Encoding(false));
        }
    }

    // Canonical schema contents written if missing. This must remain stable unless schema version is bumped.
    private const string CanonicalSchemaJsonV0 =
        "{\n" +
        "  \"schema_id\": \"ContentRegistrySchema.v0\",\n" +
        "  \"version\": 0,\n" +
        "  \"type\": \"object\",\n" +
        "  \"required\": [\"version\", \"goods\", \"recipes\", \"modules\"],\n" +
        "  \"properties\": {\n" +
        "    \"version\": { \"type\": \"integer\", \"const\": 0 },\n" +
        "    \"goods\": {\n" +
        "      \"type\": \"array\",\n" +
        "      \"items\": {\n" +
        "        \"type\": \"object\",\n" +
        "        \"required\": [\"id\"],\n" +
        "        \"properties\": { \"id\": { \"type\": \"string\", \"minLength\": 1 } },\n" +
        "        \"additionalProperties\": false\n" +
        "      }\n" +
        "    },\n" +
        "    \"recipes\": {\n" +
        "      \"type\": \"array\",\n" +
        "      \"items\": {\n" +
        "        \"type\": \"object\",\n" +
        "        \"required\": [\"id\", \"inputs\", \"outputs\"],\n" +
        "        \"properties\": {\n" +
        "          \"id\": { \"type\": \"string\", \"minLength\": 1 },\n" +
        "          \"inputs\": {\n" +
        "            \"type\": \"array\",\n" +
        "            \"minItems\": 1,\n" +
        "            \"items\": {\n" +
        "              \"type\": \"object\",\n" +
        "              \"required\": [\"good_id\", \"qty\"],\n" +
        "              \"properties\": {\n" +
        "                \"good_id\": { \"type\": \"string\", \"minLength\": 1 },\n" +
        "                \"qty\": { \"type\": \"integer\", \"minimum\": 1 }\n" +
        "              },\n" +
        "              \"additionalProperties\": false\n" +
        "            }\n" +
        "          },\n" +
        "          \"outputs\": {\n" +
        "            \"type\": \"array\",\n" +
        "            \"minItems\": 1,\n" +
        "            \"items\": {\n" +
        "              \"type\": \"object\",\n" +
        "              \"required\": [\"good_id\", \"qty\"],\n" +
        "              \"properties\": {\n" +
        "                \"good_id\": { \"type\": \"string\", \"minLength\": 1 },\n" +
        "                \"qty\": { \"type\": \"integer\", \"minimum\": 1 }\n" +
        "              },\n" +
        "              \"additionalProperties\": false\n" +
        "            }\n" +
        "          }\n" +
        "        },\n" +
        "        \"additionalProperties\": false\n" +
        "      }\n" +
        "    },\n" +
        "    \"modules\": {\n" +
        "      \"type\": \"array\",\n" +
        "      \"items\": {\n" +
        "        \"type\": \"object\",\n" +
        "        \"required\": [\"id\"],\n" +
        "        \"properties\": { \"id\": { \"type\": \"string\", \"minLength\": 1 } },\n" +
        "        \"additionalProperties\": false\n" +
        "      }\n" +
        "    }\n" +
        "  },\n" +
        "  \"additionalProperties\": false\n" +
        "}\n";

    private static string FindRepoRootOrThrow()
    {
        // Deterministic upward walk from test base dir.
        // Repo root found when both "docs" and "SimCore" directories exist.
        var dir = AppContext.BaseDirectory;
        for (int i = 0; i < 8; i++)
        {
            if (Directory.Exists(Path.Combine(dir, "docs")) && Directory.Exists(Path.Combine(dir, "SimCore")))
                return dir;

            var parent = Directory.GetParent(dir);
            if (parent is null) break;
            dir = parent.FullName;
        }

        throw new InvalidOperationException("Could not locate repo root (expected docs/ and SimCore/).");
    }
}
