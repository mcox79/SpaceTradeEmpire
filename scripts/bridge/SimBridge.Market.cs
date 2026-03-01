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
}
