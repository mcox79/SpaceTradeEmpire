using SimCore.Systems;

namespace SimCore.Commands;

// GATE.S7.DIPLOMACY.TREATY.001: Command to propose a diplomatic act.
public sealed class ProposeActCommand : ICommand
{
    public string FactionId { get; }
    public string ActId { get; } // For accepting/rejecting existing proposals

    public enum ProposalAction { ProposeTreaty, AcceptProposal, RejectProposal }
    public ProposalAction Action { get; }

    public ProposeActCommand(string factionId, ProposalAction action, string actId = "")
    {
        FactionId = factionId ?? "";
        Action = action;
        ActId = actId ?? "";
    }

    public void Execute(SimState state)
    {
        if (state is null) return;

        switch (Action)
        {
            case ProposalAction.ProposeTreaty:
                DiplomacySystem.ProposeTreaty(state, FactionId);
                break;
            case ProposalAction.AcceptProposal:
                DiplomacySystem.AcceptProposal(state, ActId);
                break;
            case ProposalAction.RejectProposal:
                DiplomacySystem.RejectProposal(state, ActId);
                break;
        }
    }
}
