using SimCore.Entities;
using System.Collections.Generic;

namespace SimCore.Content;

// GATE.T18.NARRATIVE.DATALOG_CONTENT.001: ~25 ancient scientist conversation
// fragments across six threads and five voices. Every log has at least one
// personal/mundane line. Scientists have relationships beyond their positions.
//
// Five voices:
//   Kesh   — Safety lead. Former mentor to Vael. Worried about Vael personally.
//   Vael   — Accommodation theorist. Hides unpublished error margins.
//   Tal    — Infrastructure engineer. Expresses care through diagrams. Grieves infrastructure.
//   Oruth  — Decision-maker. Late-stage language becomes terse.
//   Senn   — Economist/architect. Designed the pentagon ring. Genuinely amoral.
//
// Six threads:
//   Containment   (Kesh + Vael):  Should we cage the instability?
//   Lattice       (Tal + Kesh):   Infrastructure grief — what we built, what we're losing.
//   Departure     (Oruth + Tal):  Decision burden — when to leave.
//   Accommodation (Vael + Senn):  Can we adapt? Hope with hidden fear.
//   Warning       (Multiple):    Cross-thread synthesis — proof of finite lifespan.
//   EconTopology  (Senn + Oruth + Kesh): Pentagon ring design.
public static class DataLogContentV0
{
    public static readonly IReadOnlyList<DataLog> AllLogs = BuildAllLogs();

    private static List<DataLog> BuildAllLogs()
    {
        var logs = new List<DataLog>();

        // ── CONTAINMENT THREAD (Kesh + Vael) ────────────────────────
        // The central debate: cage it or live with it?

        logs.Add(MakeLog("LOG.CONTAIN.001", DataLogThread.Containment, 1,
            new[] { "Kesh", "Vael" },
            "COORDINATE_HINT",
            E(0, "Kesh", "The field readings from junction seven are outside the model again.", false),
            E(1, "Vael", "They've been outside the model for three cycles. The model is what's wrong.", false),
            E(2, "Kesh", "You sound like you did at the academy. Certain.", true),
            E(3, "Vael", "I sound like someone who's been looking at the data instead of the committee reports.", false),
            E(4, "Kesh", "Have you eaten today?", true),
            E(5, "Vael", "...No.", true),
            E(6, "Kesh", "Then eat. Then we'll talk about the junction.", true)
        ));

        logs.Add(MakeLog("LOG.CONTAIN.002", DataLogThread.Containment, 1,
            new[] { "Kesh", "Vael" },
            "",
            E(0, "Kesh", "I reviewed your containment bypass proposal. The math is elegant.", false),
            E(1, "Vael", "But?", false),
            E(2, "Kesh", "But elegant math has killed more people than bad math. Bad math fails visibly.", false),
            E(3, "Vael", "So has inaction, Kesh. The lattice is already degrading.", false),
            E(4, "Kesh", "I taught you better than this. 'Already degrading' is not the same as 'inevitably lost.'", true),
            E(5, "Vael", "You taught me to follow the evidence. That's what I'm doing.", false)
        ));

        logs.Add(MakeLog("LOG.CONTAIN.003", DataLogThread.Containment, 2,
            new[] { "Kesh", "Vael" },
            "CALIBRATION_DATA",
            E(0, "Vael", "I've run the containment scenarios. All of them. There are eleven.", false),
            E(1, "Kesh", "And?", false),
            E(2, "Vael", "Seven maintain stability for fifty to two hundred cycles. Then they fail.", false),
            E(3, "Kesh", "And the other four?", false),
            E(4, "Vael", "Immediate cascade. Kesh, I'm not enjoying this.", true),
            E(5, "Kesh", "I know.", true),
            E(6, "Vael", "You always say that. I don't think you do.", true)
        ));

        logs.Add(MakeLog("LOG.CONTAIN.004", DataLogThread.Containment, 2,
            new[] { "Kesh" },
            "",
            E(0, "Kesh", "[Personal log.] Vael presented the bypass results to the committee today. They didn't listen.", false),
            E(1, "Kesh", "I wanted them to listen. I just wanted her to be wrong.", true),
            E(2, "Kesh", "She wasn't wrong. She's never wrong about the data. Only about what to do with it.", false)
        ));

        logs.Add(MakeLog("LOG.CONTAIN.005", DataLogThread.Containment, 3,
            new[] { "Kesh", "Vael" },
            "RESONANCE_LOCATION",
            E(0, "Kesh", "If we cage it, we cage everything. Trade. Travel. Connection.", false),
            E(1, "Vael", "If we don't cage it, it grows. You've seen the projections.", false),
            E(2, "Kesh", "I helped write them.", false),
            E(3, "Vael", "Then you know.", false),
            E(4, "Kesh", "I know the numbers. I don't know if the numbers are everything.", true),
            E(5, "Vael", "When did you start saying things like that?", true),
            E(6, "Kesh", "When the numbers stopped being enough.", true)
        ));

        // ── LATTICE THREAD (Tal + Kesh) ─────────────────────────────
        // Infrastructure grief. Tal catalogs what they built and what's failing.

        logs.Add(MakeLog("LOG.LATTICE.001", DataLogThread.Lattice, 1,
            new[] { "Tal", "Kesh" },
            "",
            E(0, "Tal", "Junction fourteen lost coherence overnight. That's the one with the observation gallery.", true),
            E(1, "Kesh", "I remember. You put a window facing the thread convergence.", true),
            E(2, "Tal", "It was the best view in the network. Three threads visible simultaneously.", true),
            E(3, "Kesh", "Was?", false),
            E(4, "Tal", "I can draw you what it looks like now if you want. I've been documenting them all.", true),
            E(5, "Kesh", "The failures?", false),
            E(6, "Tal", "The views. While they still exist.", true)
        ));

        logs.Add(MakeLog("LOG.LATTICE.002", DataLogThread.Lattice, 1,
            new[] { "Tal" },
            "COORDINATE_HINT",
            E(0, "Tal", "[Infrastructure log.] Catalog of thread junction views, entry 47 of 312.", false),
            E(1, "Tal", "Junction 47: binary star reflection through lattice filament. Best at third-cycle midpoint.", true),
            E(2, "Tal", "I built the walkway here so maintenance crews would have something to look at during shifts.", true),
            E(3, "Tal", "Structural integrity: 94%. Estimated remaining cycles: unknown. Everything is 'unknown' now.", false)
        ));

        logs.Add(MakeLog("LOG.LATTICE.003", DataLogThread.Lattice, 2,
            new[] { "Tal", "Kesh" },
            "",
            E(0, "Tal", "Twenty-three junctions failed this cycle. I mapped every one.", false),
            E(1, "Kesh", "Why map them? The data is in the system.", false),
            E(2, "Tal", "The system records failures. I record what was there.", true),
            E(3, "Kesh", "Tal...", true),
            E(4, "Tal", "Don't. I know what you're going to say. I know it's not productive.", true),
            E(5, "Tal", "But someone should remember that junction twenty-nine had the best acoustics in the network.", true)
        ));

        logs.Add(MakeLog("LOG.LATTICE.004", DataLogThread.Lattice, 3,
            new[] { "Tal" },
            "",
            E(0, "Tal", "[Final infrastructure log.] I've stopped counting.", false),
            E(1, "Tal", "There's no point cataloging views from windows that won't exist next cycle.", false),
            E(2, "Tal", "Oruth asked me to help with the departure calculations. The geometry is familiar.", true),
            E(3, "Tal", "I used to build things. Now I calculate how to leave them behind.", true)
        ));

        // ── DEPARTURE THREAD (Oruth + Tal) ──────────────────────────
        // Decision burden. Someone has to choose when to leave.

        logs.Add(MakeLog("LOG.DEPART.001", DataLogThread.Departure, 2,
            new[] { "Oruth", "Tal" },
            "COORDINATE_HINT",
            E(0, "Oruth", "Timeline.", false),
            E(1, "Tal", "If you mean the infrastructure, twelve cycles. Maybe eight.", false),
            E(2, "Oruth", "I mean all of it.", false),
            E(3, "Tal", "Then I need Vael's projections, and she won't share them.", false),
            E(4, "Oruth", "She shared them with me. Don't ask me the numbers.", true),
            E(5, "Tal", "That bad?", false),
            E(6, "Oruth", "Timeline.", false)
        ));

        logs.Add(MakeLog("LOG.DEPART.002", DataLogThread.Departure, 2,
            new[] { "Oruth" },
            "",
            E(0, "Oruth", "[Decision log.] Departure window analysis, revision fourteen.", false),
            E(1, "Oruth", "Early departure: lose the research. We could still learn what this is.", false),
            E(2, "Oruth", "Late departure: risk the population. Cannot guarantee transit stability.", false),
            E(3, "Oruth", "I asked Senn for the economic analysis. The answer was one sentence.", true),
            E(4, "Oruth", "'The pentagon will sustain itself.' Not helpful.", true)
        ));

        logs.Add(MakeLog("LOG.DEPART.003", DataLogThread.Departure, 3,
            new[] { "Oruth", "Tal" },
            "",
            E(0, "Oruth", "We leave in six cycles.", false),
            E(1, "Tal", "The infrastructure won't—", false),
            E(2, "Oruth", "I know.", false),
            E(3, "Tal", "The research stations—", false),
            E(4, "Oruth", "I know.", false),
            E(5, "Tal", "...All right.", true),
            E(6, "Oruth", "Draw me a diagram. I need to see what we're leaving.", true),
            E(7, "Tal", "I already have one.", true)
        ));

        logs.Add(MakeLog("LOG.DEPART.004", DataLogThread.Departure, 3,
            new[] { "Oruth" },
            "",
            E(0, "Oruth", "[Final decision log.]", false),
            E(1, "Oruth", "We did not agree.", false),
            E(2, "Oruth", "Kesh wanted containment. Vael wanted accommodation. Senn said it didn't matter.", false),
            E(3, "Oruth", "Tal said nothing. Drew a map of everything we'd lose.", true),
            E(4, "Oruth", "I chose departure. The only choice no one wanted.", false),
            E(5, "Oruth", "This is not a testing environment.", false)
        ));

        // ── ACCOMMODATION THREAD (Vael + Senn) ──────────────────────
        // Can we adapt? Hope layered with hidden fear.

        logs.Add(MakeLog("LOG.ACCOM.001", DataLogThread.Accommodation, 1,
            new[] { "Vael", "Senn" },
            "",
            E(0, "Vael", "The shimmer zones aren't hostile. They're different. We could learn to read them.", false),
            E(1, "Senn", "You could learn to read them. Some species. Not all.", false),
            E(2, "Vael", "We'd adapt the technology—", false),
            E(3, "Senn", "Technology adapts to the species that builds it. Not the other way around.", false),
            E(4, "Vael", "That's unusually philosophical for you.", true),
            E(5, "Senn", "It's economics. Adaptation costs scale with biological variance.", false)
        ));

        logs.Add(MakeLog("LOG.ACCOM.002", DataLogThread.Accommodation, 1,
            new[] { "Vael" },
            "TRADE_INTEL",
            E(0, "Vael", "[Research log.] Shimmer zone exposure test, day forty.", false),
            E(1, "Vael", "The subjects who grew up near the zones are adapting. Navigation improves measurably.", false),
            E(2, "Vael", "The subjects from stable zones are not adapting. They're compensating.", false),
            E(3, "Vael", "There's a difference. I haven't told Senn. He'd use it.", true),
            E(4, "Vael", "Published error margins: ±2%. Actual error margins: ±11%.", true),
            E(5, "Vael", "If anyone finds this log: the published numbers are wrong.", true)
        ));

        logs.Add(MakeLog("LOG.ACCOM.003", DataLogThread.Accommodation, 2,
            new[] { "Vael", "Senn" },
            "",
            E(0, "Senn", "Your accommodation proposal. It works for the Communion. Not for the others.", false),
            E(1, "Vael", "It could work for everyone with the right infrastructure—", false),
            E(2, "Senn", "You're projecting. The Communion evolved in shimmer space. They tolerate uncertainty. The Concord didn't. The Assembly didn't.", false),
            E(3, "Vael", "So we just... abandon the other species?", false),
            E(4, "Senn", "I'm not abandoning anyone. I'm describing a constraint.", false),
            E(5, "Vael", "Sometimes you frighten me, Senn.", true),
            E(6, "Senn", "I know.", true)
        ));

        logs.Add(MakeLog("LOG.ACCOM.004", DataLogThread.Accommodation, 2,
            new[] { "Vael" },
            "",
            E(0, "Vael", "[Personal log.] I can't sleep.", true),
            E(1, "Vael", "Senn is right about the species constraint. The data supports it.", false),
            E(2, "Vael", "But Kesh is right that accommodation at galactic scale is untested.", false),
            E(3, "Vael", "Tested once. Haven station. Controlled conditions. Forty subjects.", false),
            E(4, "Vael", "Galactic-scale accommodation means running the Haven experiment on four hundred billion people.", false),
            E(5, "Vael", "I believe in the theory. I'm terrified of the practice.", true)
        ));

        logs.Add(MakeLog("LOG.ACCOM.005", DataLogThread.Accommodation, 3,
            new[] { "Vael", "Kesh" },
            "RESONANCE_LOCATION",
            E(0, "Vael", "I found the error in my models. The one Kesh kept saying was there.", false),
            E(1, "Kesh", "How bad?", false),
            E(2, "Vael", "Accommodation works. But only for species with shimmer-zone evolutionary history.", false),
            E(3, "Kesh", "So the Communion.", false),
            E(4, "Vael", "The Communion. Maybe one or two others, given centuries of exposure.", false),
            E(5, "Kesh", "Vael. I'm sorry.", true),
            E(6, "Vael", "Don't be sorry. Be angry. I was wrong and people might die because I was persuasive.", true)
        ));

        // ── WARNING THREAD (Multiple voices) ────────────────────────
        // Cross-thread synthesis: the mathematical proof.

        logs.Add(MakeLog("LOG.WARN.001", DataLogThread.Warning, 2,
            new[] { "Kesh", "Vael", "Senn" },
            "",
            E(0, "Kesh", "I've combined Vael's cascade models with Tal's infrastructure decay curves.", false),
            E(1, "Vael", "And?", false),
            E(2, "Kesh", "There is a finite lifespan for any topology exposed to instability. It's not a theory. It's arithmetic.", false),
            E(3, "Senn", "How finite?", false),
            E(4, "Kesh", "Depends on the topology. For a five-point ring? Roughly four thousand cycles at current growth.", false),
            E(5, "Senn", "That's longer than I expected.", false),
            E(6, "Vael", "That's not long enough.", false),
            E(7, "Kesh", "It's never long enough.", true)
        ));

        logs.Add(MakeLog("LOG.WARN.002", DataLogThread.Warning, 3,
            new[] { "Vael", "Oruth" },
            "CALIBRATION_DATA",
            E(0, "Vael", "Oruth. The proof is done. Kesh and I agree on the math.", false),
            E(1, "Oruth", "You agree?", false),
            E(2, "Vael", "On the math. Not on what to do about it.", false),
            E(3, "Oruth", "What does the math say?", false),
            E(4, "Vael", "Whatever we do — contain, accommodate, depart — the instability wins eventually.", false),
            E(5, "Oruth", "Eventually.", false),
            E(6, "Vael", "Eventually. The question is whether the people living in 'eventually' get to choose.", false),
            E(7, "Oruth", "Do they?", false),
            E(8, "Vael", "If someone tells them. If someone leaves a warning.", true)
        ));

        logs.Add(MakeLog("LOG.WARN.003", DataLogThread.Warning, 3,
            new[] { "Kesh", "Tal", "Oruth" },
            "",
            E(0, "Kesh", "We're leaving these logs, then.", false),
            E(1, "Tal", "I'll encode them in the infrastructure. Lattice junction markers. They'll survive longer than data storage.", false),
            E(2, "Oruth", "Will anyone find them?", false),
            E(3, "Tal", "Someone always builds on old foundations. It's what I'd do.", true),
            E(4, "Kesh", "What do we tell them?", false),
            E(5, "Oruth", "The truth. We couldn't agree.", false),
            E(6, "Kesh", "That's the warning? 'We couldn't agree'?", false),
            E(7, "Oruth", "It's the only honest one.", true)
        ));

        // ── ECON TOPOLOGY THREAD (Senn + Oruth + Kesh) ──────────────
        // Pentagon ring design. Why five factions, why this shape.

        logs.Add(MakeLog("LOG.ECON.001", DataLogThread.EconTopology, 1,
            new[] { "Senn", "Oruth" },
            "TRADE_INTEL",
            E(0, "Senn", "Five species. Five trade dependencies. A ring.", false),
            E(1, "Oruth", "You're describing the current situation.", false),
            E(2, "Senn", "I'm describing the optimal topology. The current situation is accidental. The optimal one wouldn't be.", false),
            E(3, "Oruth", "You want to engineer the trade network?", false),
            E(4, "Senn", "It's already engineered. Just badly. I want to make it self-sustaining.", false),
            E(5, "Oruth", "Self-sustaining. Even through instability?", false),
            E(6, "Senn", "Especially through instability. The ring is resilient precisely because each node depends on exactly two neighbors.", false),
            E(7, "Oruth", "You drew this on a napkin at lunch. I kept it.", true)
        ));

        logs.Add(MakeLog("LOG.ECON.002", DataLogThread.EconTopology, 2,
            new[] { "Senn", "Kesh" },
            "",
            E(0, "Kesh", "Your pentagon proposal. The factions won't know they're in a designed system?", false),
            E(1, "Senn", "They'll think they evolved naturally. Organic trade dependencies. Cultural affinity.", false),
            E(2, "Kesh", "And you don't see a problem with that?", false),
            E(3, "Senn", "I see a population that survives. The alternative is five populations that die separately.", false),
            E(4, "Kesh", "There's something missing from your analysis. I can feel it but I can't name it.", true),
            E(5, "Senn", "That's not analysis. That's intuition.", false),
            E(6, "Kesh", "Intuition has kept me alive longer than your models.", true)
        ));

        logs.Add(MakeLog("LOG.ECON.003", DataLogThread.EconTopology, 2,
            new[] { "Senn" },
            "",
            E(0, "Senn", "[Economic log.] Pentagon ring stability analysis, final revision.", false),
            E(1, "Senn", "The ring sustains itself through mutual dependency. No faction can withdraw without collapsing two neighbors.", false),
            E(2, "Senn", "This is by design. Dependency is stability. Freedom is fragility.", false),
            E(3, "Senn", "Kesh thinks I'm building a cage. I'm building a raft.", false),
            E(4, "Senn", "Vael thinks I lack empathy. I lack the luxury of empathy.", true),
            E(5, "Senn", "The ring will outlast all of us. That's enough.", false)
        ));

        logs.Add(MakeLog("LOG.ECON.004", DataLogThread.EconTopology, 3,
            new[] { "Senn", "Oruth", "Kesh" },
            "RESONANCE_LOCATION",
            E(0, "Oruth", "The ring is running. The five factions are in place.", false),
            E(1, "Senn", "Self-sustaining in forty cycles. After that, it doesn't need us.", false),
            E(2, "Kesh", "It doesn't need us already. We're the ones who need it.", true),
            E(3, "Oruth", "What happens when they find out?", false),
            E(4, "Senn", "They won't. The dependencies are too deep to reverse-engineer.", false),
            E(5, "Kesh", "Senn. Someone always finds out.", true),
            E(6, "Senn", "Then I hope they understand why.", false),
            E(7, "Kesh", "They won't. Understanding 'why' requires living through the alternative.", true)
        ));

        return logs;
    }

    private static DataLog MakeLog(
        string logId, DataLogThread thread, int tier,
        string[] speakers, string hook,
        params DataLogEntry[] entries)
    {
        var log = new DataLog
        {
            LogId = logId,
            Thread = thread,
            RevelationTier = tier,
            MechanicalHook = hook
        };
        foreach (var s in speakers)
            log.Speakers.Add(s);
        foreach (var e in entries)
            log.Entries.Add(e);
        return log;
    }

    private static DataLogEntry E(int idx, string speaker, string text, bool isPersonal)
    {
        return new DataLogEntry
        {
            EntryIndex = idx,
            Speaker = speaker,
            Text = text,
            IsPersonal = isPersonal
        };
    }
}
