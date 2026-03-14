using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimCore.Entities;

// GATE.S7.DIPLOMACY.FRAMEWORK.001: Diplomatic act types.
public enum DiplomaticActType
{
    Treaty = 0,     // Trade agreement: tariff reduction + safe passage
    Bounty = 1,     // Kill target: faction posts bounty on hostile NPC
    Sanction = 2,   // Punitive: tariff increase + trade restrictions
    Proposal = 3    // Pending proposal from faction (accept/reject)
}

// GATE.S7.DIPLOMACY.FRAMEWORK.001: Status of a diplomatic act.
public enum DiplomaticActStatus
{
    Pending = 0,    // Proposal awaiting response
    Active = 1,     // In effect
    Completed = 2,  // Bounty claimed or treaty expired
    Violated = 3,   // Treaty broken by player action
    Rejected = 4    // Proposal rejected
}

// GATE.S7.DIPLOMACY.FRAMEWORK.001: A diplomatic act between player and a faction.
public class DiplomaticAct
{
    [JsonInclude] public string Id { get; set; } = "";
    [JsonInclude] public string FactionId { get; set; } = "";
    [JsonInclude] public DiplomaticActType ActType { get; set; } = DiplomaticActType.Treaty;
    [JsonInclude] public DiplomaticActStatus Status { get; set; } = DiplomaticActStatus.Pending;
    [JsonInclude] public int CreatedTick { get; set; }
    [JsonInclude] public int ExpiryTick { get; set; } = -1; // -1 = no expiry

    // Treaty fields
    [JsonInclude] public int TariffReductionBps { get; set; } // Basis points tariff reduction
    [JsonInclude] public bool SafePassage { get; set; }       // Patrol won't attack

    // Bounty fields
    [JsonInclude] public string BountyTargetFleetId { get; set; } = "";
    [JsonInclude] public int BountyRewardCredits { get; set; }
    [JsonInclude] public int BountyRewardRep { get; set; }

    // Sanction fields
    [JsonInclude] public int SanctionTariffIncreaseBps { get; set; }
    [JsonInclude] public int SanctionRepPenalty { get; set; }
}
