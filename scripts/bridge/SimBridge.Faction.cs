#nullable enable

using Godot;
using SimCore;
using SimCore.Systems;
using SimCore.Tweaks;
using System;

namespace SpaceTradeEmpire.Bridge;

public partial class SimBridge
{
    // ── GATE.S7.NARRATIVE_DELIVERY.FACTION_GREETING.001: Faction station greetings keyed to rep tier ──

    private static readonly System.Collections.Generic.Dictionary<(string faction, string tier), string> _factionGreetings
        = new()
    {
        // Concord — bureaucratic human trade federation
        { ("concord", "Hostile"), "Attention, pilot. Your docking privileges are under review. Do not linger." },
        { ("concord", "Neutral"), "Welcome to Concord space. Please observe all posted trade regulations." },
        { ("concord", "Friendly"), "Welcome back, captain. Concord facilities are at your disposal." },
        { ("concord", "Allied"), "Captain, it's good to see you. The Council extends its warmest regards." },

        // Chitin — insectoid collective
        { ("chitin", "Hostile"), "Outsider. Your presence here is... tolerated. Barely." },
        { ("chitin", "Neutral"), "The Swarm observes your arrival. Trade fairly, or depart." },
        { ("chitin", "Friendly"), "You carry the scent of cooperation. The Swarm welcomes you." },
        { ("chitin", "Allied"), "Brood-friend. The hive sings at your approach. All chambers are open to you." },

        // Weavers — networked energy beings
        { ("weavers", "Hostile"), "Signal detected. Threat assessment: elevated. State your purpose." },
        { ("weavers", "Neutral"), "Connection acknowledged. Standard protocols apply." },
        { ("weavers", "Friendly"), "Your frequency is recognized. The Weave extends bandwidth." },
        { ("weavers", "Allied"), "Honored partner. The Weave remembers your contributions." },

        // Valorin — honor-bound warrior species
        { ("valorin", "Hostile"), "You dare approach our walls? Speak quickly, or face the lance." },
        { ("valorin", "Neutral"), "Stranger. Honor demands we hear you, though trust is not yet earned." },
        { ("valorin", "Friendly"), "Well met, warrior. Your deeds have been noted in the Hall of Valor." },
        { ("valorin", "Allied"), "Shield-kin! The Valorin stand beside you. Our armories are yours." },

        // Communion — mystical psionic collective
        { ("communion", "Hostile"), "Your thoughts are... discordant. We permit your presence, nothing more." },
        { ("communion", "Neutral"), "We sense your arrival. The Communion offers equilibrium, if you seek it." },
        { ("communion", "Friendly"), "Your resonance is harmonious. The Communion opens its sanctum to you." },
        { ("communion", "Allied"), "Beloved pilgrim. Your light burns bright in the Communion's song." },
    };

    /// <summary>
    /// Returns a greeting string for a faction station based on the player's reputation tier.
    /// Pure lookup — reads rep tier from SimState if repTierOverride is empty, otherwise uses the override.
    /// </summary>
    public string GetFactionGreetingV0(string factionId, string repTierOverride = "")
    {
        string tier = repTierOverride;

        if (string.IsNullOrEmpty(tier))
        {
            // Read current rep tier from SimState.
            TryExecuteSafeRead(state =>
            {
                if (string.IsNullOrEmpty(factionId)) return;
                var repTier = ReputationSystem.GetRepTier(state, factionId);
                tier = repTier switch
                {
                    RepTier.Allied => "Allied",
                    RepTier.Friendly => "Friendly",
                    RepTier.Hostile or RepTier.Enemy => "Hostile",
                    _ => "Neutral",
                };
            }, 0);
        }

        if (string.IsNullOrEmpty(tier)) tier = "Neutral";

        if (_factionGreetings.TryGetValue((factionId ?? "", tier), out var greeting))
            return greeting;

        return "Docking protocols engaged.";
    }

    // ── GATE.S7.FACTION.BRIDGE_QUERIES.001: Faction doctrine, reputation, and territory access queries ──

    /// <summary>
    /// Returns faction doctrine info: trade_policy (string: Open/Guarded/Closed),
    /// tariff_rate (float 0-1), aggression (int 0-2).
    /// Nonblocking read — returns empty/default dict on lock failure.
    /// </summary>
    public Godot.Collections.Dictionary GetFactionDoctrineV0(string factionId)
    {
        var result = new Godot.Collections.Dictionary
        {
            ["faction_id"] = factionId ?? "",
            ["trade_policy"] = "Unknown",
            ["tariff_rate"] = 0.0f,
            ["aggression"] = 0,
            ["found"] = false,
        };

        TryExecuteSafeRead(state =>
        {
            if (string.IsNullOrEmpty(factionId)) return;

            if (state.FactionTariffRates.TryGetValue(factionId, out var rate))
            {
                result["tariff_rate"] = rate;
                result["found"] = true;
            }

            if (state.FactionTradePolicy.TryGetValue(factionId, out var policyInt))
            {
                result["trade_policy"] = policyInt switch
                {
                    0 => "Open",
                    1 => "Guarded",
                    2 => "Closed",
                    _ => "Unknown",
                };
            }

            if (state.FactionAggressionLevel.TryGetValue(factionId, out var aggro))
            {
                result["aggression"] = aggro;
            }
        }, 0);

        return result;
    }

    /// <summary>
    /// Returns the player's reputation standing with a faction.
    /// Returns {faction_id (string), reputation (int), label (string)}.
    /// Nonblocking read.
    /// </summary>
    public Godot.Collections.Dictionary GetPlayerReputationV0(string factionId)
    {
        var result = new Godot.Collections.Dictionary
        {
            ["faction_id"] = factionId ?? "",
            ["reputation"] = FactionTweaksV0.ReputationDefault,
            ["label"] = "Neutral",
        };

        TryExecuteSafeRead(state =>
        {
            if (string.IsNullOrEmpty(factionId)) return;

            int rep = ReputationSystem.GetReputation(state, factionId);
            result["reputation"] = rep;

            result["label"] = rep switch
            {
                >= 75 => "Allied",
                >= 25 => "Friendly",
                >= -25 => "Neutral",
                >= -75 => "Hostile",
                _ => "Enemy",
            };
        }, 0);

        return result;
    }

    /// <summary>
    /// Returns territory access info for a node: controlling faction, tariff bps, can_trade.
    /// Returns {node_id, faction_id, tariff_bps (int), can_trade (bool), trade_policy (string)}.
    /// Nonblocking read.
    /// </summary>
    public Godot.Collections.Dictionary GetTerritoryAccessV0(string nodeId)
    {
        var result = new Godot.Collections.Dictionary
        {
            ["node_id"] = nodeId ?? "",
            ["faction_id"] = "",
            ["tariff_bps"] = 0,
            ["can_trade"] = true,
            ["trade_policy"] = "Open",
            ["rep_tier"] = "Neutral",
            ["price_modifier_bps"] = 0,
        };

        TryExecuteSafeRead(state =>
        {
            if (string.IsNullOrEmpty(nodeId)) return;

            if (!state.NodeFactionId.TryGetValue(nodeId, out var factionId))
                return; // Unclaimed node.

            result["faction_id"] = factionId;

            // GATE.S7.REPUTATION.UI_INDICATORS.001: rep tier + pricing.
            var repTier = ReputationSystem.GetRepTier(state, factionId);
            result["rep_tier"] = repTier.ToString();
            result["price_modifier_bps"] = MarketSystem.GetRepPricingBps(state, factionId);

            // Find market at this node for tariff calculation.
            if (state.Nodes.TryGetValue(nodeId, out var node) && !string.IsNullOrEmpty(node.MarketId))
            {
                int tariffBps = MarketSystem.GetEffectiveTariffBps(state, node.MarketId);
                result["tariff_bps"] = tariffBps;
                result["can_trade"] = MarketSystem.CanTradeByReputation(state, node.MarketId);
            }

            if (state.FactionTradePolicy.TryGetValue(factionId, out var policyInt))
            {
                result["trade_policy"] = policyInt switch
                {
                    0 => "Open",
                    1 => "Guarded",
                    2 => "Closed",
                    _ => "Unknown",
                };
            }
        }, 0);

        return result;
    }

    // GATE.S7.TERRITORY.BRIDGE_DISPLAY.001: Territory regime query for galaxy view.
    // Returns {node_id, regime (string), regime_color (Color)}.
    public Godot.Collections.Dictionary GetTerritoryRegimeV0(string nodeId)
    {
        var result = new Godot.Collections.Dictionary
        {
            ["node_id"] = nodeId ?? "",
            ["regime"] = "Open",
            ["regime_color"] = new Color(0.4f, 1.0f, 0.4f), // Green for Open
        };

        TryExecuteSafeRead(state =>
        {
            if (string.IsNullOrEmpty(nodeId)) return;
            var regime = ReputationSystem.ComputeTerritoryRegime(state, nodeId);
            result["regime"] = regime.ToString();
            result["regime_color"] = regime switch
            {
                TerritoryRegime.Open => new Color(0.4f, 1.0f, 0.4f),       // Green
                TerritoryRegime.Guarded => new Color(1.0f, 1.0f, 0.4f),    // Yellow
                TerritoryRegime.Restricted => new Color(1.0f, 0.6f, 0.2f), // Orange
                TerritoryRegime.Hostile => new Color(1.0f, 0.2f, 0.2f),    // Red
                _ => new Color(0.8f, 0.8f, 0.8f),
            };
        }, 0);

        return result;
    }

    // ── GATE.S7.TERRITORY.EMBARGO_BRIDGE.001: Embargo status queries ──

    /// <summary>
    /// Returns embargoes affecting a market: array of {good_id, faction_id, reason}.
    /// Nonblocking read.
    /// </summary>
    public Godot.Collections.Array GetEmbargoesV0(string marketId)
    {
        var result = new Godot.Collections.Array();

        TryExecuteSafeRead(state =>
        {
            if (string.IsNullOrEmpty(marketId)) return;

            // Find node for this market to get its controlling faction.
            string? nodeId = null;
            foreach (var kv in state.Nodes)
            {
                if (StringComparer.Ordinal.Equals(kv.Value.MarketId, marketId))
                {
                    nodeId = kv.Key;
                    break;
                }
            }
            if (nodeId == null) return;
            if (!state.NodeFactionId.TryGetValue(nodeId, out var nodeFactionId)) return;

            // Check each embargo for relevance to this market's controlling faction.
            if (state.Embargoes == null) return;
            foreach (var embargo in state.Embargoes)
            {
                if (StringComparer.Ordinal.Equals(embargo.EnforcingFactionId, nodeFactionId))
                {
                    result.Add(new Godot.Collections.Dictionary
                    {
                        ["good_id"] = embargo.GoodId,
                        ["faction_id"] = embargo.EnforcingFactionId,
                        ["reason"] = $"Embargo by {embargo.EnforcingFactionId} (war with {embargo.TargetFactionId})",
                    });
                }
            }
        }, 0);

        return result;
    }

    /// <summary>
    /// Returns whether a specific good is embargoed at a node's market.
    /// Accepts nodeId (looks up market internally). Nonblocking read.
    /// </summary>
    public bool IsGoodEmbargoedV0(string nodeId, string goodId)
    {
        bool embargoed = false;

        TryExecuteSafeRead(state =>
        {
            if (string.IsNullOrEmpty(nodeId) || string.IsNullOrEmpty(goodId)) return;
            if (!state.Nodes.TryGetValue(nodeId, out var node)) return;
            if (string.IsNullOrEmpty(node.MarketId)) return;
            embargoed = MarketSystem.IsGoodEmbargoed(state, node.MarketId, goodId);
        }, 0);

        return embargoed;
    }

    // ── GATE.S7.INSTABILITY.BRIDGE.001: Node instability queries ──

    /// <summary>
    /// Returns instability info for a node: level (0-100+), phase name, phase index (0-4).
    /// Returns {node_id, level (int), phase (string), phase_index (int)}.
    /// Nonblocking read.
    /// </summary>
    public Godot.Collections.Dictionary GetNodeInstabilityV0(string nodeId)
    {
        var result = new Godot.Collections.Dictionary
        {
            ["node_id"] = nodeId ?? "",
            ["level"] = 0,
            ["phase"] = "Stable",
            ["phase_index"] = 0,
        };

        TryExecuteSafeRead(state =>
        {
            if (string.IsNullOrEmpty(nodeId)) return;
            if (!state.Nodes.TryGetValue(nodeId, out var node)) return;

            int level = node.InstabilityLevel;
            result["level"] = level;
            result["phase"] = SimCore.Tweaks.InstabilityTweaksV0.GetPhaseName(level);
            result["phase_index"] = SimCore.Tweaks.InstabilityTweaksV0.GetPhaseIndex(level);
        }, 0);

        return result;
    }

    // ── GATE.S7.INSTABILITY.EFFECTS_BRIDGE.001: Instability phase effects query ──

    /// <summary>
    /// Returns instability effects for a node: phase, price_jitter_pct, lane_delay_pct,
    /// trade_failure_pct, market_closed.
    /// Nonblocking read.
    /// </summary>
    public Godot.Collections.Dictionary GetInstabilityEffectsV0(string nodeId)
    {
        var result = new Godot.Collections.Dictionary
        {
            ["node_id"] = nodeId ?? "",
            ["phase"] = "Stable",
            ["phase_index"] = 0,
            ["price_jitter_pct"] = 0,
            ["lane_delay_pct"] = 0,
            ["trade_failure_pct"] = 0,
            ["market_closed"] = false,
        };

        TryExecuteSafeRead(state =>
        {
            if (string.IsNullOrEmpty(nodeId)) return;
            if (!state.Nodes.TryGetValue(nodeId, out var node)) return;

            int level = node.InstabilityLevel;
            int phaseIndex = SimCore.Tweaks.InstabilityTweaksV0.GetPhaseIndex(level);
            result["phase"] = SimCore.Tweaks.InstabilityTweaksV0.GetPhaseName(level);
            result["phase_index"] = phaseIndex;

            // Phase-based effects from tweaks
            result["price_jitter_pct"] = phaseIndex >= 1 ? SimCore.Tweaks.InstabilityTweaksV0.ShimmerPriceJitterPct : 0;
            result["lane_delay_pct"] = phaseIndex >= 2 ? SimCore.Tweaks.InstabilityTweaksV0.DriftLaneDelayPct : 0;
            result["trade_failure_pct"] = phaseIndex >= 3 ? SimCore.Tweaks.InstabilityTweaksV0.FractureTradeFailurePct : 0;
            result["market_closed"] = phaseIndex >= 4;
        }, 0);

        return result;
    }

    // ── GATE.S7.FACTION.IDENTITY_PANEL.001: Faction detail query ──

    /// <summary>
    /// Returns detailed faction identity: species, philosophy, produces, needs, relations.
    /// Returns {faction_id, species, philosophy, produces (Array), needs (Array),
    ///          relations (Dictionary: factionId → int), aggression, tariff_rate, trade_policy,
    ///          territory_count, reputation, rep_label}.
    /// Nonblocking read.
    /// </summary>
    public Godot.Collections.Dictionary GetFactionDetailV0(string factionId)
    {
        var result = new Godot.Collections.Dictionary
        {
            ["faction_id"] = factionId ?? "",
            ["species"] = "",
            ["philosophy"] = "",
            ["produces"] = new Godot.Collections.Array(),
            ["needs"] = new Godot.Collections.Array(),
            ["relations"] = new Godot.Collections.Dictionary(),
            ["aggression"] = 0,
            ["tariff_rate"] = 0.0f,
            ["trade_policy"] = "Unknown",
            ["territory_count"] = 0,
            ["reputation"] = 0,
            ["rep_label"] = "Neutral",
            ["found"] = false,
        };

        // Resolve faction identity from FactionTweaksV0 constants (static, no SimState needed).
        var ft = FactionTweaksV0.AllFactionIds;
        bool known = System.Array.IndexOf(ft, factionId) >= 0;
        if (known)
        {
            result["found"] = true;

            // Species + philosophy from tweaks constants.
            result["species"] = factionId switch
            {
                FactionTweaksV0.ConcordId    => FactionTweaksV0.ConcordSpecies,
                FactionTweaksV0.ChitinId     => FactionTweaksV0.ChitinSpecies,
                FactionTweaksV0.WeaversId    => FactionTweaksV0.WeaversSpecies,
                FactionTweaksV0.ValorinId    => FactionTweaksV0.ValorinSpecies,
                FactionTweaksV0.CommunionId  => FactionTweaksV0.CommunionSpecies,
                _ => "",
            };
            result["philosophy"] = factionId switch
            {
                FactionTweaksV0.ConcordId    => FactionTweaksV0.ConcordPhilosophy,
                FactionTweaksV0.ChitinId     => FactionTweaksV0.ChitinPhilosophy,
                FactionTweaksV0.WeaversId    => FactionTweaksV0.WeaversPhilosophy,
                FactionTweaksV0.ValorinId    => FactionTweaksV0.ValorinPhilosophy,
                FactionTweaksV0.CommunionId  => FactionTweaksV0.CommunionPhilosophy,
                _ => "",
            };

            // Produces = goods this faction supplies in the pentagon ring + cross-links.
            var produces = new Godot.Collections.Array();
            foreach (var entry in FactionTweaksV0.PentagonRing)
                if (StringComparer.Ordinal.Equals(entry.Supplier, factionId)) produces.Add(entry.Good);
            foreach (var entry in FactionTweaksV0.SecondaryCrossLinks)
                if (StringComparer.Ordinal.Equals(entry.Supplier, factionId)) produces.Add(entry.Good);
            result["produces"] = produces;

            // Needs = goods this faction consumes in the pentagon ring + cross-links.
            var needs = new Godot.Collections.Array();
            foreach (var entry in FactionTweaksV0.PentagonRing)
                if (StringComparer.Ordinal.Equals(entry.Consumer, factionId)) needs.Add(entry.Good);
            foreach (var entry in FactionTweaksV0.SecondaryCrossLinks)
                if (StringComparer.Ordinal.Equals(entry.Consumer, factionId)) needs.Add(entry.Good);
            result["needs"] = needs;
        }

        TryExecuteSafeRead(state =>
        {
            if (string.IsNullOrEmpty(factionId)) return;
            if (!state.FactionTariffRates.ContainsKey(factionId)) return;

            result["found"] = true;
            if (state.FactionTariffRates.TryGetValue(factionId, out var rate))
                result["tariff_rate"] = rate;
            if (state.FactionTradePolicy.TryGetValue(factionId, out var policy))
                result["trade_policy"] = policy switch { 0 => "Open", 1 => "Guarded", 2 => "Closed", _ => "Unknown" };
            if (state.FactionAggressionLevel.TryGetValue(factionId, out var aggro))
                result["aggression"] = aggro;

            // Relations from warfront state.
            var relations = new Godot.Collections.Dictionary();
            if (state.Warfronts != null)
            {
                foreach (var wf in state.Warfronts.Values)
                {
                    if (StringComparer.Ordinal.Equals(wf.CombatantA, factionId))
                        relations[wf.CombatantB] = -1;
                    else if (StringComparer.Ordinal.Equals(wf.CombatantB, factionId))
                        relations[wf.CombatantA] = -1;
                }
            }
            result["relations"] = relations;

            // Territory count.
            int territoryCount = 0;
            foreach (var kv in state.NodeFactionId)
            {
                if (StringComparer.Ordinal.Equals(kv.Value, factionId))
                    territoryCount++;
            }
            result["territory_count"] = territoryCount;

            // Reputation.
            int rep = ReputationSystem.GetReputation(state, factionId);
            result["reputation"] = rep;
            result["rep_label"] = rep switch
            {
                >= 75 => "Allied",
                >= 25 => "Friendly",
                >= -25 => "Neutral",
                >= -75 => "Hostile",
                _ => "Enemy",
            };
        }, 0);

        return result;
    }

    /// <summary>
    /// Returns all faction IDs with their reputation and trade policy as an array of dicts.
    /// Each: {faction_id, reputation, trade_policy, tariff_rate, territory_count}.
    /// Nonblocking read.
    /// </summary>
    public Godot.Collections.Array GetAllFactionsV0()
    {
        var result = new Godot.Collections.Array();

        TryExecuteSafeRead(state =>
        {
            // Deterministic: iterate by sorted faction id.
            var factionIds = new System.Collections.Generic.List<string>(state.FactionTariffRates.Keys);
            factionIds.Sort(StringComparer.Ordinal);

            foreach (var fid in factionIds)
            {
                int rep = ReputationSystem.GetReputation(state, fid);
                float tariffRate = state.FactionTariffRates.TryGetValue(fid, out var r) ? r : 0f;
                string policy = "Open";
                if (state.FactionTradePolicy.TryGetValue(fid, out var p))
                {
                    policy = p switch { 0 => "Open", 1 => "Guarded", 2 => "Closed", _ => "Unknown" };
                }

                // Count territory nodes.
                int territoryCount = 0;
                foreach (var kv in state.NodeFactionId)
                {
                    if (StringComparer.Ordinal.Equals(kv.Value, fid))
                        territoryCount++;
                }

                result.Add(new Godot.Collections.Dictionary
                {
                    ["faction_id"] = fid,
                    ["reputation"] = rep,
                    ["trade_policy"] = policy,
                    ["tariff_rate"] = tariffRate,
                    ["territory_count"] = territoryCount,
                });
            }
        }, 0);

        return result;
    }

    // ── GATE.S7.FACTION_VIS.COLOR_PALETTE.001: Faction color palette query ──

    /// <summary>
    /// Returns faction visual colors: primary, secondary, accent as Godot Color objects.
    /// Returns {faction_id, primary (Color), secondary (Color), accent (Color), found (bool)}.
    /// Pure lookup — no SimState needed, reads from FactionTweaksV0 constants.
    /// </summary>
    public Godot.Collections.Dictionary GetFactionColorsV0(string factionId)
    {
        bool known = System.Array.IndexOf(FactionTweaksV0.AllFactionIds, factionId) >= 0;
        var colors = FactionTweaksV0.GetFactionColors(factionId ?? "");

        return new Godot.Collections.Dictionary
        {
            ["faction_id"] = factionId ?? "",
            ["primary"] = new Color(colors.Primary.R, colors.Primary.G, colors.Primary.B),
            ["secondary"] = new Color(colors.Secondary.R, colors.Secondary.G, colors.Secondary.B),
            ["accent"] = new Color(colors.Accent.R, colors.Accent.G, colors.Accent.B),
            ["found"] = known,
        };
    }

    // ── GATE.S7.FACTION_VIS.TERRITORY_OVERLAY.001: Node-to-faction territory mapping ──

    /// <summary>
    /// Returns an array of {node_id, faction_id} for all claimed nodes.
    /// Used by galaxy map territory overlay.
    /// </summary>
    public Godot.Collections.Array GetNodeFactionMapV0()
    {
        var result = new Godot.Collections.Array();

        TryExecuteSafeRead(state =>
        {
            foreach (var kv in state.NodeFactionId)
            {
                result.Add(new Godot.Collections.Dictionary
                {
                    ["node_id"] = kv.Key,
                    ["faction_id"] = kv.Value,
                });
            }
        }, 0);

        return result;
    }

    // GATE.S7.FACTION_COMMISSION.BRIDGE.001: Active commission query.
    // Returns {has_commission, faction_id, start_tick, stipend_credits, elapsed_ticks}.
    public Godot.Collections.Dictionary GetActiveCommissionV0()
    {
        var result = new Godot.Collections.Dictionary
        {
            ["has_commission"] = false,
            ["faction_id"] = "",
            ["start_tick"] = 0,
            ["stipend_credits"] = 0,
            ["elapsed_ticks"] = 0,
        };

        TryExecuteSafeRead(state =>
        {
            if (state.ActiveCommission == null) return;
            var c = state.ActiveCommission;
            result["has_commission"] = true;
            result["faction_id"] = c.FactionId ?? "";
            result["start_tick"] = c.StartTick;
            result["stipend_credits"] = c.StipendCreditsPerCycle;
            result["elapsed_ticks"] = state.Tick - c.StartTick;
        }, 0);

        return result;
    }

    // GATE.S7.FACTION_COMMISSION.BRIDGE.001: Rep modifier stack — named breakdown of modifiers.
    // Returns Array of {source, delta, description} for a faction's reputation.
    public Godot.Collections.Array GetRepModifierStackV0(string factionId)
    {
        var result = new Godot.Collections.Array();
        if (string.IsNullOrEmpty(factionId)) return result;

        TryExecuteSafeRead(state =>
        {
            int rep = ReputationSystem.GetReputation(state, factionId);
            int infamy = state.InfamyByFaction.TryGetValue(factionId, out var inf) ? inf : 0;
            int maxRep = ReputationSystem.GetMaxRepForInfamy(state, factionId);

            result.Add(new Godot.Collections.Dictionary
            {
                ["source"] = "Standing",
                ["value"] = rep,
                ["description"] = $"Current reputation: {rep}",
            });

            if (infamy > 0)
            {
                result.Add(new Godot.Collections.Dictionary
                {
                    ["source"] = "Infamy",
                    ["value"] = infamy,
                    ["description"] = $"Infamy {infamy} — max rep capped at {maxRep}",
                });
            }

            // Commission bonus
            if (state.ActiveCommission != null
                && string.Equals(state.ActiveCommission.FactionId, factionId, StringComparison.Ordinal))
            {
                result.Add(new Godot.Collections.Dictionary
                {
                    ["source"] = "Commission",
                    ["value"] = SimCore.Tweaks.CommissionTweaksV0.EmployerRepGainPerCycle,
                    ["description"] = $"+{SimCore.Tweaks.CommissionTweaksV0.EmployerRepGainPerCycle}/cycle from commission",
                });
            }

            // Rep tier label
            var tier = ReputationSystem.GetRepTier(rep);
            result.Add(new Godot.Collections.Dictionary
            {
                ["source"] = "Tier",
                ["value"] = (int)tier,
                ["description"] = $"Tier: {tier}",
            });
        }, 0);

        return result;
    }
}
