using SimCore.Intents;
using SimCore.Entities;
using System.Collections.Generic;
using System;

namespace SimCore;

public partial class SimState
{
    /// <summary>
    /// Deterministic entrypoint for systems to enqueue intents.
    /// Mirrors SimKernel's wrap behavior (Seq, tick, kind).
    /// </summary>
    public void EnqueueIntent(IIntent intent)
    {
        if (intent is null) return;

        var seq = NextIntentSeq;
        NextIntentSeq = checked(NextIntentSeq + 1);

        PendingIntents.Add(new IntentEnvelope
        {
            Seq = seq,
            CreatedTick = Tick,
            Kind = intent.Kind,
            Intent = intent
        });
    }

    // Slice 3 / GATE.LOGI.RESERVE.001
    // Reservation helpers (deterministic).
    public int GetTotalReservedRemaining(string marketId, string goodId)
    {
        if (LogisticsReservations is null) return 0;
        if (string.IsNullOrWhiteSpace(marketId)) return 0;
        if (string.IsNullOrWhiteSpace(goodId)) return 0;

        var sum = 0;
        foreach (var r in LogisticsReservations.Values)
        {
            if (r is null) continue;
            if (!string.Equals(r.MarketId, marketId, StringComparison.Ordinal)) continue;
            if (!string.Equals(r.GoodId, goodId, StringComparison.Ordinal)) continue;
            if (r.Remaining <= 0) continue;
            sum = checked(sum + r.Remaining);
        }
        return sum;
    }

    public int GetUnreservedAvailable(string marketId, string goodId)
    {
        if (string.IsNullOrWhiteSpace(marketId)) return 0;
        if (string.IsNullOrWhiteSpace(goodId)) return 0;

        if (!Markets.TryGetValue(marketId, out var m) || m is null) return 0;
        var inv = m.Inventory.TryGetValue(goodId, out var v) ? v : 0;
        if (inv <= 0) return 0;

        var reserved = GetTotalReservedRemaining(marketId, goodId);
        var unreserved = inv - reserved;
        return unreserved <= 0 ? 0 : unreserved;
    }

    public bool TryCreateLogisticsReservation(string marketId, string goodId, string fleetId, int requestedQty, out string reservationId, out int reservedQty)
    {
        reservationId = "";
        reservedQty = 0;

        if (string.IsNullOrWhiteSpace(marketId)) return false;
        if (string.IsNullOrWhiteSpace(goodId)) return false;
        if (string.IsNullOrWhiteSpace(fleetId)) return false;
        if (requestedQty <= 0) return false;

        // Reserve only from currently unreserved pool (does not mutate inventory).
        var unreserved = GetUnreservedAvailable(marketId, goodId);
        if (unreserved <= 0) return true; // optional: no reservation created

        var qty = Math.Min(unreserved, requestedQty);
        if (qty <= 0) return true;

        var id = $"R{NextLogisticsReservationSeq}";
        NextLogisticsReservationSeq = checked(NextLogisticsReservationSeq + 1);

        LogisticsReservations ??= new Dictionary<string, SimCore.Entities.LogisticsReservation>(StringComparer.Ordinal);
        LogisticsReservations[id] = new SimCore.Entities.LogisticsReservation
        {
            Id = id,
            MarketId = marketId,
            GoodId = goodId,
            FleetId = fleetId,
            Remaining = qty
        };

        reservationId = id;
        reservedQty = qty;
        return true;
    }

    public bool TryGetLogisticsReservation(string reservationId, out SimCore.Entities.LogisticsReservation? res)
    {
        res = null;
        if (LogisticsReservations is null) return false;
        if (string.IsNullOrWhiteSpace(reservationId)) return false;
        return LogisticsReservations.TryGetValue(reservationId, out res);
    }

    public void ConsumeLogisticsReservation(string reservationId, int consumeQty)
    {
        if (consumeQty <= 0) return;
        if (!TryGetLogisticsReservation(reservationId, out var r) || r is null) return;

        var next = r.Remaining - consumeQty;
        if (next <= 0)
        {
            LogisticsReservations.Remove(reservationId);
            return;
        }

        r.Remaining = next;
    }

    public void ReleaseLogisticsReservation(string reservationId)
    {
        if (LogisticsReservations is null) return;
        if (string.IsNullOrWhiteSpace(reservationId)) return;
        LogisticsReservations.Remove(reservationId);
    }
}
