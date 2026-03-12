using SimCore.Entities;
using System.Collections.Generic;

namespace SimCore.Content;

// GATE.T18.NARRATIVE.KNOWLEDGE_GRAPH.001: Pre-authored connection definitions
// between discoveries. Connections reveal as "?" when both endpoints are Seen,
// and fully reveal when both are Analyzed. Turns the knowledge graph from a
// record into a puzzle surface.
public static class KnowledgeGraphContentV0
{
    // Connection templates. SourceDiscoveryId and TargetDiscoveryId use
    // pattern tokens that are resolved during world gen:
    //   $KEPLER_1 .. $KEPLER_6 = Kepler chain discoveries
    //   $LOG.<thread>.<num>     = data log discovery sites
    // Direct discovery IDs are used where the connection references
    // specific seeded discoveries.
    public sealed class ConnectionTemplate
    {
        public string TemplateId { get; init; } = "";
        public string SourcePattern { get; init; } = "";
        public string TargetPattern { get; init; } = "";
        public KnowledgeConnectionType ConnectionType { get; init; }
        public string Description { get; init; } = "";
    }

    public static readonly IReadOnlyList<ConnectionTemplate> AllTemplates = new List<ConnectionTemplate>
    {
        // ── Kepler Chain connections ──────────────────────────────
        // Each Kepler piece leads to the next
        new ConnectionTemplate
        {
            TemplateId = "KC.LEAD.001",
            SourcePattern = "$KEPLER_1",
            TargetPattern = "$KEPLER_2",
            ConnectionType = KnowledgeConnectionType.Lead,
            Description = "Matching transponder frequency detected at a second site."
        },
        new ConnectionTemplate
        {
            TemplateId = "KC.LEAD.002",
            SourcePattern = "$KEPLER_2",
            TargetPattern = "$KEPLER_3",
            ConnectionType = KnowledgeConnectionType.Lead,
            Description = "Identical weapon scarring connects these two wrecks."
        },
        new ConnectionTemplate
        {
            TemplateId = "KC.LEAD.003",
            SourcePattern = "$KEPLER_3",
            TargetPattern = "$KEPLER_4",
            ConnectionType = KnowledgeConnectionType.Lead,
            Description = "The same ancient weapon marks appear at a much older site."
        },
        new ConnectionTemplate
        {
            TemplateId = "KC.LEAD.004",
            SourcePattern = "$KEPLER_4",
            TargetPattern = "$KEPLER_5",
            ConnectionType = KnowledgeConnectionType.Lead,
            Description = "Pre-faction ruins contain references to a containment site."
        },
        new ConnectionTemplate
        {
            TemplateId = "KC.ORIGIN.001",
            SourcePattern = "$KEPLER_1",
            TargetPattern = "$KEPLER_3",
            ConnectionType = KnowledgeConnectionType.SameOrigin,
            Description = "Both vessels were searching for the same thing."
        },
        new ConnectionTemplate
        {
            TemplateId = "KC.QUESTION.001",
            SourcePattern = "$KEPLER_5",
            TargetPattern = "$KEPLER_6",
            ConnectionType = KnowledgeConnectionType.LoreFragment,
            Description = "What were both factions looking for?"
        },

        // ── Data Log cross-thread connections ────────────────────
        // Containment ↔ Accommodation (Kesh/Vael debate)
        new ConnectionTemplate
        {
            TemplateId = "DL.LORE.001",
            SourcePattern = "$LOG.CONTAIN.003",
            TargetPattern = "$LOG.ACCOM.005",
            ConnectionType = KnowledgeConnectionType.LoreFragment,
            Description = "Vael's containment scenarios connect to her admission that accommodation has limits."
        },
        // Lattice ↔ Departure (Tal's infrastructure → departure geometry)
        new ConnectionTemplate
        {
            TemplateId = "DL.LORE.002",
            SourcePattern = "$LOG.LATTICE.004",
            TargetPattern = "$LOG.DEPART.003",
            ConnectionType = KnowledgeConnectionType.LoreFragment,
            Description = "Tal's final infrastructure log leads to the departure calculations."
        },
        // Accommodation ↔ EconTopology (species constraint → pentagon design)
        new ConnectionTemplate
        {
            TemplateId = "DL.LORE.003",
            SourcePattern = "$LOG.ACCOM.003",
            TargetPattern = "$LOG.ECON.002",
            ConnectionType = KnowledgeConnectionType.LoreFragment,
            Description = "Senn's species constraint argument informs the pentagon design rationale."
        },
        // Warning ↔ Containment (proof uses containment data)
        new ConnectionTemplate
        {
            TemplateId = "DL.LORE.004",
            SourcePattern = "$LOG.WARN.001",
            TargetPattern = "$LOG.CONTAIN.005",
            ConnectionType = KnowledgeConnectionType.LoreFragment,
            Description = "The finite lifespan proof draws directly from Kesh's containment models."
        },
        // Warning ↔ EconTopology (the ring was designed to outlast them)
        new ConnectionTemplate
        {
            TemplateId = "DL.LORE.005",
            SourcePattern = "$LOG.WARN.003",
            TargetPattern = "$LOG.ECON.004",
            ConnectionType = KnowledgeConnectionType.LoreFragment,
            Description = "The warning logs were left because the ring was designed to sustain — not to inform."
        },
        // Kepler → Containment (the ruin contains LOG.CONTAIN.001)
        new ConnectionTemplate
        {
            TemplateId = "KC.LORE.001",
            SourcePattern = "$KEPLER_5",
            TargetPattern = "$LOG.CONTAIN.001",
            ConnectionType = KnowledgeConnectionType.LoreFragment,
            Description = "The ancient ruin contains the first containment debate fragment."
        },
    };
}
