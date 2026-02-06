namespace SimCore.Intents;

public interface IIntent
{
	string Kind { get; }
	void Apply(SimState state);
}
