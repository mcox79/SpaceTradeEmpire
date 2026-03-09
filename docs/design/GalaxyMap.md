# Galaxy Map & Overlay — UI Architecture

> Design doc for the galaxy map (local system view) and galaxy overlay (strategic view).
> Companion to `EmpireDashboard.md` and `ship_modules_v0.md`.

## Implementation Status (as of Tranche 21, 2026-03-09)

The galaxy map (`scripts/view/GalaxyView.cs`, ~3200 lines) is one of the most complete
visual systems in the game. It already implements seamless zoom, 3 overlay modes, NPC route
visualization, territory display, and fog of war. This doc captures what exists, codifies
the design intent behind it, and lays out the aspiration targets for future tranches.

| Feature | Status | Notes |
|---------|--------|-------|
| Local system rendering | Done | Star, planet, moons, station, gates, discovery sites, fleets |
| Seamless altitude zoom | Done | LOD transitions at 100/200/500u altitude bands |
| Persistent star billboards | Done | Discovered = bright + colored, undiscovered = dim |
| Persistent lane lines | Done | Cyan cylinders, fog-of-war gated |
| Galaxy overlay (Tab) | Done | Node/edge graph, 3 color modes |
| Security overlay | Done | Lane edges: green/blue/orange/red by bps |
| Trade Flow overlay | Done | Lane edges: gold (active trade) / gray (idle) |
| Intel Freshness overlay | Done | Node colors: green→yellow→orange→red by tick age |
| Node detail popup | Done | Name, class, fleets, industry, security, territory, market |
| NPC route animation | Done | Gold flow dots (traders), blue (patrols), volume labels |
| Faction territory discs | Done | Semi-transparent faction-colored discs at owned nodes |
| Warp tunnel VFX | Done | Blue cylinder + speed streaks during transit |
| Player indicator ring | Done | Pulsing torus at current system |
| Faction Territory overlay | Not implemented | Dedicated lens to highlight all faction borders at once |
| Exploration Progress overlay | Not implemented | Three-state fog with strong visual contrast |
| Fleet Positions overlay | Not implemented | All player + NPC fleets with posture/role icons |
| Warfront overlay | Not implemented | Active warfront zones with intensity indicators |
| Heat/Danger overlay | Not implemented | Per-node and per-lane Heat accumulation heatmap |
| Route planning | Not implemented | Click destination → show optimal path + hop count + danger |
| Galaxy search (Ctrl+F) | Not implemented | Search across system/station/fleet/good names |
| Node clustering at wide zoom | Not implemented | Aggregate nodes when too many overlap |
| Icon category toggles | Not implemented | Per-category show/hide (fleets, routes, discoveries, territory) |
| Constellation clustering | Not implemented | Visual grouping of systems into strategic regions |
| Enhanced node popup | Not implemented | "So what?" context, action buttons, intel freshness |

---

## Design Principles

These extend the seven principles from `EmpireDashboard.md` with map-specific rules.

1. **Topology over geography.** The map exists to answer "how do I get there?" and "what's
   between here and there?" Physical distance between stars is irrelevant when travel is
   hop-based. Layout should optimize for readability — minimal edge crossings, clear
   clustering — not astronomical accuracy. (Lesson: DOTLAN's abstract topology replaced
   EVE's 3D galaxy map for 15+ years of actual player use.)

2. **2D is the primary view.** Every game that attempted a 3D galaxy map has faced usability
   backlash (Elite Dangerous: "a 2D mouse pointer is the wrong tool for a 3D space"; EVE
   Online: players abandoned the in-game map entirely). Our galaxy overlay is already 2D
   planar — this is correct and must stay. The 3D local system view serves immersion, not
   strategic planning.

3. **One lens, one color variable.** Each overlay mode recolors the entire map around ONE
   data dimension. Never stack multiple variables on the same color channel. Security colors
   the lanes green→red by danger. Trade flow colors them gold/gray by activity. These must
   remain exclusive — applying both simultaneously creates visual noise that communicates
   nothing. (Lesson: Stellaris's map modes are praised precisely because each one strips
   away everything except the single variable you care about.)

4. **Semantic zoom.** Different information belongs at different altitudes. Galaxy level
   shows topology + borders. Sector level shows names + routes + fleet icons. System level
   shows ships + stations + detail. Information that appears at the wrong zoom level is
   clutter. (Lesson: Distant Worlds 2's seamless zoom from galaxy to moon orbit is the
   gold standard — "spotless interface" despite 2,000 systems.)

5. **The map must never lose to a spreadsheet.** If a player would alt-tab to an external
   tool to find information, the map has failed. Embed decision-support data directly into
   the map surface with appropriate overlays. (Lesson: EVE Online and Elite Dangerous both
   lost their maps to third-party tools — DOTLAN, EDDB, Inara. CCP is now building a
   "DOTLAN-style 2D tactical map" into EVE, explicitly admitting the external tool was
   right.)

6. **Fog of war must be dramatic.** Three exploration states must be visually distinct
   without squinting. Endless Space 2's fog is "barely visible" — community consensus says
   this is a design failure. Unknown space should be genuinely dark. Explored-but-stale
   should be desaturated. Active sensor range should be bright and alive.

7. **Clutter kills the late game.** Stellaris's #1 community complaint is that the galaxy
   map drowns in overlapping icons, names, fleet badges, and megastructure markers at 200+
   systems. Our answer: semantic zoom (hide labels at galaxy altitude), icon category
   toggles (show only what you need), and node clustering (aggregate dense regions at
   wide zoom). (Lesson: Distant Worlds 2 solves this by pairing icon toggles with its
   automation system — hide the icons for things you've automated away.)

8. **Every popup answers "should I go there?"** The node detail popup is not an encyclopedia
   entry. It's a decision-support card. Show: security (is it safe?), market (is it
   profitable?), faction (can I trade?), fleet presence (am I already there?), and action
   buttons (set waypoint, go there). Don't show tactical detail that belongs in the dock
   menu. (Inherits EmpireDashboard principle: "every number answers 'so what?'")

---

## The Two Views

The galaxy map is two fundamentally different screens that share a coordinate space.

### Local System View (altitude < 500u)

The immersive, 3D flight space. The player flies their hero ship among physical objects:
the star, its planet, moons, the docked station, lane gates, discovery sites, NPC ships.
This is the Freelancer/Starcom DNA — you are IN the world, not above it.

**Design role:** Spatial navigation, visual spectacle, combat encounters, docking.
This view is NOT for strategic planning. It should feel like flying through space,
not reading a chart.

### Galaxy Overlay (Tab key / altitude > 500u)

The strategic, 2D planning surface. A node-link graph of all systems connected by lanes.
Color-coded by the active overlay lens. Clickable nodes open detail popups. This is the
Stellaris/DOTLAN DNA — you are ABOVE the world, making decisions.

**Design role:** Route planning, territory assessment, trade analysis, fleet management,
exploration tracking. This view IS a chart and should be proud of it.

The transition between views happens through altitude-based LOD:

| Altitude | What's Visible | What's Hidden |
|----------|---------------|---------------|
| 0–100u | Local system only (star, planet, station, gates, fleets) | Galaxy overlay nodes, persistent stars |
| 100–200u | Local system + persistent star billboards fade in | Overlay edges still hidden |
| 200–500u | Persistent stars + lane lines fade in (alpha 0→1) | Local system begins to simplify |
| 500u+ | Full galaxy overlay, node-link graph | Local system hidden entirely |

---

## Local System View

### What the Player Sees

```
                         ╭── GATE: Proxima ──╮
                         │    (orange sphere  │
                         │     at 90u)        │
                         ╰────────────────────╯
                                   │
                                   │ lane indicator
                                   │
    ╭─ STAR ─────────╮            │
    │  G-type (gold)  │   ╭── PLANET ──────────╮
    │  6u radius      │   │  Terrestrial        │
    │  OmniLight 200u │   │  orbiting at ~25u   │
    │  star dust VFX  │   │  ╭── STATION ─╮    │
    ╰─────────────────╯   │  │  orbit 60u  │    │
                          │  │  (prefab)   │    │
          ╭── NPC ──╮    │  ╰─────────────╯    │
          │ Trader   │    ╰─────────────────────╯
          │ orbit 35u│
          │ gold tint│      ╭── DISCOVERY ──╮
          ╰──────────╯      │  site at 55u   │
                            │  marker sphere │
    ╭── PLAYER ──────╮     ╰────────────────╯
    │  challenger_blue│
    │  pulsing ring   │     ╭── GATE: Vega ─╮
    ╰─────────────────╯     │  (orange, 90u) │
                            ╰────────────────╯
```

### Visual Elements

| Element | Visual Treatment | Current | Aspiration |
|---------|-----------------|---------|------------|
| **Primary star** | Addon shader, class-tinted, OmniLight | Done | Add corona particle ring, chromatic aberration at close range |
| **Binary companion** | 50% scale clone, orbit barycenter | Done | Distinct complementary color if different class |
| **Planet** | Addon shader by type (lava/sand/terrestrial/barren/ice/gas) | Done | Atmosphere glow ring, day/night terminator line |
| **Station** | Exported prefab, orbits near planet | Done | Faction-specific station mesh variants (S7.FACTION_VISUALS) |
| **Lane gates** | Orange/yellow spheres at 90u, one per neighbor | Done | Directional chevron arrow, destination name label, danger tint |
| **NPC ships** | Quaternius spaceships, faction-tinted, role-specific models | Done | Formation grouping for multi-ship fleets |
| **Player ship** | challenger_blue.tscn, pulsing torus ring | Done | Ship trail particles during flight |
| **Discovery sites** | Small marker spheres at seed positions | Done | Phase-colored (gray/amber/green per Seen/Scanned/Analyzed) |
| **Star dust** | White/pale-blue motes, diffuse sphere | Done | Vary particle density/color by star class |
| **Asteroid belt** | Tan/brown dust particles, 60% of systems | Done | Add 3-5 low-poly asteroid meshes in belt ring |

### Star Class Visual Language

Stars are the anchor visual of each system. Their class should communicate "personality"
at a glance — before reading any text.

| Class | Color | Scale | Light Range | Personality |
|-------|-------|-------|-------------|-------------|
| O (Blue giant) | Deep blue-white | 1.8x | 300u | Rare, valuable, dangerous |
| B (Blue) | Blue-white | 1.5x | 260u | Industrial heartland |
| A (White) | White | 1.3x | 240u | Commerce hubs |
| F (Yellow-white) | Warm white | 1.15x | 220u | Balanced, safe |
| G (Sun-like) | Gold | 1.0x (baseline) | 200u | Home systems, starting areas |
| K (Orange) | Deep orange | 0.8x | 160u | Frontier, mining |
| M (Red dwarf) | Deep red | 0.6x | 120u | Edge worlds, smuggling |

The color palette progresses from cold blue (power, industry) through warm gold (home,
safety) to deep red (danger, isolation). This maps to the game's thematic progression:
safe core worlds are warm; dangerous edge worlds are cold or blood-red.

### Lane Gate Enhancement

Lane gates are the most important navigational element in the local view — they're how
the player physically travels between systems. Current treatment (plain orange sphere)
undersells their importance.

**Aspirational design:**

```
        ╭─────────────────────────────────╮
        │          → PROXIMA              │
        │     ╱╲                          │
        │    ╱  ╲   2 hops │ Security: ●  │
        │   ╱ ◆  ╲  Safe                  │
        │    ╲  ╱                          │
        │     ╲╱                           │
        ╰─────────────────────────────────╯
```

- **Destination name** visible as Label3D above the gate
- **Directional chevron** pointing toward the gate (pulsing when selected)
- **Security tint** on the gate marker matching the lane's security band
  (green=safe, yellow=moderate, orange=dangerous, red=hostile)
- **Hop count** to the destination visible on hover
- **Active trade route indicator** — gold ring if an NPC trade route uses this lane

---

## Galaxy Overlay

### Layout Philosophy

The overlay is a 2D node-link graph. Each node is a system. Each edge is a lane.
The layout is derived from SimCore's galaxy topology (positions set at world genesis)
and does not change during gameplay.

**Priority order for layout quality:**
1. Minimize edge crossings (readability)
2. Show natural clustering / constellation grouping
3. Even node spacing (avoid overlap)
4. Actual coordinate fidelity (lowest priority — topology matters more)

### What the Player Sees (Default — Security Mode)

```
┌─ GALAXY OVERLAY ─────────────────────────────────────────────────────────┐
│                                                                           │
│  ◆ You are here                                                          │
│                                                                           │
│      [Sirius]══════════[Proxima]──────────[Barnard]                      │
│         ◆  ╲              │                  │                            │
│              ╲             │                  │                            │
│               [Vega]──────[Sol]──────────[Altair]                        │
│                │            │                  ╲                           │
│                │            │                   ╲                          │
│              [Deneb]──────[Wolf]                [Kepler]                  │
│                             │                      ·                      │
│                             │                      ·  (undiscovered)     │
│                          [Rigel]                  [?]                     │
│                                                                           │
│  ══ Safe (green)   ── Moderate (blue)   ·· Hostile (red)                │
│                                                                           │
│  Mode: [None] [■ Security] [Trade Flow] [Intel Age]                      │
│                                                                           │
│  [Ctrl+F Search]                              Camera: WASD  Zoom: Scroll │
└───────────────────────────────────────────────────────────────────────────┘
```

### Overlay Mode System

Overlay modes are **exclusive color lenses** — only one active at a time. Each lens
recolors nodes and/or edges to surface a single data dimension. Toggled via the mode
toolbar (`galaxy_overlay_hud.gd`).

#### Current Modes (Implemented)

| Mode | What It Colors | Color Mapping | Data Source |
|------|----------------|---------------|-------------|
| **None** | Neutral cyan nodes, white edges | Monochrome | — |
| **Security** | Lane edges by security bps | Green (>5000) → Blue (3000-5000) → Orange (1500-3000) → Red (<1500) | `GetSecurityBandV0` |
| **Trade Flow** | Lane edges by NPC activity | Gold = active trade route, Gray = no activity | `GetNpcTradeRoutesV0` |
| **Intel Freshness** | Nodes by intel age | Green (<500t) → Yellow (500-1500t) → Orange (1500-3000t) → Red (>3000t) → Gray (none) | `GetIntelFreshnessByNodeV0` |

#### Aspiration Modes (Not Yet Implemented)

| Mode | What It Colors | Color Mapping | Data Source | Why It Matters |
|------|----------------|---------------|-------------|----------------|
| **Faction Territory** | Nodes by controlling faction | Faction color palette (Concord=blue, Chitin=green, Weavers=purple, Valorin=gold, Communion=red) | `GetTerritoryRegimeV0` | Answer "who controls what?" at a glance. Currently requires clicking each node individually |
| **Exploration** | Nodes by discovery state | Black (unknown) → Dark gray (seen, never visited) → Desaturated (visited, stale) → Bright (active sensor range) | New: `GetExplorationStateV0` | Strong three-state fog. Motivates exploration by making gaps obvious |
| **Fleet Positions** | Nodes by fleet presence | Player fleet = cyan pulsing halo, NPC trader = gold dot, NPC patrol = blue dot, hostile = red dot, fleet size = dot radius | `GetFleetTransitFactsV0` per node | Answer "where are my fleets?" and "where is danger?" without checking each system |
| **Warfronts** | Nodes + edges in active warfront zones | Contested edges pulsing red/orange, warfront zone boundaries highlighted, combatant faction colors on participating nodes | `GetWarfrontsV0` | Spatial awareness of where wars are happening and which routes are disrupted |
| **Heat / Danger** | Nodes by accumulated heat | Cool blue (0 heat) → warm orange (moderate) → bright red (high heat) → white-hot (critical) | New: `GetHeatMapV0` | Answer "where have I been noticed?" for smuggling/stealth gameplay |

#### Mode Selector Toolbar — Enhanced Design

```
┌─ OVERLAY MODES ──────────────────────────────────────────────────────────┐
│                                                                           │
│  Color: (● Security) (○ Trade) (○ Intel) (○ Territory) (○ Explore)       │
│         (○ Fleets) (○ Warfronts) (○ Heat)                                │
│                                                                           │
│  Show:  [✓ Trade Routes] [✓ Fleet Icons] [✓ Territory Borders]           │
│         [✓ System Names] [✓ Discovery Sites] [  Warfront Zones]          │
│                                                                           │
└───────────────────────────────────────────────────────────────────────────┘
```

Two rows of controls:

**Color (radio buttons):** Exclusive — only one active. Determines which data dimension
drives the color of nodes and edges. Selecting one deselects the previous.

**Show (toggle checkboxes):** Additive — any combination. Determines which icon categories
are visible as overlays on top of the color lens. These are independent of the active
color mode. (Lesson: Distant Worlds 2's 9 toggleable overlays are praised because they
let players build their own information density — show fleet positions over security
coloring, or hide everything except territory borders.)

This two-tier system avoids the Stellaris trap of having 7 exclusive modes that each
show a different subset of icons. Our color modes are pure color changes; icon visibility
is orthogonal.

---

## Semantic Zoom

The overlay must present different information at different zoom levels. Showing everything
at every altitude is Stellaris's failure mode.

### Zoom Level Definitions

| Level | Altitude | Node Rendering | Edge Rendering | Labels | Icons |
|-------|----------|---------------|----------------|--------|-------|
| **Galaxy** | >800u | Dot (3px), color by active lens | Thin line (1px), color by active lens | Hidden | Hidden |
| **Sector** | 400–800u | Circle (8px), color by lens, faction border glow | Line (2px), color by lens, animated flow dots | System names (largest font, spaced) | Fleet summary badges ("3 traders"), warfront markers |
| **Region** | 200–400u | Circle (12px), status ring, territory disc | Line (3px), flow dots, trade volume thickness | All system names, station names on hover | Individual fleet icons, discovery phase markers |
| **Approach** | 100–200u | Transitions to persistent star billboards | Lane lines as 3D cylinders | Label3D in world space | Full detail — NPC models, gate labels |

### Progressive Label Density

This is the most critical anti-clutter mechanism. Names are the #1 source of overlap
in Stellaris's galaxy map (confirmed bug reports from Paradox forums, patch 3.3+).

**Rules:**
1. At **Galaxy** zoom: NO names. Topology and color are sufficient.
2. At **Sector** zoom: Show names for systems with >N fleets, active warfronts, or the
   player's current system. Use a collision grid — if two labels would overlap, hide the
   less important one.
3. At **Region** zoom: Show all system names. Station names appear on hover only.
4. At **Approach** zoom: Show everything — system names, station names, gate destination
   labels, fleet names.

**Label priority** (when collision forces a choice):
1. Player's current system (always shown)
2. Waypoint destination (always shown)
3. Systems with active warfronts
4. Systems with player fleets
5. Faction capital systems
6. Systems with active alerts (Needs Attention)
7. All others (by alphabetical tiebreak)

---

## Node Detail Popup — Enhanced Design

The current popup shows: name, class, fleet count, industry count, security band,
territory, and market table. This is functional but violates several EmpireDashboard
principles: numbers lack "so what?" context, there are no action buttons, and entity
names aren't links.

### Current vs. Aspiration

```
┌─ CURRENT ──────────────────────┐    ┌─ ASPIRATION ──────────────────────────────┐
│ SIRIUS                     [×] │    │ SIRIUS STATION                        [×] │
│ ─────────────────────────────  │    │ G-type │ Sol Federation territory          │
│ Class: ClassG                  │    │                                             │
│ Fleets: 3                     │    │ Security: Safe ●  │  3 hops from you       │
│ Industry: 4 sites             │    │ Fleets: 3 (1 yours — Vanguard, trading)    │
│ Security: 50.00% (Safe)       │    │ Industry: 4 sites (avg eff: 82%)           │
│ Territory: Sol Federation     │    │                                             │
│ ─────────────────────────────  │    │ ── MARKET ──                               │
│ Market (4 goods)              │    │ Ore:    12/10  (buy cheaper @ Vega: 8)     │
│ Good    Buy  Sell  Qty        │    │ Metal:  42/38  (sell higher @ Proxima: 78) │
│ Ore      12   10   45         │    │ Fuel:    8/ 6                               │
│ Metal    42   38   12         │    │ Prices: 45t ago (stale — rescan?)          │
│ Fuel      8    6   30         │    │                                             │
│ Munition  95   88    3        │    │ [Set Waypoint] [View in Fleet Tab]          │
└────────────────────────────────┘    └───────────────────────────────────────────┘
```

### Popup Principles

**Every number answers "so what?"**

| Current | Aspiration |
|---------|------------|
| "Fleets: 3" | "Fleets: 3 (1 yours — Vanguard, trading)" |
| "Industry: 4 sites" | "Industry: 4 sites (avg eff: 82%)" |
| "Security: 50.00%" | "Security: Safe ●" (named band + color dot) |
| "Ore: Buy 12 Sell 10 Qty 45" | "Ore: 12/10 (buy cheaper @ Vega: 8)" |

**Action buttons solve info-action divorce:**

| Button | When Shown | Action |
|--------|-----------|--------|
| `[Set Waypoint]` | Always | Sets nav waypoint to this system |
| `[Go Here]` | When 1 hop away and undocked | Initiates travel |
| `[View in Fleet Tab]` | When player fleet is present | Opens Empire → Fleet tab filtered to this system |
| `[View Market]` | When docked at this node | Opens Dock → Market tab |
| `[Dock]` | When at this node and undocked | Initiates docking |

**Intel freshness on market prices:**
- White text = fresh observation (<500 ticks)
- Gray italic = stale observation (>500 ticks), with "(Nt ago)" suffix
- `--` = no intel, never observed
- "(stale — rescan?)" prompt when all prices are old

**Contextual sections — show only what's relevant:**
- Market section: hidden if no market at this node
- Fleet section: hidden if no fleets present
- Warfront banner: shown only if node is in an active warfront zone
- Discovery section: shown only if unresolved discoveries exist

### Popup Variants by Node Type

**Station popup** (has market, industry, docking):
```
┌─ SIRIUS STATION ──────────────────── [×] ┐
│ G-type │ Sol Federation territory          │
│                                             │
│ Security: Safe ●  │  3 hops from you       │
│ Fleets: 3 (1 yours — Vanguard, trading)    │
│ Industry: 4 sites (avg eff: 82%)           │
│                                             │
│ ── MARKET ──                               │
│ Ore:    12/10   Metal:  42/38              │
│ Fuel:    8/ 6   Munitions: 95/88           │
│ (45t ago)                                   │
│                                             │
│ [Set Waypoint]  [View in Fleet Tab]        │
└─────────────────────────────────────────────┘
```

**Uninhabited system** (no market, no station):
```
┌─ KEPLER-7 ────────────────────────── [×] ┐
│ K-type │ Unclaimed territory               │
│                                             │
│ Security: Dangerous ●  │  5 hops from you  │
│ Fleets: 0                                   │
│                                             │
│ ── DISCOVERIES ──                          │
│ ▪ Derelict Wreck — Seen                    │
│ ░ Signal Source — Scanned                  │
│                                             │
│ [Set Waypoint]                              │
└─────────────────────────────────────────────┘
```

**Undiscovered system** (no intel at all):
```
┌─ UNKNOWN SYSTEM ──────────────────── [×] ┐
│ Unexplored                                  │
│                                             │
│ No data available.                         │
│ Scanner range: Out of range (7 hops)       │
│                                             │
│ [Set Waypoint]                              │
└─────────────────────────────────────────────┘
```

The absence of data IS information — it motivates exploration. Don't hide empty states;
show them with clear "you haven't been here yet" messaging.

---

## Route Planning

Currently, the player travels by clicking lane gates in the local system view. There is
no route planning from the overlay. This forces the player to navigate one hop at a time
with no forward visibility on security, distance, or danger.

### Aspirational Route Planner

**Trigger:** Click any node while the overlay is open → in addition to the popup, show
the optimal route as a highlighted path.

```
┌─ GALAXY OVERLAY — ROUTE PREVIEW ─────────────────────────────────────────┐
│                                                                           │
│      [SIRIUS]═══▶═══[Proxima]───▶───[Barnard]                           │
│         ◆                              ▼                                  │
│                                    [Altair]                               │
│                                        ▼                                  │
│                                    [KEPLER] ← destination                │
│                                                                           │
│  Route: Sirius → Proxima → Barnard → Altair → Kepler                    │
│  Hops: 4  │  Est. travel: ~4 min  │  Security: ●●●○ (1 dangerous lane)  │
│                                                                           │
│  [Set Waypoint]  [Auto-Navigate]  [Cancel]                               │
└───────────────────────────────────────────────────────────────────────────┘
```

**Route display rules:**
- Highlighted path uses thick, pulsing line (distinct from normal edges)
- Each hop shows a directional chevron (▶)
- Route summary bar at bottom: total hops, estimated travel time, worst-security-lane
  indicator (using the same green→red dots)
- If multiple paths exist, show the safest by default. Offer toggle: "Shortest" vs "Safest"
  (EVE Online's route preference system is the gold standard here)
- Path fades if the player pans away or clicks elsewhere

**Route info overlay on lanes:**
When a route is planned, each lane segment on the route shows:
- Security band color (already present)
- Hop number ("1/4", "2/4", etc.)
- Warning icon if hostile or if active warfront crosses this lane

### Pathfinding

SimCore already has the graph topology. The bridge needs a new query:

```
GetRouteV0(fromNodeId, toNodeId, preference)
  → { hops: [...], total_distance: N, worst_security: "dangerous", estimated_ticks: N }
```

Preference: `"shortest"` (fewest hops) or `"safest"` (maximize minimum security on path).
Pathfinding runs in SimCore (deterministic, headless). The overlay just renders the result.

---

## Fog of War & Exploration Visualization

### Three-State Model

| State | Visual Treatment (Overlay) | Visual Treatment (Local) | Trigger |
|-------|---------------------------|--------------------------|---------|
| **Unknown** | Node not rendered OR rendered as dim ghost dot with "?" | N/A (can't enter) | Default for all systems at game start |
| **Discovered** | Node rendered at 40% opacity, desaturated, no lens coloring | Persistent star billboard at dim brightness | Adjacent to a visited system, or within scanner range |
| **Visited** | Full brightness, full lens coloring, all data available | Full local system rendering with all elements | Player has physically entered this system |

**Current implementation gap:** Discovered vs. Visited distinction exists for star
billboards (dim vs. bright) but the overlay treats all known systems identically. The
aspiration is a clear three-state visual:

```
    [SIRIUS]════════[Proxima]────────[Barnard]────· · · ·[?]
       ◆               │                │                  ·
     (bright)      (bright)         (desaturated)     (ghost dot)
     visited        visited          discovered        unknown
```

### Scanner Range Visualization

When the Exploration overlay lens is active, show the player's scanner range as a
translucent circle centered on their current system:

```
                    ╭─── scanner range (3 hops) ───╮
                    │                               │
            [Sirius]═══[Proxima]───[Barnard]        │
               ◆           │           │            │
                        [Sol]──────[Altair]         │
                    │                               │
                    ╰───────────────────────────────╯
                                              [Kepler]  ← outside range
```

Systems inside the scanner circle show current data. Systems outside show stale or
no data. This makes the scanner tech upgrade feel impactful — you SEE your knowledge
frontier expand.

---

## Constellation Clustering

Endless Space 2's constellation system is praised for creating natural strategic regions
in the galaxy. Our galaxy already has implicit clusters from the world generator's
topology, but we don't visually communicate them.

### How It Would Work

1. **SimCore tags each system** with a constellation ID during world generation (a graph
   clustering algorithm — Louvain or simple connected-component after removing long edges)
2. **The overlay draws faint boundary lines** around constellation groups — thin, dotted,
   low-opacity borders that group 3-6 systems
3. **Constellation names** appear at sector zoom level (one name per cluster, positioned
   at the centroid)
4. **Strategic value:** Constellations become natural "regions" for territory discussion,
   warfront zones, and trade regions. "The Sirius Cluster" is more memorable than "systems
   4, 7, 12, and 15."

```
    ╭─ Sirius Cluster ──────────────────╮
    │                                    │
    │   [Sirius]═══[Proxima]───[Sol]    │
    │       ◆           │               │
    │                [Barnard]          │
    │                                    │
    ╰────────────────────────────────────╯
                         │
              (inter-cluster lane)
                         │
    ╭─ Frontier Reach ───────────────────╮
    │                                     │
    │   [Altair]───[Kepler]              │
    │       │          │                  │
    │   [Deneb]───[Rigel]                │
    │                                     │
    ╰─────────────────────────────────────╯
```

Constellation boundaries are passive — they don't block travel, they just organize
perception. The player never needs to learn what a "constellation" is; the visual
grouping communicates it intuitively.

---

## Icon Toggle System

Inspired by Distant Worlds 2's approach where icon visibility pairs with game automation:
if you've automated your trade fleets, you can hide trade fleet icons to reduce clutter.

### Toggle Categories

| Category | What It Shows | Default | Automation Pairing |
|----------|--------------|---------|-------------------|
| **Trade Routes** | Gold flow lines on active NPC trade edges, animated dots | On | Hide when trade is fully automated |
| **Fleet Icons** | Individual fleet position markers (player + NPC) | On | Hide NPC fleets when patrol is automated |
| **Territory Borders** | Faction-colored semi-transparent discs/borders | On | — |
| **System Names** | Text labels for system names | On | — |
| **Discovery Sites** | Phase-colored markers for unresolved discoveries | On | — |
| **Warfront Zones** | Pulsing zone boundaries for active wars | Off | Show only during wartime |
| **Trade Volume** | Line thickness encoding trade volume per lane | Off | — |

Toggles are independent of the color lens. You can view the Security lens with trade
routes visible, or the Territory lens with fleet icons hidden. This combinatorial
flexibility avoids the Stellaris problem where changing map mode changes BOTH the color
scheme AND the icon set, preventing useful combinations.

---

## Galaxy Search

`Ctrl+F` opens a search bar — one of the most requested features in 4X games (Civ 7
removed it from Civ 6 and players listed it as one of the most painful regressions).

### Search Targets

| Type | Example | Action on Select |
|------|---------|-----------------|
| System | "Sirius" | Center map on system, open popup |
| Station | "Forge-7" | Center on parent system, highlight station |
| Fleet | "Vanguard" | Center on fleet's current system, show fleet badge |
| Good | "Ore" | Activate Trade Flow lens, highlight systems that trade ore |
| Faction | "Concord" | Activate Territory lens, highlight faction systems |
| Tech | "Warp III" | Open Empire → Research tab (cross-scope link) |

### Search UX

```
┌─ SEARCH ────────────────────────────────────────┐
│ 🔍 sir                                          │
│                                                   │
│  Systems                                         │
│    ● Sirius Station  (3 hops, Safe, Sol Fed.)   │
│                                                   │
│  Fleets                                          │
│    ● Sirius Patrol  (NPC, Concord, at Sirius)   │
│                                                   │
│  Goods                                            │
│    (no matches)                                   │
└───────────────────────────────────────────────────┘
```

Results are grouped by type, show contextual detail (distance, security, faction), and
selecting a result performs the navigation action. Type-ahead filtering narrows results
as the player types.

---

## Color System Reference

All map colors should follow a consistent, learnable system. Players should never
wonder "what does orange mean?" — it should mean the same thing everywhere.

### The Universal Palette

| Color | Meaning (Everywhere) | Map Usage |
|-------|---------------------|-----------|
| **Green** | Safe / healthy / fresh / positive | Safe security, fresh intel, healthy efficiency |
| **Blue** | Moderate / neutral / player-owned | Moderate security, player fleet markers |
| **Yellow/Gold** | Commerce / trade / attention | Active trade routes, gold for profit |
| **Orange** | Warning / stale / declining | Dangerous security, aging intel, declining efficiency |
| **Red** | Danger / hostile / critical / urgent | Hostile security, very stale intel, warfront zones |
| **Cyan** | UI accent / selection / current | Selected node, player current system, UI chrome |
| **Gray** | Inactive / unknown / absent | No trade activity, no intel, undiscovered |
| **White** | Neutral data / fresh values | Fresh market prices, text labels |
| **Faction colors** | Identity (qualitative) | Territory lens, fleet tinting, regime labels |

### Color-Blind Safety

4% of players have color vision deficiency. Every color-coded element must have a
secondary channel:

| Primary Channel (Color) | Secondary Channel |
|------------------------|-------------------|
| Security: green→red gradient | Security band name text ("Safe", "Moderate", "Dangerous", "Hostile") |
| Intel age: green→red gradient | Age in ticks as text ("45t ago", ">3000t ago") |
| Faction territory: distinct hues | Faction name labels + distinct border patterns (solid, dashed, dotted) |
| Discovery phase: gray/amber/green | Phase icon (▪/░/✓) + text label ("Seen", "Scanned", "Analyzed") |

---

## Scaling Strategy

### Node Count Thresholds

The current galaxy has ~20 systems. The design must work at 50 and remain usable at 100.

| System Count | Map Behavior |
|-------------|-------------|
| 10-20 | All nodes visible at all zoom levels, all names shown |
| 20-50 | Semantic zoom — names hidden at galaxy altitude, collision-based label hiding at sector altitude |
| 50-100 | Constellation clustering activates — faint group boundaries, cluster names replace individual names at galaxy altitude |
| 100+ | Node aggregation — at galaxy altitude, a cluster of 8 systems becomes a single large dot with count badge ("8") |

### Edge Density Management

A fully-connected graph of 50 nodes can have hundreds of edges. Without management,
the map becomes a web of spaghetti.

**Rules:**
1. At galaxy zoom: show only edges between constellations (inter-cluster lanes) and hide
   intra-cluster edges. The cluster boundary implies internal connectivity.
2. At sector zoom: show all edges within the visible region.
3. Edge thickness encodes importance: trade routes are thicker, inactive lanes are thinner.
4. Edges between undiscovered systems are hidden entirely (existing fog-of-war behavior).

---

## Anti-Patterns to Avoid

| Anti-Pattern | Game That Failed | Our Rule |
|---|---|---|
| **3D map for strategic planning** | Elite Dangerous | 2D overlay for all strategic decisions. 3D only for immersive local flight |
| **All icons at all zoom levels** | Stellaris late-game | Semantic zoom — labels and icons appear/disappear by altitude |
| **Map modes that change BOTH color AND icons** | Stellaris | Color lens and icon toggles are independent |
| **Fog of war you can't see** | Endless Space 2 | Three-state fog with dramatic visual contrast (dark/dim/bright) |
| **No search** | Civ 7 (regression from Civ 6) | Ctrl+F searches all entity types, results navigate the map |
| **Popup without actions** | Victoria 3 | Every popup has action buttons — Set Waypoint, Go Here, View in Fleet Tab |
| **Numbers without context** | Generic | "Security: 50%" → "Security: Safe ●". "Fleets: 3" → "Fleets: 3 (1 yours)" |
| **External tools required for planning** | EVE Online, Elite Dangerous | Embed trade comparison, route planning, fleet overview directly in the map |
| **Name overlap at scale** | Stellaris 3.3+ | Collision grid for labels, priority-based hiding |
| **No distance/travel info** | Distant Worlds 2 | Hover any node to see hop count + estimated travel time from current position |

---

## Reference Games

| Mechanic | Best Reference | Key Lesson | Our Approach |
|---|---|---|---|
| Topology layout | DOTLAN (EVE) | Functional abstraction beats visual realism | 2D node-link graph, readability over coordinates |
| Map modes/lenses | Stellaris | One mode = one data variable, clean switching | 8 exclusive color lenses + independent icon toggles |
| Seamless zoom | Distant Worlds 2 | Galaxy → planet in one camera move | Already implemented — altitude-based LOD |
| Constellation regions | Endless Space 2 | Cluster grouping creates natural strategic regions | Faint boundary lines, cluster names at sector zoom |
| Security color coding | EVE Online | Green→yellow→red gradient = traffic-light intuition | Already using this palette for security lens |
| Node popup design | Star Traders: Frontiers | Maximum info density without clutter | Enhanced popup with "so what?" context + action buttons |
| Route planning | EVE Online | Shortest vs. safest preference toggle | Click destination → highlighted path + summary bar |
| Icon toggles | Distant Worlds 2 | Player-controlled information density, paired with automation | 7 toggle categories independent of color lens |
| Trade lane infrastructure | Freelancer | Physical lane gates as diegetic navigation objects | Lane gates already exist as orange sphere markers |
| Exploration overlay | Outer Wilds Ship Log | Web of discoveries with clear phase states | Three-state fog + discovery phase markers (▪/░/✓) |
| Search | Civilization 6 | Cross-entity search with type-grouped results | Ctrl+F with systems/stations/fleets/goods/factions |

---

## Cross-Scope Links

The galaxy map connects to every other UI scope. Following the EmpireDashboard principle
that the player should never feel trapped in one view:

| From (Map) | To | Trigger |
|---|---|---|
| Node popup → Dock Menu | Player is docked at this node | `[Open Dock Menu]` button |
| Node popup → Empire Fleet Tab | Player fleet present | `[View in Fleet Tab]` button |
| Node popup → Empire Industry Tab | Industry sites at this node | `[View Industry]` button |
| Node popup → Empire Trade Tab | Active trade routes through this node | `[View Routes]` button |
| Route preview → Galaxy Map | Route planned | Highlighted path on overlay |
| Search result → Galaxy Map | Entity found | Map centers + popup opens |
| Overlay → Local System | Double-click node or zoom in | Altitude transition |

And the reverse — other screens linking INTO the map:

| From | To (Map) | Trigger |
|---|---|---|
| Empire Fleet Tab → Galaxy Map | Fleet location clicked | Map centers on fleet's system |
| Empire Trade Tab → Galaxy Map | Route endpoints clicked | Map centers, route highlighted |
| Empire Industry Tab → Galaxy Map | Site location clicked | Map centers on site's system |
| HUD alert → Galaxy Map | "Fleet stalled at Kepler" clicked | Map centers on Kepler |
| Dock Intel Tab → Galaxy Map | "Nearby Markets" station clicked | Map centers on that station |

---

## SimBridge Queries — Existing and Needed

### Existing (Used by Current Implementation)

| Query | Returns | Used By |
|-------|---------|---------|
| `GetGalaxySnapshotV0()` | All nodes, positions, connections | Overlay graph construction |
| `GetSystemSnapshotV0(nodeId)` | Local system detail (star, planet, station, gates) | Local system rendering |
| `GetStarInfoV0(nodeId)` | Star class, luminosity, color RGB | Star visual tinting |
| `GetNodeDetailV0(nodeId)` | Name, class, fleet count, industry count, security bps | Node popup |
| `GetPlayerMarketViewV0(nodeId)` | Good inventory with buy/sell prices | Popup market table |
| `GetFleetTransitFactsV0(nodeId)` | NPC fleets at this node (role, faction, is_hostile) | Fleet markers |
| `GetNpcTradeRoutesV0()` | Active trade routes (edges, good, volume) | Trade Flow overlay |
| `GetIntelFreshnessByNodeV0()` | Per-node intel age in ticks | Intel Freshness overlay |
| `GetSecurityBandV0(from, to)` | Security bps for a lane | Security overlay |
| `GetTerritoryRegimeV0(nodeId)` | Controlling faction, regime color | Popup territory label |
| `GetTerritoryAccessV0(nodeId)` | Access rights at this node | Popup territory detail |
| `GetFactionColorsV0(factionId)` | Faction color palette | Territory disc tinting |
| `GetWarfrontsV0()` | Active warfronts with combatants, intensity | Warfront data |

### Needed for Aspiration Features

| Query | Returns | Needed By |
|-------|---------|-----------|
| `GetRouteV0(from, to, pref)` | Optimal path, hop list, worst security, estimated ticks | Route planner |
| `GetExplorationStateV0()` | Per-node: Unknown / Discovered / Visited | Exploration overlay |
| `GetHeatMapV0()` | Per-node: Heat accumulation value | Heat overlay |
| `GetConstellationsV0()` | Cluster assignments: node → constellation ID, constellation names | Constellation borders |
| `GetFleetSummaryByNodeV0()` | Per-node: player fleet count, NPC counts by role | Fleet Positions overlay |
| `GetTradeOpportunitiesV0(nodeId)` | Best buy/sell comparisons from IntelBook for popup | Enhanced popup market |

---

## Implementation Notes

- All data reads go through SimBridge snapshot queries (existing pattern)
- Galaxy overlay is a Godot Node3D tree, NOT a Control/UI tree — nodes and edges are
  meshes in 3D space viewed from above. This is an architectural choice that enables the
  seamless zoom from overlay to local system
- Overlay refresh runs via `RefreshFromSnapshotV0()` when overlay opens and periodically
  while open — NOT every frame (performance budget)
- Node click detection uses raycasting from camera through mouse position to hit node
  collision shapes — this is already implemented
- The mode selector toolbar (`galaxy_overlay_hud.gd`) is a separate GDScript UI layer
  that calls into GalaxyView.cs via `SetOverlayModeV0(int)`
- Camera controller (`galaxy_camera_controller.gd`) handles pan (WASD) and zoom (scroll)
  independently from the main player camera
- Label3D nodes are used for in-world text — they billboard toward the camera automatically
  but need manual distance-based visibility management to prevent clutter
- Performance constraint: overlay refresh with 50 nodes should complete in < 16ms
  (one frame at 60fps). Node clustering and LOD-based edge hiding are performance
  features, not just aesthetic ones
