using System.Collections.Generic;

namespace SimCore.Content;

// GATE.T41.ANOMALY_CHAIN.CONTENT.001: Three authored anomaly chain templates.
// Follow KeplerChainContentV0 pattern: static readonly list of chain templates.
// Each chain has escalating narrative with breadcrumb leads between steps.
public static class AnomalyChainContentV0
{
    public sealed class ChainTemplate
    {
        public string ChainId { get; init; } = "";
        public string Title { get; init; } = "";
        public IReadOnlyList<StepTemplate> Steps { get; init; } = new List<StepTemplate>();
    }

    public sealed class StepTemplate
    {
        public int StepIndex { get; init; }
        public string DiscoveryKind { get; init; } = "";
        public int MinHopsFromStarter { get; init; }
        public int MaxHopsFromStarter { get; init; }
        public string NarrativeText { get; init; } = "";
        public string LeadText { get; init; } = "";
        public Dictionary<string, int> LootOverrides { get; init; } = new();
    }

    // Chain 1: Valorin Expedition (3 steps)
    // A Valorin scouting expedition that ended in disaster. Follow the trail to an ancient defense installation.
    public static readonly ChainTemplate ValorinExpedition = new()
    {
        ChainId = "CHAIN.VALORIN_EXPEDITION",
        Title = "The Valorin Expedition",
        Steps = new List<StepTemplate>
        {
            new()
            {
                StepIndex = 0,
                DiscoveryKind = "DERELICT",
                MinHopsFromStarter = 1,
                MaxHopsFromStarter = 2,
                NarrativeText = "A Valorin scout vessel, hull breached by weapon fire of unknown origin. The navigation computer is intact. Final log entry: 'Signal source confirmed at bearing 217. Proceeding to investigate. Commander Valorin-Kesh authorizes full sensor sweep.'",
                LeadText = "The scout's last bearing points toward a signal source two jumps further out.",
                LootOverrides = new Dictionary<string, int> { { "credits", 50 } }
            },
            new()
            {
                StepIndex = 1,
                DiscoveryKind = "SIGNAL",
                MinHopsFromStarter = 2,
                MaxHopsFromStarter = 4,
                NarrativeText = "An automated emergency beacon, still transmitting after centuries. The frequency matches the signal referenced in the scout's logs. But this beacon isn't Valorin — it's far older. The distress code uses a mathematical language no known faction developed.",
                LeadText = "The beacon's origin coordinates point deeper into the void. Something built this. Something that needed help.",
                LootOverrides = new Dictionary<string, int> { { "credits", 80 }, { "exotic_matter", 1 } }
            },
            new()
            {
                StepIndex = 2,
                DiscoveryKind = "RUIN",
                MinHopsFromStarter = 3,
                MaxHopsFromStarter = 5,
                NarrativeText = "An ancient defense installation, dormant but intact. The same weapon signatures that destroyed the Valorin scout. Automated sentries, designed to protect something beneath. The installation predates both Valorin and Communion by millennia. Deep scans reveal containment architecture — the same geometric patterns found in thread infrastructure.",
                LeadText = "",
                LootOverrides = new Dictionary<string, int> { { "credits", 200 }, { "exotic_matter", 3 } }
            }
        }
    };

    // Chain 2: Communion Frequency (4 steps)
    // A strange hum detected by Communion listening posts. It leads to a prototype accommodation device.
    public static readonly ChainTemplate CommunionFrequency = new()
    {
        ChainId = "CHAIN.COMMUNION_FREQUENCY",
        Title = "The Communion Frequency",
        Steps = new List<StepTemplate>
        {
            new()
            {
                StepIndex = 0,
                DiscoveryKind = "SIGNAL",
                MinHopsFromStarter = 2,
                MaxHopsFromStarter = 3,
                NarrativeText = "A low-frequency hum, just below standard sensor thresholds. Communion listening posts logged it decades ago as background noise. But it's not noise — it's modulated. Something is transmitting, very slowly, on a wavelength that predates radio astronomy.",
                LeadText = "The hum intensifies along a specific vector. There's a source.",
                LootOverrides = new Dictionary<string, int> { { "credits", 40 } }
            },
            new()
            {
                StepIndex = 1,
                DiscoveryKind = "SIGNAL",
                MinHopsFromStarter = 3,
                MaxHopsFromStarter = 4,
                NarrativeText = "A Communion listening post, abandoned but functional. Whoever staffed this was tracking the same frequency. Their final report: 'The signal is not broadcast. It is resonance. The source is vibrating at a frequency that makes space itself carry the message. We believe it is a calibration tone.'",
                LeadText = "The listening post's directional array points further into deep space. Calibration implies something being tuned.",
                LootOverrides = new Dictionary<string, int> { { "credits", 60 } }
            },
            new()
            {
                StepIndex = 2,
                DiscoveryKind = "RUIN",
                MinHopsFromStarter = 4,
                MaxHopsFromStarter = 5,
                NarrativeText = "A structure embedded in an asteroid, almost invisible to conventional sensors. Inside: resonance chambers. The walls are shaped to amplify the frequency, focusing it to a point at the center. The calibration tone wasn't a message — it was alignment. Something here was being tuned to match thread geometry.",
                LeadText = "The resonance chambers converge on a deeper structure. The calibration has a purpose.",
                LootOverrides = new Dictionary<string, int> { { "credits", 100 }, { "exotic_matter", 2 } }
            },
            new()
            {
                StepIndex = 3,
                DiscoveryKind = "DERELICT",
                MinHopsFromStarter = 5,
                MaxHopsFromStarter = 7,
                NarrativeText = "At the convergence point: a device. Not a weapon. Not a shield. An accommodation prototype — designed to reshape local thread geometry rather than contain it. The same philosophy the Communion espouses, but engineered millennia before they existed. Someone tried this approach before. The device is inactive. Its power source is depleted. But the design is intact.",
                LeadText = "",
                LootOverrides = new Dictionary<string, int> { { "credits", 250 }, { "exotic_matter", 5 } }
            }
        }
    };

    // Chain 3: Pentagon Audit (5 steps)
    // A trade anomaly leads to evidence of the Pentagon's suppression of pre-schism knowledge.
    public static readonly ChainTemplate PentagonAudit = new()
    {
        ChainId = "CHAIN.PENTAGON_AUDIT",
        Title = "The Pentagon Audit",
        Steps = new List<StepTemplate>
        {
            new()
            {
                StepIndex = 0,
                DiscoveryKind = "SIGNAL",
                MinHopsFromStarter = 1,
                MaxHopsFromStarter = 2,
                NarrativeText = "Market data from this node shows a statistical anomaly: certain rare minerals are consistently underpriced here, as if someone is deliberately suppressing their market value. The pattern matches no known economic model — unless the goal isn't profit, but concealment.",
                LeadText = "The price suppression pattern originates from a specific supplier. Their shipping routes lead outward.",
                LootOverrides = new Dictionary<string, int> { { "credits", 30 } }
            },
            new()
            {
                StepIndex = 1,
                DiscoveryKind = "RUIN",
                MinHopsFromStarter = 2,
                MaxHopsFromStarter = 3,
                NarrativeText = "An abandoned mineral survey site. The extracted samples are still here — tagged with Pentagon Concord classification markers. These minerals have unusual thread-resonant properties. The survey was thorough, professional, and then abruptly terminated. All data was marked for destruction. Someone didn't finish the job.",
                LeadText = "The survey data references a Concord intel vessel that collected the findings. Its last known position is logged.",
                LootOverrides = new Dictionary<string, int> { { "credits", 60 } }
            },
            new()
            {
                StepIndex = 2,
                DiscoveryKind = "DERELICT",
                MinHopsFromStarter = 3,
                MaxHopsFromStarter = 4,
                NarrativeText = "The Concord intel vessel, scuttled. Self-destruct charges fired, but the hull survived — Concord engineering. Inside: encrypted data cores. Most are wiped. One survived. It contains survey reports from dozens of sites like this — all documenting thread-resonant minerals, all ordered destroyed by the same authority: 'Directorate of Equilibrium Maintenance.'",
                LeadText = "The encrypted data references suppression equipment deployed to one of the survey sites. The coordinates are intact.",
                LootOverrides = new Dictionary<string, int> { { "credits", 100 }, { "exotic_matter", 2 } }
            },
            new()
            {
                StepIndex = 3,
                DiscoveryKind = "RUIN",
                MinHopsFromStarter = 4,
                MaxHopsFromStarter = 5,
                NarrativeText = "Suppression equipment: devices designed to dampen thread resonance in the surrounding area. They're still active, running on minimal power, making this entire region appear unremarkable to standard sensors. The Pentagon has been hiding these sites — not from enemies, but from everyone. The equipment predates the current Pentagon leadership by generations.",
                LeadText = "The suppression array's maintenance logs reference a central archive. The coordinates were never encrypted — they didn't expect anyone to get this far.",
                LootOverrides = new Dictionary<string, int> { { "credits", 150 }, { "exotic_matter", 3 } }
            },
            new()
            {
                StepIndex = 4,
                DiscoveryKind = "RUIN",
                MinHopsFromStarter = 5,
                MaxHopsFromStarter = 7,
                NarrativeText = "Oruth's Archive. Named for the Sennai researcher who first mapped thread containment geometry — before the schism, before Valorin and Communion existed as separate philosophies. This archive contains the original research: containment AND accommodation, studied as complementary approaches by a unified civilization. The Pentagon didn't suppress this knowledge to protect anyone. They suppressed it because it proves the schism was unnecessary.",
                LeadText = "",
                LootOverrides = new Dictionary<string, int> { { "credits", 400 }, { "exotic_matter", 8 } }
            }
        }
    };

    public static readonly IReadOnlyList<ChainTemplate> AllChains = new List<ChainTemplate>
    {
        ValorinExpedition,
        CommunionFrequency,
        PentagonAudit
    };
}
