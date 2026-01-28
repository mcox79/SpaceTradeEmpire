using SimCore.Commands;
using SimCore.Systems;

namespace SimCore;

public class SimKernel
{
    private SimState _state;
    private Queue<ICommand> _commandQueue = new();

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
        // 1. Process Input
        while (_commandQueue.TryDequeue(out var cmd))
        {
            cmd.Execute(_state);
        }

        // 2. Systems Execution [cite: 1442]
        MovementSystem.Process(_state);

        // 3. Tick Advance
        _state.AdvanceTick();
    }
}