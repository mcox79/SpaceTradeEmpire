namespace SimCore;
public interface ICommand
{
    void Execute(SimState state);
}