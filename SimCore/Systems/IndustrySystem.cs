using System;
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

				double efficiency = 1.0;

				foreach (var input in site.Inputs)
				{
					if (input.Value <= 0) continue;

					int available = InventoryLedger.Get(market.Inventory, input.Key);
					double ratio = (double)available / input.Value;

					if (ratio < efficiency) efficiency = ratio;
				}

				if (efficiency <= 0.0) continue;
				if (efficiency > 1.0) efficiency = 1.0;

				// Consume inputs (preserve zero keys for markets)
				foreach (var input in site.Inputs)
				{
					if (input.Value <= 0) continue;

					int available = InventoryLedger.Get(market.Inventory, input.Key);
					int targetConsume = (int)(input.Value * efficiency);
					int consume = Math.Min(available, targetConsume);

					if (consume > 0)
					{
						InventoryLedger.TryRemoveMarket(market.Inventory, input.Key, consume);
					}
					else
					{
						// Preserve key semantics for existing tests and callers
						if (!market.Inventory.ContainsKey(input.Key)) market.Inventory[input.Key] = 0;
					}
				}

				// Produce outputs (preserve zero keys for markets)
				foreach (var output in site.Outputs)
				{
					if (output.Value <= 0) continue;

					int produced = (int)(output.Value * efficiency);
					if (produced > 0)
					{
						InventoryLedger.AddMarket(market.Inventory, output.Key, produced);
					}
					else
					{
						if (!market.Inventory.ContainsKey(output.Key)) market.Inventory[output.Key] = 0;
					}
				}
			}
		}
	}
}
