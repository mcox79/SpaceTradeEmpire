using System;

namespace SimCore.Systems;

public static class BandedTime
{
        // Deterministic time banding for UI. Fixed thresholds.
        //
        // Inputs:
        // - ticks: remaining ticks until event. Use int.MaxValue for "infinite".
        // - ticksPerDay: sim constant, typically IndustrySystem.TicksPerDay.
        //
        // Output:
        // - short stable labels intended for UI display and tests.
        public static string BandTicks(int ticks, int ticksPerDay)
        {
                if (ticks < 0) return "?";
                if (ticks == int.MaxValue) return "INF";
                if (ticksPerDay <= 0) return "?";

                // Derive ticks/hour deterministically (ceil to avoid 0).
                var ticksPerHour = Math.Max(1, ticksPerDay / 24);

                if (ticks == 0) return "NOW";
                if (ticks < 1 * ticksPerHour) return "<1h";
                if (ticks < 6 * ticksPerHour) return "<6h";
                if (ticks < 24 * ticksPerHour) return "<1d";
                if (ticks < 3 * ticksPerDay) return "<3d";
                if (ticks < 7 * ticksPerDay) return "<7d";
                return "7d+";
        }

        public static string BandDays(float days)
        {
                if (float.IsNaN(days)) return "?";
                if (days < 0f) return "?";
                if (float.IsPositiveInfinity(days)) return "INF";

                if (days <= 0f) return "NOW";
                if (days < (1f / 24f)) return "<1h";
                if (days < (6f / 24f)) return "<6h";
                if (days < 1f) return "<1d";
                if (days < 3f) return "<3d";
                if (days < 7f) return "<7d";
                return "7d+";
        }
}
