using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimCore.Events;

// GATE.S4.INDU_STRUCT.SHORTFALL_LOG.001
// Schema-bound, deterministic industry shortfall event stream.
// Emitted by IndustrySystem when a site operates below full efficiency.
public static class IndustryEvents
{
    public const int EventsVersion = 1;

    public sealed class ShortfallEvent
    {
        [JsonInclude] public int Version { get; set; } = EventsVersion;
        [JsonInclude] public long Seq { get; set; }
        [JsonInclude] public int Tick { get; set; }
        [JsonInclude] public string SiteId { get; set; } = "";
        [JsonInclude] public string RecipeId { get; set; } = "";
        [JsonInclude] public string MissingGoodId { get; set; } = "";
        [JsonInclude] public int RequiredQty { get; set; }
        [JsonInclude] public int AvailableQty { get; set; }
        [JsonInclude] public int EfficiencyBps { get; set; }
    }
}
