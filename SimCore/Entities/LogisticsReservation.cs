using System.Text.Json.Serialization;

namespace SimCore.Entities;

// Slice 3 / GATE.LOGI.RESERVE.001
// Virtual reservation that protects a portion of market inventory from being loaded by others.
// NOTE: Does NOT mutate market inventory. Enforcement happens in LoadCargoCommand.
public sealed class LogisticsReservation
{
    [JsonInclude] public string Id { get; set; } = "";

    [JsonInclude] public string MarketId { get; set; } = "";
    [JsonInclude] public string GoodId { get; set; } = "";

    // Who owns the reservation (used as a deterministic tie breaker and audit surface).
    [JsonInclude] public string FleetId { get; set; } = "";

    // The reserved remaining quantity that is protected from non-owner LOAD_CARGO.
    [JsonInclude] public int Remaining { get; set; } = 0;

    // Optional note/debug surface.
    [JsonInclude] public string Note { get; set; } = "";
}
