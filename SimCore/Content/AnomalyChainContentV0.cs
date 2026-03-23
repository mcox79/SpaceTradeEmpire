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

    // GATE.T48.ANOMALY.CHAIN_CONTENT.001: Chain 4 — Derelict Signal (3 steps, salvage narrative).
    public static readonly ChainTemplate DerelictSignal = new()
    {
        ChainId = "CHAIN.DERELICT_SIGNAL",
        Title = "The Derelict Signal",
        Steps = new List<StepTemplate>
        {
            new()
            {
                StepIndex = 0,
                DiscoveryKind = "SIGNAL",
                MinHopsFromStarter = 1,
                MaxHopsFromStarter = 2,
                NarrativeText = "A repeating distress signal on a deprecated frequency. The pattern is automated — a ship's emergency beacon, still cycling after what must be decades. The signal strength suggests a vessel of considerable mass, drifting in the gap between trade lanes.",
                LeadText = "Triangulating the signal source reveals it's moving, slowly, on a predictable drift trajectory.",
                LootOverrides = new Dictionary<string, int> { { "credits", 40 }, { "salvaged_tech", 1 } }
            },
            new()
            {
                StepIndex = 1,
                DiscoveryKind = "DERELICT",
                MinHopsFromStarter = 2,
                MaxHopsFromStarter = 3,
                NarrativeText = "A bulk carrier, hull designation scraped clean. The cargo bays are empty — professionally stripped, not looted. Someone salvaged this vessel methodically and then set it adrift. But they missed the engineering section. Deep in the reactor housing: a sealed compartment with environmental controls still running on backup power.",
                LeadText = "Inside the sealed compartment: navigation data pointing to a rendezvous coordinate. Someone was using derelicts as dead drops.",
                LootOverrides = new Dictionary<string, int> { { "credits", 80 }, { "salvaged_tech", 2 } }
            },
            new()
            {
                StepIndex = 2,
                DiscoveryKind = "RUIN",
                MinHopsFromStarter = 3,
                MaxHopsFromStarter = 5,
                NarrativeText = "The rendezvous point: a hollowed asteroid converted into a warehouse. Racks of sealed containers, each tagged with coordinates of a different derelict in the region. This is a salvage operation's central depot — and it's been abandoned recently. The last log entry: 'Thread activity increasing. Pulling out. Coordinates of remaining targets encrypted and transmitted to [REDACTED]. Payment received.' Whoever ran this operation knew where every derelict in this sector was located.",
                LeadText = "",
                LootOverrides = new Dictionary<string, int> { { "credits", 200 }, { "salvaged_tech", 5 } }
            }
        }
    };

    // GATE.T48.ANOMALY.CHAIN_CONTENT.001: Chain 5 — Precursor Echo (5 steps, Haven-adjacent lore).
    public static readonly ChainTemplate PrecursorEcho = new()
    {
        ChainId = "CHAIN.PRECURSOR_ECHO",
        Title = "The Precursor Echo",
        Steps = new List<StepTemplate>
        {
            new()
            {
                StepIndex = 0,
                DiscoveryKind = "SIGNAL",
                MinHopsFromStarter = 1,
                MaxHopsFromStarter = 2,
                NarrativeText = "A faint harmonic in the background radiation — not natural, but not from any known transmitter. The frequency matches theoretical models of thread resonance that predate both containment and accommodation philosophies. Something ancient is still vibrating.",
                LeadText = "The harmonic has a directional component. Following it deeper into unstable space.",
                LootOverrides = new Dictionary<string, int> { { "credits", 30 } }
            },
            new()
            {
                StepIndex = 1,
                DiscoveryKind = "RUIN",
                MinHopsFromStarter = 2,
                MaxHopsFromStarter = 3,
                NarrativeText = "A structure built into the crust of a rogue planetoid. The architecture is neither Valorin defensive nor Communion adaptive — it predates the schism entirely. Inside: resonance chambers shaped to amplify the same harmonic. The walls are covered in geometric patterns that describe thread topology with mathematical precision.",
                LeadText = "The patterns describe a network of linked sites. This is one node in a larger system.",
                LootOverrides = new Dictionary<string, int> { { "credits", 60 }, { "exotic_matter", 1 } }
            },
            new()
            {
                StepIndex = 2,
                DiscoveryKind = "SIGNAL",
                MinHopsFromStarter = 3,
                MaxHopsFromStarter = 4,
                NarrativeText = "A second node in the network, broadcasting a different harmonic that complements the first. Together, the two signals create an interference pattern. The pattern encodes spatial coordinates — a map of local thread geometry, updated in real-time. Someone built a distributed sensing network to monitor thread behavior.",
                LeadText = "The combined signal points to a central hub where all harmonics converge.",
                LootOverrides = new Dictionary<string, int> { { "credits", 80 }, { "exotic_matter", 2 } }
            },
            new()
            {
                StepIndex = 3,
                DiscoveryKind = "RUIN",
                MinHopsFromStarter = 4,
                MaxHopsFromStarter = 6,
                NarrativeText = "The hub — a station built around a natural thread nexus. The structure channels thread energy through engineered pathways. Not containment. Not accommodation. Observation. The builders didn't want to control the threads — they wanted to understand them. Their data archives are intact: millennia of thread behavior observations, pattern analysis, prediction models.",
                LeadText = "The archives reference a final installation — a prototype built using these observations.",
                LootOverrides = new Dictionary<string, int> { { "credits", 150 }, { "exotic_matter", 3 } }
            },
            new()
            {
                StepIndex = 4,
                DiscoveryKind = "RUIN",
                MinHopsFromStarter = 5,
                MaxHopsFromStarter = 7,
                NarrativeText = "The prototype: a device designed to harmonize with thread geometry rather than resist or redirect it. It doesn't contain or accommodate — it sings in tune. The implications shake the foundation of both Valorin and Communion doctrine. The threads aren't a force to be controlled or accepted. They're a medium to be played. Haven's own resonance architecture echoes this same principle, though its builders may not have known why it worked.",
                LeadText = "",
                LootOverrides = new Dictionary<string, int> { { "credits", 350 }, { "exotic_matter", 6 } }
            }
        }
    };

    // GATE.T48.ANOMALY.CHAIN_CONTENT.001: Chain 6 — Biological Drift (4 steps, Chitin territory).
    public static readonly ChainTemplate BiologicalDrift = new()
    {
        ChainId = "CHAIN.BIOLOGICAL_DRIFT",
        Title = "The Biological Drift",
        Steps = new List<StepTemplate>
        {
            new()
            {
                StepIndex = 0,
                DiscoveryKind = "SIGNAL",
                MinHopsFromStarter = 1,
                MaxHopsFromStarter = 2,
                NarrativeText = "Sensor anomaly: organic compounds in concentrations that shouldn't exist in hard vacuum. Not debris from a ship or station — these compounds are being actively synthesized. Something is alive out here, producing complex biochemistry in the void between stars.",
                LeadText = "The compound trail leads deeper into unstable space, toward a region the Chitin Collective has quietly claimed as restricted.",
                LootOverrides = new Dictionary<string, int> { { "credits", 35 }, { "organics", 3 } }
            },
            new()
            {
                StepIndex = 1,
                DiscoveryKind = "DERELICT",
                MinHopsFromStarter = 2,
                MaxHopsFromStarter = 3,
                NarrativeText = "A Chitin research vessel, abandoned but recently active. The bio-labs are still warm. Sample containers hold specimens of void-adapted organisms — creatures that metabolize thread energy directly. The research logs are encrypted, but the specimen labels are clear: 'Generation 14.' This is a breeding program. The Chitin have been cultivating thread-feeding organisms for at least fourteen generations.",
                LeadText = "The vessel's navigation computer contains coordinates for a 'primary cultivation site' deeper in the restricted zone.",
                LootOverrides = new Dictionary<string, int> { { "credits", 70 }, { "organics", 5 } }
            },
            new()
            {
                StepIndex = 2,
                DiscoveryKind = "RUIN",
                MinHopsFromStarter = 3,
                MaxHopsFromStarter = 5,
                NarrativeText = "The cultivation site: an asteroid hollowed out and converted into a massive vivarium. Inside, a self-sustaining ecosystem of thread-feeding organisms. They range from microscopic to the size of shuttles, forming a complete food chain. The largest specimens emit the same harmonics found in precursor structures. They're not just feeding on threads — they're communicating through them.",
                LeadText = "The organisms' communication patterns reference a specific location in nearby unstable space. They're drawn to something.",
                LootOverrides = new Dictionary<string, int> { { "credits", 120 }, { "exotic_matter", 2 }, { "organics", 8 } }
            },
            new()
            {
                StepIndex = 3,
                DiscoveryKind = "RUIN",
                MinHopsFromStarter = 4,
                MaxHopsFromStarter = 6,
                NarrativeText = "A natural thread nexus — and the organisms have colonized it. They've woven themselves into the thread geometry, forming a living lattice that stabilizes the local thread environment. The instability readings here are zero. Not low. Zero. These organisms have achieved what both Valorin containment and Communion accommodation attempt, through pure biological adaptation. The Chitin knew. They've been studying this for generations, quietly developing an alternative to both doctrines.",
                LeadText = "",
                LootOverrides = new Dictionary<string, int> { { "credits", 250 }, { "exotic_matter", 4 }, { "organics", 10 } }
            }
        }
    };

    public static readonly IReadOnlyList<ChainTemplate> AllChains = new List<ChainTemplate>
    {
        ValorinExpedition,
        CommunionFrequency,
        PentagonAudit,
        DerelictSignal,
        PrecursorEcho,
        BiologicalDrift,
    };
}
