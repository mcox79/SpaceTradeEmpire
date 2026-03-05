#nullable enable

using Godot;
using SimCore;
using SimCore.Systems;
using System;
using System.Collections.Generic;

namespace SpaceTradeEmpire.Bridge;

// GATE.X.PRESSURE.BRIDGE.001: SimBridge pressure queries.
public partial class SimBridge
{
    /// <summary>
    /// Returns all pressure domains with current state.
    /// [{domain_id, tier, tier_name, direction, accumulated_bps, alert_count, is_crisis}]
    /// </summary>
    public Godot.Collections.Array GetPressureDomainsV0()
    {
        var result = new Godot.Collections.Array();

        TryExecuteSafeRead(state =>
        {
            if (state.Pressure?.Domains == null) return;

            var keys = new List<string>(state.Pressure.Domains.Keys);
            keys.Sort(StringComparer.Ordinal);

            foreach (var key in keys)
            {
                var domain = state.Pressure.Domains[key];
                result.Add(new Godot.Collections.Dictionary
                {
                    ["domain_id"] = domain.DomainId,
                    ["tier"] = (int)domain.Tier,
                    ["tier_name"] = domain.Tier.ToString(),
                    ["direction"] = (int)domain.Direction,
                    ["direction_name"] = domain.Direction.ToString(),
                    ["accumulated_bps"] = domain.AccumulatedPressureBps,
                    ["alert_count"] = domain.AlertCount,
                    ["is_crisis"] = PressureSystem.IsCrisis(domain),
                });
            }
        }, 0);

        return result;
    }

    /// <summary>
    /// Returns alert count for a specific domain. -1 if domain not found.
    /// </summary>
    public int GetPressureAlertCountV0(string domainId)
    {
        int count = -1;
        if (string.IsNullOrEmpty(domainId)) return count;

        TryExecuteSafeRead(state =>
        {
            if (state.Pressure?.Domains != null &&
                state.Pressure.Domains.TryGetValue(domainId, out var domain))
            {
                count = domain.AlertCount;
            }
        });

        return count;
    }

    /// <summary>
    /// Returns domain forecast: {domain_id, tier, direction, accumulated_bps, max_alerts, alerts_used, budget_remaining}
    /// </summary>
    public Godot.Collections.Dictionary GetDomainForecastV0(string domainId)
    {
        var result = new Godot.Collections.Dictionary();
        if (string.IsNullOrEmpty(domainId)) return result;

        TryExecuteSafeRead(state =>
        {
            if (state.Pressure?.Domains == null) return;
            if (!state.Pressure.Domains.TryGetValue(domainId, out var domain)) return;

            int maxAlerts = PressureSystem.GetMaxAlerts(domain);
            result["domain_id"] = domain.DomainId;
            result["tier"] = (int)domain.Tier;
            result["tier_name"] = domain.Tier.ToString();
            result["direction"] = (int)domain.Direction;
            result["direction_name"] = domain.Direction.ToString();
            result["accumulated_bps"] = domain.AccumulatedPressureBps;
            result["max_alerts"] = maxAlerts;
            result["alerts_used"] = domain.AlertCount;
            result["budget_remaining"] = maxAlerts - domain.AlertCount;
        });

        return result;
    }
}
