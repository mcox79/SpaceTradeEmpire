#nullable enable

using Godot;
using SimCore;
using SimCore.Gen;
using SimCore.Commands;
using SimCore.Intents;
using SimCore.Systems;
using SimCore.Programs;
using SimCore.Events;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

namespace SpaceTradeEmpire.Bridge;

public partial class SimBridge
{
    // Cached profit summary for lock-contention resilience.
    private Godot.Collections.Dictionary? _cachedProfitSummaryV0;

    // --- Intents: UI.002 contract (buy/sell generates intents) ---

    public void SubmitBuyIntent(string marketId, string goodId, int quantity)
    {
        if (IsLoading) return;
        if (string.IsNullOrWhiteSpace(marketId)) return;
        if (string.IsNullOrWhiteSpace(goodId)) return;
        if (quantity <= 0) return;

        EnqueueIntent(new BuyIntent(marketId, goodId, quantity));
    }

    public void SubmitSellIntent(string marketId, string goodId, int quantity)
    {
        if (IsLoading) return;
        if (string.IsNullOrWhiteSpace(marketId)) return;
        if (string.IsNullOrWhiteSpace(goodId)) return;
        if (quantity <= 0) return;

        EnqueueIntent(new SellIntent(marketId, goodId, quantity));
    }

    // Legacy GDScript API used by ActiveStation.gd (non-blocking intent submit with pre-checks)
    public bool TryBuyCargo(string marketId, string goodId, int quantity)
    {
        if (IsLoading) return false;
        if (string.IsNullOrWhiteSpace(marketId)) return false;
        if (string.IsNullOrWhiteSpace(goodId)) return false;
        if (quantity <= 0) return false;

        _stateLock.EnterReadLock();
        try
        {
            var state = _kernel.State;
            if (!state.Markets.TryGetValue(marketId, out var market)) return false;

            var price = market.GetPrice(goodId);
            if (price <= 0) return false;

            var total = (long)price * (long)quantity;
            if (state.PlayerCredits < total) return false;

            var supply = market.Inventory.TryGetValue(goodId, out var v) ? v : 0;
            if (supply < quantity) return false;
        }
        finally
        {
            _stateLock.ExitReadLock();
        }

        SubmitBuyIntent(marketId, goodId, quantity);
        return true;
    }

    public bool TrySellCargo(string marketId, string goodId, int quantity)
    {
        if (IsLoading) return false;
        if (string.IsNullOrWhiteSpace(marketId)) return false;
        if (string.IsNullOrWhiteSpace(goodId)) return false;
        if (quantity <= 0) return false;

        _stateLock.EnterReadLock();
        try
        {
            var state = _kernel.State;

            var have = state.PlayerCargo.TryGetValue(goodId, out var v) ? v : 0;
            if (have < quantity) return false;

            if (!state.Markets.ContainsKey(marketId)) return false;
        }
        finally
        {
            _stateLock.ExitReadLock();
        }

        SubmitSellIntent(marketId, goodId, quantity);
        return true;
    }

    // --- Read APIs for UI.001 (inventory, price, intel age) ---

    public int GetMarketPrice(string marketId, string goodId)
    {
        if (IsLoading) return 0;
        if (string.IsNullOrWhiteSpace(marketId)) return 0;
        if (string.IsNullOrWhiteSpace(goodId)) return 0;

        _stateLock.EnterReadLock();
        try
        {
            var state = _kernel.State;
            if (!state.Markets.TryGetValue(marketId, out var market)) return 0;
            return market.GetPrice(goodId);
        }
        finally
        {
            _stateLock.ExitReadLock();
        }
    }

    public int GetIntelAgeTicks(string marketId, string goodId)
    {
        if (IsLoading) return -1;
        if (string.IsNullOrWhiteSpace(marketId)) return -1;
        if (string.IsNullOrWhiteSpace(goodId)) return -1;

        // Defensive: if caller is already inside a safe-read (or write) lock on this thread,
        // do NOT attempt to re-enter the lock (LockRecursionPolicy.NoRecursion).
        if (_stateLock.IsReadLockHeld || _stateLock.IsWriteLockHeld)
        {
            return GetIntelAgeTicks_NoLock(_kernel.State, marketId, goodId);
        }

        var age = -1;
        TryExecuteSafeRead(state =>
        {
            age = GetIntelAgeTicks_NoLock(state, marketId, goodId);
        });
        return age;
    }

    // IMPORTANT: caller must already be inside TryExecuteSafeRead.
    // This method must not acquire _stateLock or call other locking bridge methods.
    public int GetIntelAgeTicks_NoLock(SimCore.SimState state, string marketId, string goodId)
    {
        var view = IntelSystem.GetMarketGoodView(state, marketId, goodId);
        return view.AgeTicks;
    }

    public Godot.Collections.Array GetSustainmentSnapshot(string marketId)
    {
        if (IsLoading) return new Godot.Collections.Array();
        if (string.IsNullOrWhiteSpace(marketId)) return new Godot.Collections.Array();

        // Defensive: if caller is already inside a safe-read (or write) lock on this thread,
        // do NOT attempt to re-enter the lock (LockRecursionPolicy.NoRecursion).
        if (_stateLock.IsReadLockHeld || _stateLock.IsWriteLockHeld)
        {
            return GetSustainmentSnapshot_NoLock(_kernel.State, marketId);
        }

        _stateLock.EnterReadLock();
        try
        {
            return GetSustainmentSnapshot_NoLock(_kernel.State, marketId);
        }
        finally
        {
            _stateLock.ExitReadLock();
        }
    }

    // IMPORTANT: caller must already be inside TryExecuteSafeRead.
    // Do NOT acquire _stateLock or call other locking bridge methods.
    public Godot.Collections.Array GetSustainmentSnapshot_NoLock(SimCore.SimState state, string marketId)
    {
        var arr = new Godot.Collections.Array();
        if (state == null) return arr;
        if (string.IsNullOrWhiteSpace(marketId)) return arr;

        var sites = SustainmentSnapshot.BuildForNode(state, marketId);

        foreach (var s in sites)
        {
            var d = new Godot.Collections.Dictionary
            {
                ["site_id"] = s.SiteId,
                ["node_id"] = s.NodeId,
                ["health_bps"] = s.HealthBps,
                ["eff_bps_now"] = s.EffBpsNow,
                ["degrade_per_day_bps"] = s.DegradePerDayBps,
                ["worst_buffer_margin"] = s.WorstBufferMargin,

                ["time_to_starve_ticks"] = s.TimeToStarveTicks,
                ["time_to_starve_days"] = s.TimeToStarveDays,
                ["time_to_failure_ticks"] = s.TimeToFailureTicks,
                ["time_to_failure_days"] = s.TimeToFailureDays,

                ["starve_band"] = s.StarveBand,
                ["fail_band"] = s.FailBand,
            };

            // GATE.S4.INDU.MIN_LOOP.001: attach minimal construction readout (if present).
            // GATE.S4.UI_INDU.001: also emit deterministic why-blocked chain and next-actions (Facts-only).
            if (state.IndustryBuilds != null &&
                !string.IsNullOrWhiteSpace(s.SiteId) &&
                state.IndustryBuilds.TryGetValue(s.SiteId, out var b) &&
                b != null)
            {
                d["build_active"] = b.Active;
                d["build_recipe_id"] = b.RecipeId ?? "";
                d["build_stage_index"] = b.StageIndex;
                d["build_stage_name"] = b.StageName ?? "";
                d["build_ticks_remaining"] = b.StageTicksRemaining;
                d["build_blocker"] = b.BlockerReason ?? "";
                d["build_suggested_action"] = b.SuggestedAction ?? "";

                // Deterministic: arrays emitted in a single stable order (at most one entry each in v0).
                var whyChain = new Godot.Collections.Array();
                var nextActions = new Godot.Collections.Array();

                var stageName = (b.StageName ?? "");
                var ticksRemaining = b.StageTicksRemaining;
                var blocker = (b.BlockerReason ?? "");
                var suggested = (b.SuggestedAction ?? "");

                // Build why-blocked chain from the existing deterministic blocker format.
                // Current v0 blocker format (IndustrySystem): "missing_input good=... need=... have=..."
                if (!string.IsNullOrWhiteSpace(blocker))
                {
                    var token = "";
                    var rest = blocker;

                    var sp = blocker.IndexOf(' ');
                    if (sp > 0)
                    {
                        token = blocker.Substring(0, sp);
                        rest = blocker.Substring(sp + 1);
                    }
                    else
                    {
                        token = blocker;
                        rest = "";
                    }

                    string goodId = "";
                    int need = 0;
                    int have = 0;

                    if (!string.IsNullOrWhiteSpace(rest))
                    {
                        var parts = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var p in parts)
                        {
                            var eq = p.IndexOf('=');
                            if (eq <= 0) continue;

                            var k = p.Substring(0, eq);
                            var v = p.Substring(eq + 1);

                            if (string.Equals(k, "good", StringComparison.Ordinal)) goodId = v;
                            else if (string.Equals(k, "need", StringComparison.Ordinal) && int.TryParse(v, out var n)) need = n;
                            else if (string.Equals(k, "have", StringComparison.Ordinal) && int.TryParse(v, out var h)) have = h;
                        }
                    }

                    var wd = new Godot.Collections.Dictionary
                    {
                        ["token"] = token,
                        ["stage_name"] = stageName,
                        ["good_id"] = goodId,
                        ["need_units"] = need,
                        ["have_units"] = have,
                    };
                    whyChain.Add(wd);

                    // Next action derived deterministically from the blocker payload.
                    if (string.Equals(token, "missing_input", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(goodId))
                    {
                        var missing = need - have;
                        if (missing < 0) missing = 0;

                        var ad = new Godot.Collections.Dictionary
                        {
                            ["token"] = "acquire_input",
                            ["stage_name"] = stageName,
                            ["good_id"] = goodId,
                            ["qty_units"] = missing,
                        };
                        nextActions.Add(ad);
                    }
                }
                else
                {
                    // Not blocked: provide a deterministic next-action hint for UI.
                    if (ticksRemaining > 0)
                    {
                        nextActions.Add(new Godot.Collections.Dictionary
                        {
                            ["token"] = "wait",
                            ["stage_name"] = stageName,
                            ["ticks_remaining"] = ticksRemaining,
                        });
                    }
                    else if (!string.IsNullOrWhiteSpace(stageName))
                    {
                        nextActions.Add(new Godot.Collections.Dictionary
                        {
                            ["token"] = "start_stage",
                            ["stage_name"] = stageName,
                        });
                    }
                }

                // If IndustrySystem provided a suggested action string, surface it as an informational hint
                // without parsing assumptions beyond stable passthrough.
                if (!string.IsNullOrWhiteSpace(suggested))
                {
                    nextActions.Add(new Godot.Collections.Dictionary
                    {
                        ["token"] = "hint",
                        ["text"] = suggested,
                    });
                }

                d["why_blocked_chain"] = whyChain;
                d["next_actions"] = nextActions;
            }
            else
            {
                d["build_active"] = false;
                d["build_recipe_id"] = "";
                d["build_stage_index"] = 0;
                d["build_stage_name"] = "";
                d["build_ticks_remaining"] = 0;
                d["build_blocker"] = "";
                d["build_suggested_action"] = "";

                d["why_blocked_chain"] = new Godot.Collections.Array();
                d["next_actions"] = new Godot.Collections.Array();
            }

            var inputsArr = new Godot.Collections.Array();
            foreach (var i in s.Inputs)
            {
                var id = new Godot.Collections.Dictionary
                {
                    ["good_id"] = i.GoodId,

                    ["have_units"] = i.HaveUnits,
                    ["per_tick_required"] = i.PerTickRequired,
                    ["buffer_target_units"] = i.BufferTargetUnits,

                    ["coverage_ticks"] = i.CoverageTicks,
                    ["coverage_days"] = i.CoverageDays,
                    ["coverage_band"] = i.CoverageBand,
                    ["buffer_margin"] = i.BufferMargin,
                };
                inputsArr.Add(id);
            }

            d["inputs"] = inputsArr;
            arr.Add(d);
        }

        return arr;
    }

    // GATE.S18.TRADE_GOODS.BRIDGE_MARKET.001: Market snapshot with display names and effective prices.
    public Godot.Collections.Array GetMarketGoodsSnapshotV1(string marketId)
    {
        var result = new Godot.Collections.Array();
        if (IsLoading) return result;
        if (string.IsNullOrWhiteSpace(marketId)) return result;

        _stateLock.EnterReadLock();
        try
        {
            var state = _kernel.State;
            if (!state.Markets.TryGetValue(marketId, out var market)) return result;

            var registry = SimCore.Content.ContentRegistryLoader.LoadFromJsonOrThrow(SimCore.Content.ContentRegistryLoader.DefaultRegistryJsonV0);

            // Collect all goods: market inventory + player cargo
            var allGoods = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            foreach (var k in market.Inventory.Keys) allGoods.Add(k);
            foreach (var k in state.PlayerCargo.Keys) allGoods.Add(k);

            var sorted = new System.Collections.Generic.List<string>(allGoods);
            sorted.Sort(StringComparer.Ordinal);

            foreach (var goodId in sorted)
            {
                int qty = market.Inventory.TryGetValue(goodId, out var v) ? v : 0;
                int playerQty = state.PlayerCargo.TryGetValue(goodId, out var pv) ? pv : 0;
                int effectivePrice = SimCore.Systems.MarketSystem.GetEffectivePrice(goodId, qty, registry);
                int publishedPrice = market.GetPrice(goodId);

                var row = new Godot.Collections.Dictionary
                {
                    ["good_id"] = goodId,
                    ["display_name"] = FormatDisplayNameV0(goodId),
                    ["market_qty"] = qty,
                    ["player_qty"] = playerQty,
                    ["price"] = publishedPrice > 0 ? publishedPrice : effectivePrice,
                    ["effective_price"] = effectivePrice,
                };
                result.Add(row);
            }
        }
        finally
        {
            _stateLock.ExitReadLock();
        }
        return result;
    }

    // GATE.X.MARKET_PRICING.BREAKDOWN_BRIDGE.001: Price breakdown query.
    // Returns line-item breakdown: base, scarcity, rep_mod, tariff, instability, fee, total.
    public Godot.Collections.Dictionary GetPriceBreakdownV0(string nodeOrMarketId, string goodId, bool isBuy)
    {
        var result = new Godot.Collections.Dictionary
        {
            ["base"] = 0,
            ["scarcity"] = 0,
            ["rep_mod"] = 0,
            ["tariff"] = 0,
            ["instability"] = 0,
            ["fee"] = 0,
            ["total"] = 0,
        };
        if (string.IsNullOrWhiteSpace(nodeOrMarketId) || string.IsNullOrWhiteSpace(goodId))
            return result;

        TryExecuteSafeRead(state =>
        {
            var marketId = ResolveMarketIdFromNodeOrMarket(state, nodeOrMarketId);
            if (string.IsNullOrWhiteSpace(marketId)) return;
            if (!state.Markets.TryGetValue(marketId, out var market)) return;

            // Base price from market
            int basePrice = isBuy ? market.GetBuyPrice(goodId) : market.GetSellPrice(goodId);
            result["base"] = basePrice;

            // Scarcity component (effective price - base reference)
            var registry = SimCore.Content.ContentRegistryLoader.LoadFromJsonOrThrow(
                SimCore.Content.ContentRegistryLoader.DefaultRegistryJsonV0);
            int qty = market.Inventory.TryGetValue(goodId, out var v) ? v : 0;
            int effectivePrice = MarketSystem.GetEffectivePrice(goodId, qty, registry);
            int scarcity = effectivePrice - basePrice;
            result["scarcity"] = scarcity;

            // Rep pricing modifier
            int repMod = 0;
            string controlFaction = MarketSystem.GetControllingFactionIdForMarket(state, marketId);
            if (!string.IsNullOrEmpty(controlFaction))
            {
                int repBps = MarketSystem.GetRepPricingBps(state, controlFaction);
                repMod = MarketSystem.ApplyRepPricing(basePrice, repBps) - basePrice;
            }
            result["rep_mod"] = repMod;

            // Tariff
            int tariffBps = MarketSystem.GetEffectiveTariffBps(state, marketId);
            int tariff = (int)((long)basePrice * tariffBps / 10000L);
            result["tariff"] = tariff;

            // Instability multiplier
            int instMultBps = MarketSystem.GetInstabilityPriceMultiplierBps(state, marketId, goodId);
            int instMod = (instMultBps != 10000) ? (int)((long)basePrice * (instMultBps - 10000) / 10000L) : 0;
            result["instability"] = instMod;

            // Fee
            int subtotal = basePrice + repMod + tariff + instMod;
            int fee = MarketSystem.ComputeTransactionFeeCredits(state, subtotal);
            result["fee"] = fee;

            // Total
            result["total"] = subtotal + fee;
        }, 0);

        return result;
    }

    private static string ResolveMarketIdFromNodeOrMarket(SimState state, string nodeOrMarketId)
    {
        if (state is null) return "";
        if (string.IsNullOrWhiteSpace(nodeOrMarketId)) return "";

        if (state.Markets.ContainsKey(nodeOrMarketId)) return nodeOrMarketId;

        if (state.Nodes.TryGetValue(nodeOrMarketId, out var node))
        {
            if (!string.IsNullOrWhiteSpace(node.MarketId) && state.Markets.ContainsKey(node.MarketId))
            {
                return node.MarketId;
            }
        }

        return "";
    }

    // GATE.X.LEDGER.BRIDGE.001: Transaction ledger queries.

    /// <summary>
    /// Returns the last N transaction records from the ledger.
    /// Each entry: {tick, cash_delta, good_id, quantity, source, node_id}.
    /// Nonblocking: returns cached if read lock unavailable.
    /// </summary>
    public Godot.Collections.Array GetTransactionLogV0(int maxRecords = 50)
    {
        var result = new Godot.Collections.Array();
        if (IsLoading) return result;
        if (maxRecords <= 0) maxRecords = 50;

        TryExecuteSafeRead(state =>
        {
            var log = state.TransactionLog;
            if (log is null || log.Count == 0) return;

            int start = log.Count > maxRecords ? log.Count - maxRecords : 0;
            for (int i = start; i < log.Count; i++)
            {
                var tx = log[i];
                result.Add(new Godot.Collections.Dictionary
                {
                    ["tick"] = tx.Tick,
                    ["cash_delta"] = tx.CashDelta,
                    ["good_id"] = tx.GoodId ?? "",
                    ["quantity"] = tx.Quantity,
                    ["source"] = tx.Source ?? "",
                    ["node_id"] = tx.NodeId ?? "",
                });
            }
        });

        return result;
    }

    /// <summary>
    /// Aggregates the transaction log to compute profit summary.
    /// Returns {total_revenue, total_expense, net_profit, top_good}.
    /// Nonblocking: returns cached if read lock unavailable.
    /// </summary>
    public Godot.Collections.Dictionary GetProfitSummaryV0()
    {
        var result = new Godot.Collections.Dictionary
        {
            ["total_revenue"] = (long)0,
            ["total_expense"] = (long)0,
            ["net_profit"] = (long)0,
            ["top_good"] = "",
        };
        if (IsLoading) return result;

        TryExecuteSafeRead(state =>
        {
            var log = state.TransactionLog;
            if (log is null || log.Count == 0) return;

            long totalRevenue = 0;
            long totalExpense = 0;
            var profitByGood = new System.Collections.Generic.Dictionary<string, long>(StringComparer.Ordinal);

            foreach (var tx in log)
            {
                if (tx.CashDelta > 0)
                {
                    totalRevenue += tx.CashDelta;
                }
                else if (tx.CashDelta < 0)
                {
                    totalExpense += -tx.CashDelta; // Store as positive number
                }

                var goodId = tx.GoodId ?? "";
                if (!string.IsNullOrEmpty(goodId) && tx.CashDelta > 0)
                {
                    if (!profitByGood.TryGetValue(goodId, out var current))
                        current = 0;
                    profitByGood[goodId] = current + tx.CashDelta;
                }
            }

            string topGood = "";
            long topProfit = 0;
            foreach (var kv in profitByGood)
            {
                if (kv.Value > topProfit)
                {
                    topProfit = kv.Value;
                    topGood = kv.Key;
                }
            }

            result["total_revenue"] = totalRevenue;
            result["total_expense"] = totalExpense;
            result["net_profit"] = totalRevenue - totalExpense;
            result["top_good"] = topGood;
            lock (_snapshotLock) { _cachedProfitSummaryV0 = result.Duplicate(); }
        });

        // On lock timeout the lambda never ran — return cached snapshot.
        if ((long)result["total_revenue"] == 0 && (long)result["total_expense"] == 0)
        {
            lock (_snapshotLock)
            {
                if (_cachedProfitSummaryV0 != null)
                    return _cachedProfitSummaryV0.Duplicate();
            }
        }

        return result;
    }

    /// <summary>
    /// Returns recent credit balance history as 10 data points for spark-chart display.
    /// Each point = cumulative credit delta over a tick bucket. Returns {points: int[], current_credits: int}.
    /// </summary>
    public Godot.Collections.Dictionary GetCreditHistoryV0()
    {
        var points = new Godot.Collections.Array();
        var result = new Godot.Collections.Dictionary { ["points"] = points, ["current_credits"] = (long)0 };
        if (IsLoading) return result;

        TryExecuteSafeRead(state =>
        {
            result["current_credits"] = state.PlayerCredits;
            var log = state.TransactionLog;
            if (log is null || log.Count == 0) return;

            // Find tick range of last 100 ticks (or full log if shorter).
            int currentTick = state.Tick;
            int startTick = Math.Max(0, currentTick - 100);
            int bucketSize = Math.Max(1, (currentTick - startTick) / 10);

            // Build 10 buckets of net credit delta.
            var buckets = new long[10];
            foreach (var tx in log)
            {
                if (tx.Tick < startTick) continue;
                int idx = Math.Min((tx.Tick - startTick) / bucketSize, 9);
                buckets[idx] += tx.CashDelta;
            }

            foreach (var b in buckets)
                points.Add(b);
        });

        return result;
    }

    // ── GATE.S9.SYSTEMIC.CONTEXT_BRIDGE.001: Station context queries ──

    /// <summary>
    /// Returns the current station's economic context (shortages, opportunities, demand).
    /// {context_type (string), primary_good_id (string), last_update_tick (int)}.
    /// Enables dock UI to show situation not just menu.
    /// </summary>
    public Godot.Collections.Dictionary GetStationContextV0(string nodeOrMarketId)
    {
        var result = new Godot.Collections.Dictionary
        {
            ["context_type"] = "Calm",
            ["primary_good_id"] = "",
            ["last_update_tick"] = 0,
        };
        if (string.IsNullOrWhiteSpace(nodeOrMarketId)) return result;

        TryExecuteSafeRead(state =>
        {
            // Resolve market ID from node or market
            var marketId = ResolveMarketIdFromNodeOrMarket(state, nodeOrMarketId);
            if (string.IsNullOrWhiteSpace(marketId)) return;

            if (state.StationContexts != null &&
                state.StationContexts.TryGetValue(marketId, out var ctx))
            {
                result["context_type"] = ctx.ContextType.ToString();
                result["primary_good_id"] = ctx.PrimaryGoodId ?? "";
                result["last_update_tick"] = ctx.LastUpdateTick;
            }
        }, 0);

        return result;
    }

    // ── GATE.X.LEDGER.COST_BASIS_BRIDGE.001: Cargo with cost basis + unrealized P/L ──

    /// <summary>
    /// Returns per-good cargo with avg buy price + current market price + unrealized P/L.
    /// Array of {good_id, qty, avg_cost, market_price, unrealized_pl}.
    /// market_price is the sell price at the specified market (0 if not at a market).
    /// </summary>
    public Godot.Collections.Array GetCargoWithCostBasisV0(string marketId)
    {
        var result = new Godot.Collections.Array();
        if (IsLoading) return result;

        TryExecuteSafeRead(state =>
        {
            SimCore.Entities.Market? market = null;
            if (!string.IsNullOrWhiteSpace(marketId))
                state.Markets.TryGetValue(marketId, out market);

            var sorted = new System.Collections.Generic.List<string>(state.PlayerCargo.Keys);
            sorted.Sort(StringComparer.Ordinal);

            foreach (var goodId in sorted)
            {
                int qty = state.PlayerCargo.TryGetValue(goodId, out var v) ? v : 0;
                if (qty <= 0) continue;

                state.PlayerCargoCostBasis.TryGetValue(goodId, out int avgCost);
                int marketPrice = market != null ? market.GetSellPrice(goodId) : 0;
                int unrealizedPl = marketPrice > 0 ? (marketPrice - avgCost) * qty : 0;

                result.Add(new Godot.Collections.Dictionary
                {
                    ["good_id"] = goodId,
                    ["display_name"] = FormatDisplayNameV0(goodId),
                    ["qty"] = qty,
                    ["avg_cost"] = avgCost,
                    ["market_price"] = marketPrice,
                    ["unrealized_pl"] = unrealizedPl,
                });
            }
        }, 0);

        return result;
    }

    // GATE.S8.STORY_STATE.COVER_NAMES.001: Pre/post-revelation name switching.
    public string GetCoverNameV0(string rawName)
    {
        bool hasR1 = false;
        TryExecuteSafeRead(state =>
        {
            hasR1 = state.StoryState?.HasRevelation(SimCore.Entities.RevelationFlags.R1_Module) ?? false;
        });
        return ApplyCoverName(rawName, hasR1);
    }

    /// <summary>Lock-free cover-name substitution. Call from inside TryExecuteSafeRead lambdas.</summary>
    private static string ApplyCoverName(string rawName, bool hasR1)
    {
        var result = rawName ?? "";
        if (!hasR1)
        {
            result = result switch
            {
                "Fracture Drive" => "Structural Resonance Engine",
                "fracture" => "spatial distortion",
                "Fracture" => "Spatial Distortion",
                "instability" => "metric anomaly",
                "Instability" => "Metric Anomaly",
                _ => result
            };
        }
        return result;
    }

    // ── Ambient Life: Economy snapshot for station visual signals ──

    /// <summary>
    /// Returns synthesized economy snapshot for a node: traffic level, prosperity,
    /// industry type, warfront tier, docked fleets. Used by Godot for ambient life visuals.
    /// </summary>
    public Godot.Collections.Dictionary GetNodeEconomySnapshotV0(string nodeId)
    {
        var result = new Godot.Collections.Dictionary
        {
            ["traffic_level"] = 0,
            ["prosperity"] = 0.0f,
            ["industry_type"] = "none",
            ["warfront_tier"] = 0,
            ["faction_id"] = "",
            ["docked_fleets"] = 0,
        };
        if (string.IsNullOrWhiteSpace(nodeId)) return result;

        TryExecuteSafeRead(state =>
        {
            // Traffic: fleets targeting or at this node.
            int traffic = 0;
            int docked = 0;
            foreach (var f in state.Fleets.Values)
            {
                if (string.Equals(f.CurrentNodeId, nodeId, StringComparison.Ordinal))
                {
                    traffic++;
                    if (!f.IsMoving) docked++;
                }
                else if (string.Equals(f.DestinationNodeId, nodeId, StringComparison.Ordinal)
                    || string.Equals(f.FinalDestinationNodeId, nodeId, StringComparison.Ordinal))
                {
                    traffic++;
                }
            }
            result["traffic_level"] = traffic;
            result["docked_fleets"] = docked;

            // Prosperity: avg inventory / ideal stock.
            if (state.Markets.TryGetValue(nodeId, out var mkt))
            {
                int total = 0; int count = 0;
                foreach (var v in mkt.Inventory.Values) { total += v; count++; }
                if (count > 0)
                    result["prosperity"] = (float)total / count / SimCore.Entities.Market.IdealStock;
            }

            // Industry type from first industry site at this node.
            foreach (var site in state.IndustrySites.Values)
            {
                if (!string.Equals(site.NodeId, nodeId, StringComparison.Ordinal)) continue;
                result["industry_type"] = site.RecipeId switch
                {
                    "" when site.Outputs.ContainsKey("fuel") => "fuel_well",
                    var r when r.Contains("ore") => "mine",
                    var r when r.Contains("metal") => "refinery",
                    var r when r.Contains("munitions") => "munitions_fab",
                    var r when r.Contains("food") => "food_processor",
                    var r when r.Contains("electronics") => "electronics_fab",
                    var r when r.Contains("composites") => "composites_fab",
                    var r when r.Contains("components") => "components_fab",
                    _ => "factory",
                };
                break;
            }

            result["warfront_tier"] = MarketSystem.GetNodeWarfrontIntensity(state, nodeId);
            result["faction_id"] = state.NodeFactionId != null && state.NodeFactionId.TryGetValue(nodeId, out var fid) ? fid : "";
        }, 0);

        return result;
    }

    // ── Economy Digest: Market alerts for price spikes, drops, stockouts ──

    /// <summary>
    /// Returns market alerts for player-visited nodes: price spikes, drops, stockouts.
    /// Array of {node_id, good_id, type, old_price, new_price, change_pct}.
    /// </summary>
    public Godot.Collections.Array GetMarketAlertsV0(int maxAlerts = 10)
    {
        var result = new Godot.Collections.Array();
        if (IsLoading) return result;
        if (maxAlerts <= 0) maxAlerts = 10;

        TryExecuteSafeRead(state =>
        {
            var visitedNodes = new System.Collections.Generic.List<string>(state.PlayerVisitedNodeIds);
            visitedNodes.Sort(StringComparer.Ordinal);

            foreach (var nodeId in visitedNodes)
            {
                if (!state.Markets.TryGetValue(nodeId, out var mkt)) continue;

                var goodIds = new System.Collections.Generic.List<string>(mkt.Inventory.Keys);
                goodIds.Sort(StringComparer.Ordinal);

                foreach (var goodId in goodIds)
                {
                    int currentMid = mkt.GetMidPrice(goodId);
                    int publishedMid = mkt.GetPublishedMidPrice(goodId);

                    // Stockout alert.
                    int stock = mkt.Inventory.TryGetValue(goodId, out var sv) ? sv : 0;
                    if (stock == 0)
                    {
                        result.Add(new Godot.Collections.Dictionary
                        {
                            ["node_id"] = nodeId,
                            ["good_id"] = goodId,
                            ["type"] = "stockout",
                            ["old_price"] = publishedMid,
                            ["new_price"] = currentMid,
                            ["change_pct"] = 0,
                        });
                        continue;
                    }

                    // Price change alert (>20% swing).
                    if (publishedMid <= 0) continue;
                    int changeBps = Math.Abs(currentMid - publishedMid) * 10000 / publishedMid;
                    if (changeBps < 2000) continue;

                    result.Add(new Godot.Collections.Dictionary
                    {
                        ["node_id"] = nodeId,
                        ["good_id"] = goodId,
                        ["type"] = currentMid > publishedMid ? "price_spike" : "price_drop",
                        ["old_price"] = publishedMid,
                        ["new_price"] = currentMid,
                        ["change_pct"] = (currentMid - publishedMid) * 100 / publishedMid,
                    });
                }
            }

            // Cap at maxAlerts.
            while (result.Count > maxAlerts) result.RemoveAt(result.Count - 1);
        }, 0);

        return result;
    }

    public string GetMarketExplainTranscript(string marketId)
    {
        if (IsLoading) return "";
        if (string.IsNullOrWhiteSpace(marketId)) return "";
        _stateLock.EnterReadLock();
        try
        {
            var state = _kernel.State;

            var lines = new System.Collections.Generic.List<string>(256);
            lines.Add("[EXPLAIN] v1");
            lines.Add($"tick={state.Tick}");
            lines.Add($"market_id={marketId}");
            lines.Add($"player_credits={state.PlayerCredits}");
            lines.Add($"player_location={state.PlayerLocationNodeId}");

            if (state.Markets.TryGetValue(marketId, out var market))
            {
                lines.Add("market_inventory:");
                foreach (var kv in market.Inventory.OrderBy(k => k.Key, StringComparer.Ordinal))
                {
                    lines.Add($"  {kv.Key}={kv.Value}");
                }
            }
            else
            {
                lines.Add("market_inventory: (missing)");
            }

            lines.Add("player_cargo:");
            foreach (var kv in state.PlayerCargo.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                lines.Add($"  {kv.Key}={kv.Value}");
            }

            lines.Add("programs:");
            if (state.Programs is null)
            {
                lines.Add("  (none)");
            }
            else
            {
                var programs = state.Programs.Instances.Values
                    .Where(p => string.Equals(p.MarketId, marketId, StringComparison.Ordinal))
                    .OrderBy(p => p.Id, StringComparer.Ordinal)
                    .ToArray();

                if (programs.Length == 0)
                {
                    lines.Add("  (none)");
                }
                else
                {
                    foreach (var p in programs)
                    {
                        var snap = ProgramQuoteSnapshot.Capture(state, p.Id);
                        var quote = ProgramQuote.BuildFromSnapshot(snap);

                        lines.Add($"  {quote.ProgramId} kind={quote.Kind} status={p.Status} good={quote.GoodId} qty={quote.Quantity} cad={quote.CadenceTicks}");
                        lines.Add($"    constraints: market_exists={quote.Constraints.MarketExists} credits_now={quote.Constraints.HasEnoughCreditsNow} supply_now={quote.Constraints.HasEnoughSupplyNow} cargo_now={quote.Constraints.HasEnoughCargoNow}");

                        if (quote.Risks.Count > 0)
                        {
                            lines.Add("    risks:");
                            foreach (var r in quote.Risks)
                            {
                                lines.Add($"      {r}");
                            }
                        }
                    }
                }
            }

            lines.Add("sustainment:");
            var sites = SustainmentSnapshot.BuildForNode(state, marketId)
                .OrderBy(s => s.SiteId, StringComparer.Ordinal)
                .ToArray();

            if (sites.Length == 0)
            {
                lines.Add("  (none)");
            }
            else
            {
                foreach (var s in sites)
                {
                    lines.Add($"  {s.SiteId} node={s.NodeId} health_bps={s.HealthBps} eff_bps={s.EffBpsNow} margin={s.WorstBufferMargin:0.00} starve={s.StarveBand} fail={s.FailBand}");
                    foreach (var inp in s.Inputs.OrderBy(i => i.GoodId, StringComparer.Ordinal))
                    {
                        lines.Add($"    {inp.GoodId} have={inp.HaveUnits} req_per_tick={inp.PerTickRequired} target={inp.BufferTargetUnits} cover={inp.CoverageBand} margin={inp.BufferMargin:0.00}");
                    }
                }
            }

            var transcript = string.Join("\n", lines);
            var hash = Fnv1a64(transcript);
            return $"hash64={hash:X16}\n" + transcript;
        }
        catch
        {
            return "";
        }
        finally
        {
            _stateLock.ExitReadLock();
        }
    }

    // GATE.T61.MARKET.BRIDGE_DEPTH.001: Market depth, spread, and impact queries.

    /// Returns market depth info per good: {good_id, bid, ask, depth, spread_bps, volatility}.
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetMarketDepthV0(string marketId)
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        TryExecuteSafeRead(state =>
        {
            if (!state.Markets.TryGetValue(marketId, out var market)) return;
            int dynSpreadBps = MarketSystem.GetDynamicSpreadAdjustmentBps(state, marketId);
            foreach (var goodId in market.Inventory.Keys.OrderBy(k => k, StringComparer.Ordinal))
            {
                int buy = market.GetBuyPrice(goodId);
                int sell = market.GetSellPrice(goodId);
                var entry = new Godot.Collections.Dictionary
                {
                    ["good_id"] = goodId,
                    ["bid"] = sell,
                    ["ask"] = buy,
                    ["depth"] = market.Depth,
                    ["spread_bps"] = dynSpreadBps,
                    ["volatility"] = market.VolatilityScore,
                };
                result.Add(entry);
            }
        }, 0);
        return result;
    }

    /// Returns estimated cost for a quantity trade: {total_cost, avg_price, impact_bps}.
    public Godot.Collections.Dictionary GetPriceImpactPreviewV0(string marketId, string goodId, int qty, bool isBuy)
    {
        var result = new Godot.Collections.Dictionary
        {
            ["total_cost"] = 0,
            ["avg_price"] = 0,
            ["impact_bps"] = 0,
        };
        TryExecuteSafeRead(state =>
        {
            if (!state.Markets.TryGetValue(marketId, out var market)) return;
            int basePrice = isBuy ? market.GetBuyPrice(goodId) : market.GetSellPrice(goodId);
            int impactBps = MarketSystem.ComputeDepthImpactBps(qty, market.Depth);
            int dynBps = MarketSystem.GetDynamicSpreadAdjustmentBps(state, marketId);
            int totalAdjBps = isBuy ? impactBps + dynBps / 2 : -(impactBps + dynBps / 2);
            int adjPrice = (int)System.Math.Max(1, (long)basePrice * (10000 + totalAdjBps) / 10000);
            result["total_cost"] = adjPrice * qty;
            result["avg_price"] = adjPrice;
            result["impact_bps"] = impactBps;
        }, 0);
        return result;
    }

    /// Returns market volatility score for a market.
    public int GetMarketVolatilityV0(string marketId)
    {
        int vol = 0;
        TryExecuteSafeRead(state =>
        {
            if (state.Markets.TryGetValue(marketId, out var market))
                vol = market.VolatilityScore;
        }, 0);
        return vol;
    }

}
