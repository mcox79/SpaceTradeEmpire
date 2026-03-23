#nullable enable

using Godot;
using SimCore;
using SimCore.Entities;

namespace SpaceTradeEmpire.Bridge;

// GATE.S8.WIN.BRIDGE.001: Endgame state queries for GameShell.
public partial class SimBridge
{
    // GetGameResultV0: Returns current game result state.
    // InProgress (0), Victory (1), Death (2), Bankruptcy (3).
    private Godot.Collections.Dictionary _cachedGameResultV0 = new();

    public Godot.Collections.Dictionary GetGameResultV0()
    {
        TryExecuteSafeRead(state =>
        {
            var d = new Godot.Collections.Dictionary
            {
                ["result"] = (int)state.GameResultValue,
                ["result_name"] = state.GameResultValue.ToString(),
                ["is_terminal"] = state.GameResultValue != GameResult.InProgress,
                ["chosen_path"] = (int)state.Haven.ChosenEndgamePath,
                ["chosen_path_name"] = state.Haven.ChosenEndgamePath.ToString(),
            };

            lock (_snapshotLock) { _cachedGameResultV0 = d; }
        }, 0);

        lock (_snapshotLock) { return _cachedGameResultV0; }
    }

    // GetEndgameProgressV0: Returns per-path progress (0-100%) + requirements checklist.
    // Only meaningful when an endgame path has been chosen.
    private Godot.Collections.Dictionary _cachedEndgameProgressV0 = new();

    public Godot.Collections.Dictionary GetEndgameProgressV0()
    {
        TryExecuteSafeRead(state =>
        {
            var ep = state.EndgameProgress;
            var d = new Godot.Collections.Dictionary
            {
                ["completion_percent"] = ep.CompletionPercent,
                ["haven_tier_met"] = ep.HavenTierMet,
                ["haven_tier_current"] = ep.HavenTierCurrent,
                ["haven_tier_required"] = ep.HavenTierRequired,
                ["faction_rep1_met"] = ep.FactionRep1Met,
                ["faction_rep1_id"] = ep.FactionRep1Id,
                ["faction_rep1_current"] = ep.FactionRep1Current,
                ["faction_rep1_required"] = ep.FactionRep1Required,
                ["faction_rep2_met"] = ep.FactionRep2Met,
                ["faction_rep2_id"] = ep.FactionRep2Id,
                ["faction_rep2_current"] = ep.FactionRep2Current,
                ["faction_rep2_required"] = ep.FactionRep2Required,
                ["fragment1_met"] = ep.Fragment1Met,
                ["fragment1_id"] = ep.Fragment1Id,
                ["fragment2_met"] = ep.Fragment2Met,
                ["fragment2_id"] = ep.Fragment2Id,
                ["revelations_current"] = ep.RevelationsCurrent,
                ["revelations_required"] = ep.RevelationsRequired,
                ["revelations_met"] = ep.RevelationsMet,
            };

            lock (_snapshotLock) { _cachedEndgameProgressV0 = d; }
        }, 0);

        lock (_snapshotLock) { return _cachedEndgameProgressV0; }
    }

    // GetLossInfoV0: Returns loss reason and final stats when game ends in death/bankruptcy.
    private Godot.Collections.Dictionary _cachedLossInfoV0 = new();

    public Godot.Collections.Dictionary GetLossInfoV0()
    {
        TryExecuteSafeRead(state =>
        {
            string lossReason = state.GameResultValue switch
            {
                GameResult.Death => "death",
                GameResult.Bankruptcy => "bankruptcy",
                _ => ""
            };

            // Gather final stats for loss screen.
            int nodesVisited = state.PlayerVisitedNodeIds.Count;
            int missionsCompleted = state.Missions.CompletedMissionIds.Count;
            string shipClass = "corvette";
            int modulesInstalled = 0;

            if (state.Fleets.TryGetValue("fleet_trader_1", out var fleet))
            {
                shipClass = fleet.ShipClassId;
                foreach (var slot in fleet.Slots)
                {
                    if (!string.IsNullOrEmpty(slot.InstalledModuleId))
                        modulesInstalled++;
                }
            }

            var d = new Godot.Collections.Dictionary
            {
                ["loss_reason"] = lossReason,
                ["final_credits"] = state.PlayerCredits,
                ["final_tick"] = state.Tick,
                ["nodes_visited"] = nodesVisited,
                ["missions_completed"] = missionsCompleted,
                ["ship_class"] = shipClass,
                ["modules_installed"] = modulesInstalled,
                ["captain_name"] = state.CaptainName,
                ["chosen_path"] = state.Haven.ChosenEndgamePath.ToString(),
                ["haven_tier"] = (int)state.Haven.Tier,
                ["revelation_count"] = state.StoryState.RevelationCount,
            };

            lock (_snapshotLock) { _cachedLossInfoV0 = d; }
        }, 0);

        lock (_snapshotLock) { return _cachedLossInfoV0; }
    }

    // ForceSetGameResultV0: Bot helper to force a game result (for endgame coverage testing).
    // Does NOT trigger scene changes — just sets the SimState value.
    // resultCode: 0=InProgress, 1=Victory, 2=Death, 3=Bankruptcy
    public void ForceSetGameResultV0(int resultCode)
    {
        _stateLock.EnterWriteLock();
        try
        {
            _kernel.State.GameResultValue = (GameResult)resultCode;
        }
        finally { _stateLock.ExitWriteLock(); }
    }

    // GetVictoryInfoV0: Returns victory path info and final stats for epilogue.
    private Godot.Collections.Dictionary _cachedVictoryInfoV0 = new();

    public Godot.Collections.Dictionary GetVictoryInfoV0()
    {
        TryExecuteSafeRead(state =>
        {
            int nodesVisited = state.PlayerVisitedNodeIds.Count;
            int missionsCompleted = state.Missions.CompletedMissionIds.Count;
            string shipClass = "corvette";
            int modulesInstalled = 0;
            int fragmentsCollected = state.AdaptationFragments.Count;

            if (state.Fleets.TryGetValue("fleet_trader_1", out var fleet))
            {
                shipClass = fleet.ShipClassId;
                foreach (var slot in fleet.Slots)
                {
                    if (!string.IsNullOrEmpty(slot.InstalledModuleId))
                        modulesInstalled++;
                }
            }

            var d = new Godot.Collections.Dictionary
            {
                ["chosen_path"] = state.Haven.ChosenEndgamePath.ToString(),
                ["chosen_path_id"] = (int)state.Haven.ChosenEndgamePath,
                ["final_credits"] = state.PlayerCredits,
                ["final_tick"] = state.Tick,
                ["nodes_visited"] = nodesVisited,
                ["missions_completed"] = missionsCompleted,
                ["ship_class"] = shipClass,
                ["modules_installed"] = modulesInstalled,
                ["captain_name"] = state.CaptainName,
                ["haven_tier"] = (int)state.Haven.Tier,
                ["revelation_count"] = state.StoryState.RevelationCount,
                ["fragments_collected"] = fragmentsCollected,
                ["pentagon_cascade"] = state.StoryState.PentagonCascadeActive,
            };

            lock (_snapshotLock) { _cachedVictoryInfoV0 = d; }
        }, 0);

        lock (_snapshotLock) { return _cachedVictoryInfoV0; }
    }
}
