using System;
using System.Linq;
using SimCore.Intents;

namespace SimCore.Systems;

public static class IntentSystem
{
	public static void Process(SimState state)
	{
		if (state is null) throw new ArgumentNullException(nameof(state));
		if (state.PendingIntents.Count == 0) return;

		var now = state.Tick;

		var due = state.PendingIntents
			.Where(x => x.CreatedTick <= now)
			.OrderBy(x => x.CreatedTick)
			.ThenBy(x => x.Seq)
			.ThenBy(x => x.Kind, StringComparer.Ordinal)
			.ToList();

		if (due.Count == 0) return;

		foreach (var env in due)
		{
			env.Intent?.Apply(state);
		}

		// Remove processed intents deterministically
		var dueSeq = due.Select(x => x.Seq).ToHashSet();
		state.PendingIntents.RemoveAll(x => dueSeq.Contains(x.Seq));
	}
}
