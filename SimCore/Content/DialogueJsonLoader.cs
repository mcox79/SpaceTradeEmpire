using System;
using System.Reflection;
using System.Text.Json;

namespace SimCore.Content;

/// <summary>
/// Loads embedded JSON dialogue resources from the SimCore assembly.
/// Used by TutorialContentV0 and FirstOfficerContentV0 to externalize narrative text.
/// </summary>
internal static class DialogueJsonLoader
{
    /// <summary>
    /// Load an embedded JSON resource by filename and deserialize to T.
    /// The resource must be in SimCore.Content.Data namespace (matching folder structure).
    /// </summary>
    internal static T Load<T>(string resourceFileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"SimCore.Content.Data.{resourceFileName}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{resourceName}' not found. Available: {string.Join(", ", assembly.GetManifestResourceNames())}");

        return JsonSerializer.Deserialize<T>(stream, JsonOpts)
            ?? throw new InvalidOperationException($"Failed to deserialize '{resourceName}'.");
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };
}
