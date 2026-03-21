# Cruise Flight Design — v0

## Overview

Cruise drive is the inter-system travel mechanic. Press C to toggle high-speed
3D flight that pitches the ship toward neighboring star systems. Sits between
Freelancer's 2D-only cruise and Elite Dangerous's full 3D supercruise — tuned
for a top-down camera trading game.

## Industry Research

Six reference games evaluated for cruise/fast-travel control schemes.

| Game | Manual Pitch in Fast Travel | Auto-Orient | Pitch Limit | Anti-Stuck Mechanism |
|------|----------------------------|-------------|-------------|----------------------|
| Freelancer | No (2D plane) | No | N/A | Mouse steers freely at all speeds |
| Elite Dangerous | Yes (full 3DOF) | Optional module (SC Assist) | None | Exit FSD anywhere; full steering live |
| X4: Foundations | Nominal but near-zero effect | Autopilot AP recommended | None | Exit travel drive; AP handles routing |
| Star Citizen | No (quantum locked) | Required pre-align | None in cruise | TCS reduces speed to match turn authority |
| No Man's Sky | No (pulse locked) | Manual pre-align | N/A | Exit-realign-re-engage pattern |
| Everspace 2 | Yes (full 3DOF) | No | None | Interrupt-on-contact |

### Key Takeaways

1. **Auto-orient is increasingly standard.** Elite Dangerous added Supercruise
   Assist as a purchasable module because players wanted it. We provide it
   natively — good for a trading game where inter-system travel is routine.
2. **Most games restrict or lock controls at top speed.** Only Elite and
   Everspace give full steering during fast travel. X4 technically allows it
   but steering authority is ~7.5% of normal.
3. **Every reference game has a charge-up delay** (1-10s). None allow instant
   cruise toggle. This provides feedback and an abort window.
4. **Player override priority matters.** Star Citizen's TCS philosophy: system
   assists but never overrides player intent. Our implementation follows this
   (player pitch torque 4x auto-pitch torque).

## Controls

### Normal Flight (cruise off)

| Input | Action | Notes |
|-------|--------|-------|
| Mouse pointer | Yaw toward cursor | Ship-relative, XZ plane only |
| W | Thrust forward | |
| S | Brake / reverse thrust | |
| A / D | Turn left / right | Override pointer steering |
| LMB | Click-to-fly autopilot | Cancel with any WASD |
| C | Toggle cruise | |

Ship is constrained to the solar system's Y plane by a spring force
(`Y_SPRING_K = 40`, `Y_DAMP_K = 12`).

### Cruise Flight (cruise on)

| Input | Action | Notes |
|-------|--------|-------|
| Mouse pointer | Yaw toward cursor | Same as normal; auto-thrust forward |
| W | Pitch down (nose toward surface) | Suppresses auto-pitch while held |
| S | Pitch up (nose away from surface) | Suppresses auto-pitch while held |
| A / D | Turn left / right | Same as normal |
| C | Disengage cruise | Speed ramps down at 60 u/s^2 |

**Auto-pitch**: When no manual pitch input (W/S), the ship automatically
pitches toward the nearest non-current star system. This creates a natural 3D
arc between systems.

### Tuning Constants

| Constant | Value | Purpose |
|----------|-------|---------|
| `CRUISE_MAX_SPEED_V0` | 120.0 u/s | ~6.7x normal max speed |
| `CRUISE_ACCEL_V0` | 30.0 u/s^2 | Speed ramp-up rate |
| `CRUISE_DECEL_V0` | 60.0 u/s^2 | Faster decel on disengage (abort-friendly) |
| `CRUISE_THRUST_MULT_V0` | 8.0 | Thrust multiplier during cruise |
| `CRUISE_PITCH_GAIN_V0` | 3.0 | Auto-pitch steering responsiveness |
| `CRUISE_PITCH_MAX_TORQUE_V0` | 1.5 | Max auto-pitch torque (softer than yaw) |
| `CRUISE_PITCH_MAX_ANGLE_V0` | 0.7 rad (~40 deg) | Max auto-pitch angle from horizontal |
| `CRUISE_PLAYER_PITCH_TORQUE_V0` | 6.0 | Manual W/S pitch torque (4x auto-pitch) |

### Destination Lock (Heading-Based)

When cruise spool begins (C pressed), the system locks a destination star:

1. Sample the ship's XZ heading direction.
2. Score all non-current stars: `dot(heading, direction_to_star) / distance`.
3. Stars behind the ship (dot < 0.3) are excluded.
4. Highest-scoring star wins — the one most aligned with heading and closest.
5. Fallback: nearest non-current star (if no star in front of ship).
6. Destination is **locked for the entire cruise** — no mid-flight target switching.

This prevents the "wrong target" bug where nearest-star selection steered
toward a cluster behind the player.

### Auto-Pitch Behavior

1. Uses the locked destination position (`_cruise_dest_pos`).
2. Computes pitch error (cross product of ship forward vs direction to target).
3. Applies torque scaled by gain, clamped to max torque.
4. **Soft angle limit**: Quadratic falloff approaching 40 degrees. At 40 deg,
   auto-pitch authority reaches zero. Angular damping (5.0) naturally
   decelerates the ship's pitch rotation.
5. **Player override**: When W or S is held, auto-pitch is suppressed entirely.
   Player pitch torque (6.0) gives direct control.

### Spool-Up Delay

Cruise does not engage instantly. Pressing C starts a 1-second spool-up:

- During spool: destination is locked, engine visual can play (future).
- Press C again during spool: **cancel** (no cruise, no penalty).
- After 1 second: cruise engages, speed ramp begins.
- Benefits: prevents accidental activation, provides abort window, adds feel/weight.

### Cruise State Transitions

```
IN_FLIGHT ──[C key]──> SPOOL_UP (1s)
    │                       │
    │                       ├── [C key during spool] ──> cancel, back to IN_FLIGHT
    │                       └── [timer expires] ──> CRUISE_ACTIVE
    │                                                   │
    │                                                   ├── [C key] ──> disengage
    │                                                   ├── [Dock proximity] ──> auto-disengage
    │                                                   ├── [Lane gate] ──> auto-disengage
    │                                                   └── [DOCKED / IN_LANE_TRANSIT] ──> auto-disengage
    │
    └── Y-spring active (ship stays on solar plane)
                                                        └── Y-spring disabled (3D flight enabled)
```

During cruise, the Y-spring obstacle avoidance is disabled so the ship can
pitch freely between star systems at different Y positions.

## Camera Interaction

During cruise, the camera switches to a **3D chase camera**: positioned behind
and above the ship, looking forward along the ship's heading. This naturally
follows the ship's pitch without additional logic. Spring omega is 8.0 for
a slightly laggy cinematic feel.

```
Normal flight: top-down fixed camera (altitude 80-120u above player)
Cruise active: chase camera (40u behind, 25u above, looking 30u ahead)
```

The chase camera provides spatial awareness during 3D inter-system flight while
the top-down camera is used for all in-system navigation.

## Known Limitations & Future Work

### Low Priority

- Cruise speed tiers (hold Shift for maximum, tap C for moderate)
- Destination ETA display on HUD during cruise
- Proximity auto-disengage near planets/stations (Everspace 2 pattern)
- Engine glow/charge-up visual during spool-up (audio + particles)

## Architecture

### Files

| File | Role |
|------|------|
| `scripts/core/hero_ship_flight_controller.gd` | Cruise physics, pitch logic, speed ramp |
| `scripts/view/player_follow_camera.gd` | Chase camera during cruise (3D pitch follow) |
| `scripts/core/game_manager.gd` | State transitions, cruise auto-disengage on dock/lane |
| `docs/design/input_controls_v0.md` | Action registry (cruise_toggle) |

### Input Action

`cruise_toggle` — C key (keyboard), not yet mapped to gamepad. Future: Left
Bumper (LB) on gamepad.
