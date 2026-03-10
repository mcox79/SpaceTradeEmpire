using SimCore.Entities;

namespace SimCore.Systems;

// GATE.S7.AUTOMATION_MGMT.DOCTRINE.001: Doctrine evaluation system.
// Evaluates fleet retreat conditions and effective engagement stance.
public static class DoctrineSystem
{
    /// <summary>
    /// Returns true if the fleet's hull percentage is at or below the retreat threshold.
    /// If HullHpMax is uninitialized (-1) or zero, retreat is never triggered.
    /// </summary>
    public static bool EvaluateRetreat(Fleet fleet)
    {
        if (fleet == null) return false;
        if (fleet.HullHpMax <= 0) return false;

        int hullPct = (int)((long)fleet.HullHp * 100 / fleet.HullHpMax);
        return hullPct <= fleet.Doctrine.RetreatThresholdPct;
    }

    /// <summary>
    /// Returns the effective engagement stance from the fleet's doctrine.
    /// </summary>
    public static EngagementStance GetEffectiveStance(Fleet fleet)
    {
        if (fleet == null) return EngagementStance.Defensive;
        return fleet.Doctrine.Stance;
    }
}
