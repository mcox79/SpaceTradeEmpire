namespace SimCore;

public sealed record SimSnapshot(
    string CurrentNodeId,
    string? SelectedDestinationNodeId
);
