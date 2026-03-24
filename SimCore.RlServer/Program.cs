using SimCore;
using SimCore.Commands;
using SimCore.Gen;
using SimCore.Entities;
using SimCore.RlServer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

/// <summary>
/// Headless RL training server for SpaceTradeEmpire.
/// Communicates via stdin/stdout JSON-line protocol.
/// Each line is a JSON object; responses are flushed immediately.
///
/// Protocol:
///   {"type":"reset", "seed":42, "star_count":12, "curriculum_stage":0, "max_episode_ticks":2000}
///   {"type":"step", "action":7}
///   {"type":"observe"}
///   {"type":"shutdown"}
/// </summary>
class Program
{
    static SimKernel? _kernel;
    static int _maxEpisodeTicks = 2000;
    static int _curriculumStage;
    static long _prevCredits;
    static int _prevVisited;
    static long _totalProfit;
    static List<string> _neighborCache = new();

    static void Main()
    {
        // Redirect stderr for SimCore debug output so it doesn't corrupt the JSON protocol
        Console.SetError(new StreamWriter(Stream.Null));

        string? line;
        while ((line = Console.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                var req = Protocol.ParseRequest(line);
                if (req == null)
                {
                    Respond(new RlResponse { Type = "error", Error = "parse_failed" });
                    continue;
                }

                switch (req.Type)
                {
                    case "reset":
                        HandleReset(req);
                        break;
                    case "step":
                        HandleStep(req);
                        break;
                    case "observe":
                        HandleObserve();
                        break;
                    case "shutdown":
                        return;
                    default:
                        Respond(new RlResponse { Type = "error", Error = $"unknown_type:{req.Type}" });
                        break;
                }
            }
            catch (Exception ex)
            {
                Respond(new RlResponse { Type = "error", Error = ex.Message });
            }
        }
    }

    static void HandleReset(RlRequest req)
    {
        int seed = req.Seed > 0 ? req.Seed : Random.Shared.Next(1, 100_000);
        int starCount = req.StarCount > 0 ? req.StarCount : 12;
        _maxEpisodeTicks = req.MaxEpisodeTicks > 0 ? req.MaxEpisodeTicks : 2000;
        _curriculumStage = req.CurriculumStage;

        // Curriculum stage adjustments
        switch (_curriculumStage)
        {
            case 0: starCount = Math.Min(starCount, 4); _maxEpisodeTicks = Math.Min(_maxEpisodeTicks, 500); break;
            case 1: starCount = Math.Min(starCount, 8); _maxEpisodeTicks = Math.Min(_maxEpisodeTicks, 1000); break;
            // Stages 2+ use full parameters
        }

        _kernel = new SimKernel(seed);
        GalaxyGenerator.Generate(_kernel.State, starCount, 100f);

        // Create player fleet (WorldLoader.Apply normally does this)
        var state0 = _kernel.State;
        state0.PlayerCredits = 1000;
        if (!state0.Fleets.ContainsKey("fleet_trader_1"))
        {
            var classDef = SimCore.Content.ShipClassContentV0.GetById("corvette");
            state0.Fleets["fleet_trader_1"] = new Fleet
            {
                Id = "fleet_trader_1",
                OwnerId = "player",
                ShipClassId = "corvette",
                State = FleetState.Idle,
                HullHp = classDef?.CoreHull ?? 100,
                HullHpMax = classDef?.CoreHull ?? 100,
                ShieldHp = classDef?.BaseShield ?? 50,
                ShieldHpMax = classDef?.BaseShield ?? 50,
                FuelCurrent = classDef?.BaseFuelCapacity ?? 200,
                FuelCapacity = classDef?.BaseFuelCapacity ?? 200,
            };
        }
        state0.HydrateAfterLoad();

        // Place player at first node
        var startNode = state0.Nodes.Keys.OrderBy(k => k, StringComparer.Ordinal).First();
        _kernel.EnqueueCommand(new PlayerArriveCommand(startNode));
        _kernel.Step();

        // Initialize tracking
        _prevCredits = _kernel.State.PlayerCredits;
        _prevVisited = _kernel.State.PlayerVisitedNodeIds.Count;
        _totalProfit = 0;
        _neighborCache = StateEncoder.GetNeighborNodeIds(_kernel.State, _kernel.State.PlayerLocationNodeId);

        var obs = StateEncoder.Encode(_kernel.State, _maxEpisodeTicks, _neighborCache);
        var mask = StateEncoder.ComputeActionMask(_kernel.State, _neighborCache);

        Respond(new RlResponse
        {
            Type = "reset_ok",
            Obs = obs,
            ActionMask = mask,
            Info = new Dictionary<string, object>
            {
                ["seed"] = seed,
                ["star_count"] = starCount,
                ["node_count"] = _kernel.State.Nodes.Count,
                ["edge_count"] = _kernel.State.Edges.Count,
                ["curriculum_stage"] = _curriculumStage,
            }
        });
    }

    static void HandleStep(RlRequest req)
    {
        if (_kernel == null)
        {
            Respond(new RlResponse { Type = "error", Error = "not_initialized" });
            return;
        }

        var state = _kernel.State;
        long creditsBefore = state.PlayerCredits;

        // Execute action
        var result = ActionDecoder.Execute(_kernel, req.Action, _neighborCache);

        // Refresh neighbor cache after potential movement
        _neighborCache = StateEncoder.GetNeighborNodeIds(state, state.PlayerLocationNodeId);

        // ── Compute reward (dense, multi-component) ──
        float reward = 0f;

        // Profit component (main driver)
        float creditDelta = (state.PlayerCredits - creditsBefore) / 1000f;
        reward += Clamp(creditDelta, -1f, 1f);

        // Trade completion bonus
        if (result.TradeProfit > 0)
            reward += 0.1f;

        // Exploration bonus
        if (result.NewNodeVisited)
            reward += 0.2f;

        // Mission completion bonus
        if (result.MissionCompleted)
            reward += 0.5f;

        // Haven upgrade bonus
        if (result.HavenUpgraded)
            reward += 1.0f;

        // Research completion bonus
        if (result.ResearchCompleted)
            reward += 0.3f;

        // Time penalty (urgency)
        reward -= 0.01f;

        // Terminal penalties
        bool terminated = state.GameResultValue != GameResult.InProgress;
        bool truncated = state.Tick >= _maxEpisodeTicks;

        if (terminated)
        {
            if (state.GameResultValue == GameResult.Death)
                reward -= 10f;
            else if (state.GameResultValue == GameResult.Bankruptcy)
                reward -= 5f;
            else if (state.GameResultValue == GameResult.Victory)
                reward += 20f;
        }

        // Track cumulative
        _totalProfit += state.PlayerCredits - _prevCredits;
        _prevCredits = state.PlayerCredits;

        var obs = StateEncoder.Encode(state, _maxEpisodeTicks, _neighborCache);
        var mask = StateEncoder.ComputeActionMask(state, _neighborCache);

        Respond(new RlResponse
        {
            Type = "step_ok",
            Obs = obs,
            Reward = reward,
            Terminated = terminated,
            Truncated = truncated,
            ActionMask = mask,
            Info = new Dictionary<string, object>
            {
                ["tick"] = state.Tick,
                ["credits"] = state.PlayerCredits,
                ["nodes_visited"] = state.PlayerVisitedNodeIds.Count,
                ["total_profit"] = _totalProfit,
                ["action_label"] = result.ActionLabel,
                ["ticks_consumed"] = result.TicksConsumed,
            }
        });
    }

    static void HandleObserve()
    {
        if (_kernel == null)
        {
            Respond(new RlResponse { Type = "error", Error = "not_initialized" });
            return;
        }

        var obs = StateEncoder.Encode(_kernel.State, _maxEpisodeTicks, _neighborCache);
        var mask = StateEncoder.ComputeActionMask(_kernel.State, _neighborCache);

        Respond(new RlResponse
        {
            Type = "observe_ok",
            Obs = obs,
            ActionMask = mask,
        });
    }

    static void Respond(RlResponse response)
    {
        Console.WriteLine(Protocol.Serialize(response));
        Console.Out.Flush();
    }

    static float Clamp(float v, float min, float max) => v < min ? min : (v > max ? max : v);
}
