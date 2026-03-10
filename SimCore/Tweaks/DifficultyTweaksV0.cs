namespace SimCore.Tweaks;

/// <summary>
/// Difficulty presets for new voyage creation.
/// Stored in SimState and used by systems that read difficulty multipliers.
/// </summary>
public enum DifficultyPreset
{
    Normal = 0,
    Hard = 1,
    Brutal = 2
}

/// <summary>
/// Difficulty multiplier set returned by GetMultipliers().
/// All values are integer percentages (100 = 1.0x) to preserve determinism.
/// </summary>
public sealed class DifficultyMultipliers
{
    public int GalaxySizePct { get; init; }
    public int EconomySpeedPct { get; init; }
    public int CombatDamagePct { get; init; }
    public int SustainDecayPct { get; init; }
}

/// <summary>
/// GATE.S7.MAIN_MENU.NEW_VOYAGE.001: Difficulty pacing constants (integers only for determinism).
/// All gameplay numeric literals for the difficulty system live here.
/// </summary>
public static class DifficultyTweaksV0
{
    // --- Normal preset (baseline: all multipliers = 100%) ---
    public const int NormalGalaxySizePct = 100;
    public const int NormalEconomySpeedPct = 100;
    public const int NormalCombatDamagePct = 100;
    public const int NormalSustainDecayPct = 100;

    // --- Hard preset ---
    public const int HardGalaxySizePct = 100;
    public const int HardEconomySpeedPct = 80;   // slower profits
    public const int HardCombatDamagePct = 150;   // enemies hit harder
    public const int HardSustainDecayPct = 130;   // faster decay

    // --- Brutal preset ---
    public const int BrutalGalaxySizePct = 150;   // more systems (larger galaxy)
    public const int BrutalEconomySpeedPct = 60;  // much slower profits
    public const int BrutalCombatDamagePct = 200;  // enemies hit 2x
    public const int BrutalSustainDecayPct = 180;  // much faster decay

    /// <summary>
    /// Returns the multiplier set for the given difficulty preset.
    /// Unknown values fall back to Normal (determinism-safe).
    /// </summary>
    public static DifficultyMultipliers GetMultipliers(DifficultyPreset preset)
    {
        return preset switch
        {
            DifficultyPreset.Hard => new DifficultyMultipliers
            {
                GalaxySizePct = HardGalaxySizePct,
                EconomySpeedPct = HardEconomySpeedPct,
                CombatDamagePct = HardCombatDamagePct,
                SustainDecayPct = HardSustainDecayPct,
            },
            DifficultyPreset.Brutal => new DifficultyMultipliers
            {
                GalaxySizePct = BrutalGalaxySizePct,
                EconomySpeedPct = BrutalEconomySpeedPct,
                CombatDamagePct = BrutalCombatDamagePct,
                SustainDecayPct = BrutalSustainDecayPct,
            },
            _ => new DifficultyMultipliers
            {
                GalaxySizePct = NormalGalaxySizePct,
                EconomySpeedPct = NormalEconomySpeedPct,
                CombatDamagePct = NormalCombatDamagePct,
                SustainDecayPct = NormalSustainDecayPct,
            },
        };
    }
}
