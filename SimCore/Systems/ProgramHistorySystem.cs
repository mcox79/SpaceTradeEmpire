using System.Collections.Generic;
using System.Linq;
using SimCore.Entities;

namespace SimCore.Systems;

// GATE.S7.AUTOMATION_MGMT.PROGRAM_HISTORY.001: Program outcome history ring buffer.
// Maintains up to MaxEntries recent program outcomes per fleet (newest first).
public static class ProgramHistorySystem
{
    /// <summary>Maximum number of history entries retained per fleet.</summary>
    public const int MaxEntries = 20;

    /// <summary>
    /// Record a program outcome. Inserts at position 0 (newest first).
    /// If the list exceeds MaxEntries, the oldest entry is removed.
    /// </summary>
    public static void RecordOutcome(Fleet fleet, ProgramHistoryEntry entry)
    {
        if (fleet == null || entry == null) return;

        fleet.History.Insert(0, entry);

        // Trim to ring buffer size.
        while (fleet.History.Count > MaxEntries)
        {
            fleet.History.RemoveAt(fleet.History.Count - 1);
        }
    }

    /// <summary>
    /// Returns the history list (newest first). Returns empty list if fleet is null.
    /// </summary>
    public static List<ProgramHistoryEntry> GetHistory(Fleet fleet)
    {
        if (fleet == null) return new List<ProgramHistoryEntry>();
        return fleet.History;
    }

    /// <summary>
    /// Returns the success rate as a float 0.0-1.0.
    /// Returns 0 if no history entries exist.
    /// </summary>
    public static float GetSuccessRate(Fleet fleet)
    {
        if (fleet == null || fleet.History.Count == 0) return 0f;
        int successes = 0;
        for (int i = 0; i < fleet.History.Count; i++)
        {
            if (fleet.History[i].Success) successes++;
        }
        return (float)successes / fleet.History.Count;
    }

    /// <summary>
    /// GATE.S7.AUTOMATION.FAILURE_REASONS.001: Count failures by reason in recent history.
    /// Returns dictionary of reason → count.
    /// </summary>
    public static Dictionary<ProgramFailureReason, int> GetFailureReasonCounts(Fleet fleet)
    {
        var counts = new Dictionary<ProgramFailureReason, int>();
        if (fleet == null) return counts;
        foreach (var entry in fleet.History)
        {
            if (!entry.Success && entry.FailureReason != ProgramFailureReason.None)
            {
                counts.TryGetValue(entry.FailureReason, out int c);
                counts[entry.FailureReason] = c + 1;
            }
        }
        return counts;
    }
}
