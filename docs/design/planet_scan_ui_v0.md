# Planet Scan UI Design — v0

> Design doc for the GDScript UI layer that surfaces the T42 PlanetScanSystem to the player.
> Backend: PlanetScanSystem (SimCore), SimBridge.Planet.cs (6 bridge methods), PlanetScanContentV0 (270+ lines of flavor/hints/FO lines).

---

## Design Principles

1. **Scanning is a strategic choice, not a chore.** Mode selection is the skill. The UI must make the choice legible (show affinity previews) without solving it for the player.
2. **Two tiers of commitment.** Orbital scan from flight (quick peek, 1 charge). Landing scan while docked (deeper, 1 charge + fuel). UI reflects this split.
3. **Results DO something.** Every scan result connects to a player decision: a trade route, a discovery to investigate, a fragment to collect, a signal to follow. The UI must show the action, not just the data.
4. **Progressive reveal.** Show data arriving, not arrived. Category icon slides in, flavor text types out, hint fades in. Audio punctuates each phase.
5. **Charge budget is always visible at planet nodes.** The player must know their remaining capacity to plan scan allocation across multiple planets in a system.

### Reference Pattern Summary

| Pattern | Source | Application |
|---|---|---|
| Scanning as infrastructure | X4 Foundations | Scan results enable SurveyProgram automation |
| Two-tier scan (sweep + detail) | Elite Dangerous FSS/DSS | Orbital = sweep, Landing = detail |
| Active choice over hold-to-wait | Elite Dangerous frequency tuning | Mode selection IS the skill expression |
| Progressive reveal | No Man's Sky "???" fields | Typewriter flavor text, staged card reveal |
| Knowledge graph feeding | Outer Wilds ship log | Scan results create KG connections visibly |
| Completion tracking creates pull | No Man's Sky fauna counter | "3/6 planet types surveyed" in scanner panel |
| Audio is critical | All reference games | Rising tone, category-specific chime, ambient sensor ping |

### Anti-Patterns Avoided

- Hold-button-and-wait with no variation (our scans are instant after mode choice)
- Information overload on results (headline + flavor + hint, details on drill-down)
- Results go into a codex nobody opens (results show actionable next steps)
- Mandatory exhaustive scanning (charge budget prevents this by design)
- Scanning disconnected from core loop (ResourceIntel feeds trade, SignalLead feeds exploration, FragmentCache feeds Haven)

---

## UI Components

### Component 1: Scanner Charge HUD Indicator

**Location:** HUD Zone C (left side), position Vector2(10, 395), above existing scan progress.
**Visibility:** Shown when player is at a node with a planet. Hidden during transit and at planet-less nodes.
**Update:** Every frame (poll `GetScanChargesV0()`).

```
┌─────────────────────┐
│ ⟨ 2/2 ⟩  SCANNER    │   ← Green when >1, Orange when =1, Red when =0
│ [■] Mineral          │   ← Active mode highlighted
│ [□] Signal  (locked) │   ← Greyed + "(locked)" if tier too low
│ [□] Arch    (locked) │
│                      │
│ [ ORBITAL SCAN ]     │   ← Button, enabled if charges > 0
└─────────────────────┘
```

**Behavior:**
- Click mode button to select scan mode (persists until changed).
- Click "ORBITAL SCAN" to execute from flight. Result appears as scan result toast (Component 3).
- Mode buttons show lock state based on `GetScanChargesV0()` fields: `mineral_available`, `signal_available`, `archaeological_available`.
- Charge count: `remaining` / `max` from bridge.
- On charge spent: brief pulse animation on the counter (scale 1.0 → 1.2 → 1.0 over 0.3s).
- On charges exhausted: counter turns red, scan button disabled, tooltip "Charges depleted. Travel to another system to reset."

**Node structure:**
```
ScannerHudPanel (PanelContainer)
  ├── ScannerVBox (VBoxContainer)
  │   ├── ChargeLabel (Label)          "⟨ 2/2 ⟩  SCANNER"
  │   ├── ModeContainer (VBoxContainer)
  │   │   ├── MineralButton (Button)
  │   │   ├── SignalButton (Button)
  │   │   └── ArchButton (Button)
  │   └── ScanButton (Button)          "ORBITAL SCAN"
  └── (StyleBoxFlat: dark bg, 2px left border YELLOW)
```

**Styling:** Matches Active Leads panel pattern — dark background (0.05, 0.07, 0.12, 0.85), 2px left border in UITheme.YELLOW, FONT_CAPTION for charge label.

---

### Component 2: Station Tab Scan Section

**Location:** `hero_trade_menu.gd`, Station tab (_tab_station), new section after Port Briefing Section 6 (Signals).
**Visibility:** Shown when docked at a planet node. Hidden at stations without planets.
**Update:** On dock open + after each scan action.

```
━━━ PLANET SCANNER ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

  Desert Alpha  ·  Sand World  ·  1.2g  ·  20% atmo  ·  Hot  ·  Mining

  Scanner: Mk1 (3/3 charges)     Fuel: 8

  ┌─ MODE AFFINITY ────────────────────────────────────┐
  │  Mineral Survey   ████████████████░░░░  1.5x  ★    │  ← Best match highlighted
  │  Signal Sweep     ████████░░░░░░░░░░░░  0.6x       │
  │  Archaeological   ██████████████░░░░░░  1.1x       │
  └────────────────────────────────────────────────────┘

  [ ORBITAL SCAN ]    [ LANDING SCAN ]    [ INVESTIGATE ]
       1 charge          1 charge             docked
                         + 1 fuel             5-15 ticks

  ━━━ SCAN HISTORY (2 results) ━━━━━━━━━━━━━━━━━━━━━━━━

  ┌─ Mineral Survey · Orbital · ResourceIntel ─────────┐
  │  "Rare metal concentrations detected in deep       │
  │   substrate. Purity grade: commercial."            │
  │                                                     │
  │  💡 Strong signal harmonics in the substrate.       │
  │     A Signal Sweep might reveal more here.          │
  │                                                     │
  │  Affinity: 1.5x  ·  Tick 847                       │
  └─────────────────────────────────────────────────────┘

  ┌─ Mineral Survey · Landing · PhysicalEvidence ──────┐
  │  "Excavation site. Erosion patterns indicate       │
  │   deliberate mining — then abandonment."           │
  │                                                     │
  │  [ INVESTIGATE ]  Spend 5-15 ticks docked for      │
  │                   bonus knowledge connections.      │
  │                                                     │
  │  Affinity: 1.5x  ·  Tick 852                       │
  └─────────────────────────────────────────────────────┘
```

**Behavior:**
- **Mode Affinity bars:** Calculated from `PlanetScanTweaksV0.GetAffinityBps(mode, planetType)`. Bars scaled 0-20 chars where 10000 bps = 10 chars. Star icon on highest affinity mode.
- **Orbital Scan button:** Calls `OrbitalScanV0(nodeId, selectedMode)`. Disabled if charges = 0.
- **Landing Scan button:** Calls `LandingScanV0(nodeId, selectedMode)`. Disabled if not landable, charges = 0, or fuel < 1. Shows "Requires planetary_landing_mk1" tooltip if tech-locked.
- **Atmospheric Sample button:** Replaces Landing Scan for Gaseous planets. Calls `AtmosphericSampleV0(nodeId, selectedMode)`.
- **Investigate button:** Calls `InvestigateFindingV0(scanId)`. Only shown for PhysicalEvidence results where `investigation_available = true` and `investigated = false`. Shows "Investigated" badge once complete.
- **Scan History:** Lists all results from `GetPlanetScanResultsV0(nodeId)`, newest first. Each result is a card with: mode, phase, category, flavor text, hint text (orbital only), affinity score, tick. Max 5 visible with scroll.
- **On scan completion:** New result card animates in at top of history (slide down from top, 0.3s). Toast fires simultaneously (Component 3).

**Node structure (added to _station_info_container):**
```
ScanSection (VBoxContainer)
  ├── ScanHeaderLabel (Label)           "━━━ PLANET SCANNER ━━━"
  ├── PlanetSummaryLabel (Label)        Type + properties one-liner
  ├── ChargeStatusLabel (Label)         "Scanner: Mk1 (3/3 charges)  Fuel: 8"
  ├── AffinityContainer (VBoxContainer)
  │   ├── AffinityHeader (Label)        "MODE AFFINITY"
  │   ├── MineralAffinityRow (HBox)     [Label "Mineral Survey"] [ProgressBar] [Label "1.5x"]
  │   ├── SignalAffinityRow (HBox)      ...
  │   └── ArchAffinityRow (HBox)        ...
  ├── ActionContainer (HBoxContainer)
  │   ├── OrbitalScanButton (Button)
  │   ├── LandingScanButton (Button)
  │   └── InvestigateButton (Button)
  ├── HistoryHeaderLabel (Label)        "━━━ SCAN HISTORY (N results) ━━━"
  └── HistoryScroll (ScrollContainer)
      └── HistoryVBox (VBoxContainer)
          └── [ScanResultCard] x N
```

---

### Component 3: Scan Result Toast

**Location:** ToastManager (top-right stack).
**Trigger:** On any successful scan (orbital, landing, atmospheric).
**Priority:** "milestone" (gold, 4.0s duration).

```
┌─────────────────────────────────┐
│ ★ SCAN COMPLETE                 │
│ ResourceIntel · Mineral Survey  │
│ "Rare metal concentrations..."  │
└─────────────────────────────────┘
```

**Behavior:**
- Category icon + category name + mode name on line 1.
- Truncated flavor text (first 60 chars + "...") on line 2.
- For rare findings (FragmentCache, DataArchive, PhysicalEvidence with investigation): use "critical" priority (red, 5.0s, persist until dismissed) with action hint.
- Audio: Play category-specific chime (see Audio section).

---

### Component 4: Scan Result Modal (P1 polish)

**Location:** Center screen overlay, above dock menu.
**Trigger:** On scan completion, after toast. Optional — player can dismiss immediately.
**Duration:** Auto-dismiss after 5s or on click.

```
┌──────────────────────────────────────────────┐
│                                              │
│         ◈  RESOURCE INTEL                    │  ← Category icon + name (fade in 0.3s)
│                                              │
│  "Rare metal concentrations detected in      │  ← Flavor text (typewriter 40 chars/s)
│   deep substrate. Purity grade: commercial." │
│                                              │
│  💡 Strong signal harmonics in the           │  ← Hint text (fade in after flavor, 0.5s delay)
│     substrate. A Signal Sweep might          │
│     reveal more here.                        │
│                                              │
│  ──────────────────────────────────────────  │
│  Mode: Mineral Survey  ·  Affinity: 1.5x    │  ← Stats line (fade in 0.3s)
│  Phase: Orbital  ·  Charges: 1/3 remaining   │
│                                              │
│           [ DISMISS ]  [ VIEW IN LOG ]       │
└──────────────────────────────────────────────┘
```

**Behavior:**
- **Progressive reveal:** Icon slides in (0.3s) → flavor text typewriter (40 chars/s) → hint text fades in (0.5s delay after flavor complete) → stats line fades in (0.3s after hint).
- **Category-specific styling:** ResourceIntel = blue accent, SignalLead = purple, PhysicalEvidence = amber, FragmentCache = green, DataArchive = cyan.
- **"View in Log" button:** Switches to Station tab and scrolls to the result card.
- **Skip animation:** Click anywhere during reveal to instantly show all content.

---

### Component 5: Galaxy Map Scan Markers (P1)

**Location:** GalaxyView.cs, rendered as 3D overlays on planet nodes.
**Visibility:** When galaxy map is open (TAB key).

**Node visual states:**
- **No planet:** Standard node beacon (existing).
- **Planet, unscanned:** Small planet-type icon (colored dot: brown=Sand, white=Ice, red=Lava, blue=Gaseous, grey=Barren, green=Terrestrial).
- **Planet, partially scanned:** Planet icon + scan ring (half-circle, yellow).
- **Planet, fully scanned (all modes used):** Planet icon + full ring (green).
- **Signal Lead connection:** Dashed purple line between triangulated signal sources.

---

## Audio Design

### Scan Execution
- **Mode selection click:** Soft UI click (existing `ui_click` SFX).
- **Scan initiated:** Rising tone (0.5s, pitch 200Hz → 800Hz). Distinct from discovery scan chime.
- **Scan complete:** Category-specific chime:
  - ResourceIntel: Cash register "cha-ching" (short, economic)
  - SignalLead: Radar ping (2 short blips, mysterious)
  - PhysicalEvidence: Deep resonant tone (archaeological weight)
  - FragmentCache: Crystal chime (rare, precious)
  - DataArchive: Data modem burst (information arriving)

### Charge Budget
- **Charge spent:** Soft "click-descend" (pitch drops slightly per charge spent).
- **Charges exhausted:** Low warning tone (single, not alarming — this is expected, not an error).
- **Charges reset (travel):** Soft ascending chime (3 notes, "refreshed").

### Ambient
- **At planet node (flight):** Subtle sensor ping every 3-5s (ambient, not attention-grabbing). Pitch varies by planet type. Signals "there's something here to scan."
- **During dock at planet:** Sensor ping becomes slightly more insistent (every 2s). Stops after first scan.

---

## Interaction Flow

### Flow 1: First Planet Encounter (Tutorial-Adjacent)

```
Player arrives at node with Sand planet "Korrath Prime"
  → HUD: Scanner panel appears (Component 1)
     "⟨ 2/2 ⟩ SCANNER  [■ Mineral] [□ Signal locked] [□ Arch locked]"
  → Ambient: Sensor ping begins
  → FO trigger: FIRST_PLANET_SURVEYED fires on first scan

Player clicks [ORBITAL SCAN]
  → Audio: Rising tone → ResourceIntel chime
  → Toast: "★ SCAN COMPLETE — ResourceIntel · Mineral Survey"
  → Modal (P1): Flavor text typewriter + hint about Signal Sweep
  → HUD: Charges update to "⟨ 1/2 ⟩"
  → FO dialogue: "Interesting readings, Captain..." (teaches scanning)

Player docks at planet
  → Station Tab: Scan Section appears (Component 2)
  → Affinity bars show Mineral Survey at 1.5x (best)
  → History shows the orbital result
  → Landing Scan button enabled (landable Sand world)

Player clicks [LANDING SCAN]
  → Audio: Rising tone → PhysicalEvidence deep tone
  → New result card slides into history
  → Investigation button appears on the result
  → Charges: "⟨ 0/2 ⟩" (red)

Player clicks [INVESTIGATE] on Physical Evidence
  → Button changes to "Investigating... (5-15 ticks)"
  → On completion: "Investigated" badge, bonus KG connections created
  → Toast: "Investigation complete — 2 knowledge connections discovered"
```

### Flow 2: Experienced Player Multi-Planet System

```
Player arrives at system with Ice World + Lava World + Gas Giant
  → HUD: "⟨ 3/3 ⟩ SCANNER" (Mk1 tier)
  → Player must choose: 3 planets, 3 charges

Scans Ice World (Mineral Survey) → ResourceIntel (1.0x affinity)
  → Charges: 2/3
  → Hint: "Archaeological mode would find more here"

Scans Lava World (Signal Sweep) → SignalLead (1.4x affinity)
  → Charges: 1/3
  → Signal Lead created on galaxy map
  → FO: "Cross-referencing signals..."

Scans Gas Giant (Signal Sweep) → SignalLead (1.5x, best match)
  → Charges: 0/3
  → Second Signal Lead → TRIANGULATION!
  → FO: "Triangulation complete — precise coordinates locked"
  → Galaxy map: dashed purple line connects the two signals to a resolved location

Player travels to new system → Charges reset to 3/3
```

### Flow 3: Mk3 Dual-Mode Scan

```
Player has Mk3 scanner (5 charges, dual-mode unlocked)
  → All 3 mode buttons enabled
  → Orbital scan produces primary result + secondary result (at -30% affinity penalty)
  → Two result cards appear in history from single scan
  → Charge cost: still 1 per scan, but more information per charge
```

---

## Progressive Disclosure

| Player State | What's Visible | What's Hidden |
|---|---|---|
| No scanner (impossible — Basic is default) | — | — |
| Basic scanner, never scanned | HUD charge indicator at planet nodes | Landing scan, investigation, history |
| After first orbital scan | + Toast results, FO commentary | Landing scan (if not landed) |
| After first dock at planet | + Station Tab scan section, affinity bars, landing scan button | Investigation (no evidence yet) |
| After first Physical Evidence | + Investigation button | — |
| Mk1 unlocked | + Signal Sweep mode button | Archaeological still locked |
| Mk2 unlocked | + Archaeological mode button, tech-gated planets accessible | Dual-mode |
| Mk3 unlocked | + Dual-mode indicator, secondary result display | — |
| Multiple Signal Leads | + Triangulation line on galaxy map | — |

---

## Data Flow

```
Player clicks [ORBITAL SCAN]
  → GDScript: bridge.call("OrbitalScanV0", node_id, mode_str)
  → SimBridge: write lock → PlanetScanSystem.ExecuteOrbitalScan()
  → Returns: Dictionary {scan_id, category, flavor_text, hint_text, affinity_bps, ...}
  → GDScript:
      1. Update HUD charge display (GetScanChargesV0)
      2. Fire toast (ToastManager)
      3. Show result modal (if P1 enabled)
      4. Rebuild Station Tab scan history (if docked)
      5. Play category-specific audio
```

---

## Implementation Priority

### P0 — Core Scan Interaction (T43 gates)
- Scanner HUD indicator (Component 1) — charge display + mode selector + orbital scan button
- Station Tab scan section (Component 2) — planet info, affinity bars, scan buttons, result history
- Scan result toast (Component 3) — category + flavor text notification
- Audio: scan initiated tone, 5 category chimes, charge spent click

### P1 — Polish & Depth
- Scan result modal with progressive reveal (Component 4)
- Galaxy map scan markers (Component 5)
- Planet 3D mesh in dock view (shader spawner)
- Investigation progress display (tick countdown while docked)
- Triangulation line on galaxy map
- Ambient sensor ping at planet nodes
- Completion tracking ("3/6 planet types surveyed")

### P2 — Late-Game
- Mk3 dual-mode result display (two cards from one scan)
- Fracture Scanner UI (instability zone visual)
- SurveyProgram scan results feed (automated scan notifications)
- Scan history log (cross-planet, accessible from Intel tab)

---

## File Manifest

| File | Type | Purpose |
|---|---|---|
| `scripts/ui/scanner_hud_panel.gd` | NEW | HUD charge indicator + mode selector + orbital scan button |
| `scripts/ui/hero_trade_menu.gd` | MODIFY | Add scan section to Station tab |
| `scripts/ui/hud.gd` | MODIFY | Instantiate + show/hide scanner_hud_panel |
| `scripts/audio/scan_audio.gd` | NEW | Category-specific chimes + scan tones |
| `scripts/core/game_manager.gd` | MODIFY | Wire scan button press to bridge calls |
| `scripts/view/GalaxyView.cs` | MODIFY (P1) | Planet type icons + scan state markers |
| `scripts/view/planet_mesh_builder.gd` | NEW (P1) | 3D planet mesh spawner for dock view |
