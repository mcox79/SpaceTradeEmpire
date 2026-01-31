using SimCore.Commands;
using SimCore.Systems;
using System.Collections.Concurrent;

namespace SimCore;

public class SimKernel
{
    private SimState _state;
    private ConcurrentQueue<ICommand> _commandQueue = new();
    public SimState State => _state;

    public SimKernel(int seed)
    {
        _state = new SimState(seed);
    }

    public void EnqueueCommand(ICommand cmd)
    {
        _commandQueue.Enqueue(cmd);
    }

    public void Step()
    {
        while (_commandQueue.TryDequeue(out var cmd))
        {
            cmd.Execute(_state);
        }
        MovementSystem.Process(_state);
        FractureSystem.Process(_state);
        ContainmentSystem.Process(_state);
        LogisticsSystem.Process(_state);
        IndustrySystem.Process(_state);
        MarketSystem.Process(_state); // SLICE 3: Economic Heat Decay
        _state.AdvanceTick();
    }

    public string SaveToString()
    {
        return SerializationSystem.Serialize(_state);
    }

    public void LoadFromString(string data)
    {
        _state = SerializationSystem.Deserialize(data);
    }
}