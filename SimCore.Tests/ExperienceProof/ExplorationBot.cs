using SimCore;
using SimCore.Commands;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Gen;
using SimCore.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimCore.Tests.ExperienceProof;

/// <summary>
/// EPIC.X.EXPERIENCE_PROOF.V0 — Layer 3: Exploration Bot
///
/// Decision-making bot that plays the game autonomously, records every
/// action and outcome, and flags issues automatically.
///
/// Key difference from scripted scenarios: the bot discovers what's broken
/// by exploring the state space, not by following a predetermined path.
/// Each flag is a potential work item with diagnostic context.
///
/// Pattern: Decide → Execute → Verify → Flag
/// </summary>
public sealed class ExplorationBot
{
    // ── Configuration ──

    /// <summary>How many ticks to run the simulation.</summary>
    public int TickBudget { get; set; } = 2000;

    /// <summary>Max units to buy per transaction.</summary>
    public int MaxBuyQty { get; set; } = 10;

    /// <summary>Bot acts every N ticks to let the economy breathe between actions.</summary>
    public int ActEveryNTicks { get; set; } = 5;

    /// <summary>Flag if the bot is idle for more than this many consecutive action cycles.</summary>
    public int StuckThreshold { get; set; } = 10;

    /// <summary>Flag if credits don't change for this many consecutive snapshots.</summary>
    public int StaleCreditsThreshold { get; set; } = 50;

    /// <summary>After this many trades without exploring, force a visit to an unvisited node.</summary>
    public int ExploreEveryNTrades { get; set; } = 4;

    // ── Internal state ──

    private SimKernel _kernel = null!;
    private SimState State => _kernel.State;

    // Graph
    private readonly Dictionary<string, HashSet<string>> _adj = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _marketToNode = new(StringComparer.Ordinal);

    // Tracking
    private readonly List<BotAction> _actions = new();
    private readonly List<BotFlag> _flags = new();
    private readonly List<long> _creditTrajectory = new();
    private readonly HashSet<string> _visitedNodes = new(StringComparer.Ordinal);
    private readonly HashSet<string> _goodsBought = new(StringComparer.Ordinal);
    private readonly HashSet<string> _goodsSold = new(StringComparer.Ordinal);
    private readonly Dictionary<string, long> _buyPriceTracker = new(StringComparer.Ordinal);
    private int _totalBuys, _totalSells, _totalTravels, _totalIdles;
    private long _totalSpent, _totalEarned;
    private int _consecutiveIdles;
    private int _tradesSinceExplore;
    private string _explorationTarget = ""; // committed exploration destination

    // Level 1: Per-good profitability tracking
    private readonly Dictionary<string, int> _perGoodBuyAttempts = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _perGoodSellAttempts = new(StringComparer.Ordinal);
    private readonly Dictionary<string, long> _perGoodProfit = new(StringComparer.Ordinal);

    // Level 2: Mission tracking
    private int _missionsAccepted, _missionsCompleted, _missionAcceptFailures;
    private int _missionStepsAdvanced;
    private readonly HashSet<string> _missionsAttempted = new(StringComparer.Ordinal);

    // Level 3: Research tracking
    private int _researchStarted, _researchCompleted, _researchStartFailures;
    private readonly HashSet<string> _techsResearched = new(StringComparer.Ordinal);

    // Level 4: Combat tracking
    private int _combatsStarted, _combatsWon, _combatsLost;
    private bool _combatAttempted;

    // Level 5: Industry/maintenance tracking (analysis only, no tracking fields needed)

    // Level 6: Discovery tracking
    private int _discoveriesSeen, _discoveriesScanned;

    // ── Public API ──

    public BotReport Run(int seed, int starCount = 12, float radius = 100f)
    {
        _kernel = new SimKernel(seed);
        GalaxyGenerator.Generate(State, starCount, radius);
        BuildAdjacency();
        EnsurePlayerFleet();

        // Place player at first node
        var startNode = State.Nodes.Keys.OrderBy(k => k, StringComparer.Ordinal).First();
        _kernel.EnqueueCommand(new PlayerArriveCommand(startNode));
        _kernel.Step();
        _visitedNodes.Add(startNode);

        long startCredits = State.PlayerCredits;
        _creditTrajectory.Add(startCredits);

        for (int tick = 0; tick < TickBudget; tick++)
        {
            if (tick % ActEveryNTicks == 0)
            {
                var action = Decide(tick);
                var creditsBefore = State.PlayerCredits;
                var cargoBefore = CountPlayerCargo();
                var locBefore = State.PlayerLocationNodeId;

                Execute(action);
                _kernel.Step();

                Verify(action, creditsBefore, cargoBefore, locBefore);

                // Exercise non-trade systems periodically
                TryMissionActions(tick);
                TryResearchActions(tick);
                TryCombatActions(tick);
            }
            else
            {
                _kernel.Step();
            }

            _creditTrajectory.Add(State.PlayerCredits);
        }

        AnalyzeTrajectory(seed);
        AnalyzeCoverage(seed);
        AnalyzeMissions();
        AnalyzeResearch();
        AnalyzeCombat();
        AnalyzeIndustryHealth();
        AnalyzeDiscoveries();
        AnalyzePerGoodProfitability();

        return new BotReport
        {
            Seed = seed,
            TotalTicks = TickBudget,
            Actions = new List<BotAction>(_actions),
            Flags = new List<BotFlag>(_flags),
            CreditTrajectory = new List<long>(_creditTrajectory),
            NodesVisited = _visitedNodes.Count,
            TotalNodes = State.Nodes.Count,
            TotalBuys = _totalBuys,
            TotalSells = _totalSells,
            TotalTravels = _totalTravels,
            TotalIdles = _totalIdles,
            GoodsBought = new HashSet<string>(_goodsBought),
            GoodsSold = new HashSet<string>(_goodsSold),
            StartCredits = startCredits,
            EndCredits = State.PlayerCredits,
            TotalSpent = _totalSpent,
            TotalEarned = _totalEarned,
            MissionsAccepted = _missionsAccepted,
            MissionsCompleted = _missionsCompleted,
            ResearchCompleted = _researchCompleted,
            TechsResearched = new HashSet<string>(_techsResearched),
            CombatsStarted = _combatsStarted,
            CombatsWon = _combatsWon,
        };
    }

    /// <summary>Create player fleet with slots if GalaxyGenerator didn't (it doesn't).</summary>
    private void EnsurePlayerFleet()
    {
        const string pfId = "fleet_trader_1";
        if (State.Fleets.ContainsKey(pfId)) return;
        var startNode = State.Nodes.Keys.OrderBy(k => k, StringComparer.Ordinal).FirstOrDefault() ?? "";
        State.Fleets[pfId] = new Fleet
        {
            Id = pfId,
            OwnerId = "player",
            CurrentNodeId = startNode,
            Speed = 1.0f,
            State = FleetState.Idle,
            Supplies = 100,
            HullHp = 100,
            HullHpMax = 100,
            ShieldHp = 50,
            ShieldHpMax = 50,
            Slots = new List<ModuleSlot>
            {
                new ModuleSlot { SlotId = "weapon_0", SlotKind = SlotKind.Weapon },
                new ModuleSlot { SlotId = "engine_0", SlotKind = SlotKind.Engine },
                new ModuleSlot { SlotId = "utility_0", SlotKind = SlotKind.Utility },
            }
        };
    }

    // ── Decision logic ──

    private BotAction Decide(int tick)
    {
        var loc = State.PlayerLocationNodeId;
        if (string.IsNullOrEmpty(loc) || !State.Nodes.TryGetValue(loc, out var node))
            return MakeAction(tick, "IDLE", loc, "", 0, "no valid location");

        if (!State.Markets.TryGetValue(node.MarketId, out var market))
            return MakeAction(tick, "IDLE", loc, "", 0, "no market at location");

        // ── Phase 0: Committed exploration — keep traveling until we reach target ──
        if (!string.IsNullOrEmpty(_explorationTarget))
        {
            if (loc == _explorationTarget || _visitedNodes.Contains(_explorationTarget))
            {
                _explorationTarget = ""; // reached or already visited
            }
            else
            {
                var hopToTarget = GetNextHop(loc, _explorationTarget);
                if (hopToTarget != null)
                {
                    _consecutiveIdles = 0;
                    return MakeAction(tick, "TRAVEL", hopToTarget, "", 0,
                        $"committed exploration toward {_explorationTarget}");
                }
                _explorationTarget = ""; // unreachable, abandon
            }
        }

        // ── Phase 0b: Exploration pressure — after N trades, commit to exploring ──
        bool unvisitedExist = State.Nodes.Keys.Any(n => !_visitedNodes.Contains(n));
        if (unvisitedExist && _tradesSinceExplore >= ExploreEveryNTrades && State.PlayerCargo.Count == 0)
        {
            // Find nearest unvisited node and commit to reaching it
            var target = FindNearestUnvisited(loc);
            if (target != null)
            {
                _explorationTarget = target;
                _tradesSinceExplore = 0;
                var hopToTarget = GetNextHop(loc, target);
                if (hopToTarget != null)
                {
                    _consecutiveIdles = 0;
                    return MakeAction(tick, "TRAVEL", hopToTarget, "", 0,
                        $"starting exploration toward {target}");
                }
            }
        }

        // ── Phase 1: If carrying cargo → find best sell ──
        if (State.PlayerCargo.Count > 0)
        {
            var sellAction = TrySellOrTravelToSell(tick, loc, market);
            if (sellAction != null) return sellAction;
        }

        // ── Phase 2: No cargo → find best buy opportunity ──
        var buyAction = TryBuyOrTravelToBuy(tick, loc, market);
        if (buyAction != null) return buyAction;

        // ── Phase 3: No profitable trades → explore unvisited ──
        var fallbackExplore = TryExplore(tick, loc);
        if (fallbackExplore != null) return fallbackExplore;

        // ── Phase 4: Nothing to do ──
        _consecutiveIdles++;
        if (_consecutiveIdles >= StuckThreshold)
        {
            AddFlag("STUCK_NO_ACTIONS", "CRITICAL", tick,
                $"Bot idle for {_consecutiveIdles} cycles at {loc}. All nodes visited, no profitable trades.",
                "Economy may have collapsed or bot is in a dead-end. Check: market inventory levels, " +
                "industry production rates, price convergence across markets.");
        }
        return MakeAction(tick, "IDLE", loc, "", 0, "nothing useful to do");
    }

    private BotAction? TrySellOrTravelToSell(int tick, string loc, Market localMarket)
    {
        string bestGood = "";
        string bestSellNodeId = "";
        int bestSellPrice = 0;

        foreach (var kv in State.PlayerCargo)
        {
            if (kv.Value <= 0) continue;

            foreach (var mktKv in State.Markets)
            {
                if (!mktKv.Value.Inventory.ContainsKey(kv.Key)) continue;
                int sell = mktKv.Value.GetSellPrice(kv.Key);
                if (sell > bestSellPrice)
                {
                    bestSellPrice = sell;
                    bestGood = kv.Key;
                    bestSellNodeId = NodeForMarket(mktKv.Key);
                }
            }
        }

        if (string.IsNullOrEmpty(bestSellNodeId)) return null;

        if (bestSellNodeId == loc)
        {
            int qty = State.PlayerCargo.GetValueOrDefault(bestGood, 0);
            _consecutiveIdles = 0;
            return MakeAction(tick, "SELL", loc, bestGood, qty,
                $"sell {qty}x {bestGood} @ {bestSellPrice}/u = {bestSellPrice * qty}cr");
        }

        var nextHop = GetNextHop(loc, bestSellNodeId);
        if (nextHop != null)
        {
            _consecutiveIdles = 0;
            return MakeAction(tick, "TRAVEL", nextHop, bestGood, 0,
                $"travel toward {bestSellNodeId} to sell {bestGood}");
        }

        return null;
    }

    private BotAction? TryBuyOrTravelToBuy(int tick, string loc, Market localMarket)
    {
        string buyNodeId = "", buyGoodId = "";
        int bestProfit = 0, bestBuyPrice = 0;

        foreach (var mktKv in State.Markets)
        {
            var mkt = mktKv.Value;
            foreach (var goodId in mkt.Inventory.Keys)
            {
                int available = mkt.Inventory[goodId];
                if (available <= 0) continue;

                int bp = mkt.GetBuyPrice(goodId);
                if (bp > State.PlayerCredits) continue;

                // Find best sell price elsewhere
                int bestSell = 0;
                foreach (var otherMkt in State.Markets)
                {
                    if (otherMkt.Key == mktKv.Key) continue;
                    if (!otherMkt.Value.Inventory.ContainsKey(goodId)) continue;
                    int sp = otherMkt.Value.GetSellPrice(goodId);
                    if (sp > bestSell) bestSell = sp;
                }

                int profit = bestSell - bp;
                if (profit > bestProfit)
                {
                    bestProfit = profit;
                    buyNodeId = NodeForMarket(mktKv.Key);
                    buyGoodId = goodId;
                    bestBuyPrice = bp;
                }
            }
        }

        if (string.IsNullOrEmpty(buyNodeId) || bestProfit <= 0) return null;

        if (buyNodeId == loc)
        {
            int available = localMarket.Inventory.GetValueOrDefault(buyGoodId, 0);
            int affordable = bestBuyPrice > 0 ? (int)(State.PlayerCredits / bestBuyPrice) : 0;
            int qty = Math.Min(Math.Min(MaxBuyQty, available), affordable);
            if (qty <= 0) return null;

            _consecutiveIdles = 0;
            return MakeAction(tick, "BUY", loc, buyGoodId, qty,
                $"buy {qty}x {buyGoodId} @ {bestBuyPrice}/u, expected profit {bestProfit}/u");
        }

        var nextHop = GetNextHop(loc, buyNodeId);
        if (nextHop != null)
        {
            _consecutiveIdles = 0;
            return MakeAction(tick, "TRAVEL", nextHop, buyGoodId, 0,
                $"travel toward {buyNodeId} to buy {buyGoodId} (profit {bestProfit}/u)");
        }

        return null;
    }

    private BotAction? TryExplore(int tick, string loc)
    {
        // Find closest unvisited node via BFS
        var unvisited = State.Nodes.Keys
            .Where(n => !_visitedNodes.Contains(n))
            .ToHashSet(StringComparer.Ordinal);

        if (unvisited.Count == 0) return null;

        // BFS from current location to find nearest unvisited
        var queue = new Queue<string>();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var parent = new Dictionary<string, string>(StringComparer.Ordinal);
        queue.Enqueue(loc);
        visited.Add(loc);

        string? target = null;
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (unvisited.Contains(current))
            {
                target = current;
                break;
            }

            if (!_adj.TryGetValue(current, out var neighbors)) continue;
            foreach (var n in neighbors.OrderBy(x => x, StringComparer.Ordinal))
            {
                if (visited.Add(n))
                {
                    parent[n] = current;
                    queue.Enqueue(n);
                }
            }
        }

        if (target == null) return null;

        var nextHop = GetNextHop(loc, target);
        if (nextHop != null)
        {
            _consecutiveIdles = 0;
            return MakeAction(tick, "TRAVEL", nextHop, "", 0,
                $"exploring toward unvisited {target}");
        }

        return null;
    }

    // ── Execution ──

    private void Execute(BotAction action)
    {
        switch (action.Type)
        {
            case "BUY":
                var buyNode = State.Nodes[action.NodeId];
                _kernel.EnqueueCommand(new BuyCommand(buyNode.MarketId, action.GoodId, action.Quantity));
                break;

            case "SELL":
                var sellNode = State.Nodes[action.NodeId];
                _kernel.EnqueueCommand(new SellCommand(sellNode.MarketId, action.GoodId, action.Quantity));
                break;

            case "TRAVEL":
                _kernel.EnqueueCommand(new PlayerArriveCommand(action.NodeId));
                break;

            case "IDLE":
                break;
        }
    }

    // ── Verification ──

    private void Verify(BotAction action, long creditsBefore, int cargoBefore, string locBefore)
    {
        action.CreditsBefore = creditsBefore;
        action.CreditsAfter = State.PlayerCredits;

        switch (action.Type)
        {
            case "BUY":
                bool bought = State.PlayerCredits < creditsBefore || CountPlayerCargo() > cargoBefore;
                action.Succeeded = bought;
                if (!bought)
                {
                    AddFlag("TRADE_NO_EFFECT", "CRITICAL", action.Tick,
                        $"BUY {action.Quantity}x {action.GoodId} at {action.NodeId} had no effect. " +
                        $"Credits: {creditsBefore}→{State.PlayerCredits}, Cargo: {cargoBefore}→{CountPlayerCargo()}",
                        "BuyCommand.Execute silently returned. Check: market inventory, player credits, " +
                        "InventoryLedger.TryRemoveMarket. Files: SimCore/Commands/BuyCommand.cs");
                }
                else
                {
                    _totalBuys++;
                    _tradesSinceExplore++;
                    long spent = creditsBefore - State.PlayerCredits;
                    _totalSpent += spent;
                    _goodsBought.Add(action.GoodId);
                    _buyPriceTracker[action.GoodId] = spent / Math.Max(1, action.Quantity);
                    _perGoodBuyAttempts.TryGetValue(action.GoodId, out var ba);
                    _perGoodBuyAttempts[action.GoodId] = ba + 1;
                    _perGoodProfit.TryGetValue(action.GoodId, out var gp);
                    _perGoodProfit[action.GoodId] = gp - spent;
                }
                break;

            case "SELL":
                bool sold = State.PlayerCredits > creditsBefore || CountPlayerCargo() < cargoBefore;
                action.Succeeded = sold;
                if (!sold)
                {
                    AddFlag("TRADE_NO_EFFECT", "CRITICAL", action.Tick,
                        $"SELL {action.Quantity}x {action.GoodId} at {action.NodeId} had no effect. " +
                        $"Credits: {creditsBefore}→{State.PlayerCredits}, Cargo: {cargoBefore}→{CountPlayerCargo()}",
                        "SellCommand.Execute silently returned. Check: player cargo for good, " +
                        "InventoryLedger.TryRemoveCargo. Files: SimCore/Commands/SellCommand.cs");
                }
                else
                {
                    _totalSells++;
                    _tradesSinceExplore++;
                    long earned = State.PlayerCredits - creditsBefore;
                    _totalEarned += earned;
                    _goodsSold.Add(action.GoodId);
                    _perGoodSellAttempts.TryGetValue(action.GoodId, out var sa);
                    _perGoodSellAttempts[action.GoodId] = sa + 1;
                    _perGoodProfit.TryGetValue(action.GoodId, out var gpe);
                    _perGoodProfit[action.GoodId] = gpe + earned;
                }
                break;

            case "TRAVEL":
                bool moved = State.PlayerLocationNodeId != locBefore;
                action.Succeeded = moved;
                if (moved)
                {
                    _totalTravels++;
                    _visitedNodes.Add(State.PlayerLocationNodeId);
                }
                else
                {
                    AddFlag("TRAVEL_FAILED", "WARNING", action.Tick,
                        $"Travel to {action.NodeId} from {locBefore} failed. Player still at {State.PlayerLocationNodeId}.",
                        "PlayerArriveCommand.Execute returned without moving. Check: node exists in state, " +
                        "TargetNodeId is valid. Files: SimCore/Commands/PlayerArriveCommand.cs");
                }
                break;

            case "IDLE":
                action.Succeeded = true; // idle always "succeeds"
                _totalIdles++;
                break;
        }

        _actions.Add(action);
    }

    // ── Post-run analysis ──

    private void AnalyzeTrajectory(int seed)
    {
        // Net loss after trading
        if (_totalBuys > 0 && _totalSells > 0 && State.PlayerCredits < _creditTrajectory[0])
        {
            AddFlag("NET_LOSS", "WARNING", -1,
                $"Bot ended with {State.PlayerCredits}cr (started {_creditTrajectory[0]}cr) after " +
                $"{_totalBuys} buys, {_totalSells} sells. Spent {_totalSpent}, earned {_totalEarned}.",
                "Trading is not profitable. Check: price model (buy always > sell at same market is correct, " +
                "but cross-market arbitrage should exist). Files: SimCore/Entities/Market.cs");
        }

        // Stale credits (flat line)
        int maxStale = 0, currentStale = 0;
        for (int i = 1; i < _creditTrajectory.Count; i++)
        {
            if (_creditTrajectory[i] == _creditTrajectory[i - 1])
                currentStale++;
            else
            {
                maxStale = Math.Max(maxStale, currentStale);
                currentStale = 0;
            }
        }
        maxStale = Math.Max(maxStale, currentStale);

        if (maxStale > StaleCreditsThreshold)
        {
            AddFlag("CREDITS_STALE", "WARNING", -1,
                $"Credits unchanged for {maxStale} consecutive ticks (threshold: {StaleCreditsThreshold}). " +
                $"Suggests economy is dead or bot is stuck.",
                "Check: are there profitable trade routes? Is the bot finding them? " +
                "Look at action log for IDLE streaks.");
        }

        // Credits never increased (no successful trade)
        bool everIncreased = false;
        for (int i = 1; i < _creditTrajectory.Count; i++)
        {
            if (_creditTrajectory[i] > _creditTrajectory[i - 1])
            {
                everIncreased = true;
                break;
            }
        }
        if (!everIncreased && TickBudget > 100)
        {
            AddFlag("CREDITS_NEVER_INCREASED", "CRITICAL", -1,
                $"Credits never increased across {TickBudget} ticks. Bot may not have sold anything.",
                "Either no profitable trades exist, or sell commands are failing silently. " +
                "Check action log for SELL attempts.");
        }
    }

    private void AnalyzeCoverage(int seed)
    {
        float visitPct = (float)_visitedNodes.Count / Math.Max(1, State.Nodes.Count);

        if (visitPct < 0.5f && TickBudget >= 500)
        {
            AddFlag("LOW_EXPLORATION", "WARNING", -1,
                $"Visited {_visitedNodes.Count}/{State.Nodes.Count} nodes ({visitPct:P0}). " +
                $"Bot didn't reach half the galaxy in {TickBudget} ticks.",
                "Check: graph connectivity, travel rate (ActEveryNTicks), pathfinding.");
        }

        // No AI fleets
        int aiFleets = State.Fleets.Values.Count(f =>
            !string.Equals(f.OwnerId, "player", StringComparison.Ordinal));
        if (aiFleets == 0)
        {
            AddFlag("NO_AI_FLEETS", "INFO", -1,
                "No non-player fleets exist in the galaxy.",
                "GalaxyGenerator may not seed AI fleets. Check: fleet creation in generation.");
        }

        // No industry
        if (State.IndustrySites.Count == 0)
        {
            AddFlag("NO_INDUSTRY", "CRITICAL", -1,
                "No industry sites exist. Economy has no production.",
                "MarketInitGen should create mines, refineries, fuel wells. " +
                "Files: SimCore/Gen/MarketInitGen.cs");
        }

        // Check goods variety
        var allGoods = new HashSet<string>(StringComparer.Ordinal);
        foreach (var mkt in State.Markets.Values)
            foreach (var goodId in mkt.Inventory.Keys)
                allGoods.Add(goodId);

        if (allGoods.Count <= 1)
        {
            AddFlag("SINGLE_GOOD_ECONOMY", "CRITICAL", -1,
                $"Only {allGoods.Count} tradeable good(s): [{string.Join(", ", allGoods)}]. " +
                "Economy lacks variety.",
                "MarketInitGen should seed fuel, ore, metal, hull_plating. " +
                "Files: SimCore/Gen/MarketInitGen.cs, SimCore/Content/WellKnownGoodIds.cs");
        }

        // Check for dead markets (all goods at 0 inventory)
        int deadMarkets = 0;
        foreach (var mkt in State.Markets.Values)
        {
            bool allZero = mkt.Inventory.Values.All(v => v <= 0);
            if (allZero) deadMarkets++;
        }
        if (deadMarkets > 0)
        {
            AddFlag("DEAD_MARKETS", "WARNING", -1,
                $"{deadMarkets}/{State.Markets.Count} markets have zero inventory for all goods.",
                "Industry may not be producing, or AI fleets are draining markets. " +
                "Check: IndustrySystem output rates, LogisticsSystem distribution.");
        }

        // No profitable cross-market trades exist
        bool anyProfitable = false;
        foreach (var goodId in allGoods)
        {
            int minBuy = int.MaxValue;
            int maxSell = 0;
            foreach (var mkt in State.Markets.Values)
            {
                if (!mkt.Inventory.ContainsKey(goodId)) continue;
                int b = mkt.GetBuyPrice(goodId);
                int s = mkt.GetSellPrice(goodId);
                if (b > 0 && b < minBuy) minBuy = b;
                if (s > maxSell) maxSell = s;
            }
            if (maxSell > minBuy) { anyProfitable = true; break; }
        }
        if (!anyProfitable)
        {
            AddFlag("ECONOMY_COLLAPSED", "CRITICAL", -1,
                "No profitable cross-market trades exist for any good. Economy is dead.",
                "All markets converged to identical prices, or spreads eliminated arbitrage. " +
                "Check: price model differentiation, inventory variance across markets. " +
                "Files: SimCore/Entities/Market.cs, SimCore/Gen/MarketInitGen.cs");
        }

        // Check for goods the bot never traded
        var untradedGoods = allGoods.Except(_goodsBought).Except(_goodsSold).ToList();
        if (untradedGoods.Count > 0 && TickBudget >= 500)
        {
            AddFlag("UNTRADED_GOODS", "INFO", -1,
                $"Bot never traded: [{string.Join(", ", untradedGoods.OrderBy(g => g))}]. " +
                $"Traded buy: [{string.Join(", ", _goodsBought.OrderBy(g => g))}], " +
                $"sell: [{string.Join(", ", _goodsSold.OrderBy(g => g))}].",
                "Some goods may lack profitable routes, or bot prioritization skips them.");
        }
    }

    // ── Level 2: Mission exercise ──

    private void TryMissionActions(int tick)
    {
        // Only try missions every 20 action cycles to avoid spam
        if (tick % (ActEveryNTicks * 20) != 0) return;

        // If no active mission, try to accept one
        if (string.IsNullOrEmpty(State.Missions.ActiveMissionId))
        {
            var available = MissionSystem.GetAvailableMissions(State);
            foreach (var def in available)
            {
                if (_missionsAttempted.Contains(def.MissionId)) continue;
                _missionsAttempted.Add(def.MissionId);

                bool accepted = MissionSystem.AcceptMission(State, def.MissionId);
                MissionSystem.Process(State); // auto-advance already-met steps
                if (accepted)
                {
                    _missionsAccepted++;
                    break;
                }
                else
                {
                    _missionAcceptFailures++;
                }
            }
        }

        // Track mission step advancement for active mission
        if (!string.IsNullOrEmpty(State.Missions.ActiveMissionId))
        {
            _missionStepsAdvanced = State.Missions.ActiveSteps.Count(s => s.Completed);
        }

        // Track completed missions (CompletedMissionIds grows over time)
        _missionsCompleted = State.Missions.CompletedMissionIds.Count;
    }

    // ── Level 3: Research exercise ──

    private void TryResearchActions(int tick)
    {
        // Only try research every 50 action cycles
        if (tick % (ActEveryNTicks * 50) != 0) return;

        // If not researching, start a tech
        if (!State.Tech.IsResearching)
        {
            // Check if something just completed
            if (State.Tech.UnlockedTechIds.Count > _techsResearched.Count)
            {
                _researchCompleted += State.Tech.UnlockedTechIds.Count - _techsResearched.Count;
                foreach (var tid in State.Tech.UnlockedTechIds)
                    _techsResearched.Add(tid);
            }

            // Find next researchable tech
            foreach (var tech in TechContentV0.AllTechs)
            {
                if (State.Tech.UnlockedTechIds.Contains(tech.TechId)) continue;
                if (tech.CreditCost > State.PlayerCredits) continue;

                var result = ResearchSystem.StartResearch(State, tech.TechId);
                if (result.Success)
                {
                    _researchStarted++;
                    break;
                }
                else
                {
                    _researchStartFailures++;
                }
            }
        }
    }

    // ── Level 4: Combat exercise ──

    private void TryCombatActions(int tick)
    {
        // Only attempt combat once per run, around tick 500
        if (_combatAttempted || tick < 500) return;

        var loc = State.PlayerLocationNodeId;
        if (string.IsNullOrEmpty(loc)) return;

        // Find an AI fleet at the same node
        string? opponentId = null;
        foreach (var fleet in State.Fleets.Values)
        {
            if (string.Equals(fleet.OwnerId, "player", StringComparison.Ordinal)) continue;
            if (fleet.CurrentNodeId == loc)
            {
                opponentId = fleet.Id;
                break;
            }
        }

        if (opponentId == null) return;
        _combatAttempted = true;

        // Initialize combat HP if needed
        var playerFleet = State.Fleets.GetValueOrDefault("fleet_trader_1");
        var opponent = State.Fleets[opponentId];

        if (playerFleet != null && playerFleet.HullHp < 0)
        {
            playerFleet.HullHp = playerFleet.HullHpMax > 0 ? playerFleet.HullHpMax : 100;
            playerFleet.ShieldHp = playerFleet.ShieldHpMax > 0 ? playerFleet.ShieldHpMax : 50;
        }

        if (opponent.HullHp < 0)
        {
            opponent.HullHp = 50;
            opponent.HullHpMax = 50;
            opponent.ShieldHp = 0;
            opponent.ShieldHpMax = 0;
        }

        int playerHpBefore = playerFleet?.HullHp ?? -1;
        int opponentHpBefore = opponent.HullHp;

        _kernel.EnqueueCommand(new StartCombatCommand("fleet_trader_1", opponentId));
        _kernel.Step();
        _combatsStarted++;

        // Check results
        int playerHpAfter = playerFleet?.HullHp ?? -1;
        int opponentHpAfter = opponent.HullHp;

        if (opponentHpAfter <= 0)
            _combatsWon++;
        else if (playerHpAfter <= 0)
            _combatsLost++;

        // Clear combat flag
        _kernel.EnqueueCommand(new ClearCombatCommand());
        _kernel.Step();
    }

    // ── Post-run analysis: new systems ──

    private void AnalyzeMissions()
    {
        var available = MissionSystem.GetAvailableMissions(State);
        int totalDefs = MissionContentV0.AllMissions.Count;

        if (totalDefs == 0)
        {
            AddFlag("NO_MISSION_CONTENT", "CRITICAL", -1,
                "MissionContentV0.AllMissions is empty. No missions exist.",
                "Files: SimCore/Content/MissionContentV0.cs");
            return;
        }

        if (_missionsAccepted == 0 && TickBudget >= 500)
        {
            AddFlag("MISSION_NEVER_ACCEPTED", "WARNING", -1,
                $"Bot never accepted a mission in {TickBudget} ticks. " +
                $"{available.Count} available, {totalDefs} total defined, " +
                $"{_missionAcceptFailures} accept failures.",
                "MissionSystem.AcceptMission may be failing, or prerequisites aren't met. " +
                "Check: binding token resolution ($PLAYER_START, $ADJACENT_1, $MARKET_GOOD_1). " +
                "Files: SimCore/Systems/MissionSystem.cs, SimCore/Content/MissionContentV0.cs");
        }

        // Use final count from state (most accurate)
        _missionsCompleted = State.Missions.CompletedMissionIds.Count;

        if (_missionsAccepted > 0 && _missionsCompleted == 0 && TickBudget >= 1000)
        {
            AddFlag("MISSION_NEVER_COMPLETED", "WARNING", -1,
                $"Bot accepted {_missionsAccepted} mission(s) but completed 0. " +
                $"Steps advanced: {_missionStepsAdvanced}. Active: '{State.Missions.ActiveMissionId}'.",
                "Mission steps may not be triggering. Check: MissionSystem.Process(), " +
                "trigger evaluation for ArriveAtNode/HaveCargoMin/NoCargoAtNode. " +
                "The bot naturally fulfills trade missions through normal gameplay.");
        }
    }

    private void AnalyzeResearch()
    {
        int totalTechs = TechContentV0.AllTechs.Count;
        if (totalTechs == 0)
        {
            AddFlag("NO_TECH_CONTENT", "CRITICAL", -1,
                "TechContentV0.AllTechs is empty. No techs exist.",
                "Files: SimCore/Content/TechContentV0.cs");
            return;
        }

        int unlockedCount = State.Tech.UnlockedTechIds.Count;

        if (_researchStarted == 0 && TickBudget >= 500)
        {
            AddFlag("RESEARCH_NEVER_STARTED", "WARNING", -1,
                $"Bot never started research in {TickBudget} ticks. " +
                $"{totalTechs} techs exist, {_researchStartFailures} start failures. " +
                $"Player credits at end: {State.PlayerCredits}.",
                "ResearchSystem.StartResearch may be failing. Common issues: " +
                "insufficient credits, tier-locked techs, prerequisites not met. " +
                "Files: SimCore/Systems/ResearchSystem.cs");
        }

        if (_researchStarted > 0 && unlockedCount == 0 && TickBudget >= 1000)
        {
            AddFlag("RESEARCH_NEVER_COMPLETED", "WARNING", -1,
                $"Bot started {_researchStarted} research(es) but unlocked 0 techs in {TickBudget} ticks. " +
                $"Currently researching: {State.Tech.CurrentResearchTechId}, " +
                $"progress: {State.Tech.ResearchProgressTicks}/{State.Tech.ResearchTotalTicks}.",
                "ResearchSystem.ProcessResearch may not be advancing. Check: " +
                "is it called in SimKernel.Step()? Does it deduct credits per tick? " +
                "Files: SimCore/Systems/ResearchSystem.cs, SimCore/SimKernel.cs");
        }

        if (State.Tech.IsResearching && State.Tech.ResearchProgressTicks == 0 && TickBudget >= 500)
        {
            AddFlag("RESEARCH_STALLED", "WARNING", -1,
                $"Research on '{State.Tech.CurrentResearchTechId}' has 0 progress after bot run. " +
                $"Credits: {State.PlayerCredits}, cost: {State.Tech.ResearchCreditsSpent}.",
                "ProcessResearch may stall when credits are 0. Check: credit deduction logic. " +
                "Files: SimCore/Systems/ResearchSystem.cs");
        }
    }

    private void AnalyzeCombat()
    {
        // Check AI fleet existence
        int aiFleetCount = State.Fleets.Values.Count(f =>
            !string.Equals(f.OwnerId, "player", StringComparison.Ordinal));

        if (aiFleetCount == 0)
        {
            // Already flagged by NO_AI_FLEETS in AnalyzeCoverage
            return;
        }

        if (!_combatAttempted && TickBudget >= 500)
        {
            AddFlag("COMBAT_NEVER_ATTEMPTED", "INFO", -1,
                $"Bot never found an AI fleet at its location to fight. " +
                $"{aiFleetCount} AI fleets exist but none co-located during bot's visit.",
                "AI fleets may always be at different nodes than the player. " +
                "Check: StarNetworkGen.SeedAiFleets placement.");
        }

        if (_combatsStarted > 0)
        {
            // Check combat log exists
            if (State.CombatLogs.Count == 0)
            {
                AddFlag("COMBAT_NO_LOG", "CRITICAL", -1,
                    $"Started {_combatsStarted} combat(s) but CombatLogs is empty.",
                    "StartCombatCommand.Execute may not be calling CombatSystem.RunEncounter. " +
                    "Files: SimCore/Commands/StartCombatCommand.cs, SimCore/Systems/CombatSystem.cs");
            }

            if (_combatsWon == 0 && _combatsLost == 0)
            {
                AddFlag("COMBAT_NO_RESOLUTION", "WARNING", -1,
                    $"Started {_combatsStarted} combat(s) but neither fleet reached 0 HP.",
                    "Combat may not deal enough damage to resolve. Check: weapon definitions, " +
                    "damage calculation. Files: SimCore/Systems/CombatSystem.cs");
            }
        }
    }

    private void AnalyzeIndustryHealth()
    {
        if (State.IndustrySites.Count == 0) return; // Already flagged by NO_INDUSTRY

        int total = 0, degraded = 0, zeroEfficiency = 0;
        foreach (var site in State.IndustrySites.Values)
        {
            total++;
            if (site.HealthBps < 5000) degraded++;
            if (site.Efficiency <= 0.0f) zeroEfficiency++;
        }

        if (degraded > total / 2)
        {
            AddFlag("INDUSTRY_DEGRADED", "WARNING", -1,
                $"{degraded}/{total} industry sites have health < 50% after {TickBudget} ticks.",
                "MaintenanceSystem.ProcessDecay may be degrading sites faster than repair. " +
                "Check: DegradePerDayBps rates, supply levels. " +
                "Files: SimCore/Systems/MaintenanceSystem.cs");
        }

        if (zeroEfficiency > 0)
        {
            AddFlag("INDUSTRY_ZERO_EFFICIENCY", "WARNING", -1,
                $"{zeroEfficiency}/{total} industry sites have 0% efficiency.",
                "Sites with zero efficiency produce nothing. Check: health threshold, " +
                "upkeep consumption. Files: SimCore/Systems/IndustrySystem.cs");
        }

        // Check if industry is actually producing (inventory should be > 0 at production sites)
        int productiveSites = 0;
        foreach (var site in State.IndustrySites.Values)
        {
            if (site.Outputs.Count > 0 && site.Efficiency > 0) productiveSites++;
        }
        if (productiveSites == 0 && total > 0)
        {
            AddFlag("INDUSTRY_NOT_PRODUCING", "CRITICAL", -1,
                $"No industry sites are actively producing (0/{total} have outputs + nonzero efficiency).",
                "Check: IndustrySystem.Process(), site.Outputs, site.Efficiency. " +
                "Files: SimCore/Systems/IndustrySystem.cs");
        }
    }

    private void AnalyzeDiscoveries()
    {
        // Check intel book for discoveries that were seen during exploration
        int seenCount = 0, scannedCount = 0;
        foreach (var disc in State.Intel.Discoveries.Values)
        {
            if (disc.Phase >= DiscoveryPhase.Seen) seenCount++;
            if (disc.Phase >= DiscoveryPhase.Scanned) scannedCount++;
        }
        _discoveriesSeen = seenCount;
        _discoveriesScanned = scannedCount;

        // Check if nodes have seeded discoveries
        int nodesWithDiscoveries = 0;
        int totalSeededDiscoveries = 0;
        foreach (var node in State.Nodes.Values)
        {
            if (node.SeededDiscoveryIds.Count > 0)
            {
                nodesWithDiscoveries++;
                totalSeededDiscoveries += node.SeededDiscoveryIds.Count;
            }
        }

        if (totalSeededDiscoveries == 0)
        {
            AddFlag("NO_SEEDED_DISCOVERIES", "INFO", -1,
                "No nodes have seeded discoveries. Discovery system has nothing to find.",
                "GalaxyGenerator may not seed discovery IDs on nodes. " +
                "Files: SimCore/Gen/GalaxyGenerator.cs, SimCore/Gen/DiscoverySeedGen.cs");
        }
        else if (seenCount == 0 && _visitedNodes.Count > 3)
        {
            AddFlag("DISCOVERIES_NOT_TRIGGERING", "WARNING", -1,
                $"{totalSeededDiscoveries} discoveries seeded across {nodesWithDiscoveries} nodes, " +
                $"but 0 discovered after visiting {_visitedNodes.Count} nodes.",
                "PlayerArriveCommand may not trigger discovery 'Seen' phase. Check: " +
                "does arriving at a node with SeededDiscoveryIds update Intel.Discoveries? " +
                "Files: SimCore/Commands/PlayerArriveCommand.cs");
        }
    }

    private void AnalyzePerGoodProfitability()
    {
        var allGoods = new HashSet<string>(StringComparer.Ordinal);
        foreach (var mkt in State.Markets.Values)
            foreach (var goodId in mkt.Inventory.Keys)
                allGoods.Add(goodId);

        var unprofitableGoods = new List<string>();
        foreach (var goodId in allGoods.OrderBy(g => g, StringComparer.Ordinal))
        {
            // Check if cross-market arbitrage exists for this good
            int minBuy = int.MaxValue;
            int maxSell = 0;
            foreach (var mkt in State.Markets.Values)
            {
                if (!mkt.Inventory.ContainsKey(goodId)) continue;
                int b = mkt.GetBuyPrice(goodId);
                int s = mkt.GetSellPrice(goodId);
                if (b > 0 && b < minBuy) minBuy = b;
                if (s > maxSell) maxSell = s;
            }
            if (maxSell <= minBuy)
            {
                unprofitableGoods.Add($"{goodId}(buy≥{minBuy},sell≤{maxSell})");
            }
        }

        if (unprofitableGoods.Count > 0)
        {
            AddFlag("GOODS_NO_ARBITRAGE", "WARNING", -1,
                $"{unprofitableGoods.Count}/{allGoods.Count} goods have no profitable cross-market route: " +
                $"[{string.Join(", ", unprofitableGoods)}].",
                "These goods have identical or inverted prices everywhere. " +
                "Check: inventory variance in MarketInitGen, price model spread. " +
                "Files: SimCore/Entities/Market.cs, SimCore/Gen/MarketInitGen.cs");
        }

        // Report per-good P&L for goods the bot actually traded
        var tradedGoodsPnL = new List<string>();
        foreach (var kv in _perGoodProfit.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            int buys = _perGoodBuyAttempts.GetValueOrDefault(kv.Key, 0);
            int sells = _perGoodSellAttempts.GetValueOrDefault(kv.Key, 0);
            if (buys > 0 && sells > 0 && kv.Value < 0)
            {
                tradedGoodsPnL.Add($"{kv.Key}({kv.Value}cr,{buys}b/{sells}s)");
            }
        }

        if (tradedGoodsPnL.Count > 0)
        {
            AddFlag("GOODS_NEGATIVE_PNL", "WARNING", -1,
                $"Bot lost money trading these goods: [{string.Join(", ", tradedGoodsPnL)}].",
                "The bot's arbitrage found these routes profitable upfront but they yielded a loss. " +
                "Possible: prices shifted during travel, or spread is too thin.");
        }
    }

    // ── Graph helpers ──

    private void BuildAdjacency()
    {
        _adj.Clear();
        _marketToNode.Clear();

        foreach (var node in State.Nodes.Values)
        {
            _adj[node.Id] = new HashSet<string>(StringComparer.Ordinal);
            if (!string.IsNullOrEmpty(node.MarketId))
                _marketToNode[node.MarketId] = node.Id;
        }

        foreach (var edge in State.Edges.Values)
        {
            if (_adj.TryGetValue(edge.FromNodeId, out var fromSet))
                fromSet.Add(edge.ToNodeId);
            if (_adj.TryGetValue(edge.ToNodeId, out var toSet))
                toSet.Add(edge.FromNodeId);
        }
    }

    private string NodeForMarket(string marketId)
    {
        return _marketToNode.TryGetValue(marketId, out var nodeId) ? nodeId : "";
    }

    /// <summary>BFS to find nearest unvisited node from current location.</summary>
    private string? FindNearestUnvisited(string from)
    {
        var queue = new Queue<string>();
        var visited = new HashSet<string>(StringComparer.Ordinal) { from };
        queue.Enqueue(from);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!_visitedNodes.Contains(current) && current != from)
                return current;

            if (!_adj.TryGetValue(current, out var neighbors)) continue;
            foreach (var n in neighbors.OrderBy(x => x, StringComparer.Ordinal))
            {
                if (visited.Add(n))
                    queue.Enqueue(n);
            }
        }
        return null;
    }

    /// <summary>BFS to find next hop from 'from' toward 'to'.</summary>
    private string? GetNextHop(string from, string to)
    {
        if (from == to) return null;

        var queue = new Queue<string>();
        var parent = new Dictionary<string, string>(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal) { from };
        queue.Enqueue(from);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == to)
            {
                // Trace back to find first hop
                var hop = to;
                while (parent.TryGetValue(hop, out var p) && p != from)
                    hop = p;
                return hop;
            }

            if (!_adj.TryGetValue(current, out var neighbors)) continue;
            foreach (var n in neighbors.OrderBy(x => x, StringComparer.Ordinal))
            {
                if (visited.Add(n))
                {
                    parent[n] = current;
                    queue.Enqueue(n);
                }
            }
        }

        return null; // unreachable
    }

    // ── Utility ──

    private int CountPlayerCargo()
    {
        int total = 0;
        foreach (var v in State.PlayerCargo.Values)
            total += v;
        return total;
    }

    private static BotAction MakeAction(int tick, string type, string nodeId, string goodId, int qty, string detail)
    {
        return new BotAction
        {
            Tick = tick,
            Type = type,
            NodeId = nodeId,
            GoodId = goodId,
            Quantity = qty,
            Detail = detail
        };
    }

    private void AddFlag(string id, string severity, int tick, string detail, string diagnostic)
    {
        // Deduplicate by id (don't spam the same flag)
        if (_flags.Any(f => f.Id == id)) return;

        _flags.Add(new BotFlag
        {
            Id = id,
            Severity = severity,
            Tick = tick,
            Detail = detail,
            Diagnostic = diagnostic
        });
    }
}

// ── Data types ──

public sealed class BotAction
{
    public int Tick { get; init; }
    public string Type { get; init; } = "";
    public string NodeId { get; init; } = "";
    public string GoodId { get; init; } = "";
    public int Quantity { get; init; }
    public string Detail { get; set; } = "";
    public long CreditsBefore { get; set; }
    public long CreditsAfter { get; set; }
    public bool Succeeded { get; set; }
}

public sealed class BotFlag
{
    public string Id { get; init; } = "";
    public string Severity { get; init; } = "WARNING";
    public int Tick { get; init; }
    public string Detail { get; init; } = "";
    public string Diagnostic { get; init; } = "";
}

public sealed class BotReport
{
    public int Seed { get; init; }
    public int TotalTicks { get; init; }
    public List<BotAction> Actions { get; init; } = new();
    public List<BotFlag> Flags { get; init; } = new();
    public List<long> CreditTrajectory { get; init; } = new();
    public int NodesVisited { get; init; }
    public int TotalNodes { get; init; }
    public int TotalBuys { get; init; }
    public int TotalSells { get; init; }
    public int TotalTravels { get; init; }
    public int TotalIdles { get; init; }
    public HashSet<string> GoodsBought { get; init; } = new();
    public HashSet<string> GoodsSold { get; init; } = new();
    public long StartCredits { get; init; }
    public long EndCredits { get; init; }
    public long TotalSpent { get; set; }
    public long TotalEarned { get; set; }
    public long NetProfit => EndCredits - StartCredits;

    // Level 2+
    public int MissionsAccepted { get; init; }
    public int MissionsCompleted { get; init; }
    public int ResearchCompleted { get; init; }
    public HashSet<string> TechsResearched { get; init; } = new();
    public int CombatsStarted { get; init; }
    public int CombatsWon { get; init; }

    /// <summary>Human-readable summary for diagnostic output.</summary>
    public string GetSummary()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"═══ Exploration Bot Report (seed={Seed}) ═══");
        sb.AppendLine($"Ticks: {TotalTicks} | Nodes: {NodesVisited}/{TotalNodes} | Net: {NetProfit:+#;-#;0}cr");
        sb.AppendLine($"Actions: {TotalBuys} buys, {TotalSells} sells, {TotalTravels} travels, {TotalIdles} idles");
        sb.AppendLine($"Spent: {TotalSpent}cr | Earned: {TotalEarned}cr | Credits: {StartCredits}→{EndCredits}");
        sb.AppendLine($"Goods bought: [{string.Join(", ", GoodsBought.OrderBy(g => g))}]");
        sb.AppendLine($"Goods sold: [{string.Join(", ", GoodsSold.OrderBy(g => g))}]");
        sb.AppendLine($"Missions: {MissionsAccepted} accepted, {MissionsCompleted} completed");
        sb.AppendLine($"Research: {ResearchCompleted} completed, techs: [{string.Join(", ", TechsResearched.OrderBy(t => t))}]");
        sb.AppendLine($"Combat: {CombatsStarted} fights, {CombatsWon} won");

        if (Flags.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"── {Flags.Count} Flag(s) ──");
            foreach (var flag in Flags.OrderBy(f => f.Severity == "CRITICAL" ? 0 : f.Severity == "WARNING" ? 1 : 2))
            {
                sb.AppendLine($"  [{flag.Severity}] {flag.Id}");
                sb.AppendLine($"    {flag.Detail}");
                sb.AppendLine($"    → {flag.Diagnostic}");
            }
        }
        else
        {
            sb.AppendLine();
            sb.AppendLine("── No flags. All systems healthy. ──");
        }

        return sb.ToString();
    }
}
