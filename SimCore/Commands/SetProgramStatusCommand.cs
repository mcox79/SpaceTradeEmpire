using SimCore.Programs;

namespace SimCore.Commands;

public sealed class SetProgramStatusCommand : ICommand
{
        public string ProgramId { get; set; } = "";
        public ProgramStatus Status { get; set; } = ProgramStatus.Paused;

        public SetProgramStatusCommand(string programId, ProgramStatus status)
        {
                ProgramId = programId ?? "";
                Status = status;
        }

        public void Execute(SimState state)
        {
                if (state is null) return;
                if (string.IsNullOrWhiteSpace(ProgramId)) return;
                if (state.Programs is null) return;

                if (!state.Programs.Instances.TryGetValue(ProgramId, out var p)) return;

                p.Status = Status;

                // If starting, ensure it can run on/after now.
                if (Status == ProgramStatus.Running)
                {
                        if (p.NextRunTick < state.Tick) p.NextRunTick = state.Tick;
                }
        }
}
