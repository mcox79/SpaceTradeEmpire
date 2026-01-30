using SimCore.Entities;
using System;
using System.Linq;

namespace SimCore.Systems;

public static class IndustrySystem
{
    public static void Process(SimState state)
    {
        foreach (var site in state.IndustrySites.Values)
        {
            if (!site.Active) continue;
            if (!state.Markets.TryGetValue(site.NodeId, out var market)) continue;

            // 1. Calculate Production Limit (Limiting Factor)
            float scale = 1.0f;
            foreach (var input in site.Inputs)
            {
                if (input.Value <= 0) continue;
                int available = 0;
                if (market.Inventory.ContainsKey(input.Key)) available = market.Inventory[input.Key];
                
                float ratio = (float)available / (float)input.Value;
                if (ratio < scale) scale = ratio;
            }

            // 2. Efficiency Penalty (Optional: could add randomness or maintenance logic here)
            scale *= site.Efficiency;

            if (scale <= 0.01f) continue; // Skip negligible production

            // 3. Consume Inputs
            foreach (var input in site.Inputs)
            {
                int consumed = (int)Math.Floor(input.Value * scale);
                if (consumed > 0)
                {
                    market.Inventory[input.Key] -= consumed;
                    // Safety clamp
                    if (market.Inventory[input.Key] < 0) market.Inventory[input.Key] = 0;
                }
            }

            // 4. Produce Outputs
            foreach (var output in site.Outputs)
            {
                int produced = (int)Math.Floor(output.Value * scale);
                if (produced > 0)
                {
                    if (!market.Inventory.ContainsKey(output.Key)) market.Inventory[output.Key] = 0;
                    market.Inventory[output.Key] += produced;
                }
            }
        }
    }
}