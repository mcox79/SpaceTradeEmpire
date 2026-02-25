using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SimCore.Content;

// GATE.X.CONTENT_SUBSTRATE.001
// Deterministic content registry v0 (goods%recipes%modules) loader and canonical digest.
public static class ContentRegistryLoader
{
    // NOTE: No runtime IO in SimCore. World initialization uses this embedded default.
    // This must stay byte-for-byte stable unless version is bumped.
    public const string DefaultRegistryJsonV0 =
        "{\n" +
        "  \"version\": 0,\n" +
        "  \"goods\": [\n" +
        "    { \"id\": \"food\" },\n" +
        "    { \"id\": \"ore\" }\n" +
        "  ],\n" +
        "  \"recipes\": [\n" +
        "    {\n" +
        "      \"id\": \"recipe_refine_ore_to_food\",\n" +
        "      \"inputs\": [ { \"good_id\": \"ore\", \"qty\": 2 } ],\n" +
        "      \"outputs\": [ { \"good_id\": \"food\", \"qty\": 1 } ]\n" +
        "    }\n" +
        "  ],\n" +
        "  \"modules\": [\n" +
        "    { \"id\": \"cap_module_refinery\" }\n" +
        "  ]\n" +
        "}\n";

    public sealed class ContentRegistryV0
    {
        public int Version { get; set; } = 0;
        public List<GoodDefV0> Goods { get; set; } = new();
        public List<RecipeDefV0> Recipes { get; set; } = new();
        public List<ModuleDefV0> Modules { get; set; } = new();
    }

    public sealed class GoodDefV0
    {
        public string Id { get; set; } = "";
    }

    public sealed class ModuleDefV0
    {
        public string Id { get; set; } = "";
    }

    public sealed class RecipeDefV0
    {
        public string Id { get; set; } = "";
        public List<RecipeLineV0> Inputs { get; set; } = new();
        public List<RecipeLineV0> Outputs { get; set; } = new();
    }

    public sealed class RecipeLineV0
    {
        public string GoodId { get; set; } = "";
        public int Qty { get; set; } = 0;
    }


    public static ContentRegistryV0 LoadFromJsonOrThrow(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) throw new InvalidOperationException("Content registry JSON is empty.");

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("Content registry root must be an object.");

        // Schema law (v0): additionalProperties=false at root and all nested objects.
        AssertNoExtraPropertiesOrThrow(root, "version", "goods", "recipes", "modules");

        var reg = new ContentRegistryV0();

        // version (required)
        if (!root.TryGetProperty("version", out var v) || v.ValueKind != JsonValueKind.Number)
            throw new InvalidOperationException("Content registry missing required field: version (number).");
        reg.Version = v.GetInt32();
        if (reg.Version != 0) throw new InvalidOperationException($"Unsupported content registry version: {reg.Version} (expected 0).");

        // goods (required array)
        reg.Goods = ReadGoods(root);

        // recipes (required array)
        reg.Recipes = ReadRecipes(root);

        // modules (required array)
        reg.Modules = ReadModules(root);

        NormalizeInPlace(reg);
        ValidateNormalizedOrThrow(reg);

        return reg;
    }

    private static List<GoodDefV0> ReadGoods(JsonElement root)
    {
        if (!root.TryGetProperty("goods", out var a) || a.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("Content registry missing required field: goods (array).");

        var list = new List<GoodDefV0>();
        foreach (var e in a.EnumerateArray())
        {
            if (e.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("goods[] entries must be objects.");

            AssertNoExtraPropertiesOrThrow(e, "id");

            if (!e.TryGetProperty("id", out var idEl) || idEl.ValueKind != JsonValueKind.String)
                throw new InvalidOperationException("goods[] missing required field: id (string).");
            list.Add(new GoodDefV0 { Id = idEl.GetString() ?? "" });
        }
        return list;
    }

    private static List<ModuleDefV0> ReadModules(JsonElement root)
    {
        if (!root.TryGetProperty("modules", out var a) || a.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("Content registry missing required field: modules (array).");

        var list = new List<ModuleDefV0>();
        foreach (var e in a.EnumerateArray())
        {
            if (e.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("modules[] entries must be objects.");

            AssertNoExtraPropertiesOrThrow(e, "id");

            if (!e.TryGetProperty("id", out var idEl) || idEl.ValueKind != JsonValueKind.String)
                throw new InvalidOperationException("modules[] missing required field: id (string).");
            list.Add(new ModuleDefV0 { Id = idEl.GetString() ?? "" });
        }
        return list;
    }

    private static List<RecipeDefV0> ReadRecipes(JsonElement root)
    {
        if (!root.TryGetProperty("recipes", out var a) || a.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("Content registry missing required field: recipes (array).");

        var list = new List<RecipeDefV0>();
        foreach (var e in a.EnumerateArray())
        {
            if (e.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("recipes[] entries must be objects.");

            AssertNoExtraPropertiesOrThrow(e, "id", "inputs", "outputs");

            if (!e.TryGetProperty("id", out var idEl) || idEl.ValueKind != JsonValueKind.String)
                throw new InvalidOperationException("recipes[] missing required field: id (string).");

            var r = new RecipeDefV0 { Id = idEl.GetString() ?? "" };
            r.Inputs = ReadRecipeLines(e, "inputs");
            r.Outputs = ReadRecipeLines(e, "outputs");
            list.Add(r);
        }
        return list;
    }

    private static List<RecipeLineV0> ReadRecipeLines(JsonElement recipeObj, string field)
    {
        if (!recipeObj.TryGetProperty(field, out var a) || a.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException($"recipes[] missing required field: {field} (array).");

        var list = new List<RecipeLineV0>();
        foreach (var e in a.EnumerateArray())
        {
            if (e.ValueKind != JsonValueKind.Object) throw new InvalidOperationException($"{field}[] entries must be objects.");

            AssertNoExtraPropertiesOrThrow(e, "good_id", "qty");

            if (!e.TryGetProperty("good_id", out var gEl) || gEl.ValueKind != JsonValueKind.String)
                throw new InvalidOperationException($"{field}[] missing required field: good_id (string).");
            if (!e.TryGetProperty("qty", out var qEl) || qEl.ValueKind != JsonValueKind.Number)
                throw new InvalidOperationException($"{field}[] missing required field: qty (number).");

            list.Add(new RecipeLineV0
            {
                GoodId = gEl.GetString() ?? "",
                Qty = qEl.GetInt32()
            });
        }
        return list;
    }

    // Deterministic normalization with explicit ordering keys and tie-breakers.
    public static void NormalizeInPlace(ContentRegistryV0 reg)
    {
        reg.Goods = (reg.Goods ?? new List<GoodDefV0>())
            .OrderBy(g => g.Id, StringComparer.Ordinal)
            .ToList();

        reg.Modules = (reg.Modules ?? new List<ModuleDefV0>())
            .OrderBy(m => m.Id, StringComparer.Ordinal)
            .ToList();

        reg.Recipes = (reg.Recipes ?? new List<RecipeDefV0>())
            .OrderBy(r => r.Id, StringComparer.Ordinal)
            .ToList();

        foreach (var r in reg.Recipes)
        {
            r.Inputs = (r.Inputs ?? new List<RecipeLineV0>())
                .OrderBy(x => x.GoodId, StringComparer.Ordinal)
                .ThenBy(x => x.Qty) // tie-breaker
                .ToList();

            r.Outputs = (r.Outputs ?? new List<RecipeLineV0>())
                .OrderBy(x => x.GoodId, StringComparer.Ordinal)
                .ThenBy(x => x.Qty) // tie-breaker
                .ToList();
        }
    }

    private static void AssertNoExtraPropertiesOrThrow(JsonElement obj, params string[] allowed)
    {
        // Deterministic error reporting: unknown field list sorted Ordinal.
        var allow = new HashSet<string>(allowed, StringComparer.Ordinal);
        var unknown = new List<string>();

        foreach (var p in obj.EnumerateObject())
        {
            if (!allow.Contains(p.Name)) unknown.Add(p.Name);
        }

        if (unknown.Count == 0) return;

        unknown.Sort(StringComparer.Ordinal);
        throw new InvalidOperationException("Unknown field(s): " + string.Join(", ", unknown));
    }

    public static void ValidateNormalizedOrThrow(ContentRegistryV0 reg)
    {
        // goods ids non-empty + unique
        var seenGoods = new HashSet<string>(StringComparer.Ordinal);
        foreach (var g in reg.Goods)
        {
            if (string.IsNullOrWhiteSpace(g.Id)) throw new InvalidOperationException("goods[].id must be non-empty.");
            if (!seenGoods.Add(g.Id)) throw new InvalidOperationException($"Duplicate goods id: {g.Id}");
        }

        // modules ids non-empty + unique
        var seenModules = new HashSet<string>(StringComparer.Ordinal);
        foreach (var m in reg.Modules)
        {
            if (string.IsNullOrWhiteSpace(m.Id)) throw new InvalidOperationException("modules[].id must be non-empty.");
            if (!seenModules.Add(m.Id)) throw new InvalidOperationException($"Duplicate modules id: {m.Id}");
        }

        // recipes ids non-empty + unique; recipe lines must reference known goods; qty > 0
        var seenRecipes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var r in reg.Recipes)
        {
            if (string.IsNullOrWhiteSpace(r.Id)) throw new InvalidOperationException("recipes[].id must be non-empty.");
            if (!seenRecipes.Add(r.Id)) throw new InvalidOperationException($"Duplicate recipe id: {r.Id}");

            ValidateRecipeLines(r.Id, "inputs", r.Inputs, seenGoods);
            ValidateRecipeLines(r.Id, "outputs", r.Outputs, seenGoods);
        }
    }

    private static void ValidateRecipeLines(string recipeId, string label, List<RecipeLineV0> lines, HashSet<string> knownGoods)
    {
        if (lines is null) throw new InvalidOperationException($"Recipe {recipeId} missing {label}.");
        if (lines.Count == 0) throw new InvalidOperationException($"Recipe {recipeId} must have at least 1 {label} line.");

        foreach (var ln in lines)
        {
            if (string.IsNullOrWhiteSpace(ln.GoodId)) throw new InvalidOperationException($"Recipe {recipeId} {label} has empty good_id.");
            if (!knownGoods.Contains(ln.GoodId)) throw new InvalidOperationException($"Recipe {recipeId} {label} references unknown good_id: {ln.GoodId}");
            if (ln.Qty <= 0) throw new InvalidOperationException($"Recipe {recipeId} {label} qty must be > 0 for good_id: {ln.GoodId}");
        }
    }

    // Canonical digest: SHA256(UTF-8 canonical text) rendered as uppercase hex.
    public static string ComputeDigestUpperHex(ContentRegistryV0 reg)
    {
        var canonical = ToCanonicalText(reg);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(hash);
    }

    public static string ToCanonicalText(ContentRegistryV0 reg)
    {
        // Fixed keys, fixed order, LF newlines only.
        var sb = new StringBuilder(512);
        sb.Append("CONTENT_REGISTRY_V0\n");
        sb.Append("version=").Append(reg.Version).Append('\n');

        sb.Append("goods=");
        sb.Append(string.Join(',', reg.Goods.Select(g => g.Id)));
        sb.Append('\n');

        sb.Append("recipes=");
        sb.Append(string.Join(',', reg.Recipes.Select(r => r.Id)));
        sb.Append('\n');

        foreach (var r in reg.Recipes)
        {
            sb.Append("recipe=").Append(r.Id).Append("|in=");
            sb.Append(string.Join(',', r.Inputs.Select(x => x.GoodId + ":" + x.Qty)));
            sb.Append("|out=");
            sb.Append(string.Join(',', r.Outputs.Select(x => x.GoodId + ":" + x.Qty)));
            sb.Append('\n');
        }

        sb.Append("modules=");
        sb.Append(string.Join(',', reg.Modules.Select(m => m.Id)));
        sb.Append('\n');

        return sb.ToString();
    }

    public static string BuildDigestReportText(ContentRegistryV0 reg)
    {
        var digest = ComputeDigestUpperHex(reg);
        // Stable, no timestamps, LF only.
        return
            "CONTENT_REGISTRY_DIGEST_V0\n" +
            "version=" + reg.Version + "\n" +
            "goods_count=" + reg.Goods.Count + "\n" +
            "recipes_count=" + reg.Recipes.Count + "\n" +
            "modules_count=" + reg.Modules.Count + "\n" +
            "digest_sha256_upper=" + digest + "\n";
    }

    // GATE.S3_5.CONTENT_SUBSTRATE.002
    // Deterministic content pack validation report v0.
    public sealed class PackValidationResultV0
    {
        public bool IsValid { get; init; }
        public List<string> Failures { get; init; } = new();
        public ContentRegistryV0? Registry { get; init; }
    }

    // Validates the pack JSON and accumulates failures deterministically.
    // Does NOT throw on validation failure; callers decide how to handle invalid packs.
    public static PackValidationResultV0 ValidatePackJsonV0(string json)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(json))
        {
            failures.Add("JSON_EMPTY");
            failures.Sort(StringComparer.Ordinal);
            return new PackValidationResultV0 { IsValid = false, Failures = failures, Registry = null };
        }

        JsonDocument? doc = null;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch
        {
            // Do not include exception messages (runtime dependent). Keep a stable token.
            failures.Add("JSON_PARSE_ERROR");
            failures.Sort(StringComparer.Ordinal);
            return new PackValidationResultV0 { IsValid = false, Failures = failures, Registry = null };
        }

        using (doc)
        {
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                failures.Add("ROOT_NOT_OBJECT");
            }
            else
            {
                // Root allowed fields.
                CollectUnknownFields(root, "ROOT_UNKNOWN_FIELDS", failures, "version", "goods", "recipes", "modules");

                // version
                if (!root.TryGetProperty("version", out var vEl))
                {
                    failures.Add("MISSING:version");
                }
                else if (vEl.ValueKind != JsonValueKind.Number)
                {
                    failures.Add("TYPE:version:not_number");
                }
                else
                {
                    var ver = vEl.GetInt32();
                    if (ver != 0) failures.Add("VALUE:version:expected_0");
                }

                // goods
                ValidateIdArray(root, "goods", "goods[]", failures);

                // modules
                ValidateIdArray(root, "modules", "modules[]", failures);

                // recipes (with nested lines)
                ValidateRecipes(root, failures);
            }
        }

        failures.Sort(StringComparer.Ordinal);

        if (failures.Count != 0)
            return new PackValidationResultV0 { IsValid = false, Failures = failures, Registry = null };

        // If structural validation passed, perform canonical load+normalize+digest.
        // Any throw here indicates an internal mismatch between the structural validator and loader rules.
        var reg = LoadFromJsonOrThrow(json);
        return new PackValidationResultV0 { IsValid = true, Failures = failures, Registry = reg };
    }

    public static string BuildPackValidationReportTextV0(string pack_id, PackValidationResultV0 result)
    {
        // Stable, no timestamps, LF only.
        var sb = new StringBuilder(512);
        sb.Append("CONTENT_PACK_VALIDATION_REPORT_V0\n");
        sb.Append("pack_id=").Append(pack_id ?? "").Append('\n');
        sb.Append("schema_id=ContentRegistrySchema.v0\n");
        sb.Append("schema_version=0\n");
        sb.Append("is_valid=").Append(result.IsValid ? "true" : "false").Append('\n');
        sb.Append("failure_count=").Append(result.Failures.Count).Append('\n');

        if (result.Registry is not null)
        {
            var digest = ComputeDigestUpperHex(result.Registry);
            sb.Append("registry_version=").Append(result.Registry.Version).Append('\n');
            sb.Append("goods_count=").Append(result.Registry.Goods.Count).Append('\n');
            sb.Append("recipes_count=").Append(result.Registry.Recipes.Count).Append('\n');
            sb.Append("modules_count=").Append(result.Registry.Modules.Count).Append('\n');
            sb.Append("digest_sha256_upper=").Append(digest).Append('\n');
        }

        sb.Append("failures=\n");
        // Failures already sorted Ordinal by validator; keep this loop order.
        foreach (var f in result.Failures)
            sb.Append("- ").Append(f).Append('\n');

        return sb.ToString();
    }

    private static void CollectUnknownFields(JsonElement obj, string token, List<string> failures, params string[] allowed)
    {
        var allow = new HashSet<string>(allowed, StringComparer.Ordinal);
        var unknown = new List<string>();

        foreach (var p in obj.EnumerateObject())
            if (!allow.Contains(p.Name)) unknown.Add(p.Name);

        if (unknown.Count == 0) return;

        unknown.Sort(StringComparer.Ordinal);
        failures.Add(token + ":" + string.Join(",", unknown));
    }

    private static void ValidateIdArray(JsonElement root, string field, string label, List<string> failures)
    {
        if (!root.TryGetProperty(field, out var a))
        {
            failures.Add("MISSING:" + field);
            return;
        }
        if (a.ValueKind != JsonValueKind.Array)
        {
            failures.Add("TYPE:" + field + ":not_array");
            return;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);

        int idx = 0;
        foreach (var e in a.EnumerateArray())
        {
            if (e.ValueKind != JsonValueKind.Object)
            {
                failures.Add(label + "[" + idx + "]_NOT_OBJECT");
                idx++;
                continue;
            }

            CollectUnknownFields(e, label + "[" + idx + "]_UNKNOWN_FIELDS", failures, "id");

            if (!e.TryGetProperty("id", out var idEl))
            {
                failures.Add(label + "[" + idx + "]_MISSING:id");
            }
            else if (idEl.ValueKind != JsonValueKind.String)
            {
                failures.Add(label + "[" + idx + "]_TYPE:id:not_string");
            }
            else
            {
                var id = idEl.GetString() ?? "";
                if (string.IsNullOrWhiteSpace(id))
                    failures.Add(label + "[" + idx + "]_VALUE:id:empty");
                else if (!seen.Add(id))
                    failures.Add(label + "_DUPLICATE_ID:" + id);
            }

            idx++;
        }
    }

    private static void ValidateRecipes(JsonElement root, List<string> failures)
    {
        if (!root.TryGetProperty("recipes", out var a))
        {
            failures.Add("MISSING:recipes");
            return;
        }
        if (a.ValueKind != JsonValueKind.Array)
        {
            failures.Add("TYPE:recipes:not_array");
            return;
        }

        // Gather known goods for reference checks if available and valid.
        var knownGoods = new HashSet<string>(StringComparer.Ordinal);
        if (root.TryGetProperty("goods", out var goodsEl) && goodsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var g in goodsEl.EnumerateArray())
            {
                if (g.ValueKind != JsonValueKind.Object) continue;
                if (!g.TryGetProperty("id", out var idEl) || idEl.ValueKind != JsonValueKind.String) continue;
                var id = idEl.GetString() ?? "";
                if (!string.IsNullOrWhiteSpace(id)) knownGoods.Add(id);
            }
        }

        var seenRecipes = new HashSet<string>(StringComparer.Ordinal);

        int ridx = 0;
        foreach (var r in a.EnumerateArray())
        {
            var prefix = "recipes[" + ridx + "]";
            if (r.ValueKind != JsonValueKind.Object)
            {
                failures.Add(prefix + "_NOT_OBJECT");
                ridx++;
                continue;
            }

            CollectUnknownFields(r, prefix + "_UNKNOWN_FIELDS", failures, "id", "inputs", "outputs");

            string recipeId = "";
            if (!r.TryGetProperty("id", out var idEl))
            {
                failures.Add(prefix + "_MISSING:id");
            }
            else if (idEl.ValueKind != JsonValueKind.String)
            {
                failures.Add(prefix + "_TYPE:id:not_string");
            }
            else
            {
                recipeId = idEl.GetString() ?? "";
                if (string.IsNullOrWhiteSpace(recipeId))
                    failures.Add(prefix + "_VALUE:id:empty");
                else if (!seenRecipes.Add(recipeId))
                    failures.Add("recipes[]_DUPLICATE_ID:" + recipeId);
            }

            ValidateRecipeLines(prefix, recipeId, r, "inputs", knownGoods, failures);
            ValidateRecipeLines(prefix, recipeId, r, "outputs", knownGoods, failures);

            ridx++;
        }
    }

    private static void ValidateRecipeLines(
        string prefix,
        string recipeId,
        JsonElement recipeObj,
        string field,
        HashSet<string> knownGoods,
        List<string> failures)
    {
        if (!recipeObj.TryGetProperty(field, out var a))
        {
            failures.Add(prefix + "_MISSING:" + field);
            return;
        }
        if (a.ValueKind != JsonValueKind.Array)
        {
            failures.Add(prefix + "_TYPE:" + field + ":not_array");
            return;
        }
        if (a.GetArrayLength() == 0)
        {
            failures.Add(prefix + "_VALUE:" + field + ":minItems_1");
            return;
        }

        int idx = 0;
        foreach (var e in a.EnumerateArray())
        {
            var lp = prefix + "." + field + "[" + idx + "]";
            if (e.ValueKind != JsonValueKind.Object)
            {
                failures.Add(lp + "_NOT_OBJECT");
                idx++;
                continue;
            }

            CollectUnknownFields(e, lp + "_UNKNOWN_FIELDS", failures, "good_id", "qty");

            string goodId = "";
            if (!e.TryGetProperty("good_id", out var gEl))
            {
                failures.Add(lp + "_MISSING:good_id");
            }
            else if (gEl.ValueKind != JsonValueKind.String)
            {
                failures.Add(lp + "_TYPE:good_id:not_string");
            }
            else
            {
                goodId = gEl.GetString() ?? "";
                if (string.IsNullOrWhiteSpace(goodId))
                {
                    failures.Add(lp + "_VALUE:good_id:empty");
                }
                else if (knownGoods.Count != 0 && !knownGoods.Contains(goodId))
                {
                    // Only check unknown-good when goods list is present and parseable.
                    failures.Add(lp + "_VALUE:good_id:unknown:" + goodId);
                }
            }

            if (!e.TryGetProperty("qty", out var qEl))
            {
                failures.Add(lp + "_MISSING:qty");
            }
            else if (qEl.ValueKind != JsonValueKind.Number)
            {
                failures.Add(lp + "_TYPE:qty:not_number");
            }
            else
            {
                var qty = qEl.GetInt32();
                if (qty <= 0) failures.Add(lp + "_VALUE:qty:must_be_gt_0");
            }

            idx++;
        }

        // If recipeId is known and any line failures occurred, keep report stable but do not attempt extra aggregation.
        _ = recipeId;
    }
}
