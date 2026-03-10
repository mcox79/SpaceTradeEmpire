# HUD & Information Architecture — Design Bible

> Design doc for the persistent HUD, notification system, information tiers, screen zones,
> and the rules governing what information appears where and when.
> Companion to `EmpireDashboard.md` (empire screen) and `GalaxyMap.md` (overlay).
> Content authoring specs: `content/VisualContent_TBA.md` (HUD enhancement assets).
> Epic: `EPIC.S7.HUD_ARCHITECTURE.V0`.

## Why This Doc Exists

The HUD is the permanent interface between player and game state. Every other system's
success depends on the player being able to read it. Games that add HUD elements
incrementally without a unified hierarchy create EVE Online's "uniquely horrific" UI —
the #1 driver of new player churn. Games that design the HUD architecture first (FTL,
where the ship cross-section IS the HUD) create interfaces players praise for decades.

This doc defines the information taxonomy, screen zones, progressive disclosure rules,
and notification pipeline before more UI is built.

---

## Implementation Status

| Feature | Status | Notes |
|---------|--------|-------|
| Status panel (credits, cargo, node, state) | Done | Top-left, always visible, 260×248px |
| Hull/shield progress bars | Done | Red hull, blue shield, numeric labels |
| Security band indicator | Done | Color-coded (green/orange/red), shows at nodes |
| Delay/ETA status | Done | Travel delay display, color by severity |
| Mission panel | Done | Gold text, hidden when docked |
| Research progress | Done | Idle/active/stalled states with color coding |
| Fuel indicator | Done | Low (orange) and immobilized (red) warnings |
| Edgedar screen-edge arrows | Done | Thread gates (blue), hostiles (red), quests (gold), stations (green) |
| Toast notifications | Done | Top-right, slide-in, 3s display, max 5 stack |
| Combat log panel (L key) | Done | Right-side, 20 events, gold/red coloring |
| Keybinds help (H key) | Done | Centered modal with bindings table |
| Gate transit popup | Done | Center screen, fee + traffic + balance |
| Three-tier information model | Partial | Tiers exist implicitly but aren't designed as a system |
| Screen zone allocation | Not designed | Elements placed ad-hoc |
| Notification priority/bundling | Not implemented | All toasts equal priority, no bundling |
| Dynamic HUD (show/hide by context) | Partial | Overlay hides HUD, but no other context rules |
| Color-blind secondary channels | Not implemented | Color only, no icons or patterns |
| Minimap | Not implemented | No spatial awareness when zoomed into local system |

---

## Design Principles

1. **Three tiers, one truth.** Every piece of information lives at exactly ONE tier.
   Information never duplicates across tiers — it flows between them. Glanceable data
   (Tier 1) is the doorway to readable data (Tier 2), which is the doorway to studyable
   data (Tier 3). If the same number appears in two places, one is wrong (or both are,
   because they'll inevitably diverge).

2. **The HUD is a dashboard, not a spreadsheet.** Show 5-7 KPIs at maximum on Tier 1.
   If the player needs to read more than 7 numbers without clicking anything, the HUD
   has too much ambient data. Red Dead Redemption 2's dynamic HUD is the exemplar:
   only horse stamina while riding, full combat UI only during fights.

3. **Screen zones are semantic.** Each screen corner has a purpose. Information in the
   wrong zone creates subconscious friction even if the player can't articulate why.
   Flight sims standardized this decades ago: instruments left, weapons right, status
   top, comms bottom. Our zones follow the same logic.

4. **Toasts are confirmations, not discoveries.** A toast should confirm something the
   player already expected ("Research complete" after watching the progress bar) or alert
   to something that requires attention ("Fleet stalled"). Toasts should never be the
   PRIMARY way the player learns about a game event — that's what Tier 2 is for.

5. **Clutter is cumulative.** Each HUD element seems harmless alone. 15 harmless elements
   make the screen unreadable. Every element must justify its permanent screen presence.
   If it's only relevant 10% of the time, it belongs at Tier 2 (contextual), not Tier 1
   (ambient).

---

## The Three Tiers

### Tier 1: Glanceable (< 0.5 seconds to read)

Always visible during gameplay. The player should be able to assess game state with
a quick glance — like checking a car dashboard while driving.

**Rule:** Maximum 7 elements at Tier 1. If adding one, remove one.

| Element | What It Shows | Why It's Tier 1 |
|---------|--------------|----------------|
| Credits | Current balance | Required for every trade/purchase decision |
| Cargo | Current/max slots | Constrains the most frequent action (trading) |
| Hull bar | HP with visual fill | Survival — must know instantly |
| Shield bar | HP with visual fill | Survival — must know instantly |
| Current system | Where you are | Spatial orientation |
| Ship state | Docked/Flying/Traveling | What actions are available right now |
| Alert badge | Count of pending alerts | "Do I need to open the empire screen?" |

**What's NOT Tier 1 (moved down from current implementation):**

| Element | Current Tier | Should Be | Why |
|---------|-------------|-----------|-----|
| Research progress | Tier 1 (always shown) | Tier 2 (shown on hover or when stalled) | Only actionable when choosing new tech |
| Fuel level | Tier 1 (always shown) | Tier 2 (shown when <50%, Tier 1 when critical) | Not actionable at full fuel |
| Mission text | Tier 1 (panel always visible) | Tier 2 (hover over mission icon) | Long text competes with gameplay view |
| Security band | Tier 1 (label always shown) | Tier 2 (shown on hover or when dangerous) | Redundant with edgedar hostile arrows |

### Tier 2: Readable (1-2 seconds, one interaction to reveal)

Visible on hover, single click, or contextual trigger. The player deliberately chooses
to look at this information.

| Element | Trigger | What It Shows |
|---------|---------|---------------|
| Research status | Hover over research icon (Tier 1) | Tech name, %, sustain status, ETA |
| Mission details | Hover over mission icon (Tier 1) | Objective, target, reward |
| Fuel details | Hover over state indicator, or auto-show at <50% | Fuel count, consumption rate, range |
| Security details | Hover over system name | Band name, bps value, faction territory |
| Fleet status summary | Hover over alert badge | "2 fleets idle, 1 stalled, 3 active" |
| Risk meters | Always visible when Heat/Influence/Trace > 0 | See `RiskMeters.md` |
| Edgedar tooltip | Hover over screen-edge arrow | POI name, distance, type |

### Tier 3: Studyable (5+ seconds, dedicated screen)

Requires opening a menu or overlay. The player is committing time to analysis.

| Screen | Trigger | What It Shows |
|--------|---------|---------------|
| Empire Dashboard | Tab or E key | Full empire state across 9 tabs |
| Dock Menu | Dock at station | Market, jobs, ship, station, intel |
| Galaxy Overlay | Tab key at altitude | Strategic map with overlays |
| Combat Log | L key | Last 20 combat events |
| Pause Menu | Esc | Save/load/quit |

---

## Screen Zones

```
┌─────────────────────────────────────────────────────────────────────────┐
│  ZONE A: STATUS              │                  ZONE B: NOTIFICATIONS  │
│  (Top-Left)                  │                  (Top-Right)            │
│  Credits, cargo, node, state │                  Toast stack (max 5)    │
│  Hull/shield bars            │                  Alert badge            │
│  260×248px                   │                                         │
├──────────────────────────────┤                                         │
│                              │                                         │
│  ZONE C: CONTEXT             │              ZONE D: COMBAT/LOG        │
│  (Left Edge, below status)   │              (Right Edge)              │
│  Mission (when active)       │              Combat log (L toggle)     │
│  Research (when stalled)     │              Zone armor display        │
│  Fuel warning (when low)     │              460×360px                 │
│                              │                                         │
│                              │                                         │
│                    ZONE E: CENTER                                      │
│                    (Reserved for modals)                               │
│                    Gate popup, game over, pause                        │
│                              │                                         │
│                              │                                         │
│  ZONE F: EDGE INDICATORS     │                                         │
│  (Screen perimeter, 40px)    │                                         │
│  Edgedar arrows for off-     │                                         │
│  screen POIs                 │                                         │
│                              │                                         │
├──────────────────────────────┴─────────────────────────────────────────┤
│  ZONE G: BOTTOM BAR                                                    │
│  (Bottom edge — currently empty)                                       │
│  Reserved for: risk meters, minimap, quick-action bar                  │
└─────────────────────────────────────────────────────────────────────────┘
```

### Zone Ownership Rules

| Zone | Owner | Rule |
|------|-------|------|
| **A** (top-left) | Status & survival | ONLY credits, cargo, node, state, HP bars. Nothing else. |
| **B** (top-right) | Notifications | Toasts + alert badge. Ephemeral only — nothing permanent. |
| **C** (left, below A) | Contextual info | Appears/disappears based on game state. Never permanent. |
| **D** (right) | Combat & logs | Only combat-related panels. Empty during peaceful gameplay. |
| **E** (center) | Modals | One modal at a time. Blocks gameplay interaction. |
| **F** (edges) | Spatial awareness | Edgedar arrows. Directional indicators only. |
| **G** (bottom) | Reserved | Risk meters, minimap, quick-access bar (future). |

---

## Notification System

### Toast Priority Levels

Not all toasts are equal. Define priority to prevent alert fatigue:

| Priority | Color | Duration | Example |
|----------|-------|----------|---------|
| **Critical** | Red border | 5s + persist until clicked | "Fleet destroyed at Kepler" |
| **Warning** | Orange border | 4s | "Trade Charter stalled: no ore" |
| **Info** | Default border | 3s | "Research complete: Warp III" |
| **Confirmation** | Green border | 2s | "Sold 10 Metal for +380 cr" |

### Toast Bundling

When multiple toasts of the same type fire within 2 seconds, bundle them:

```
Instead of:
  "Sold 10 Ore for +120 cr"
  "Sold 5 Metal for +190 cr"
  "Sold 3 Fuel for +24 cr"

Show:
  "3 trades completed: +334 cr total"
```

### Toast → Action Bridge

Critical and warning toasts should include an action shortcut:

```
┌─ TOAST ──────────────────────────────┐
│ ▲ Trade Charter stalled: no ore      │
│                        [View Program]│
└──────────────────────────────────────┘
```

Clicking `[View Program]` opens the Programs tab focused on that program.

### Alert Badge

The alert badge in Zone A shows the count of unresolved items:

```
[!] 3
```

- Red badge: critical alerts exist
- Orange badge: warnings only
- No badge: no pending alerts

Clicking opens Empire Dashboard → Overview → Needs Attention queue.

---

## Progressive Disclosure Rules

### When Information Promotes from Tier 2 to Tier 1

| Element | Promotes When | Demotes When |
|---------|--------------|-------------|
| Fuel level | Fuel < 50% (orange), < 25% (red, flashing) | Fuel > 75% |
| Research | Stalled (red) or completing in < 60 ticks | Running normally |
| Security | Current system is Dangerous or Hostile | System is Safe or Moderate |
| Risk meters | Any meter > 0 | All meters at 0 |
| Warfront indicator | Player is in an active warfront zone | Player leaves zone |

### When Information Hides Entirely

| Element | Hides When | Why |
|---------|-----------|-----|
| Mission panel | Player is docked | Dock menu has its own Jobs tab |
| Edgedar arrows | Galaxy overlay is open | Overlay provides full spatial awareness |
| Combat indicators | No combat in 30 seconds | Combat context ended |
| Zone armor display | No combat active | Tactical detail irrelevant in peaceful flight |

### HUD During Docking

When docked, the HUD should simplify dramatically — the dock menu IS the interface:

```
┌─ DOCKED HUD (minimal) ──────────────────────────────────┐
│ Credits: 42,100  │  Cargo: 8/12  │  Docked @ Sirius     │
│                                   │  [Undock: Space]      │
└──────────────────────────────────────────────────────────┘
```

Only credits, cargo, location, and undock shortcut. Hull/shield bars stay if damaged.
Research/fuel/mission panels hide — that information is available in the dock menu.

---

## Canvas Layer Hierarchy (Rendering Order)

Current implementation with design intent:

| Layer | Content | Design Rule |
|-------|---------|-------------|
| 0 | HUD main (Zone A + C + G) | Base gameplay layer |
| 10 | Edgedar arrows (Zone F) | Above HUD, below menus |
| 80 | Gate transit popup (Zone E) | Modal — blocks lower layers |
| 100 | Fracture panel, dock menu | Full-screen interaction mode |
| 110 | Node detail popup | Above dock menu (can coexist) |
| 115 | Combat log, keybinds help | Toggleable panels, above popups |
| 120 | Toast notifications (Zone B) | Topmost — never obscured |

**Rule:** No two permanent elements share a layer. Toggleable elements can share
if they don't coexist (combat log + keybinds are both layer 115 but both use the
right-side Zone D).

---

## Color System (UITheme Reference)

The semantic color palette is defined once and used everywhere:

| Color | Semantic Meaning | Hex/RGB | Usage |
|-------|-----------------|---------|-------|
| CYAN | Player / interactive / accent | (0.4, 0.85, 1.0) | Titles, selection, player markers |
| GREEN | Safe / profit / healthy / fresh | (0.2, 1.0, 0.4) | Safe security, profit numbers, full bars |
| RED | Danger / loss / critical / hostile | (1.0, 0.15, 0.15) | Hostile security, damage, alerts |
| ORANGE | Warning / caution / stale | (1.0, 0.6, 0.2) | Dangerous threads, low fuel, aging intel |
| GOLD | Commerce / reward / mission | (1.0, 0.85, 0.4) | Credits, loot, mission text |
| BLUE | Neutral info / moderate | (0.4, 0.7, 1.0) | Moderate security, shields |
| PURPLE_LIGHT | Discovery / anomaly / exotic | (0.9, 0.85, 1.0) | Discovery phases, anomaly markers |
| TEXT_PRIMARY | Body text | — | Standard readable text |
| TEXT_DISABLED | Inactive / locked | — | Grayed out, unavailable |
| TEXT_MUTED | Low-priority metadata | — | Timestamps, IDs |

**Rule:** These meanings are universal across HUD, empire screen, dock menu, galaxy
map, and toasts. "Orange means warning" everywhere, always.

### Color-Blind Accessibility

Every color-coded element must have a secondary channel:

| Primary (Color) | Secondary (Shape/Text) |
|-----------------|----------------------|
| Green = safe | "Safe" text + circle icon |
| Orange = warning | "Warning" text + triangle icon |
| Red = danger | "Danger" text + diamond icon |
| Progress bars | Numeric percentage always shown alongside |
| Faction colors | Faction name label + distinct border pattern |

---

## Anti-Patterns to Avoid

| Anti-Pattern | Game That Failed | Our Rule |
|---|---|---|
| **Bolt-on HUD elements** | EVE Online (decades of additions) | Screen zones are assigned, new elements go in their zone or don't exist |
| **Everything always visible** | X4 Foundations | Tier 1 has 7 elements max. Everything else is Tier 2+. |
| **Notification flood** | Stellaris large galaxy | Priority levels + bundling + category toggles |
| **Info without action** | Victoria 3 | Every toast with a warning includes an action shortcut |
| **Color-only encoding** | Many games | Every color has a shape/text secondary channel |
| **HUD that doesn't change** | Static HUD in dynamic game | Progressive disclosure: elements promote/demote by game state |
| **Minimap absence** | Current state | Bottom-right minimap (future) for spatial awareness in local system |

---

## Reference Games

| Mechanic | Best Reference | Key Lesson |
|---|---|---|
| Dynamic HUD | Red Dead Redemption 2 | Show only what's relevant to current activity |
| Ship-as-HUD | FTL | The game object IS the interface — every system visible |
| Information tiers | Mass Effect (conversation wheel) | Surface → detail → deep on progressive interaction |
| Screen zone discipline | Flight simulators (DCS, MSFS) | Each zone has a purpose, never violated |
| Toast priority | macOS/iOS notifications | Priority levels with silent/banner/alert modes |
| Alert badge | Mobile app badges | Single number communicates "attention needed" |
| Color-blind support | Fortnite | Explicit colorblind modes with shape + pattern alternatives |
