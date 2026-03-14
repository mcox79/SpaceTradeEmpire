# VFX & Visual Roadmap — v0

Based on /first-hour (20/25) and /feel (33/50) evaluations, five visual gaps
block EA readiness. This document analyzes each gap, what we already have, what
the design docs specify, and the minimum viable fix.

---

## 1. Combat VFX — Zero Visual Feedback

### What We Have
- **Combat HUD** (combat_hud.gd): Zone armor bars, stance indicator, spin RPM,
  radiator status, combat projection, weapon tracking, capturable targets — all
  working, all data-driven UI
- **Damage flash** (hud.gd): Full-screen red ColorRect flash on damage events
- **Combat vignette** (hud.gd): Red border vignette during active combat via
  screen_edge_tint shader (multi-channel: heat/influence/trace/overheat)
- **Combat log** (combat_log_panel.gd): Event list with humanized names/outcomes
- **Engine trail** (engine_trail.gd): GPUParticles3D on player ship, cyan glow

### What's Missing
No 3D combat effects whatsoever. Three combat frames look identical to peaceful
flight. The evaluator said: "A first-time player watching these frames would not
know combat was occurring."

### What the Design Docs Specify (combat_mechanics_v0.md)
The combat system has four weapon families with distinct visual signatures:
- **Kinetic** (railguns): Short thick white-yellow trails, heavy sparks on impact
- **Energy** (lasers): Long thin colored beams, electric crackle on impact
- **Point Defense**: Rapid thin streaks, small fast flashes
- **Missiles**: Smoke trail + engine glow, large explosion on impact

Shield hits: blue-white hexagonal ripple (0.3s). Hull hits: orange/red sparks +
debris. Radiator hits: silver/white sparks + liquid metal spray.

Screen shake hierarchy: 0.05 (PD) to 0.80 (shield break) to 1.0 (death).

### Minimum Viable Implementation

**Phase 1 — "Not a Prototype" (1 session)**
1. **Muzzle flash**: GPUParticles3D on turret mount positions. Emit 3-5 bright
   particles on each combat event tick. Yellow-white, 0.1s lifetime. This alone
   tells the player "weapons are firing."
2. **Hit flash on target**: When damage is dealt, spawn a brief (0.2s) emissive
   sphere at the target ship position. Blue for shield damage, orange for hull.
3. **Screen shake on damage received**: Camera shake (0.3 intensity, 0.15s
   decay) when player takes hull damage. Already have the vignette — shake adds
   tactile weight.

**Phase 2 — "Feels Like Combat" (1-2 sessions)**
4. **Projectile trails**: Line mesh or GPUParticles3D trail between attacker and
   defender. White-yellow for kinetic, cyan for energy. 0.3s lifetime.
5. **Kill explosion**: When NPC is destroyed, replace mesh with fireball
   GPUParticles3D (1.5s, orange-white, 20-30 particles expanding outward).
   Brief hitstop (0.3x timescale for 0.2s).
6. **Shield ripple shader**: On shield hit, apply a brief wave distortion to
   the target ship's material (hexagonal pattern, blue-white, 0.3s).

**Phase 3 — "Design Doc Complete" (2-3 sessions)**
7. Weapon-family-specific trails (kinetic vs energy vs missile)
8. Radiator hit VFX (silver sparks + coolant spray)
9. Heat shimmer shader at 50%/75%/90% thresholds
10. Forced vent dramatic sequence (white flash + steam jets)
11. Battle Stations spin-up visual (ship rotation + running lights red)

### Implementation Notes
- All VFX are presentation-only (GDScript). No SimCore changes.
- Combat events come via `GetRecentCombatEventsV0()` — already wired.
- Player ship is at known scene path. NPC ships are `FleetShip` group members.
- GPUParticles3D is the right node — we already use it for engine trails.
- Kenney Space Kit includes exhaust materials usable for muzzle flash.

---

## 2. Warp VFX — No Visual Event Boundary

### What We Have
- **Warp Transit HUD** (warp_transit_hud.gd): Top-center panel with destination,
  progress bar, ETA, distance. Cyan border, dark background. Working.
- **Arrival Drama** (hud.gd): Letterbox bars + title card on warp arrival.
  System name + faction. Animated tween. Working and looks good.
- **Lane beams** (GalaxyView.cs): Cylindrical mesh lanes between stars. Visible
  during transit as the blue beam the evaluator noted.
- **Camera altitude tween** (player_follow_camera.gd): Strategic zoom on
  galaxy map toggle. Smooth altitude transitions.

### What's Missing
- No departure VFX (no flash, no speed lines, no FOV zoom on lane entry)
- Warp transit beam is static (no particle flow along the lane)
- No arrival VFX (arrival drama letterbox exists but no 3D effect)

### What the Design Docs Specify (camera_cinematics_v0.md)
- Warp departure: "vortex pull" (0.8s), ship pulled toward gate
- Lane effects: Thread line visible during transit
- Arrival shimmer: Subtle distortion as ship exits

### Minimum Viable Implementation

**Phase 1 — "Departure is an Event" (1 session)**
1. **FOV zoom on departure**: When player enters IN_LANE_TRANSIT, tween camera
   FOV from 75 to 90 over 0.3s, then back to 75 over 0.5s. Creates a "punch
   into warp" sensation.
2. **White flash on departure**: Full-screen ColorRect flash (white, 0.8 alpha)
   that fades over 0.3s. Same technique as damage flash but white.
3. **Camera shake on departure**: 0.6 intensity, 0.15s decay.

**Phase 2 — "Transit Has Motion" (1 session)**
4. **Speed lines shader**: Canvas_item shader on a fullscreen ColorRect during
   transit. Radial streaks from center (like starfield_menu.gdshader layer 1
   but with high drift speed). Fade in on transit start, fade out on arrival.
5. **Particle flow along lane**: GPUParticles3D emitting along the lane
   direction. Cyan dots flowing from origin to destination. Gives sense of
   speed and direction.

**Phase 3 — "Arrival Feels Like Landing" (1 session)**
6. **Arrival flash**: Brief white flash + camera shake on system entry
   (before the letterbox drama kicks in).
7. **System reveal**: Camera starts slightly zoomed in on arrival, pulls out
   to normal altitude over 1s, revealing the new system.

### Implementation Notes
- FOV tween: `player_follow_camera.gd` already has `tween_altitude_v0`. Add
  `tween_fov_v0(target, duration)` following the same pattern.
- Flash: Same pattern as damage flash in hud.gd. New method `_flash_warp_v0()`.
- Speed lines: New shader file `scripts/view/warp_speed_lines.gdshader`. The
  menu starfield shader already implements parallax star drift — reuse the math
  with higher speed values.
- game_manager.gd `on_lane_gate_proximity_entered_v0` is the departure trigger.
  `_on_warp_arrived_v0` is the arrival trigger.

---

## 3. Station Visual at Distance — Green Box

### What We Have
- **Station scene** (scenes/station.tscn): Cylindrical hub + rotating ring +
  3 spokes + cyan accent band (emissive 2.5x). Full 3D mesh.
- **No LOD system**: Station renders at full detail at all distances.
- **Station orbit**: 54u from star center (StationOrbitRadiusU).
- **Camera altitude**: Default 120u (FLIGHT mode).

### The Problem
At 120u camera altitude looking at a station 54u away, the station mesh is very
small on screen. The cyan accent band is the only distinguishing feature. At
distance it reads as a small colored rectangle — the hub/ring/spoke detail is
below pixel threshold.

### Why It's Not Really a Green Box
The evaluator is seeing the station at extreme angle from top-down camera. The
station model IS there — it's just too small to read at the default viewing
distance. The "green box" is actually the cyan emissive accent band, which is
the only part bright enough to register at this scale.

### Minimum Viable Fix

**Option A — Emissive Billboard (recommended, 1 session)**
Add a billboard sprite (Sprite3D, billboard mode) to the station scene that
renders a distinctive icon at distance. The billboard shows when camera
altitude > 80u, hides when < 60u (when the 3D mesh becomes readable).
Icon: simple cross or diamond shape with faction-colored glow.

This is how Elite Dangerous handles stations at distance — a distinctive HUD
marker transitions to a 3D model as you approach.

**Option B — Increased Emissive Radius**
Make the station's emissive accent band larger (full ring glow, not just a
stripe). This makes the station "read" at distance without adding new nodes.
Less work but less effective.

**Option C — Station Label3D Enhancement**
We already have Label3D station labels. Add a small icon/dot above the label
in the station's faction color. This doesn't fix the mesh appearance but adds
a clear marker.

### Implementation Notes
- Station scene is `scenes/station.tscn`. Add Sprite3D child node.
- Billboard mode: `Sprite3D.billboard = BaseMaterial3D.BILLBOARD_ENABLED`
- Fade logic: check camera distance in `_process`, toggle visibility.
- Faction-specific colors available from the design docs:
  - Concord: Steel blue/silver
  - Chitin: Amber/gold
  - Valorin: Forest green/copper
  - Weavers: Deep purple/silver
  - Communion: Lavender/teal

---

## 4. Knowledge Web — 72 Entries, 0 Revealed

### What We Have
- **Knowledge Web Panel** (knowledge_web_panel.gd): Full UI with K key toggle.
  Shows connection list grouped by type, filter buttons, stats bar showing
  "X revealed / Y total". Supports [+] revealed, [?] visible, [-] hidden states.
- **Bridge queries**: `GetKnowledgeGraphV0()` returns connection array,
  `GetKnowledgeGraphStatsV0()` returns {total, revealed, question_marks}.
- **72 entries built** in SimCore — the data exists.
- **0 revealed** — no trigger has fired to reveal any entries.

### The Problem
The knowledge web has two issues:
1. **Panel suppressed**: Hidden when dock panel active (hud.gd). Player can
   only access it in-flight via K key, but there's no prompt to do so.
2. **No reveal triggers**: The 72 entries exist but nothing in the first-hour
   gameplay path triggers a reveal. The design specifies reveals on:
   - Visiting a new system (Seen phase)
   - Scanning a discovery site (Scanned phase)
   - Analyzing a discovery (Analyzed phase)
   - Completing certain missions

### What the Design Docs Specify (ExplorationDiscovery.md)
Discovery is ship-module-driven scanning with tech gating. Three phases:
Seen (auto on proximity) → Scanned (player action) → Analyzed (player action).
Each phase transition should reveal knowledge graph connections.

Adaptation Fragments use gold/purple visual language. Standard discoveries
use blue. Faction links use dotted lines.

### Minimum Viable Fix

**Phase 1 — "Something Appears" (1 session, non-hash-affecting)**
1. Auto-reveal knowledge entries when visiting a new system for the first time.
   The bot visits 8 systems — each should reveal 2-3 connections related to
   that system's faction, trade goods, or nearby discoveries.
2. Add a toast notification: "Discovery Web updated — press K to view"
3. This requires a SimCore change: on player arrival at a new node, call
   `KnowledgeGraphSystem.RevealByNode(state, nodeId)` or similar.

**Phase 2 — "Exploration Rewards Knowledge" (1-2 sessions)**
4. Reveal entries on first trade at a station (market connections)
5. Reveal entries on first combat kill (threat connections)
6. Reveal entries on mission completion (mission-specific connections)
7. Add "NEW" badge to K key hint when unrevealed entries exist

**Phase 3 — "The Web Tells a Story" (2-3 sessions)**
8. Visual web layout (node-graph rendering, not just a list)
9. Faction-colored connection lines
10. Discovery site markers on galaxy map linked to web entries
11. Ancient/Precursor entries with distinct gold/purple styling

### Implementation Notes
- The reveal trigger is in SimCore (hash-affecting). Need to add a method
  like `RevealNodeKnowledge(state, nodeId)` that marks relevant entries as
  revealed based on the node's faction, goods, and connections.
- The panel UI already handles revealed/visible/hidden states — it will "just
  work" once entries are marked revealed.
- Toast system exists (ToastManager). Add toast on reveal count change.

---

## 5. Background Starfield — Empty Black Void

### What We Have
- **Starlight addon**: Installed, provides skybox stars. Used in 3D scenes.
- **Galactic Sky shader** (galactic_sky.gdshader): Procedural nebula with
  Simplex noise — Milky Way band, dust lanes, central bulge, nearby galaxies.
  Seed-driven. This is a sophisticated shader.
- **Star Field Follow** (star_field_follow.gd): GPUParticles3D that follows
  camera position each frame. Keeps stars centered on player.
- **Menu starfield** (starfield_menu.gdshader): 4-layer parallax with
  procedural stars, nebula washes, twinkling. Canvas_item shader.

### The Problem
The in-flight screenshots show near-black backgrounds. The Galactic Sky shader
and Starlight addon are both present but may not be rendering visibly at the
default camera altitude/angle. The top-down fixed camera may be looking
"through" the skybox rather than "at" it.

### Root Cause Analysis
1. **Starlight addon**: Hides when galaxy overlay opens (via SetOverlayOpenV0).
   May also be hidden/dimmed during normal flight if the GalacticSky node takes
   precedence.
2. **Galactic Sky**: Generates nebula on a mesh — but if the mesh is positioned
   at the skybox layer and the camera is looking straight down, the nebula may
   be on the horizon (invisible from top-down view).
3. **Star Field Follow**: GPUParticles3D follows camera — but if particle
   emission is sparse or the draw pass is too small, stars may be invisible at
   the current resolution.

### Minimum Viable Fix

**Option A — 2D Background Layer (recommended, 1 session)**
Add a CanvasLayer (layer -1) with a fullscreen ColorRect using a simplified
version of `starfield_menu.gdshader`. This guarantees visible stars regardless
of 3D camera orientation. The menu shader already has 4-layer parallax with
twinkling — strip it down to 2 layers (deep dim + mid bright) for performance.

Parallax can respond to player ship position (uniform vec2 offset from world
position) giving subtle motion parallax.

This is how many 2D-perspective space games handle starfields — a
canvas-space shader behind the 3D scene. It works regardless of camera angle.

**Option B — Increase GPUParticles3D Density**
Increase star_field_follow particle count and brightness. Risk: performance
impact, and top-down camera may still not show them well.

**Option C — Skybox Reorientation**
Rotate the Galactic Sky mesh so the nebula band is visible from the top-down
camera position. This preserves the 3D feel but requires spatial tuning.

### Implementation Notes
- CanvasLayer at layer -1 renders behind everything including the 3D viewport.
- The starfield_menu shader is already written and tested — adapt it.
- Performance: The menu shader runs at 60fps on the main menu. In-game with
  a 3D scene, it should still be fine (it's a simple fragment shader).
- Ship position → shader uniform: Pass `player_ship.global_position.xz` as
  a vec2 uniform each frame for parallax offset.

---

## Priority Order

| Priority | Fix | Sessions | Hash-Affecting | Impact |
|----------|-----|----------|----------------|--------|
| 1 | Combat VFX Phase 1 (muzzle flash + hit flash + shake) | 1 | No | Removes EA_BLOCKER |
| 2 | Warp departure VFX (FOV zoom + flash + shake) | 0.5 | No | Removes EA_BLOCKER |
| 3 | Knowledge web auto-reveal on system visit | 1 | Yes | Goal 5: 3→4 |
| 4 | 2D background starfield shader | 0.5 | No | Atmosphere +1 |
| 5 | Station billboard at distance | 0.5 | No | Immersion fix |
| 6 | Warp transit speed lines | 0.5 | No | Polish |
| 7 | Combat VFX Phase 2 (trails + explosions) | 1-2 | No | Feel +1.5 |
| 8 | Combat VFX Phase 3 (weapon families + heat) | 2-3 | No | Design doc complete |

Fixes 1-5 represent ~3.5 sessions of work and would resolve all EA_BLOCKER
issues from both evaluations.

---

## Faction Visual Integration

When implementing VFX, use faction-specific color palettes from the lore:

| Faction | Primary | Secondary | Emissive | Feel |
|---------|---------|-----------|----------|------|
| Concord | Steel blue | Silver | White-blue | Institutional, precise |
| Chitin | Amber/gold | Burnt orange | Warm gold | Vibrant, swarming |
| Valorin | Matte gray | Forest green | Copper | Rugged, frontier |
| Weavers | Deep purple | Dark blue | Silver-violet | Precise, predatory |
| Communion | Soft lavender | Teal | Warm white | Ethereal, sanctuary |

Station billboards, NPC ship accents, and territory VFX should use these
palettes. The player's ship remains white/cyan (neutral) to contrast with
faction-colored elements.
