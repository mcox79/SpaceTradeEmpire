namespace SimCore.Tweaks;

// GATE.S8.STORY_STATE.ENTITY.001: Story state tuning constants.
public static class StoryStateTweaksV0
{
    // R1 Module Revelation: fracture exposure threshold + lattice visit minimum.
    public const int R1FractureExposureThreshold = 15;
    public const int R1LatticeVisitMinimum = 3;

    // R2 Concord Revelation: requires Concord reputation >= this value (Allied = 60).
    public const int R2ConcordRepThreshold = 60;

    // R3 Pentagon Break: requires all 5 PentagonTradeFlags set (automatic from trading).

    // R4 Communion Revelation: fracture exposure threshold + must have read a Communion log.
    public const int R4FractureExposureThreshold = 30;

    // R5 Instability Revelation: minimum tick and minimum collected fragments.
    public const int R5MinimumTick = 2000;
    public const int R5MinimumFragments = 8;

    // Act transition thresholds (revelation count).
    public const int Act2Threshold = 1;  // 1+ revelations -> Act2_Questioning
    public const int Act3Threshold = 3;  // 3+ revelations -> Act3_Revealed
}
