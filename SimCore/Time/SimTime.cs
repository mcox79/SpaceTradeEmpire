namespace SimCore.Time;

/// <summary>
/// Sim time constants and conversions (LOCKED).
/// Contract:
/// - 1 tick = 1 game minute
/// - 1 real second = 1 game minute (60x)
/// - no acceleration (enforced at higher layers, but constants live here)
/// </summary>
public static class SimTime
{
	public const int MinutesPerTick = 1;

	// 60x means: 1 real second advances 1 game minute.
	public const int GameMinutesPerRealSecond = 1;

	public const int SecondsPerGameMinute = 60;
	public const int GameMinutesPerGameHour = 60;
	public const int GameHoursPerGameDay = 24;

	public static int TicksToGameMinutes(int ticks)
	{
		return ticks * MinutesPerTick;
	}

	public static int GameMinutesToTicks(int gameMinutes)
	{
		// MinutesPerTick is locked to 1, but keep this explicit anyway.
		return gameMinutes / MinutesPerTick;
	}

	public static int GameHoursToTicks(int gameHours)
	{
		return gameHours * GameMinutesPerGameHour / MinutesPerTick;
	}

	public static int GameDaysToTicks(int gameDays)
	{
		return gameDays * GameHoursPerGameDay * GameMinutesPerGameHour / MinutesPerTick;
	}
}
