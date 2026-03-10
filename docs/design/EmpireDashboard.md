# Empire Dashboard — UI Architecture

> Design doc for the empire management screen and all sub-tab UI surfaces.
> Companion to `ship_modules_v0.md` and `trade_goods_v0.md`.

## Implementation Status (as of Tranche 20, 2026-03-08)

The code implementation (scripts/ui/EmpireDashboard.cs) diverged from this design during
development. The actual implementation has **9 tabs** (not 7), adds 3 tabs not in the
original design, and omits the Fleet tab entirely.

| Design Tab | Code Tab | Keybind | Status |
|-----------|----------|---------|--------|
| Overview | Overview | F1 | ✅ Implemented (basic cards + needs-attention alerts) |
| Economy | Trade | F2 | ✅ Implemented (renamed; economy data + trade routes) |
| Fleet | — | F3 | ❌ **NOT IMPLEMENTED** — no BuildFleetTab() exists |
| Industry | Production | F4 | ✅ Implemented (site list table) |
| Research | Research | F5 | ✅ Implemented (text table sorted by tier) |
| Explore | Intel | F6 | ⚠️ Partial (market intel age only, no discovery phases) |
| Factions | Factions | F7 | ✅ Implemented (rep bars, trade policy, territory count) |
| — | Programs | — | ✅ Added (automation programs — not in original design) |
| — | Stats | — | ✅ Added (player stats + milestones — not in original design) |
| — | Warfronts | — | ✅ Added (faction warfare — not in original design) |

### Key Design Aspirations NOT Yet Implemented
- Fleet tab: master-detail panel with fleet list, cargo, modules, programs
- Sankey-style production chain visualization
- Graphical tech tree with prerequisite lines and domain threads
- Price comparison sparkline table
- Breadcrumb navigation and global search (Ctrl+F)
- Direct action buttons in Needs Attention queue
- Progressive disclosure gating ("Requires [Tech]" on locked tabs)

---

## Design Principles

1. **Every noun is a link.** Fleet names, station names, system names, good names — all clickable, all navigate to that entity's detail view. This is CK3's biggest win and Victoria 3's biggest miss.

2. **Every number answers "so what?"** Not "Efficiency: 60%" but "Efficiency: 60% (ore shortage)." Not "Program: Running" but "Program: buying ore at Sirius, next run 45t." Context transforms data into decisions.

3. **Three-tier information model.** Ambient (always visible HUD) → Contextual (one click/hover) → Deep (dedicated tab). Information lives at ONE tier and flows smoothly between tiers. No info-action divorce.

4. **Progressive disclosure via game state.** Tabs appear grayed with "Requires [Tech]" until unlocked. Empty states ("No contacts yet") are better than hidden tabs. Advanced features (automation, batch ops) appear when entity counts justify them.

5. **Design for late-game first.** If the screen breaks at 50 fleets and 200 trade routes, it breaks. Build for scale, then simplify for early game through progressive reveal.

6. **Click depth budget.** Actions performed 50x/session: 1 click. Actions performed 5x/session: 2 clicks. Rare actions: 3 clicks max from the game world. If a player pauses the game to find a menu, the UI has failed.

7. **Keyboard shortcuts shown in UI.** Tab labeled "Economy (F3)" teaches the shortcut through normal play.

---

## Top-Level Structure

### Navigation: Hub-and-Spoke

Persistent tab bar across the top of the empire screen. Every management surface is one click away. No nested menus to reach a primary category.

**Original design (7 tabs)**:
```
[Overview] [Economy] [Fleet] [Industry] [Research] [Explore] [Factions]
   F1         F2       F3       F4         F5         F6        F7
```

**Current implementation (9 tabs)** — code enum: Overview, Trade, Production, Programs, Intel, Research, Stats, Factions, Warfronts:
```
[Overview] [Trade] [Production] [Programs] [Intel] [Research] [Stats] [Factions] [Warfronts]
   F1        F2        F4                    F6       F5                  F7
```

> **Note**: Fleet tab (F3) was never implemented. Programs, Stats, and Warfronts tabs
> were added during Tranches 10-20 to surface automation, player progression, and
> faction warfare systems that didn't exist when this doc was written.

Seven tabs was the original upper bound for comfortable scanning. The implementation
exceeded this. Consider whether Programs/Stats/Warfronts should be nested as sub-tabs
within primary tabs to reduce top-level count.

**Opening the empire screen:** Single keypress (Tab or E). Opens to the Overview tab by default. Remembers the last-visited tab within a session.

**Closing:** Same key toggles closed, or Escape.

### Breadcrumb Bar

Visible at the top of any drill-down:

```
Empire > Fleet > Alpha Squadron > Vanguard
```

Each segment is clickable. Prevents "where am I?" disorientation in deep views.

### Global Search

`Ctrl+F` opens a search bar searching across all entity names (ships, stations, systems, goods, techs, factions). Results grouped by type. Civ 7 removed this from Civ 6 and players listed it as one of the most painful regressions.

---

## Tab 1: Overview (F1)

> ✅ **Implemented** (basic). 2x3 card grid + needs-attention queue with text alerts.
> Missing: direct action buttons, clickable entity names, severity icons, trend arrows.

The dashboard home. Answers "what needs my attention right now?" without forcing the player to check 6 tabs.

### Layout

```
┌───────────────────────────────────────────────────────────────────┐
│  OVERVIEW                                                   [F1] │
│                                                                   │
│  ┌─ ECONOMY ──────┐  ┌─ FLEET ─────────┐  ┌─ INDUSTRY ───────┐ │
│  │ Credits: 42.1k  │  │ 8 ships          │  │ 12 sites         │ │
│  │ Income: +2.4k/t↑│  │ 3 idle ▲         │  │ Avg eff: 78%     │ │
│  │ 5 routes active │  │ 4 programs run   │  │ 2 need repair ▲  │ │
│  └─────────────────┘  └──────────────────┘  └──────────────────┘ │
│                                                                   │
│  ┌─ RESEARCH ─────┐  ┌─ EXPLORATION ────┐  ┌─ SECURITY ──────┐ │
│  │ Warp III: 60%   │  │ 3 unscanned      │  │ 14 Normal        │ │
│  │ Sustain: OK ●   │  │ 1 anomaly ready  │  │ 3 Strained ▲     │ │
│  │ Queue: 2 techs  │  │ 7 analyzed       │  │ 1 Unstable ▲▲    │ │
│  └─────────────────┘  └──────────────────┘  └──────────────────┘ │
│                                                                   │
│  NEEDS ATTENTION                                                  │
│  [!] Fleet "Vanguard" — program stalled (no ore)    [Configure]  │
│  [!] Forge-7 — health 23%, repair cost 1.2k         [Repair]     │
│  [i] Research complete — choose next tech            [Open Tree]  │
│  [i] Discovery analyzed — anomaly available          [View]       │
│  [i] Trade route expired — Sirius→Proxima            [Renew]      │
└───────────────────────────────────────────────────────────────────┘
```

### Summary Cards

Each card shows 2-3 KPIs with trend arrows (↑↓) and a colored status dot:
- **Green ●** — healthy, no action needed
- **Yellow ●** — attention soon (idle fleets, research near completion)
- **Red ●** — action needed (sites critical, programs stalled)

Each card is clickable → navigates to that tab.

### Needs Attention Queue

Priority-sorted list of items requiring player action. Each entry has:
- Severity icon: `[!]` urgent (red), `[i]` informational (yellow)
- Entity name (clickable link to entity detail)
- Situation description (the "so what?")
- Direct action button (one click to resolve)

The action button is critical — it solves Victoria 3's fatal flaw of showing problems with no path to the fix. Clicking `[Repair]` should open the maintenance panel for that specific site, not a generic industry screen.

**Notification settings:** Gear icon opens category toggles (Fleet alerts, Industry alerts, Research alerts, etc.) with per-category enable/disable. Auto-bundle similar notifications: "3 trade routes expired" rather than 3 separate entries.

---

## Tab 2: Economy (F2)

> ✅ **Implemented** as "Trade" tab. Economy overview + trade routes list.
> Missing: price comparison table, sparkline history, route detail right-panel,
> income/expense breakdown, "Best Buy/Sell" columns, stale price indication.

### Purpose
Income/expenses breakdown, active trade routes, market price comparison across known stations.

### Layout: Master-Detail

```
┌─ ECONOMY ────────────────────────────────────────────────────────┐
│                                                                   │
│  Income: +2,412/t    Expenses: -1,890/t    Net: +522/t ↑        │
│                                                                   │
│  ┌─ TRADE ROUTES (left panel) ──┐  ┌─ ROUTE DETAIL (right) ───┐ │
│  │ Sort: [Profit ▼] Filter: [▽] │  │ Sirius → Proxima          │ │
│  │                               │  │                            │ │
│  │ ● Sirius→Proxima    +340/run │  │ Good: Metal                │ │
│  │ ● Vega→Sol          +280/run │  │ Buy at: 42 (Sirius)       │ │
│  │ ○ Altair→Deneb      expired  │  │ Sell at: 78 (Proxima)     │ │
│  │ ● Barnard→Wolf      +95/run  │  │ Margin: +36 per unit      │ │
│  │                               │  │ Volume: 10/run             │ │
│  │                               │  │ Fleet: "Hauler-2"          │ │
│  │                               │  │ Program: Trade Charter     │ │
│  │                               │  │ Last run: 12t ago          │ │
│  │                               │  │                            │ │
│  │                               │  │ [Edit Route] [Pause] [End]│ │
│  └───────────────────────────────┘  └────────────────────────────┘ │
│                                                                   │
│  ┌─ PRICE COMPARISON ───────────────────────────────────────────┐ │
│  │ Good     │ Sirius │ Proxima │ Vega  │ Sol   │ Best Buy/Sell │ │
│  │ Ore      │  12    │   18    │  14   │  --   │ Buy@Sirius    │ │
│  │ Metal    │  42    │   78    │  55   │  61   │ Sell@Proxima  │ │
│  │ Fuel     │   8    │   11    │  --   │   9   │ Buy@Sirius    │ │
│  │ (stale values in gray italic, fresh in white)                │ │
│  └──────────────────────────────────────────────────────────────┘ │
└───────────────────────────────────────────────────────────────────┘
```

### Key Features

**Price comparison table:** Cross-station price grid using IntelBook observations. Fresh prices in normal text, stale prices (>N ticks old) in gray italic, unknown markets shown as `--`. Column headers are clickable station names (navigate to station). "Best Buy" and "Best Sell" columns highlight arbitrage opportunities.

**Sparkline price history:** Hovering any price cell shows a small sparkline of price over time from intel observations. Shows trend without requiring a dedicated chart screen.

**Income/expense breakdown:** Expandable categories (Trade income, Mission rewards, Program fees, Maintenance costs, Research costs, Repair costs). Follows the Stellaris green/red pattern.

**Trade route list:** Sortable by profit, good, status. Filter by good type, fleet, program status.

---

## Tab 3: Fleet (F3)

> ❌ **NOT IMPLEMENTED**. No BuildFleetTab() method exists in EmpireDashboard.cs.
> Fleet data is available via SimBridge snapshot methods but has no dedicated UI surface.
> This is the single largest missing tab — players cannot manage fleets from the empire screen.

### Purpose
All player fleets — location, status, cargo, programs, combat readiness.

### Layout: Master-Detail

Left panel: fleet list with summary row per fleet.
Right panel: selected fleet detail with full stats and actions.

### Fleet List Row

```
[Icon] Vanguard    Docked @ Sirius    ● Trading ore    Hull 100% Shield 100%
[Icon] Hauler-2    In transit → Prox  ● Trade Charter   Hull 85%  Shield 100%
[Icon] Sentinel    Patrol Vega↔Sol    ● Patrol active   Hull 92%  Shield 78%
[Icon] Explorer    Scanning Altair    ● Expedition      Hull 100% Shield 100%
```

Each row shows: name, location/action, program status with specifics, hull/shield bars.

### Fleet Detail Panel

```
┌─ VANGUARD ───────────────────────────────────────────┐
│                                                       │
│  Class: Corvette        Location: Docked @ Sirius    │
│  Hull: ████████████ 100%   Shield: ████████████ 100% │
│                                                       │
│  ── CARGO (8/12 slots) ──                            │
│  Metal ×4    Ore ×2    Fuel ×2                       │
│                                                       │
│  ── MODULES ──                                       │
│  [Coilgun Mk2]  [Particle Beam]  [Shield Gen Mk1]   │
│  [Cargo Ext]    [Scanner Mk2]    [empty]             │
│  Power: 14/18    Sustain: 2 metal + 1 fuel / cycle   │
│                                                       │
│  ── PROGRAM ──                                       │
│  Trade Charter: Buy ore @ Sirius → Sell metal @ Prox │
│  Status: Running    Next run: 45t    Last profit: +340│
│                                                       │
│  [Refit] [Set Destination] [Programs ▼] [Undock]     │
└───────────────────────────────────────────────────────┘
```

### Scaling Behavior

| Fleet Count | UI Behavior |
|---|---|
| 1-5 | Individual rows, no grouping |
| 6-15 | Sortable list with status filter (Idle/Active/Damaged) |
| 16-50 | Collapsible groups by role (Combat/Trade/Patrol/Explore) |
| 50+ | Aggregate headers ("12 Traders: 10 active, 2 idle") with expand |

### Anti-Pattern Avoidance

X4's fleet management is described as "the worst I've ever seen" because standing orders are buried in nested menus and behavior scripts conflict with manual orders. Our rule: **program status is always visible in the fleet list row**, and manual orders clearly pause/resume automation with a visible indicator.

---

## Tab 4: Industry (F4)

> ✅ **Implemented** as "Production" tab. Simple site list table.
> Missing: Sankey-style production chain visualization, bottleneck highlighting,
> efficiency breakdown on hover, repair/supply action buttons.

### Purpose
Production chain health, site efficiency, maintenance, supply logistics.

### Layout: Two Views

**Chain View (default):** Sankey-style flow diagram showing goods flowing left-to-right through the production chain at the empire level.

```
┌─ PRODUCTION CHAINS ──────────────────────────────────────────────┐
│                                                                   │
│  [Ore] ──→ [Metal] ──→ [Composites] ──→ [Components]            │
│    ↑          ↑              ↑                                    │
│  3 sites    2 sites       1 site                                 │
│  eff: 94%   eff: 78% ▲    eff: 45% ▲▲                           │
│                                                                   │
│  [Fuel] ──→ [Munitions]    [Organics] ──→ [Food]                 │
│  2 sites     1 site         1 site         1 site                │
│  eff: 100%   eff: 88%      eff: 67% ▲     eff: 100%             │
│                                                                   │
│  Bottleneck: Composites plant (Vega) — ore input shortage        │
│  Recommended: Establish logistics route for ore to Vega          │
└───────────────────────────────────────────────────────────────────┘
```

Nodes with low efficiency are highlighted (yellow <80%, red <50%). Clicking any node drills into the site list for that good.

**Site List View:** Master-detail table of all industry sites.

```
┌─ SITES ──────────────────────────┐  ┌─ SITE DETAIL ────────────┐
│ Sort: [Efficiency ▼] Filter: [▽] │  │ Metal Refinery — Sirius   │
│                                   │  │                           │
│ ● Ore Mine — Sirius       94%    │  │ Recipe: Ore → Metal       │
│ ● Ore Mine — Vega         91%    │  │ Efficiency: 78%           │
│ ▲ Metal Refinery — Sirius  78%   │  │  └─ Input avail: 82%     │
│ ▲ Ore Mine — Proxima       72%   │  │  └─ Health: 89%          │
│ ▲▲ Composites — Vega       45%   │  │  └─ Supply: 1/3 days     │
│                                   │  │                           │
│                                   │  │ Health: ████████░░ 89%   │
│                                   │  │ Supply: █░░░░░░░░ 1 day  │
│                                   │  │ Buffer: Ore 4/10 units   │
│                                   │  │                           │
│                                   │  │ Maint cost: 120/cycle    │
│                                   │  │                           │
│                                   │  │ [Repair: 450cr] [Supply] │
└───────────────────────────────────┘  └───────────────────────────┘
```

### Efficiency Breakdown

When hovering or selecting a site, the efficiency bar expands to show WHY:

```
Efficiency: 78%
  ├─ Input availability: 82% (ore supply intermittent)
  ├─ Health condition:   89% (minor degradation)
  └─ Supply level:       1/3 buffer days (low — accelerated decay)
```

This follows the "every number answers so what" principle. Anno 1800's biggest community complaint is that production buildings don't explain WHY they're underperforming — players built third-party calculators to compensate.

---

## Tab 5: Research (F5)

> ✅ **Implemented** as text table sorted by tier.
> Missing: graphical tech tree visualization, domain threads (Combat/Trade/Exploration/Industry),
> sustain status color coding, interactive prerequisite highlighting, "What do I need?" path.

### Purpose
Tech tree visualization, active research progress, sustain status.

### Layout: Tree + Active Panel

```
┌─ RESEARCH ───────────────────────────────────────────────────────┐
│                                                                   │
│  Currently Researching: Warp Drive III                           │
│  Progress: ████████████░░░░░░░░ 60%    ETA: ~200t               │
│  Sustain: ● OK (consuming 2 metal + 1 rare metal / interval)    │
│  Credit cost: 45/tick (1,890 remaining)                          │
│                                                                   │
│  ── TECH TREE ──                                                 │
│                                                                   │
│  TIER 1              TIER 2                TIER 3                │
│  ┌──────────┐        ┌──────────┐         ┌──────────┐          │
│  │✓ Basic   │───────→│✓ Adv     │────────→│░ Warp III│          │
│  │  Warp    │        │  Warp II │         │  [60%]   │          │
│  └──────────┘        └──────────┘         └──────────┘          │
│  ┌──────────┐        ┌──────────┐         ┌──────────┐          │
│  │✓ Sensors │───────→│░ Deep    │────────→│▪ Anomaly │          │
│  │  Mk1     │        │  Scan    │         │  Decode  │          │
│  └──────────┘        └──────────┘         └──────────┘          │
│  ┌──────────┐        ┌──────────┐                                │
│  │✓ Hull    │───────→│▪ Armor   │                                │
│  │  Plating │        │  Mk2     │                                │
│  └──────────┘        └──────────┘                                │
│                                                                   │
│  ✓ = completed   ░ = researching   ▪ = available   ▫ = locked   │
└──────────────────────────────────────────────────────────────────┘
```

### Key Features

**Sustain status prominently displayed.** The sustain mechanic (consuming goods while researching) is our game's distinctive research feature. Show it front-and-center with a clear status indicator:
- **● OK** (green) — all sustain inputs available
- **● Stalling** (yellow) — partial inputs, research slowed
- **● Stalled** (red) — missing inputs, research halted + reason ("no rare metals at research station")

**Hover any tech node** to see: prerequisites highlighted with glowing lines, unlock effects listed, cost breakdown (credits + sustain inputs), estimated research time.

**Hover any locked tech** to see the prerequisite path highlighted — "what do I need to unlock this?" is the #1 question players ask of any tech tree (Factorio insight).

**Domain threads.** Group techs by domain (Combat, Trade, Exploration, Industry) with horizontal threads. This follows the Endless Space 2 quadrant approach but in vertical threads to avoid horizontal scrolling.

---

## Tab 6: Exploration (F6)

> ⚠️ **Partially Implemented** as "Intel" tab. Shows market intel age (node observations).
> Missing: discovery phase visualization (Seen/Scanned/Analyzed), anomaly encounters list,
> encounter reports, scanner status/range, recent encounters log.

### Purpose
Discovery tracker, anomaly encounters, scanner status, exploration progress.

### Layout: Map + Discovery List

```
┌─ EXPLORATION ────────────────────────────────────────────────────┐
│                                                                   │
│  Scanner: Mk2 (range: 3 hops)    Coverage: 14/22 systems        │
│                                                                   │
│  ┌─ DISCOVERIES ─────────────────┐  ┌─ DETAIL ───────────────┐  │
│  │ Filter: [All ▼]               │  │                         │  │
│  │                                │  │ Derelict Wreck          │  │
│  │ ▪ Derelict — Altair     Seen  │  │ Location: Altair-4      │  │
│  │ ░ Signal — Wolf-3    Scanned  │  │ Phase: Seen             │  │
│  │ ░ Ruins — Barnard   Scanned   │  │                         │  │
│  │ ✓ Derelict — Deneb  Analyzed  │  │ "Sensor readings show   │  │
│  │ ✓ Signal — Proxima  Analyzed  │  │  a hull fragment with   │  │
│  │ ✓ Ruins — Vega      Analyzed  │  │  residual power."       │  │
│  │ ✓ Derelict — Sol    Analyzed  │  │                         │  │
│  │                                │  │ Next phase: Scan        │  │
│  │                                │  │ Requires: Scanner Mk1   │  │
│  │                                │  │                         │  │
│  │                                │  │ [Scan] [Set Waypoint]   │  │
│  └────────────────────────────────┘  └─────────────────────────┘  │
│                                                                   │
│  ── RECENT ENCOUNTERS ──                                         │
│  Deneb Derelict: Salvaged 4 metal + 2 components (12t ago)       │
│  Vega Ruins: Found ancient samples + 500 credits (45t ago)       │
└──────────────────────────────────────────────────────────────────┘
```

### Phase Visualization

Inspired by Outer Wilds' Ship Log — each discovery has a clear visual state:
- **▪ Seen** (gray) — detected, not yet scanned
- **░ Scanned** (amber) — data collected, analysis pending
- **✓ Analyzed** (green) — fully understood, encounter resolved

On the galaxy map (GalaxyView), discovery sites show these same icons at their spatial location so the player can see their exploration frontier without opening this tab.

### Encounter Reports

When an anomaly encounter resolves, show a brief card:

```
┌─ ENCOUNTER REPORT ──────────────────────────┐
│ Derelict Wreck — Deneb System                │
│                                               │
│ "The hull fragment contained salvageable      │
│  components and trace exotic matter."         │
│                                               │
│ Recovered:                                    │
│   Metal ×4    Components ×2                   │
│   Credits: +200                               │
│                                               │
│ Discovery lead: Signal detected at Altair-7   │
│                                     [Close]   │
└───────────────────────────────────────────────┘
```

---

## Tab 7: Factions (F7)

> ✅ **Implemented**. Rep bars, trade policy, territory count per faction.
> Missing: recent reputation changes log (last 5-10 actions with deltas),
> named threshold zones on rep bar (Hostile/Neutral/Friendly/Allied).

### Purpose
Reputation standings, faction territories, diplomatic effects.

### Layout: Faction List + Detail

```
┌─ FACTIONS ───────────────────────────────────────────────────────┐
│                                                                   │
│  ┌─ STANDINGS ───────────────────┐  ┌─ DETAIL ───────────────┐  │
│  │                                │  │                         │  │
│  │ Sol Federation                 │  │ SOL FEDERATION          │  │
│  │ ████████████░░░░ +35 Friendly  │  │                         │  │
│  │                                │  │ Standing: +35 Friendly  │  │
│  │ Outer Rim Syndicate            │  │ Effect: Market discount │  │
│  │ ████░░░░░░░░░░░░  -8 Neutral  │  │         active (-5%)    │  │
│  │                                │  │                         │  │
│  │ Frontier Alliance              │  │ Territory: 6 systems    │  │
│  │ ██████████░░░░░░ +22 Neutral   │  │ Stations: 4            │  │
│  │                                │  │                         │  │
│  │ Deep Core Mining Corp          │  │ ── RECENT CHANGES ──   │  │
│  │ ██████████████░░ +48 Friendly  │  │ +3 Trade at Proxima     │  │
│  │                                │  │ +2 Mission completed    │  │
│  │                                │  │ -1 Refused inspection   │  │
│  └────────────────────────────────┘  └─────────────────────────┘  │
│                                                                   │
│  THRESHOLDS                                                       │
│  -100──Hostile──-50──Unfriendly──-10──Neutral──+25──Friendly──+75──Allied──+100
└──────────────────────────────────────────────────────────────────┘
```

### Key Features

**Named thresholds on the reputation bar.** The bar itself shows Hostile/Unfriendly/Neutral/Friendly/Allied zones with color transitions. The player's current position is marked.

**Active effects.** Show what the current standing gives: market discounts, restricted access, patrol response, etc. This follows the "so what?" principle — the number means nothing without its gameplay consequence.

**Recent changes log.** Shows the last 5-10 reputation-affecting actions with delta values. Star Traders: Frontiers does this well — players understand WHY their standing changed.

---

## The UI Scope Hierarchy

The same design principles apply at every scope level, but manifest differently.

### Three Scopes

| Scope | Screen | Player Question | When Open | Density |
|---|---|---|---|---|
| **Strategic** | Empire Dashboard | "What needs attention across everything I own?" | Periodically, to plan | Summary KPIs, drill-down |
| **Tactical** | Dock Menu (Station/Planet) | "What can I do here, right now?" | While docked | Full local detail, all local actions |
| **Glance** | Galaxy Map Popup | "Should I go there? What's there?" | Browsing the map | Minimal, decision-support only |

### How Principles Apply at Each Scope

| Principle | Strategic (Empire) | Tactical (Dock) | Glance (Map Popup) |
|---|---|---|---|
| Every noun is a link | Fleet name → fleet detail | Good name → price comparison | Node name → set waypoint |
| Numbers answer "so what?" | "Avg eff: 78% (2 sites need repair)" | "eff: 72% (ore shortage)" | "Security: Moderate (patrol needed)" |
| Progressive disclosure | Tabs grayed until unlocked | Sections hidden when empty | Sections collapsed by default |
| Info-action connection | Alert → inline action button | Stat → repair/buy/sell button | Info → `[Go Here]` / `[View Market]` |
| Click depth budget | 2 clicks from HUD to any KPI | 1 click within dock menu to any action | 0 clicks (info visible on hover/click) |

### Cross-Linking Between Scopes

Every scope links to the others. The player should never feel trapped in one view:

- **Dock → Empire**: "View empire-wide prices" link in dock market → opens Economy tab
- **Empire → Dock**: Fleet detail `[Refit]` → opens dock Services (if docked at a station)
- **Empire → Map Popup**: Station name in industry list → opens galaxy map centered on that node
- **Map Popup → Dock**: `[Dock Here]` button (if in range) or `[Set Waypoint]`
- **Map Popup → Empire**: `[View in Fleet Tab]` for fleets at that node
- **Dock → Map Popup**: Station name header → centers galaxy map on current node

---

## Dock Menu (Station & Planet)

### Current State

The dock menu (`hero_trade_menu.gd`) uses 3 tabs: **Market / Jobs / Services**. The Services tab contains 8 collapsible sections (Research, Refit, Maintenance, Construction, Trade Routes, Automation, Anomaly Encounters, plus contextual additions).

### The Problem

8 collapsible sections in one tab is a mini menu-labyrinth. The player docks at a station and faces a wall of `[+]` toggles. This is the X4 anti-pattern at smaller scale — everything crammed into one surface. The Market and Jobs tabs are clean, but Services is doing too much.

### Recommended Restructure: 5 Tabs

Split the current 3-tab dock menu into 5 tabs. This stays within the comfortable scanning range and separates concerns:

```
[Market] [Jobs] [Ship] [Station] [Intel]
```

```
┌─ DOCKED @ SIRIUS STATION ──────────────────────────────────────┐
│ Sol Federation territory │ Tariff: 5% │ Security: Safe ●       │
│ Produces: Metal, Munitions │ Imports: Ore, Fuel                │
├─[Market]─[Jobs]─[Ship]─[Station]─[Intel]───────────────────────┤
│                                                                  │
│  (tab content here)                                             │
│                                                                  │
│                                              [Undock]           │
└──────────────────────────────────────────────────────────────────┘
```

#### Tab 1: Market (unchanged)
What it is now — buy/sell goods with quantity buttons, cargo display.

Add: sparkline price history on hover, "best known price" comparison column from IntelBook data.

```
┌─ MARKET ─────────────────────────────────────────────────────────┐
│  Good        Buy    Sell    Qty     Best Known              Cargo│
│  Ore          12     10     45      Buy cheaper @ Vega (8)     2│
│  Metal        42     38     12      Sell higher @ Proxima (78) 4│
│  Fuel          8      6     30      --                         2│
│  Munitions    95     88      3      --                         0│
│                                                                  │
│  [Buy 1] [Buy 5] [Buy Max]    [Sell 1] [Sell 5] [Sell Max]     │
│                                                                  │
│  Cargo: 8/12 │ Credits: 42,100                                  │
└──────────────────────────────────────────────────────────────────┘
```

The "Best Known" column is the killer feature from Elite Dangerous that the community demanded — it uses your existing IntelBook observations to show arbitrage at a glance. Gray italic if intel is stale.

#### Tab 2: Jobs (unchanged)
Active mission status, available missions, accept button.

Add: mission difficulty indicator (based on distance, combat requirement), estimated reward per tick.

#### Tab 3: Ship (refocused)
Everything about YOUR ship at this station. Consolidates Refit + fleet combat profile + cargo management.

```
┌─ SHIP ───────────────────────────────────────────────────────────┐
│  VANGUARD — Corvette                                             │
│  Hull: ████████████ 100%    Shield: ████████████ 100%           │
│                                                                  │
│  ── MODULES ──                         ── FITTING BUDGET ──     │
│  [Coilgun Mk2]   Weapon   12 pwr      Slots:   5/6             │
│  [Particle Beam]  Weapon   18 pwr      Power:  42/55 ████████░░│
│  [Shield Gen Mk1] Utility   8 pwr      Sustain: 2 metal + 1 fuel│
│  [Cargo Ext]      Cargo     0 pwr                                │
│  [Scanner Mk2]    Utility   4 pwr      ── COMBAT PROFILE ──    │
│  [empty]          Weapon    --          DPS: 14.2                │
│                                         Tank: 850 EHP           │
│  Available Modules:                     Speed: 18               │
│  ┌─────────────────────────────────┐                             │
│  │ Railgun Mk1 — Weapon — 15 pwr  │                             │
│  │ Req: Kinetics II ✓  Cost: 1.2k │                             │
│  │ [Install → Slot 6]             │                             │
│  └─────────────────────────────────┘                             │
│                                                                  │
│  Refit in progress: Shield Gen Mk2 — 3/5 ticks remaining       │
└──────────────────────────────────────────────────────────────────┘
```

All three fitting budgets (Slots, Power, Sustain) visible simultaneously — the EVE Online lesson. The combat profile (DPS, EHP, Speed) updates live as modules are changed — showing consequences, not just inputs.

#### Tab 4: Station (refocused)
Everything about THIS STATION — industry, construction, maintenance, local automation programs.

```
┌─ STATION ────────────────────────────────────────────────────────┐
│                                                                   │
│  ── INDUSTRY ──                                                  │
│  Metal Refinery    eff: 78% (ore shortage)   hp: 89%  [Repair: 450]│
│  Munitions Plant   eff: 100%                 hp: 95%             │
│  Ore Mine          eff: 94%                  hp: 100%            │
│                                                                   │
│  ── CONSTRUCTION ──                                              │
│  Composites Lab    Step 3/8    ████░░░░ 37%    Cost: 200/step    │
│                                                                   │
│  ── AUTOMATION (at this station) ──                              │
│  Auto-Buy Ore ×10/cycle     ● Running     Next: 12t             │
│  Trade Charter → Proxima    ● Running     Profit: +340 last     │
│                                                                   │
│  ── RESEARCH (if researching here) ──                            │
│  Warp Drive III    ████████░░░░ 60%    Sustain: ● OK             │
│  Consuming: 2 metal + 1 rare metal / interval                   │
│  [Change Tech] [Pause]                                           │
└──────────────────────────────────────────────────────────────────┘
```

This answers "what's happening at this station?" in one view. Industry sites show efficiency with cause, health with repair button. Construction shows progress. Automation shows only programs bound to THIS station (not all programs empire-wide — that's the Empire screen's job).

Research appears here only if the player's research node is this station. This is contextual — if you're not researching here, you don't see a research section.

#### Tab 5: Intel (refocused)
Scanner results, trade route discovery, anomaly encounters, discovery sites — everything about information gathering from this location.

```
┌─ INTEL ──────────────────────────────────────────────────────────┐
│                                                                   │
│  Scanner: Mk2 (range: 3 hops)                                   │
│                                                                   │
│  ── NEARBY MARKETS (scanner range) ──                            │
│  Proxima (2 hops)   Metal: 78 buy   Fuel: 11 buy   (45t ago)   │
│  Vega (3 hops)      Ore: 8 buy      Metal: 55 buy  (120t ago)  │
│                                                                   │
│  ── TRADE ROUTES ──                                              │
│  Metal Sirius→Proxima   margin: +36/unit   [Launch Charter]     │
│  Ore Vega→Sirius        margin: +4/unit    [Launch Charter]     │
│                                                                   │
│  ── DISCOVERIES (at this node) ──                                │
│  ▪ Derelict Wreck — Seen           [Scan]                       │
│                                                                   │
│  ── ANOMALY ENCOUNTERS ──                                        │
│  (none available)                                                │
└──────────────────────────────────────────────────────────────────┘
```

### Why 5 Tabs Instead of 3

The current 3-tab structure (Market / Jobs / Services) puts ~8 unrelated concerns into Services. The 5-tab structure groups by **domain**:

| Tab | Domain | Player Mindset |
|---|---|---|
| Market | Trading | "Buy/sell goods" |
| Jobs | Missions | "What tasks are available?" |
| Ship | My vessel | "Upgrade/configure my ship" |
| Station | This place | "What's built here? What needs repair?" |
| Intel | Information | "What do I know? What can I learn?" |

Each tab answers ONE question. No section within a tab is unrelated to its neighbors. The player's mental model maps cleanly to tab selection.

### Progressive Disclosure Within Dock Menu

- **Before first trade**: Only Market tab visible, others grayed ("Dock at a station to access services")
- **After first refit module unlocked**: Ship tab activates
- **After first industry site at a node**: Station tab populates
- **After scanner tech researched**: Intel tab activates
- **Empty sections within a tab**: Hidden entirely (existing hide-empty behavior, keep it)

---

## Galaxy Map Node Popup

### Current State

`node_detail_popup.gd` shows: node name, world class, fleet count, industry count, security band, collapsible market goods table.

### Recommended Enhancement

The popup should answer "should I go here?" with minimal data and maximum actionability.

```
┌─ SIRIUS STATION ──────────────────── [×] ┐
│ Class: G-type │ Sol Federation territory  │
│                                           │
│ Security: Safe ●                          │
│ Fleets: 3 (1 yours)   Industry: 4 sites  │
│                                           │
│ ── MARKET (tap to expand) ──             │
│ Ore: 12/10  Metal: 42/38  Fuel: 8/6     │
│ (buy/sell prices, read-only)              │
│                                           │
│ ── YOUR FLEET HERE ──                    │
│ Vanguard — Docked, Trading ore           │
│                                           │
│ [Set Waypoint]  [View in Fleet Tab]      │
└───────────────────────────────────────────┘
```

### Key Principles for the Popup

**Minimal footprint.** The popup overlays the galaxy map. It must not obscure too much of the map. Keep it narrow and short — expand on demand, not by default.

**Decision-support data only.** The popup exists to answer "should I go here?" Show: security (is it safe?), market summary (is it profitable?), faction (can I trade?), your fleet presence (am I already there?). Do NOT show industry efficiency, research progress, or construction — that's tactical detail for when you're docked.

**Action affordances.** The popup is not purely read-only:
- `[Set Waypoint]` — most common action from map browsing
- `[View in Fleet Tab]` — if your fleet is there, link to empire fleet detail
- `[Dock]` — if you're at this node and undocked
- Market prices are read-only (no buy/sell from the map — that's the dock menu's job)

**Intel freshness.** Market prices shown in the popup come from IntelBook. Show freshness: white = fresh, gray italic = stale, `--` = unknown. This connects to the scanner system — the player sees "I don't have current prices for this node, maybe I should scan it."

### Planet vs Station Popups

The same popup structure works for both, with contextual differences:

**Station popup** (above): market, industry, fleet presence.

**Planet popup:**
```
┌─ KEPLER-7b ──────────────────────── [×] ┐
│ Terrestrial │ Frontier Alliance territory│
│                                           │
│ Gravity: 1.2g │ Atmo: Thin │ Temp: Cold  │
│ Specialization: Mining Colony             │
│ Security: Moderate ●                      │
│                                           │
│ ── MARKET ──                             │
│ Ore: 8/6  Rare Metals: 120/95           │
│                                           │
│ Landing requires: Atmo Shielding Mk1 ✓  │
│                                           │
│ [Set Waypoint]                            │
└───────────────────────────────────────────┘
```

Planets show physical characteristics (gravity, atmosphere, temperature) because these have gameplay implications (landing tech requirements). Stations don't need this — they're always dockable.

### Undiscovered / Unvisited Nodes

For nodes the player hasn't visited and has no intel on:

```
┌─ UNKNOWN SYSTEM ─────────────────── [×] ┐
│ Class: K-type │ Unexplored               │
│                                           │
│ No market intel available.               │
│ Scanner range: Out of range (5 hops)     │
│                                           │
│ [Set Waypoint]                            │
└───────────────────────────────────────────┘
```

The absence of data IS information — it motivates exploration.

---

## HUD Integration

The HUD (`hud.gd`) remains the ambient (Tier 1) information layer. It should surface just enough to prompt the player to open the empire screen or dock menu:

```
┌─────────────────────────────────────────────────────────────────┐
│ Credits: 42,100 (+522/t)  │  Cargo: 8/12  │  [!] 3 alerts     │
│ Research: Warp III 60%    │  Hull: 100%   │  [Empire: Tab]     │
└─────────────────────────────────────────────────────────────────┘
```

The `[!] 3 alerts` badge is the bridge between HUD and empire screen — clicking it opens the Overview tab's Needs Attention queue.

### HUD While Docked

When docked, the HUD subtly shifts to show dock-relevant info:

```
┌─────────────────────────────────────────────────────────────────┐
│ Credits: 42,100  │  Cargo: 8/12  │  Docked @ Sirius Station    │
│ Research: 60% ●  │  Hull: 100%   │  [Undock: Space]            │
└─────────────────────────────────────────────────────────────────┘
```

The HUD doesn't duplicate dock menu info — it just shows "you're docked" and the undock shortcut.

---

## Tooltip Architecture

Three-layer tooltips throughout all empire screen surfaces:

### Layer 1: Surface (always visible)
Icon + number or short text. Example: efficiency bar showing "78%".

### Layer 2: Hover (on mouse-over)
3-5 line summary with context. Example:
```
Metal Refinery — Sirius
Efficiency: 78%
  Input availability: 82% (ore intermittent)
  Health: 89% (minor degradation)
Output: 4 metal / cycle
```

### Layer 3: Expanded (Shift+hover or click-pin)
Full breakdown with historical data. Example:
```
Metal Refinery — Sirius (Site ID: ind_003)
Recipe: 2 Ore → 1 Metal (8 ticks/cycle)
Efficiency: 78% (was 92% 200t ago)
  Input availability: 82%
    Ore buffer: 4/10 (1.2 days remaining)
    Last resupply: 45t ago via Hauler-2
  Health: 89% (decay: 2%/day at current supply)
    Supply level: 1/3 buffer days
    Repair cost: 450 credits
  Maintenance cost: 120 credits/cycle
Output: 4 metal/cycle (peak: 5.2/cycle)
```

---

## Tabs Added During Implementation (Not in Original Design)

These tabs were added during Tranches 10-20 to surface systems that didn't exist when
this doc was written. They have no corresponding design spec above.

### Programs Tab
> ✅ **Implemented** (Tranche 10). Lists active automation programs (TradeCharter,
> ResourceTap, Patrol, Escort, etc.) with status, cadence, and market bindings.
> Addresses the need to manage automation that the original design placed in the
> Economy tab's "Trade Routes" section.

### Stats Tab
> ✅ **Implemented** (Tranche 12). Player statistics (nodes visited, goods traded,
> credits earned, techs unlocked, missions completed) and milestone achievements.
> Player progression surface that didn't exist in the original design scope.

### Warfronts Tab
> ✅ **Implemented** (Tranche 20). Lists active warfronts with combatant factions,
> intensity level, war type, supply progress bars, embargo status, and instability
> effects. Surfaces faction warfare mechanics from S7 that were added after this
> doc was written.

---

## Scaling Strategy

### Auto-Grouping Thresholds

| Entity Count | UI Behavior |
|---|---|
| 1-5 | Individual cards/rows, no grouping |
| 6-15 | Sortable list with section headers |
| 16-50 | Sort + filter + collapsible groups by category |
| 51-200 | Aggregated group headers by default, expand to see individuals |
| 200+ | Empire-level summary only, drill down by exception |

### What Gets Grouped

- **Fleets** → by role (Combat / Trade / Patrol / Explore)
- **Trade routes** → by good type or by region
- **Industry sites** → by recipe output
- **Discoveries** → by phase (Seen / Scanned / Analyzed)
- **Notifications** → by category, auto-bundled ("3 trade routes expired")

---

## Anti-Patterns to Avoid

| Anti-Pattern | Example | Our Rule |
|---|---|---|
| **Info-action divorce** | V3: see problem, can't find control | Every diagnostic links to its control |
| **Menu labyrinth** | X4: one mega-screen for everything | Separate tabs for separate domains |
| **Click tax** | Stellaris: 31 clicks to manage a planet | Frequent actions ≤ 2 clicks |
| **Phantom features** | Civ 7: capabilities exist but invisible | Never hide accessible features |
| **Notification flood** | Stellaris large galaxy | Category filters + auto-bundling |
| **Numbers without context** | "Efficiency: 60%" | "Efficiency: 60% (ore shortage)" |
| **Pretty but empty** | Minimalism obscuring complexity | Elegance is a constraint, not the goal |
| **Density mismatch** | Spreadsheet for simple choices | Match density to decision complexity |

---

## Reference Games

| Mechanic | Best Reference | Key Lesson |
|---|---|---|
| Market/trading | Offworld Trading Company | Sparkline price trends per commodity |
| Module fitting | EVE Online | Three constraint bars visible simultaneously |
| Tech tree | Factorio | Show path to desired tech, not just the tree |
| Production chains | Anno 1800 (failures) + Factorio | Visual chain diagram with net production |
| Fleet automation | Distant Worlds 2 | Automation status visible in fleet list row |
| Exploration | Outer Wilds Ship Log | Web of discoveries with phase states |
| Faction standing | Star Traders: Frontiers | Named thresholds + recent changes log |
| Risk/security | EVE Online sec status | Color-coded per-system, internalized in 1 hour |
| Empire overview | Stellaris + Distant Worlds 2 | Dashboard cards with KPIs + drill-down |

---

## Implementation Notes

- All data reads go through SimBridge snapshot queries (existing pattern)
- Empire screen is a Godot Control node, not a separate scene — overlays the game world
- Tab switching is instant (no scene loads) — each tab is a prebuilt panel, shown/hidden
- The Overview tab's Needs Attention queue is populated from bridge alert queries
- Galaxy map overlays (trade flow, security, faction territory) are lens toggles on GalaxyView, not empire screen tabs — geographic data belongs on the map
