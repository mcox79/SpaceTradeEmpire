# Visual Content — To Be Authored

> **Status: TO_BE_AUTHORED**
> This document catalogs all visual assets — VFX, shaders, UI widgets, icons,
> and animated effects — that must be created for the design bibles to be
> fully realized.
>
> Companion to: `CombatFeel.md` (combat VFX), `GalaxyMap.md` (overlays),
> `RiskMeters.md` (meter widgets), `ExplorationDiscovery.md` (discovery markers),
> `HudInformationArchitecture.md` (HUD layout).

---

## Priority Tiers

- **SHADER**: GPU shader program (Godot ShaderMaterial / VisualShader)
- **PARTICLE**: GPUParticles3D / CPUParticles3D effect
- **UI_WIDGET**: Control node tree (scene + script)
- **ICON**: 2D texture/atlas sprite
- **ANIMATION**: AnimationPlayer or Tween-based effect

---

## 1. Combat VFX (Per CombatFeel.md)

### Kill Explosion (HIGH Priority)

**ID:** `VFX.COMBAT.EXPLOSION`
**Type:** PARTICLE + SHADER
**Current:** Ships just disappear (mesh freed)
**Target:** Multi-phase explosion: flash → fireball → debris → smoke

```
Phase 1 (0.0-0.1s): White flash at ship center
Phase 2 (0.1-0.5s): Orange/yellow fireball expanding (GPUParticles3D)
Phase 3 (0.3-1.0s): Debris chunks flying outward (RigidBody3D or particle)
Phase 4 (0.5-1.5s): Lingering smoke/ember particles
```

**Assets needed:**
- Fireball particle texture (2D sprite sheet or 3D noise shader)
- Debris chunk meshes (3-5 small irregular shapes)
- Smoke particle texture
- Explosion flash shader (additive blend, fast falloff)

### Shield Ripple Effect (HIGH Priority)

**ID:** `VFX.COMBAT.SHIELD_RIPPLE`
**Type:** SHADER
**Current:** No visual distinction between shield hit and hull hit
**Target:** Hexagonal ripple at impact point, blue-white, fades 0.3s

```
Shader approach:
- Sphere mesh slightly larger than ship hull
- Fragment shader: hex pattern + distance-from-impact ripple
- Uniform: impact_point (vec3), impact_time (float)
- Alpha fades from 0.8 → 0 over 0.3s
- Color: blue-white (shield) with electric crackle texture
```

**Reference:** Halo shield effect, Star Citizen shield bubble

### Shield Break Flash (MEDIUM Priority)

**ID:** `VFX.COMBAT.SHIELD_BREAK`
**Type:** PARTICLE + SHADER
**Current:** No visual when shields drop to 0
**Target:** Full-ship blue-white flash + electric discharge particles

```
Components:
- Flash: shader overlay on ship mesh, 0.2s bright → 0
- Discharge: 20-30 small electric arc particles flying outward
- Screen flash: brief blue vignette (player ship only)
```

### Floating Damage Numbers (MEDIUM Priority)

**ID:** `VFX.COMBAT.DAMAGE_NUMBERS`
**Type:** UI_WIDGET (3D billboard or 2D overlay)
**Current:** No per-hit numeric feedback
**Target:** Number appears at impact, drifts upward 0.5s, fades

```
Style:
- Shield damage: blue text, smaller font
- Hull damage: orange text, larger font
- Critical hit: white text, largest font, brief scale pulse
- Format: "-8" / "-12" / "-24!"
- Billboard: always faces camera
- Stacking: offset Y for simultaneous hits
```

### Weapon Trail Differentiation (MEDIUM Priority)

**ID:** `VFX.COMBAT.WEAPON_TRAILS`
**Type:** SHADER (trail mesh or Line2D equivalent)
**Current:** All bullets look the same (cyan projectile)
**Target:** Family-specific trails per CombatFeel.md

| Family | Trail Style | Color | Width | Length |
|--------|-----------|-------|-------|--------|
| Kinetic | Short, thick, solid | White-yellow | 3px | 0.5m |
| Energy | Long, thin, glowing | Cyan-blue | 1.5px | 2.0m |
| Neutral | Medium, default | Cyan (current) | 2px | 1.0m |
| Point Defense | Rapid thin streaks | Yellow-white | 1px | 0.3m |

### Zone Armor Visualization (LOW Priority)

**ID:** `VFX.COMBAT.ZONE_ARMOR`
**Type:** UI_WIDGET
**Current:** 4 zones exist in SimCore but are invisible
**Target:** Ship silhouette with 4 directional HP bars

```
Layout (per CombatFeel.md):
         ╭── FORE: 25/25 ──╮
        ╱                    ╲
  PORT ║   [ship silhouette]  ║ STBD
  20/20 ║                    ║ 20/20
        ╲                    ╱
         ╰── AFT: 15/15 ───╯
```

**Assets needed:**
- Ship silhouette SVG/texture (top-down view)
- 4 arc-shaped progress bars around silhouette
- Color transitions: green → yellow → red per zone HP %

---

## 2. Galaxy Map Overlays (Per GalaxyMap.md)

### Faction Territory Overlay (MEDIUM Priority)

**ID:** `VFX.MAP.FACTION_TERRITORY`
**Type:** SHADER (existing territory disc system, needs expansion)
**Current:** Territory discs exist in GalaxyView.cs but only for faction label display
**Target:** Semi-transparent color fills covering faction regions

```
Approach:
- Voronoi-like regions from faction territory node lists
- Per-faction color with 15-20% opacity fill
- Contested zones: striped/hatched pattern mixing both faction colors
- Border: dashed line at territory edge
```

### Warfront Overlay (MEDIUM Priority)

**ID:** `VFX.MAP.WARFRONT`
**Type:** SHADER + ANIMATION
**Current:** Warfronts exist in SimCore but are invisible on map
**Target:** Animated front-line rendering between contested territories

```
Components:
- Front line: jagged animated line between warring factions
- Intensity glow: brightness proportional to warfront intensity
- Contested nodes: pulsing indicator
- Supply arrows: show supply flow direction
```

### Discovery Phase Markers (MEDIUM Priority)

**ID:** `VFX.MAP.DISCOVERY_MARKERS`
**Type:** ICON (3 states)
**Current:** Discoveries exist at nodes but have no map visualization
**Target:** Per ExplorationDiscovery.md

| Phase | Icon | Color | Style |
|-------|------|-------|-------|
| Seen | ▪ (small square) | Dim gray | Static, subtle |
| Scanned | ░ (hatched square) | Amber | Subtle pulse |
| Analyzed | ✓ (checkmark) | Green | Solid, resolved |

### Route Planner Path (LOW Priority)

**ID:** `VFX.MAP.ROUTE_PLANNER`
**Type:** SHADER (thread highlight)
**Current:** No route planning visualization
**Target:** Multi-hop highlighted path from origin to destination

```
Style:
- Selected threads: bright cyan, 2x width
- Hop numbers: small labels at each node along route
- Cost summary: total time, fuel, risk at path end
- Animated flow: dashed line animation along route direction
```

### Heat Map Overlay (LOW Priority)

**ID:** `VFX.MAP.HEAT`
**Type:** SHADER
**Current:** No heat visualization
**Target:** Per-node heat intensity as color gradient

```
Colors: green (calm) → yellow (noticed) → orange (elevated) → red (critical)
Style: radial gradient around each node, opacity = heat level
```

---

## 3. Risk Meter HUD Widgets (Per RiskMeters.md)

### Three-Meter Display (HIGH Priority)

**ID:** `VFX.HUD.RISK_METERS`
**Type:** UI_WIDGET
**Current:** No risk meter visualization exists
**Target:** Zone G (bottom bar) compact meter display

```
Layout:
  🔥 ████░░░░  ◆ ████████░░  👁 ░░░░░░░░░░
  Heat: Noticed  Influence: Elevated  Trace: Calm

Components per meter:
- Icon (emoji or custom icon)
- Progress bar (gradient fill)
- Threshold name label
- Trend arrow (↑ rising / ↓ decaying / ── stable)
- Decay rate text (when decaying)
```

**Assets needed:**
- Heat icon (flame) — TextureRect or Label with emoji
- Influence icon (diamond) — TextureRect or Label
- Trace icon (eye) — TextureRect or Label
- Progress bar shader: gradient fill with meter-specific color
- Pulse animation: threshold-specific pulse rate
- Warning border: flashing border at High+

### Screen-Edge Tinting (MEDIUM Priority)

**ID:** `VFX.HUD.RISK_TINT`
**Type:** SHADER (fullscreen post-process)
**Current:** No ambient screen-edge effects
**Target:** Per RiskMeters.md, persistent tint at High+ thresholds

| Meter at High+ | Effect |
|----------------|--------|
| Heat | Warm orange vignette (10% opacity) |
| Influence | Purple shimmer at edges (pulsing, 8%) |
| Trace | Cyan scan-line sweeping top of screen (every 5s) |
| Two meters | Both tints overlay (15%) |
| All three | All tints + slight desaturation |

---

## 4. Discovery UI Assets (Per ExplorationDiscovery.md)

### Knowledge Graph Layout (LOW Priority)

**ID:** `VFX.UI.KNOWLEDGE_GRAPH`
**Type:** UI_WIDGET (custom Control with draw_* calls)
**Current:** No knowledge graph/Discovery Web exists
**Target:** Node-link diagram showing discovery connections

```
Node types:
- Analyzed: solid circle, green border
- Scanned: dashed circle, amber border
- Seen: dotted circle, gray border
- Unknown (lead only): ??? text, no border

Edge types:
- Same origin: solid line
- Lead: dashed arrow
- Faction link: dotted line
- Tech unlock: gold line

Layout: force-directed or manual placement
```

### Milestone Feedback Cards (MEDIUM Priority)

**ID:** `VFX.UI.MILESTONE_CARD`
**Type:** UI_WIDGET + ANIMATION
**Current:** Phase transitions are silent
**Target:** Per ExplorationDiscovery.md, brief celebration card

```
Card layout:
┌─ SCAN COMPLETE ────────────────────────────┐
│  ░ Derelict Wreck — Kepler System          │
│                                             │
│  "Sensor readings detect..."               │
│                                             │
│  Energy signature: Fading                  │
│  Material: Unknown composite               │
│                                             │
│  [Analyze]  [Later]                         │
└─────────────────────────────────────────────┘

Animation:
- Slide in from bottom (0.3s ease-out)
- Hold for player interaction
- Slide out on dismiss (0.2s)
```

### Scanner Sweep Animation (LOW Priority)

**ID:** `VFX.UI.SCANNER_SWEEP`
**Type:** PARTICLE + ANIMATION
**Current:** No scanner visualization
**Target:** Expanding ring of light when entering new system

```
Ring expands from player position outward
Systems within range light up as ring passes through them
Audio: radar ping per system revealed
Duration: 1-2s depending on scanner range
```

---

## 5. HUD Enhancement Assets (Per HudInformationArchitecture.md)

### Alert Badge (MEDIUM Priority)

**ID:** `VFX.HUD.ALERT_BADGE`
**Type:** UI_WIDGET
**Current:** No alert badge exists
**Target:** Zone A, shows count of pending alerts

```
Style:
- Red circle with white number: [!] 3
- Red = critical alerts
- Orange = warnings only
- Hidden when no alerts
- Click opens Empire Dashboard → Overview
```

### Toast Action Bridges (MEDIUM Priority)

**ID:** `VFX.HUD.TOAST_ACTION`
**Type:** UI_WIDGET enhancement
**Current:** Toasts are display-only, no clickable actions
**Target:** Action button in warning/critical toasts

```
┌─ TOAST ──────────────────────────────┐
│ ▲ Trade Charter stalled: no ore      │
│                        [View Program]│
└──────────────────────────────────────┘
```

### Notification Priority Borders (LOW Priority)

**ID:** `VFX.HUD.TOAST_PRIORITY`
**Type:** SHADER or StyleBox
**Current:** All toasts look identical
**Target:** Color-coded borders per priority

| Priority | Border Color | Duration |
|----------|-------------|----------|
| Critical | Red | 5s + persist |
| Warning | Orange | 4s |
| Info | Default | 3s |
| Confirmation | Green | 2s |

---

## 6. Empire Dashboard Visuals (Per EmpireDashboard.md)

### Sankey Production Chain Diagram (LOW Priority)

**ID:** `VFX.UI.SANKEY`
**Type:** UI_WIDGET (custom draw_* or addon)
**Current:** No production chain visualization
**Target:** Flow diagram showing resource transformation

```
ORE ──→ METAL ──→ COMPOSITES
  ╲              ╱
   ──→ CHEMICALS ──→ MUNITIONS
```

### Graphical Tech Tree (LOW Priority)

**ID:** `VFX.UI.TECH_TREE`
**Type:** UI_WIDGET
**Current:** Tech list is text-only
**Target:** Node-link tree with prereq lines

```
[Warp I] → [Warp II] → [Warp III]
              ↓
          [Deep Scan] → [Advanced Sensors]
```

---

## Summary

| Category | Count | Types | Priority |
|----------|-------|-------|----------|
| Combat VFX | 6 | SHADER, PARTICLE, UI_WIDGET | HIGH-LOW |
| Galaxy Map Overlays | 5 | SHADER, ICON, ANIMATION | MEDIUM-LOW |
| Risk Meter Widgets | 2 | UI_WIDGET, SHADER | HIGH-MEDIUM |
| Discovery UI | 3 | UI_WIDGET, PARTICLE, ANIMATION | MEDIUM-LOW |
| HUD Enhancements | 3 | UI_WIDGET, SHADER | MEDIUM-LOW |
| Dashboard Visuals | 2 | UI_WIDGET | LOW |
| **Total** | **21** | | |

### Implementation Strategy

1. **Shader-first**: Shield ripple, screen-edge tint, trail differentiation
   are pure shaders with no asset dependency
2. **Particle reuse**: Explosion and shield break share particle infrastructure
3. **UI_WIDGET as scenes**: Each widget is a Godot scene (`.tscn`) with attached
   script, composable into HUD/dashboard layouts
4. **Icon atlas**: All small icons (discovery markers, risk icons, alert badge)
   in a single texture atlas for draw-call efficiency
