# Knowledge Graph & Discovery — Design Spec

> Mechanical specification for the knowledge graph system, discovery phases,
> connection reveal logic, and the "puzzle web" that drives exploration.
> Companion to `ExplorationDiscovery.md` (discovery design), `NarrativeDesign.md`
> (narrative threads), and `factions_and_lore_v0.md` (lore context).

---

## AAA Reference Comparison

| Game | Knowledge Model | Discovery Pacing | Connection Reveal | Player Motivation |
|------|----------------|-------------------|-------------------|-------------------|
| **Outer Wilds** | Rumor board — nodes represent locations/knowledge, edges represent leads. Fully interconnected puzzle web. "?" edges point you to next destination. | Non-linear. Every planet available from start. Knowledge gates progress, not items. | Visiting a location reveals connected rumors. Some require observation (timing, physics). | Pure curiosity. "What does this ? lead to?" drives exploration. Gold standard for knowledge-as-progression. |
| **Mass Effect** | Codex entries unlocked by plot/discovery. Linear within each thread. | Plot-gated. Side missions expand codex. | Sequential — each entry unlocks next in category. | Lore depth. Worldbuilding reward for completionists. |
| **Subnautica** | PDA entries from scanning. Clues point to biomes/depths. Interconnected via audio logs. | Depth-gated. Deeper biomes require better equipment. | Environmental — scanning reveals related entries. | Survival need + curiosity. "What's deeper?" |
| **Return of the Obra Dinn** | Identity web — every face connected to name, cause of death, fate. Partial information reveals chain. | Logic-gated. Each correct identification unlocks cascading reveals. | Deduction — connecting 3 correct fates reveals a batch. | Pure deduction. Most satisfying reveal system in gaming. |
| **STE (Ours)** | Knowledge graph — discovery nodes with typed connections. "?" links visible when both endpoints Seen, fully revealed when both Analyzed. | Exploration-gated. Discover sites at nodes, scan to Seen, analyze to Analyzed. | Phase-based. Both endpoints must reach Analyzed for connection type/description to appear. "?" bridges visible earlier. | Trade-integrated curiosity. Discoveries provide economic intel + lore. |

### Best Practice Synthesis

1. **"?" connections are the hook** (Outer Wilds) — visible but unrevealed connections create pull. "I can see something links these two discoveries, but what?" Our phase-based reveal mimics this.
2. **Knowledge should gate progression, not items** (Outer Wilds) — the player progresses by understanding, not by collecting gear. Our tech unlock connections (TechUnlock type) bridge knowledge to gameplay.
3. **Partial information is more compelling than no information** (Obra Dinn) — showing "?" is better than showing nothing. The gap between "I know there's a connection" and "I know what it means" drives exploration.
4. **Multi-thread organization** (Mass Effect Codex) — data should be categorized into threads the player can follow. Our KnowledgeConnectionType serves this role.
5. **Discovery should reward the curious, not punish the focused** — a player who ignores the knowledge graph should not be blocked. A player who engages should feel smarter.

---

## Current Implementation

### Systems

| System | File | Purpose | Status |
|--------|------|---------|--------|
| `KnowledgeGraphSystem` | `SimCore/Systems/KnowledgeGraphSystem.cs` | Connection visibility and reveal evaluation | Implemented |
| `IntelSystem` | `SimCore/Systems/IntelSystem.cs` | Discovery phase transitions (Seen → Analyzed) | Implemented |
| `ScanDiscoveryCommand` | `SimCore/Commands/ScanDiscoveryCommand.cs` | Player-initiated discovery scanning | Implemented |

### Entities

```
KnowledgeConnection
  ConnectionId: string
  SourceDiscoveryId: string
  TargetDiscoveryId: string
  ConnectionType: KnowledgeConnectionType enum
  IsRevealed: bool
  RevealedTick: int
  Description: string (shown when revealed)

DiscoveryState (in Intel)
  Id: string
  Phase: DiscoveryPhase enum (Unknown, Seen, Analyzed)
  NodeId: string
  DiscoveredTick: int
```

### Connection Types

| Type | Value | Meaning | Example |
|------|-------|---------|---------|
| `SameOrigin` | 0 | Two discoveries from the same ancient event | Two Kepler Chain artifacts from the same expedition |
| `Lead` | 1 | One discovery points to another location | "Signal traces lead to sector 7G" |
| `FactionLink` | 2 | Discovery connects to a known faction | Ancient tech matches Valorin engineering patterns |
| `TechUnlock` | 3 | Discovery reveals a researchable technology | Analyzing artifact unlocks "fracture_harmonics" research |
| `LoreFragment` | 4 | Discovery adds to a narrative thread | Journal entry from Kepler Chain expedition |

---

## Mechanical Specification

### 1. Discovery Phases

```
Unknown → Seen → Analyzed
```

**Unknown**: Discovery exists in the world but the player hasn't encountered it.
Seeded at worldgen via `Node.SeededDiscoveryIds`.

**Seen**: Fleet enters a node with seeded discoveries → `IntelSystem.ApplySeenFromNodeEntry()`
marks them as Seen. The player knows the discovery exists and its basic type.

**Analyzed**: Player uses `ScanDiscoveryCommand` to analyze a Seen discovery.
Requires being at the discovery's node with appropriate scanner module.
Full description, connection types, and loot become available.

### 2. Connection Visibility Logic

```
Per tick (KnowledgeGraphSystem.Process):
  For each unrevealed connection:
    sourcePhase = GetDiscoveryPhase(conn.SourceDiscoveryId)
    targetPhase = GetDiscoveryPhase(conn.TargetDiscoveryId)

    if both < Seen:
      invisible — connection not shown at all

    if both >= Seen but either < Analyzed:
      VISIBLE as "?" — player sees a link exists but not its type
      (no state change — bridge layer renders based on phases)

    if both >= Analyzed:
      conn.IsRevealed = true
      conn.RevealedTick = state.Tick
      → connection type and description fully visible
```

### 3. The "?" Bridge (Outer Wilds Pattern)

The "?" bridge is the core engagement mechanic:

1. Player discovers Site A (Seen). No connections visible.
2. Player discovers Site B (Seen). If A↔B connection exists, it appears as "?".
3. Player sees "?" on the knowledge web. Curiosity: "What links these?"
4. Player analyzes Site A (Analyzed). Connection still "?" — need both analyzed.
5. Player analyzes Site B (Analyzed). Connection reveals: "SameOrigin — Both artifacts are from the third Kepler expedition."
6. Reveal may cascade: the description mentions a third site, pointing to new exploration.

### 4. Knowledge Web Templates

**Kepler Chain** (narrative thread):
```
kepler_artifact_1 ←→ kepler_artifact_2  (SameOrigin)
kepler_artifact_2 ←→ kepler_log_1       (Lead)
kepler_log_1 ←→ faction_valorin         (FactionLink)
kepler_artifact_1 ←→ fracture_harmonics  (TechUnlock)
```

**Data Log Threads** (lore fragments):
```
log_entry_A ←→ log_entry_B  (LoreFragment)
log_entry_B ←→ log_entry_C  (LoreFragment)
log_entry_C ←→ ancient_site  (Lead)
```

### 5. Query Methods

```csharp
// Count of "?" connections (visible but unrevealed)
KnowledgeGraphSystem.GetQuestionMarkCount(state)

// Check if a specific connection is visible
KnowledgeGraphSystem.IsConnectionVisible(state, conn)
```

---

## Player Experience

### The Knowledge Graph as Compass

```
Hour 1:  Player visits 3 systems. Discovers 2 sites (Seen).
         Knowledge web shows 2 isolated nodes. No connections.

Hour 2:  Player discovers 3rd site. "?" appears linking sites 1 and 3.
         First Officer comments: "Those signal patterns match..."
         Knowledge web now has a mystery to solve.

Hour 3:  Player analyzes site 1. Connection still "?".
         Player analyzes site 3. Connection reveals: Lead type.
         Description: "Signal traces point to coordinates in sector 7G."
         → New exploration target unlocked.

Hour 5:  3 "?" connections visible. Player prioritizes which to chase.
         Each reveal potentially unlocks new tech or lore.
         Knowledge web becomes the player's exploration agenda.
```

### Knowledge ↔ Trade Integration

Discoveries aren't purely narrative — they connect to the trade economy:
- **TechUnlock** connections gate research projects (new modules, ship upgrades)
- **FactionLink** connections provide diplomatic intel (improve rep strategies)
- **Lead** connections point to discovery sites near valuable trade nodes
- **SameOrigin** connections reveal ancient supply chain patterns (lore that hints at hidden production sites)

---

## System Interactions

```
IntelSystem
  ← reads Fleet arrivals at nodes
  → writes DiscoveryState.Phase (Unknown → Seen)
  → triggers knowledge graph evaluation

ScanDiscoveryCommand
  ← reads player fleet location + scanner module
  → writes DiscoveryState.Phase (Seen → Analyzed)
  → triggers knowledge graph evaluation

KnowledgeGraphSystem
  ← reads Intel.KnowledgeConnections (pre-authored at worldgen)
  ← reads Intel.Discoveries (discovery phases)
  → writes KnowledgeConnection.IsRevealed
  → writes KnowledgeConnection.RevealedTick

SimBridge.Research
  ← reads revealed TechUnlock connections
  → gates research project availability

FirstOfficerSystem
  ← reads "?" count and new reveals
  → generates contextual commentary (KNOWLEDGE_WEB_INSIGHT trigger)
```

### 6. Connection Generation Pipeline (NOT YET IMPLEMENTED)

`KnowledgeGraphContentV0.AllTemplates` defines 12 pre-authored `ConnectionTemplate`
entries with pattern tokens (`$KEPLER_1`, `$LOG.CONTAIN.003`, etc.). However, **no
worldgen code resolves these templates into `KnowledgeConnection` entities**. The
templates exist as dead content.

**Required Pipeline** (to be added to `GalaxyGenerator` or `DiscoverySeedGen`):

```
SeedKnowledgeConnectionsV0(state, seed):
  1. TEMPLATE RESOLUTION (pre-authored connections):
     For each template in KnowledgeGraphContentV0.AllTemplates:
       sourceId = ResolvePatternToken(template.SourcePattern, state, seed)
       targetId = ResolvePatternToken(template.TargetPattern, state, seed)
       if both resolve to valid discovery IDs:
         state.Intel.KnowledgeConnections.Add(new KnowledgeConnection {
           ConnectionId = "kc_" + template.TemplateId,
           SourceDiscoveryId = sourceId,
           TargetDiscoveryId = targetId,
           ConnectionType = template.ConnectionType,
           Description = template.Description,
           IsRevealed = false
         })

  2. PROCEDURAL CONNECTIONS (proximity-based):
     // Generate connections between discoveries at nearby nodes
     For each pair of discoveries (d1, d2) where d1.NodeId ≠ d2.NodeId:
       hopDistance = BFS(d1.NodeId, d2.NodeId, maxHops=3)
       if hopDistance ≤ 2:
         // Same discovery kind → SameOrigin
         if d1.DiscoveryKind == d2.DiscoveryKind:
           type = SameOrigin
           desc = ProceduralDescription(type, d1, d2)
         // Different kinds at adjacent nodes → Lead
         else if hopDistance == 1:
           type = Lead
           desc = ProceduralDescription(type, d1, d2)
         // Skip otherwise — don't over-connect
         else: continue

         connId = "kc_proc|" + d1.DiscoveryId + "|" + d2.DiscoveryId
         state.Intel.KnowledgeConnections.Add(...)

  3. FACTION LINK GENERATION:
     For each discovery at a node within faction territory:
       if discovery kind == AnomalyFamily:
         type = FactionLink
         desc = "{faction} researchers have studied similar anomalies."
         Add connection: discovery → faction homeworld discovery (if exists)

  4. TECH UNLOCK WIRING:
     For each discovery kind that gates a tech:
       type = TechUnlock
       desc = "Analysis reveals researchable applications."
       Add connection: discovery → synthetic tech-unlock node

  5. DENSITY VALIDATION:
     totalConnections = state.Intel.KnowledgeConnections.Count
     if totalConnections < MinConnectionDensity (15):
       // Backfill with LoreFragment connections between most-distant
       // discoveries until minimum density reached
```

**Pattern Token Resolution**:
```
ResolvePatternToken(pattern, state, seed):
  if pattern starts with "$KEPLER_":
    index = parse digit after "$KEPLER_"
    return state.Intel.Discoveries matching Kepler chain[index]
  if pattern starts with "$LOG.":
    thread = parse between dots (e.g., "CONTAIN")
    num = parse final number
    return state.Intel.Discoveries matching data log thread/entry
  else:
    return pattern as literal discovery ID
```

**Target Density**: 15-25 connections per galaxy (12 from templates + 3-13 procedural).
This provides:
- 6-8 "?" bridges visible by mid-game (assuming ~50% of discoveries Seen)
- 3-5 reveals per exploration arc (assuming player analyzes 6-10 discoveries)
- At least one connection per world class (spatial coverage)

**Ordering**: Connections sorted by ConnectionId (ordinal) for determinism.

---

## Design Gaps and Future Work

| Gap | Priority | Effort | Description |
|-----|----------|--------|-------------|
| **Connection generation pipeline** | CRITICAL | 2 gates | Templates exist but no worldgen code resolves them. See Section 6 above. Gate 1: template resolution + procedural generation. Gate 2: density validation + tests. |
| **Connection density** | HIGH | 1 gate | Current worldgen seeds few connections. Need 15-25 connections per galaxy for a satisfying web. Solved by pipeline above. |
| **Reveal rewards** | MEDIUM | 1 gate | Revealing a connection has no gameplay reward beyond information. Should grant XP, rep, or credits. Bridge method + MilestoneSystem integration. |
| **Knowledge web UI depth** | MEDIUM | 2 gates | Current K-key panel shows flat list. Gate 1: node-edge graph layout algorithm. Gate 2: GDScript rendering with interactive pan/zoom. |
| **Multi-connection reveals** | LOW | 1 gate | No batch reveal mechanic (Obra Dinn's "3 correct = batch reveal"). Could add cascade reveals. |
| **Knowledge persistence** | LOW | 1 gate | No cross-run knowledge. Each playthrough starts fresh. Could add a "museum" for completed knowledge webs. |
| **NPC knowledge trading** | FUTURE | 2 gates | NPCs could sell discovery leads ("I heard about ruins in sector 4"). Creates a knowledge market. Gate 1: NPC knowledge inventory. Gate 2: trade UI + pricing. |

---

## Cross-References

- **Tick order**: KnowledgeGraphSystem runs last in Phase 7 (`dynamic_tension_v0.md` → Cross-System Tick Execution Order). This ensures all discovery phase changes from IntelSystem are finalized before connection evaluation.
- **Discovery seeding**: `DiscoverySeedGen.cs` generates discovery sites; connections reference these via pattern tokens (`npc_industry_v0.md` production chain context).
- **Pressure integration**: Reveal events should inject `PressureSystem.InjectDelta("exploration", "knowledge_reveal", 100)` — currently unwired.

---

## Constants Reference

```
# Connection Types
SameOrigin    = 0
Lead          = 1
FactionLink   = 2
TechUnlock    = 3
LoreFragment  = 4

# Discovery Phases
Unknown       = 0
Seen          = 1
Analyzed      = 2

# Connection reveal requires both endpoints at Analyzed (phase 2)
# "?" visibility requires both endpoints at Seen (phase 1)
```
