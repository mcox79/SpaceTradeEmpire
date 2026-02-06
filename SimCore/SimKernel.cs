using SimCore.Commands;
using SimCore.Systems;
using System.Collections.Concurrent;

namespace SimCore;

public class SimKernel
{
    private SimState _state;
    private ConcurrentQueue<ICommand> _commandQueue = new();
    private ConcurrentQueue<SimCore.Intents.IIntent> _intentQueue = new();
    public SimState State => _state; 

    public SimKernel(int seed)
    {
        _state = new SimState(seed);
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
            var seq = _state.NextIntentSeq;
            _state.NextIntentSeq = checked(_state.NextIntentSeq + 1);

            _state.PendingIntents.Add(new SimCore.Intents.IntentEnvelope
            {
                Seq = seq,
                CreatedTick = _state.Tick,
                Kind = intent.Kind,
                Intent = intent
            });
        }

        LaneFlowSystem.Process(_state);
        IntentSystem.Process(_state);

        MovementSystem.Process(_state);

        IndustrySystem.Process(_state); // SLICE 2: LOGIC ADDED
        _state.AdvanceTick();
    }

    public string SaveToString()
    {
        return SerializationSystem.Serialize(_state);
    }

    public void LoadFromString(string data)
    {
        var loaded = SerializationSystem.Deserialize(data);
        if (loaded != null) _state = loaded;
    }
}