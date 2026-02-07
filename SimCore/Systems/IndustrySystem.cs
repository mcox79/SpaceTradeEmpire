using System;
using SimCore.Entities;

namespace SimCore.Systems
{
    public static class IndustrySystem
    {
        // TIME MODEL: 1 tick = 1 minute game time (GATE.TIME.001 already adopted).
        // Buffering is expressed in days of game time.
        public const int TicksPerDay = 1440;
        public const int Bps = 10000;

        public static int ComputeBufferTargetUnits(IndustrySite site, string goodId)
        {
            if (site is null) throw new ArgumentNullException(nameof(site));
            if (string.IsNullOrWhiteSpace(goodId)) throw new ArgumentException("goodId must be non-empty.", nameof(goodId));

            if (!site.Inputs.TryGetValue(goodId, out var perTick) || perTick <= 0) return 0;

            var days = site.BufferDays;
            if (days < 0) days = 0;

            return checked(perTick * days * TicksPerDay);
        }

        public static void Process(SimState state)
        {
            foreach (var site in state.IndustrySites.Values)
            {
                if (!site.Active) continue;
                if (!state.Markets.TryGetValue(site.NodeId, out var market)) continue;

                // Compute efficiency deterministically as basis points:
                // effBps = min over inputs of floor(available * 10000 / required).
                int effBps = Bps;

                if (site.Inputs.Count > 0)
                {
                    foreach (var input in site.Inputs)
                    {
                        if (input.Value <= 0) continue;

                        int available = InventoryLedger.Get(market.Inventory, input.Key);
                        int required = input.Value;

                        int ratioBps;
                        if (available <= 0) ratioBps = 0;
                        else ratioBps = (int)Math.Min((long)Bps, ((long)available * Bps) / required);

                        if (ratioBps < effBps) effBps = ratioBps;
                        if (effBps == 0) break;
                    }
                }

                if (effBps < 0) effBps = 0;
                if (effBps > Bps) effBps = Bps;

                site.Efficiency = effBps / (float)Bps;

                // Degrade deterministically when undersupplied.
                ApplyDegradation(site, effBps);

                // If completely starved, we still degrade but we do not consume/produce.
                if (effBps <= 0) continue;

                // Consume inputs (preserve zero keys for markets)
                foreach (var input in site.Inputs)
                {
                    if (input.Value <= 0) continue;

                    int available = InventoryLedger.Get(market.Inventory, input.Key);
                    int targetConsume = (int)(((long)input.Value * effBps) / Bps);
                    int consume = Math.Min(available, targetConsume);

                    if (consume > 0)
                    {
                        InventoryLedger.TryRemoveMarket(market.Inventory, input.Key, consume);
                    }
                    else
                    {
                        if (!market.Inventory.ContainsKey(input.Key)) market.Inventory[input.Key] = 0;
                    }
                }

                // Produce outputs (preserve zero keys for markets)
                foreach (var output in site.Outputs)
                {
                    if (output.Value <= 0) continue;

                    int produced = (int)(((long)output.Value * effBps) / Bps);

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

        private static void ApplyDegradation(IndustrySite site, int effBps)
        {
            if (site.DegradePerDayBps <= 0) return;
            if (site.HealthBps <= 0) return;

            int deficitBps = Bps - effBps;
            if (deficitBps <= 0) return;

            // Health loss per day at full deficit is DegradePerDayBps.
            // Per tick health loss = DegradePerDayBps * deficitBps / (TicksPerDay * 10000).
            long numer = (long)site.DegradePerDayBps * deficitBps;
            long denom = (long)TicksPerDay * Bps;

            site.DegradeRemainder = checked(site.DegradeRemainder + numer);

            int dec = (int)(site.DegradeRemainder / denom);
            site.DegradeRemainder = site.DegradeRemainder % denom;

            if (dec <= 0) return;

            site.HealthBps = Math.Max(0, site.HealthBps - dec);
        }
    }
}
