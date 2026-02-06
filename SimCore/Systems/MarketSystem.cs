using SimCore.Entities;

namespace SimCore.Systems;

public static class MarketSystem
{
	// GATE.MKT.002: publish cadence every 12 game hours.
	// With 1 tick = 1 sim minute, 12 hours = 720 minutes = 720 ticks.
	public const int PublishWindowTicks = 720;

	public static void Process(SimState state)
	{
		// 1. Decay Edge Heat (Cooling)
		foreach (var edge in state.Edges.Values)
		{
			if (edge.Heat > 0)
			{
				edge.Heat -= 0.05f;
				if (edge.Heat < 0) edge.Heat = 0;
			}
		}

		// 2. Publish prices on cadence (deterministic, once per bucket)
		foreach (var market in state.Markets.Values)
		{
			market.PublishPricesIfDue(state.Tick, PublishWindowTicks);
		}
	}

	// Called when a Fleet traverses an Edge with Cargo
	public static void RegisterTraffic(SimState state, string edgeId, int cargoVolume)
	{
		if (state.Edges.TryGetValue(edgeId, out var edge))
		{
			// Heat generated per unit of cargo
			edge.Heat += cargoVolume * 0.01f;
		}
	}
}
