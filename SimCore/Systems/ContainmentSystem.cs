using SimCore.Entities;
using System;
using System.Linq;

namespace SimCore.Systems;

public static class ContainmentSystem
{
    // CONSTANTS
    private const float TRACE_DECAY_RATE = 0.05f;

    public static void Process(SimState state)
    {
        foreach (var node in state.Nodes.Values)
        {
            if (node.Trace > 0)
            {
                // 1. Natural Decay
                node.Trace -= TRACE_DECAY_RATE;
                
                // 2. Clamp to Zero
                if (node.Trace < 0) node.Trace = 0;

                // 3. (Future) Trigger Hunter Fleets if Trace > Threshold
            }
        }
    }
}