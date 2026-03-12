using SimCore.Entities;
using System.Collections.Generic;

namespace SimCore.Content;

// GATE.T18.NARRATIVE.KEPLER_CHAIN.001: Six-piece proof-of-concept narrative chain.
// Must work across procedural seeds. Progressive distance placement from starter.
//
// The chain:
//   1. Derelict at starter-adjacent: Valorin scout wreck, 300 yrs old
//   2. Lead → Signal at 2-hop: matching transponder frequency
//   3. Signal at 2-hop: second Valorin vessel, Communion weapon marks
//   4. Lead → Ruin at 3-hop: same ancient weapon marks
//   5. Ruin at 3-hop: pre-dates both factions by millennia. Contains LOG.CONTAIN.001
//   6. Knowledge graph question: "What were both factions looking for?"
//
// The chain reveals that Valorin and Communion have both been searching for the
// same ancient sites — independently, for centuries. Neither knows the other is looking.
public static class KeplerChainContentV0
{
    public sealed class ChainPiece
    {
        public string PieceId { get; init; } = "";
        public int SequenceIndex { get; init; }
        public string DiscoveryKind { get; init; } = "";  // DERELICT, SIGNAL, RUIN
        public string Title { get; init; } = "";
        public string Description { get; init; } = "";
        public int MinHopsFromStarter { get; init; }
        public int MaxHopsFromStarter { get; init; }
        public string? LinkedLogId { get; init; }
    }

    public const string KindDerelict = "KEPLER_DERELICT";
    public const string KindSignal = "KEPLER_SIGNAL";
    public const string KindRuin = "KEPLER_RUIN";
    public const string KindQuestion = "KEPLER_QUESTION";

    public static readonly IReadOnlyList<ChainPiece> AllPieces = new List<ChainPiece>
    {
        new ChainPiece
        {
            PieceId = "KEPLER.001",
            SequenceIndex = 0,
            DiscoveryKind = KindDerelict,
            Title = "Valorin Scout Wreck",
            Description = "A Valorin scout vessel, roughly 300 years old. Navigation logs intact. The last entry references a transponder signal from an unknown source.",
            MinHopsFromStarter = 1,
            MaxHopsFromStarter = 2,
            LinkedLogId = null
        },
        new ChainPiece
        {
            PieceId = "KEPLER.002",
            SequenceIndex = 1,
            DiscoveryKind = KindSignal,
            Title = "Transponder Echo",
            Description = "The transponder frequency from the Valorin scout matches a signal here. A second vessel — not Valorin. Communion weapon marks on the hull.",
            MinHopsFromStarter = 2,
            MaxHopsFromStarter = 3,
            LinkedLogId = null
        },
        new ChainPiece
        {
            PieceId = "KEPLER.003",
            SequenceIndex = 2,
            DiscoveryKind = KindDerelict,
            Title = "Communion Survey Vessel",
            Description = "A Communion survey ship, destroyed by weapons that match neither faction's current arsenal. The same ancient weapon scarring found at the scout wreck.",
            MinHopsFromStarter = 2,
            MaxHopsFromStarter = 4,
            LinkedLogId = null
        },
        new ChainPiece
        {
            PieceId = "KEPLER.004",
            SequenceIndex = 3,
            DiscoveryKind = KindRuin,
            Title = "Pre-Faction Ruin",
            Description = "This structure pre-dates both factions by millennia. The same weapon marks. Whatever destroyed those ships was defending this place.",
            MinHopsFromStarter = 3,
            MaxHopsFromStarter = 5,
            LinkedLogId = null
        },
        new ChainPiece
        {
            PieceId = "KEPLER.005",
            SequenceIndex = 4,
            DiscoveryKind = KindRuin,
            Title = "Containment Archive",
            Description = "Deep inside the ruin: a data archive. The language is neither Valorin nor Communion. It's older. Much older. The first containment debate is stored here.",
            MinHopsFromStarter = 4,
            MaxHopsFromStarter = 6,
            LinkedLogId = "LOG.CONTAIN.001"
        },
        new ChainPiece
        {
            PieceId = "KEPLER.006",
            SequenceIndex = 5,
            DiscoveryKind = KindQuestion,
            Title = "The Question",
            Description = "Both factions have been searching for these sites, independently, for centuries. Neither knows the other is looking. What were they both looking for?",
            MinHopsFromStarter = 5,
            MaxHopsFromStarter = 7,
            LinkedLogId = null
        },
    };
}
