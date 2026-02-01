using System;
using System.Collections.Generic;
using SimCore.Entities;

namespace SimCore.Systems
{
    public static class IndustrySystem
    {
        public static void Process(SimState state)
        {
            foreach (var site in state.IndustrySites.Values)
            {
                if (!state.Markets.TryGetValue(site.NodeId, out var market)) continue;

                // 1. Calculate Production Efficiency (Ratio)
                // We cap at 1.0 (100% capacity per tick) but allow downscaling.
                double efficiency = 1.0;

                foreach (var input in site.Inputs)
                {
                    if (input.Value == 0) continue;
                    
                    int available = market.Inventory.ContainsKey(input.Key) ? market.Inventory[input.Key] : 0;
                    double ratio = (double)available / input.Value;
                    
                    if (ratio < efficiency)
                    {
                        efficiency = ratio;
                    }
                }

                if (efficiency <= 0.0) continue;

                // 2. Consume Inputs (Scaled)
                foreach (var input in site.Inputs)
                {
                    if (!market.Inventory.ContainsKey(input.Key)) market.Inventory[input.Key] = 0;
                    // Use integer math: floor(Req * Efficiency)
                    int consumed = (int)(input.Value * efficiency);
                    market.Inventory[input.Key] -= consumed;
                }

                // 3. Produce Outputs (Scaled)
                foreach (var output in site.Outputs)
                {
                    if (!market.Inventory.ContainsKey(output.Key)) market.Inventory[output.Key] = 0;
                    int produced = (int)(output.Value * efficiency);
                    market.Inventory[output.Key] += produced;
                }
            }
        }
    }
}