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
    public SimSnapshot CaptureSnapshot()
    {
        return new SimSnapshot(
            _state.PlayerLocationNodeId,
            string.IsNullOrWhiteSpace(_state.PlayerSelectedDestinationNodeId) ? null : _state.PlayerSelectedDestinationNodeId
        );
    }

    public void RestoreSnapshot(SimSnapshot snapshot)
    {
        _state.PlayerLocationNodeId = snapshot.CurrentNodeId;
        _state.PlayerSelectedDestinationNodeId = snapshot.SelectedDestinationNodeId ?? "";
    }

}
