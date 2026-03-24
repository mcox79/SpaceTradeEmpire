using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimCore.RlServer;

// ── Requests ──

public class RlRequest
{
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("seed")] public int Seed { get; set; }
    [JsonPropertyName("star_count")] public int StarCount { get; set; } = 12;
    [JsonPropertyName("curriculum_stage")] public int CurriculumStage { get; set; }
    [JsonPropertyName("max_episode_ticks")] public int MaxEpisodeTicks { get; set; } = 2000;
    [JsonPropertyName("action")] public int Action { get; set; }
}

// ── Responses ──

public class RlResponse
{
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("obs")] public float[]? Obs { get; set; }
    [JsonPropertyName("reward")] public float Reward { get; set; }
    [JsonPropertyName("terminated")] public bool Terminated { get; set; }
    [JsonPropertyName("truncated")] public bool Truncated { get; set; }
    [JsonPropertyName("info")] public Dictionary<string, object>? Info { get; set; }
    [JsonPropertyName("error")] public string? Error { get; set; }
    [JsonPropertyName("action_mask")] public bool[]? ActionMask { get; set; }
}

public static class Protocol
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static RlRequest? ParseRequest(string line)
    {
        return JsonSerializer.Deserialize<RlRequest>(line, JsonOpts);
    }

    public static string Serialize(RlResponse response)
    {
        return JsonSerializer.Serialize(response, JsonOpts);
    }
}
