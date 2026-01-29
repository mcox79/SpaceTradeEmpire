using System.Text.Json;

namespace SimCore.Systems;

public static class SerializationSystem
{
    private static readonly JsonSerializerOptions _options = new() { WriteIndented = true, IncludeFields = true };

    public static string Serialize(SimState state)
    {
        return JsonSerializer.Serialize(state, _options);
    }

    public static SimState Deserialize(string json)
    {
        var state = JsonSerializer.Deserialize<SimState>(json, _options);
        if (state != null)
        {
            // Restore RNG
            state.HydrateAfterLoad();
        }
        return state;
    }
}