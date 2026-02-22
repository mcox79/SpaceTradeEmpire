using SimCore.Commands;
using SimCore.Systems;
using SimCore.Programs;
using System.Collections.Concurrent;

namespace SimCore;

public class SimKernel
{
    private SimState _state;
    private ConcurrentQueue<ICommand> _commandQueue = new();
    private ConcurrentQueue<SimCore.Intents.IIntent> _intentQueue = new();
    public SimState State => _state;

    public SimKernel(int seed, string? tweakConfigJsonOverride = null)
    {
        _state = new SimState(seed);

        // Gate: save_seed_identity
        // SimState does not necessarily expose seed as a readable member. Attach it for persistence.
        SerializationSystem.AttachSeed(_state, seed);

        // GATE.X.TWEAKS.DATA.001
        // Deterministic tweak loading (override wins, else stable defaults).
        _state.LoadTweaksFromJsonOverride(tweakConfigJsonOverride);
    }

    public void EnqueueCommand(ICommand cmd)
    {
        _commandQueue.Enqueue(cmd);
    }

    public void EnqueueIntent(SimCore.Intents.IIntent intent)
    {
        _intentQueue.Enqueue(intent);
    }

    public void Step()
    {
        while (_commandQueue.TryDequeue(out var cmd))
        {
            cmd.Execute(_state);
        }

        while (_intentQueue.TryDequeue(out var intent))
        {
            // Use SimState's deterministic intent wrapper.
            _state.EnqueueIntent(intent);
        }

        // Resolve in-flight inventory arrivals first.
        LaneFlowSystem.Process(_state);

        // Programs emit intents only (no direct ledger mutation).
        ProgramSystem.Process(_state);

        // Convert intents to commands.
        IntentSystem.Process(_state);

        // Apply fleet movement/state transitions.
        MovementSystem.Process(_state);

        // Compute buffer shortages and assign logistics jobs (produces fleet jobs).
        LogisticsSystem.Process(_state);

        // Consume upkeep and produce outputs.
        IndustrySystem.Process(_state);

        _state.AdvanceTick();
    }

    public string SaveToString()
    {
        return SerializationSystem.Serialize(_state);
    }

    public void LoadFromString(string data)
    {
        var loaded = SerializationSystem.Deserialize(data);
        if (loaded != null)
        {
            _state = loaded;

            // Critical for determinism + save/load: ensure derived/transient fields are re-hydrated.
            _state.HydrateAfterLoad();
        }
    }
}
