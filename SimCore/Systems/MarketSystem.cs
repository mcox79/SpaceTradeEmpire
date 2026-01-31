using SimCore.Entities;
using System;

namespace SimCore.Systems;

public static class MarketSystem
{
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