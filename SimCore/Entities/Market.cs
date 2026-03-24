using System;
using System.Collections.Generic;
using System.Linq;

namespace SimCore.Entities;

public class Market
{
    public string Id { get; set; } = "";

    // GATE.S3_6.DISCOVERY_UNLOCK_CONTRACT.003
    // If non-empty, access to this market is gated by acquiring the referenced Permit unlock id.
    // Empty means no permit is required.
    public string RequiresPermitUnlockId { get; set; } = "";

    // INVENTORY: Raw goods storage
    public Dictionary<string, int> Inventory { get; set; } = new();

    // INDUSTRY: Production capabilities
    public Dictionary<string, Industry> Industries { get; set; } = new();

    // SLICE 1 PRICING MODEL (deterministic, inventory-based, with spread)
    // Notes:
    // - Mid price increases as stock drops below IdealStock, decreases as stock rises above IdealStock
    // - BuyPrice is what the player pays (market sells to player)
    // - SellPrice is what the player receives (market buys from player)
    // - Spread is deterministic and non-zero

    public const int BasePrice = 100; // Default fallback; per-good prices override via GoodBasePrices.
    public const int IdealStock = 50;

    // Per-good base prices populated from ContentRegistry at world load.
    // Key: goodId, Value: base price in credits. Falls back to BasePrice if absent.
    private static Dictionary<string, int> s_goodBasePrices = new(StringComparer.Ordinal);

    /// <summary>
    /// Set per-good base prices from the content registry. Called once at world load.
    /// </summary>
    public static void SetGoodBasePrices(IEnumerable<(string Id, int BasePrice)> goodPrices)
    {
        s_goodBasePrices.Clear();
        foreach (var (id, bp) in goodPrices)
        {
            if (bp > 0) s_goodBasePrices[id] = bp;
        }
    }

    /// <summary>
    /// Get the per-good base price, falling back to the constant BasePrice if not set.
    /// </summary>
    public static int GetGoodBasePrice(string goodId)
    {
        return s_goodBasePrices.TryGetValue(goodId, out var bp) ? bp : BasePrice;
    }

    /// <summary>
    /// Clear per-good base prices (revert to BasePrice fallback). Used in tests to prevent cross-test contamination.
    /// </summary>
    public static void ClearGoodBasePrices()
    {
        s_goodBasePrices.Clear();
    }

    // Min spread in absolute credits. Also enforces BuyPrice > SellPrice.
    public const int MinSpread = 2;

    // Spread in basis points of mid price (1000 = 10%). Keep simple for Slice 1.
    public const int SpreadBps = 1000;

    // SLICE 1.5 / GATE.MKT.002: Published (visible) prices
    // Published prices update at a fixed cadence (every 12 game hours = 720 ticks).
    // Underlying mid/buy/sell can vary with inventory continuously; published values only change on cadence.
    public int LastPublishedBucket { get; set; } = -1; // Tick / 720
    public int LastPublishedTick { get; set; } = -1;

    public Dictionary<string, int> PublishedMid { get; set; } = new();
    public Dictionary<string, int> PublishedBuy { get; set; } = new();
    public Dictionary<string, int> PublishedSell { get; set; } = new();

    // Backwards compatible: existing tests/callers use GetPrice().
    // For Slice 1, define GetPrice as the mid price.
    public int GetPrice(string goodId) => GetMidPrice(goodId);

    public int GetMidPrice(string goodId)
    {
        if (string.IsNullOrWhiteSpace(goodId)) throw new ArgumentException("goodId must be non-empty.", nameof(goodId));

        int stock = Inventory.TryGetValue(goodId, out var v) ? v : 0;
        int goodBase = GetGoodBasePrice(goodId);

        // GATE.T52.ECON.TRADE_DIVERSITY.001: Proportional scarcity curve.
        // Shift is proportional to base price so high-value goods respond
        // more strongly to supply changes, encouraging trade diversity.
        // ScarcityBpsPerUnit: each unit away from IdealStock shifts price by
        // this many basis points of goodBase.
        int delta = IdealStock - stock;
        int shiftBps = delta * Tweaks.MarketTweaksV0.ScarcityBpsPerUnit;
        int mid = goodBase + (int)((long)goodBase * shiftBps / 10000);

        return Math.Max(1, mid);
    }

    public int GetBuyPrice(string goodId)
    {
        int mid = GetMidPrice(goodId);
        int spread = ComputeSpread(mid);

        // Ask price (player buys from market)
        int buy = mid + (spread / 2);
        return Math.Max(1, buy);
    }

    public int GetSellPrice(string goodId)
    {
        int mid = GetMidPrice(goodId);
        int spread = ComputeSpread(mid);

        // Bid price (player sells to market)
        int sell = mid - (spread / 2);

        // sell must be at least 1.
        return Math.Max(1, sell);
    }

    public int GetPublishedMidPrice(string goodId)
    {
        if (PublishedMid.TryGetValue(goodId, out var v)) return v;
        return GetMidPrice(goodId);
    }

    public int GetPublishedBuyPrice(string goodId)
    {
        if (PublishedBuy.TryGetValue(goodId, out var v)) return v;
        return GetBuyPrice(goodId);
    }

    public int GetPublishedSellPrice(string goodId)
    {
        if (PublishedSell.TryGetValue(goodId, out var v)) return v;
        return GetSellPrice(goodId);
    }

    public void PublishPricesIfDue(int tick, int publishWindowTicks)
    {
        if (publishWindowTicks <= 0) throw new ArgumentOutOfRangeException(nameof(publishWindowTicks));

        int bucket = tick / publishWindowTicks;
        if (bucket == LastPublishedBucket) return;

        // Deterministic key set: whatever goods currently exist as inventory keys.
        // Order keys to avoid any nondeterminism.
        var goods = Inventory.Keys.OrderBy(x => x, StringComparer.Ordinal).ToArray();

        foreach (var goodId in goods)
        {
            int mid = GetMidPrice(goodId);
            int buy = GetBuyPrice(goodId);
            int sell = GetSellPrice(goodId);

            PublishedMid[goodId] = mid;
            PublishedBuy[goodId] = buy;
            PublishedSell[goodId] = sell;
        }

        LastPublishedBucket = bucket;
        LastPublishedTick = tick;
    }

    private static int ComputeSpread(int mid)
    {
        // Deterministic spread: max(MinSpread, round(mid * SpreadBps / 10000))
        // Use integer math with rounding half up.
        long numer = (long)mid * SpreadBps;
        int pctSpread = (int)((numer + 5000) / 10000);

        int spread = Math.Max(MinSpread, pctSpread);

        // Guarantee spread is at least 2 so BuyPrice can exceed SellPrice under all conditions.
        if (spread < 2) spread = 2;

        return spread;
    }
}
