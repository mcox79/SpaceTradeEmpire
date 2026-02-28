using System.Text;
using System.Security.Cryptography;
using System.Linq;
using System.Globalization;

namespace SimCore;

public partial class SimState
{
    public string GetSignature()
    {
        var sb = new StringBuilder();
        sb.Append($"Tick:{Tick}|Cred:{PlayerCredits}|Loc:{PlayerLocationNodeId}|");

        sb.Append($"Nodes:{Nodes.Count}|Edges:{Edges.Count}|Markets:{Markets.Count}|Fleets:{Fleets.Count}|Sites:{IndustrySites.Count}|");

        foreach (var f in Fleets.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            sb.Append($"Flt:{f.Key}_N:{f.Value.CurrentNodeId}_S:{f.Value.State}_D:{f.Value.DestinationNodeId}|");

            // Include cargo deterministically (keys sorted, stable formatting).
            if (f.Value.Cargo is not null && f.Value.Cargo.Count > 0)
            {
                sb.Append("Cargo:");
                foreach (var kv in f.Value.Cargo.OrderBy(kv => kv.Key, StringComparer.Ordinal))
                {
                    sb.Append($"{kv.Key}:{kv.Value},");
                }
                sb.Append("|");
            }
        }

        foreach (var m in Markets.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            sb.Append($"Mkt:{m.Key}|");
            foreach (var kv in m.Value.Inventory.OrderBy(i => i.Key, StringComparer.Ordinal))
            {
                sb.Append($"{kv.Key}:{kv.Value},");
            }
            sb.Append("|");
        }

        if (LogisticsReservations is not null && LogisticsReservations.Count > 0)
        {
            foreach (var kv in LogisticsReservations.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                var r = kv.Value;
                sb.Append($"Res:{kv.Key}|M:{r.MarketId}|G:{r.GoodId}|F:{r.FleetId}|Rem:{r.Remaining}|");
            }
        }

        if (Programs is not null && Programs.Instances is not null && Programs.Instances.Count > 0)
        {
            foreach (var kv in Programs.Instances.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                var p = kv.Value;
                sb.Append($"Prog:{p.Id}|K:{p.Kind}|S:{p.Status}|Cad:{p.CadenceTicks}|Nx:{p.NextRunTick}|Ls:{p.LastRunTick}|Site:{p.SiteId}|M:{p.MarketId}|G:{p.GoodId}|Q:{p.Quantity}|");
            }
        }

        if (Intel is not null && Intel.Observations is not null && Intel.Observations.Count > 0)
        {
            foreach (var kv in Intel.Observations.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                var obs = kv.Value;
                sb.Append($"Intel:{kv.Key}@{obs.ObservedTick}={obs.ObservedInventoryQty}|");
            }
        }

        foreach (var s in IndustrySites.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            // Include tech sustainment state so determinism drift cannot hide.
            sb.Append($"Site:{s.Key}|Eff:{s.Value.Efficiency.ToString("F4", CultureInfo.InvariantCulture)}|Health:{s.Value.HealthBps}|BufD:{s.Value.BufferDays}|Rem:{s.Value.DegradeRemainder}|");
        }

        // GATE.S4.INDU.MIN_LOOP.001
        // Include persisted construction state in signature so save%load%replay drift cannot hide.
        if (IndustryBuilds is not null && IndustryBuilds.Count > 0)
        {
            foreach (var kv in IndustryBuilds.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                var b = kv.Value;
                if (b is null) continue;
                sb.Append($"IB:{kv.Key}|A:{(b.Active ? 1 : 0)}|R:{b.RecipeId}|Si:{b.StageIndex}|Sn:{b.StageName}|Rem:{b.StageTicksRemaining}|Blk:{b.BlockerReason}|Act:{b.SuggestedAction}|");
            }
        }

        foreach (var n in Nodes.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            if (n.Value.Trace > 0.001f) sb.Append($"N_Tr:{n.Key}:{n.Value.Trace.ToString("F2", CultureInfo.InvariantCulture)}|");
        }
        foreach (var e in Edges.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            if (e.Value.Heat > 0.001f) sb.Append($"E_Ht:{e.Key}:{e.Value.Heat.ToString("F2", CultureInfo.InvariantCulture)}|");
        }

        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(bytes);
    }
}
