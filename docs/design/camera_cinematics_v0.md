# Camera & Cinematics Design — v0

## Philosophy

Camera movement is the player's window into the galaxy. Every transition should feel like a continuous, intentional shot — not a sequence of discrete phases stitched together. The mathematical standard is **C2 continuity** (acceleration continuous across all phase joins). The perceptual standard is: the player should never feel a "hitch" or "snap" during any automated camera sequence.

---

## Continuity Requirements

| Level | What's Continuous | Minimum For |
|-------|------------------|-------------|
| C0 | Position | Intentional hard cuts only |
| C1 | Velocity | Any automated transition |
| **C2** | **Acceleration** | **All cinematic sequences** |
| C3 | Jerk | Aspirational for slow dollies |

**Curvature** kappa = |d_theta/ds| measures how tightly the path turns. A straight line has kappa=0. A circle of radius R has kappa=1/R. Transitions between straight and curved segments must ramp curvature linearly (not step it), which is the Euler spiral principle.

---

## Euler Spiral (Clothoid) — The Gold Standard

The Euler spiral is the unique curve whose curvature changes linearly with arc length: kappa(s) = s/a^2. This is optimal for line-to-curve transitions because:

- Matches curvature 0 (straight line) at one end
- Matches curvature 1/R (circular arc) at the other end
- Linear ramp between = no acceleration discontinuity (C2 at both joins)
- Equivalent to turning a steering wheel at constant angular velocity

**GDScript approximation**: Quadratic angle profile gives linear curvature growth:
```gdscript
# Entering a curve (straight -> circle):
var angle = theta_start + (theta_tangent - theta_start) * t * t

# Exiting a curve (circle -> straight):
var angle = theta_end + (theta_tangent - theta_end) * (1.0 - t) * (1.0 - t)
```

Highway and railway engineers have used this since the 1880s. No closed-form parametric equations (Fresnel integrals), but numerical approximation with 24+ steps is visually perfect.

---

## Camera Tracking Rate

The camera lerp filter `factor = 1 - exp(-k * delta)` controls how tightly the camera tracks its target position:

| k value | Factor/frame @60fps | Feel |
|---------|-------------------|------|
| 8 | 0.125 | Mushy, sloppy orbits |
| 15 | 0.22 | Soft follow |
| **30** | **0.40** | **Tight cinematic tracking** |
| 60 | 0.63 | Near-snap |

**Rule**: When driving the camera from tweened targets (flyby orbit, etc.), use k >= 30. The tween provides smooth target motion; the lerp should track it crisply, not add slop. Lower k values cause the camera to trace expanded, imprecise versions of the intended path.

---

## Thread Transit Cinematic — Warp Arrival

### Curvature Profile (Ideal)

```
kappa(s):
1/R ─────────────────────────────────────────
    |           /                 \          |
    |          /                   \         |
    |         /     Circular Orbit  \        |
    |        /       (constant k)    \       |
    |       /                         \      |
  0 |------/---------------------------\-----|
    |Straight| Euler  |   Orbit    |Euler|Settle
    |Approach| Spiral |   Sweep    |Spiral|
    |  (k=0) |(ramp up)| (k=1/R)  |(ramp down)|(k=0)
```

### Phase Breakdown

**Phase 1 — Straight Approach** (existing)
- Camera in WARP_TRANSIT mode: chase cam behind transit marker, tilted forward
- Marker tweens from origin gate toward entry point (FLYBY_APPROACH_DIST from star)
- Altitude descends from cruise (~200-450u) toward ~120u
- Thread line and marker fade near end of approach

**Phase 2 — Euler Spiral Entry** (deviation)
- Camera captures exact position from WARP_TRANSIT (zero-snap transition)
- Flyby activates: camera switches to direct position control
- 24-step parametric spiral: angle sweeps 90 degrees laterally (quadratic profile)
- Radius compresses from entry distance to orbit radius
- Look-at transitions from destination toward star center
- Creates visible lateral swoop — the camera curves OFF the approach line

**Phase 3 — Orbital Sweep**
- 48-step circular arc around the star at FLYBY_ORBIT_RADIUS
- Camera looks at star center throughout
- Altitude breathes: dip mid-orbit, rise at end (+-10u)
- Radius breathes: pull in mid-orbit (75%-100% of nominal)
- First visit: full sweep (~270 degrees) + letterbox overlay
- Return visit: 60% sweep, no letterbox

**Phase 4 — Euler Spiral Exit** (settle)
- Reverse spiral from orbit back toward straight settle
- Camera transitions from looking at star to looking at ground below hero
- Up vector transitions from Vector3.UP back to Vector3.BACK (top-down)

**Phase 5 — Handoff**
- flyby_active = false, camera at (hero.x, 80, hero.z) looking down
- Matches FLIGHT mode exactly — zero-snap transition
- Player ship appears at destination gate

### Off-Center Swoop Geometry

The tangent entry point must be **90 degrees offset** from the approach direction. This creates the visible lateral deviation:

```
          Approach dir -->
    ================== *  Camera at entry point
                        \
                         \   <-- Euler spiral (kappa: 0 -> 1/R)
                          \      Camera deviates LATERALLY
                           \
                   * Star   *  Tangent point (90 deg from approach)
                  /    /   /
                 / R  /   /    <-- Circular orbit (kappa = 1/R)
                /    /   /
               *----/---/   Exit tangent point
                \
                 \   <-- Euler spiral (kappa: 1/R -> 0)
                  \
                   *  Dest gate / settle position
```

**Tangent angle selection**:
- approach_angle = atan2(-lane_dir.z, -lane_dir.x)
- CW tangent: approach_angle - PI/2
- CCW tangent: approach_angle + PI/2
- Pick whichever gives the longer sweep to dest_gate_angle (more cinematic)

### Tweakable Constants

| Constant | Default | Purpose |
|----------|---------|---------|
| FLYBY_APPROACH_DIST | 130.0 | Distance from star to start curving |
| FLYBY_ORBIT_RADIUS | 55.0 | Orbit circle radius around star |
| FLYBY_ORBIT_ALT | 45.0 | Camera height during orbit |
| FLYBY_CURVE_ON_TIME | 1.5s | Duration of entry spiral |
| FLYBY_ORBIT_TIME | 4.0s | Duration of orbital sweep |
| FLYBY_CURVE_OFF_TIME | 1.5s | Duration of exit spiral |

---

## Camera Modes

| Mode | Controller | Position Logic |
|------|-----------|----------------|
| FLIGHT | player_follow_camera | Above player, top-down with yaw/pitch |
| ORBIT | player_follow_camera | Orbiting target on mouse drag |
| WARP_TRANSIT | player_follow_camera | Chase cam behind transit marker |
| FLYBY | game_manager tweens | Direct position/look-at via flyby_* vars |
| GALAXY_MAP | player_follow_camera | High altitude strategic view |

### Transition Rules
- FLIGHT -> WARP_TRANSIT: On gate proximity enter. Input locked during departure vortex.
- WARP_TRANSIT -> FLYBY: When marker reaches entry point. Camera position captured for zero-snap.
- FLYBY -> FLIGHT: On handoff. Camera position matches flight mode exactly.
- Any -> GALAXY_MAP: On galaxy overlay toggle. Altitude tweens up.

---

## Settle Phase — Critically-Damped Spring

For the final settle to any target camera position, a critically-damped spring is optimal:

```
x(t) = target + (x0 - target + (v0 + omega*(x0-target))*t) * e^(-omega*t)
```

where omega = 2*PI / settle_time. Properties:
- No overshoot (unlike underdamped)
- Fastest convergence (unlike overdamped)
- Velocity approaches zero smoothly at target

---

## AAA Reference Points

| Game | Arrival Technique | Takeaway |
|------|------------------|----------|
| Elite Dangerous | Cockpit-locked, star rush, manual decel | Proximity = adrenaline |
| No Man's Sky | Ship-centered, environment moves | Warp can mask loading |
| Star Citizen | Behind-ship chase, quantum decel | Per-ship VFX sell identity |
| EVE Online | Math-modeled accel/cruise/decel phases | UI shake sells speed |
| Homeworld | Free orbital cam, slow sweeps | Slow = cinematic in space |
| Freelancer | On-rails gate, re-orient at dest | Automated drama works |

Common patterns:
- FOV manipulation (widen for speed, narrow for arrival)
- Controls locked during transit (prevents disorientation)
- Reveal sweep at destination (establish new location)
- Audio design is as important as camera path (warp whoosh, arrival chime)

---

## File Locations

| File | Responsibility |
|------|---------------|
| `scripts/view/player_follow_camera.gd` | All camera modes, flyby lerp, input handling |
| `scripts/core/game_manager.gd` | Transit orchestration, flyby tween sequences |
| `scripts/tests/lane_transfer_diag_bot.gd` | Headless verification of transit telemetry |

## Version History

- v0 (2026-03-08): Initial document. Euler spiral framework, flyby system, off-center swoop geometry.
