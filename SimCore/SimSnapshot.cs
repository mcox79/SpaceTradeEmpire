namespace SimCore;

public sealed record SimSnapshot(
    string CurrentSystemId,
    string? SelectedDestinationSystemId
);