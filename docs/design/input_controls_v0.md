# Input Controls Design — v1

## Overview

Industry-standard input system with full remapping, gamepad support, and named
actions for all inputs. Replaces hardcoded `KEY_*` checks throughout the codebase.

v1 adds: steering mode options, combat input layer, LMB context resolution,
target cycling, and fixes for action collisions discovered in code audit.

---

## v0 → v1 Audit Findings

### Critical Problems

| # | Problem | Severity | Detail |
|---|---------|----------|--------|
| 1 | **Mouse-aim overrides everything** | Critical | Pointer steering is always-on when WASD not held. Releasing A/D to click an enemy makes the ship instantly track the cursor. In combat, this makes target selection impossible without disrupting flight path. Freelancer-style is the ONLY mode — no tank-controls or hybrid option. |
| 2 | **LMB is both click-to-fly AND combat fire** | Critical | `combat_fire_primary` = LMB. Click-to-fly autopilot = LMB. Same button, no mode switch. During combat, clicking to fly will fire weapons; clicking to fire will set an autopilot waypoint. These are mutually exclusive intents on the same input. |
| 3 | **Combat fire has no handler** | Critical | `combat_fire_primary` and `combat_fire_secondary` are defined in project.godot but **no GDScript code reads them**. The code audit found zero `is_action_pressed("combat_fire_primary")` calls anywhere. Combat appears to be entirely sim-driven with no player fire input. |
| 4 | **B key collision** | High | `battle_stations` (B) and `ui_automation` (B) are both bound to the B key. Pressing B in flight will trigger both simultaneously. |
| 5 | **No target cycling** | Medium | R targets nearest enemy only. No way to cycle through multiple targets (Tab-target or R-again-to-cycle). In multi-enemy fights, the player cannot select a specific threat. |
| 6 | **Undocumented actions** | Medium | `cruise_toggle` (C), `battle_stations` (B), `ui_automation` (B), `ui_warfront_dashboard` (N), `ui_megaproject` (G), `ui_fo_panel` (F), `ui_data_log` (P) all exist in project.godot but are missing from v0 doc. |
| 7 | **Non-existent actions referenced** | Low | Flight hints referenced `ship_dock` and `ui_haven` — neither exists in InputMap. Fixed in code (ship_dock → ui_dock_confirm, ui_haven → guarded empty). |
| 8 | **E-key doc vs reality mismatch** | Low | Doc says `ui_empire_dashboard` = E. Actual project.godot binds it to Ctrl (physical_keycode 4194306). E is only `ui_dock_confirm`. |

### Design Gaps vs Industry Standard

| Gap | Industry norm | Our state | Reference games |
|-----|---------------|-----------|-----------------|
| No steering mode choice | 2-3 selectable modes | 1 mode (Freelancer-style), hardcoded | Reassembly (3 modes via R), Starsector (2 modes in settings) |
| No combat/exploration mode split | LMB changes meaning by context | LMB always = click-to-fly | Elite Dangerous (Analysis/Combat toggle), Starsector (shield-facing vs movement) |
| No weapon group system | Number keys for weapon groups | No weapon groups at all | Starsector (Ctrl+1-7), Cosmoteer (Ctrl+0-9), SPAZ (1-4) |
| No strafe modifier | Shift+A/D = strafe while mouse-aiming | No strafe at all | Starsector (Shift modifier, universally praised) |
| No aim assist (gamepad) | Sticky aim + lead indicators | No aim assist | Everspace, SPAZ 2, Hades |
| No camera combat zoom-out | Auto zoom-out in combat | Camera unchanged | Starsector tactical zoom, Star Valor |
| Binary deadzone | Scaled gradient deadzones | Binary threshold at 0.2 | Industry standard: gradient with cross-shaped axis isolation |

---

## Industry Research (Expanded)

Source: 14 top-down/space games, web research from GameDev.net, Gamedeveloper.com,
Steam community discussions, game wikis.

### Steering Paradigms

| Pattern | Description | Games | Pros | Cons |
|---------|-------------|-------|------|------|
| **A: Ship-Faces-Mouse** | Ship rotates to cursor. WASD = relative thrust. | Reassembly, Starcom, SPAZ | Intuitive for new players | Cannot aim independently of heading |
| **B: Tank + Mouse-Aim** | A/D rotate ship. Mouse aims weapons independently. Shift+A/D = strafe. | Starsector | Most precise control. Aim ≠ heading. | Steepest learning curve |
| **C: Asteroids Heritage** | One input controls both aim and steering. Drift IS the skill. | Nova Drift | Simple, momentum-based skill expression | Polarizing — many players hate it |
| **D: Mouse-Steers, WASD-Supports** | Mouse = heading. WASD = thrust/strafe. Turrets auto-lead. | SPAZ 2, Everspace | Fluid combat feel | Mouse-aim fatigue in long sessions |

**Our current state**: Pattern A (ship-faces-mouse) with click-to-fly autopilot, but
no strafe, no independent aim, and pointer steering cannot be disabled.

**Recommendation**: Default to Pattern A. Offer Pattern B (tank + independent aim) as
alternative in Settings → Controls → Steering Mode. Starsector and Reassembly both
prove that offering choice eliminates the largest category of player complaints.

### Combat Targeting Taxonomy

| Method | Description | Games |
|--------|-------------|-------|
| **Click-to-target** | Click enemy to lock. Click body part for focused fire. | Starsector, Cosmoteer, FTL |
| **Tab/cycle target** | Hotkey targets nearest, press again to cycle. | Star Valor (T), Endless Sky (T) |
| **Auto-target nearest** | Primary weapons auto-aim. Player designates intent. | Starcom, SPAZ 2 |
| **Hybrid** (recommended) | Mouse aim for primary. Hotkey cycle for lock-on. Auto-lead for turrets. | Most modern games |

### Key Binding Standards

| Function | Industry Standard | Notes |
|----------|------------------|-------|
| Primary fire | LMB (5/6 games) | Endless Sky's Tab = most criticized choice |
| Secondary fire | RMB | Shields/ability in SPAZ |
| Ship ability / spin-up | F or B | Starsector ship system = F |
| Map | M (4/6) | Starsector uses Tab for tactical map |
| Dock/interact | E or F | Starcom uses X (unusual) |
| Target nearest | R or T | Starsector R, Star Valor T |
| Target cycle | Tab or R-again | Most games use Tab |
| Zoom | Scroll wheel | Universal |
| Pause | Space or Escape | Starsector: Space. Most: Esc. |
| Fleet commands | F, G, numbered | Starsector, Endless Sky |
| Weapon groups | 1-7 or Ctrl+1-7 | Starsector, Cosmoteer |
| Strafe modifier | Shift | Starsector — universally praised |
| Cruise/boost | C or Shift | Elite Dangerous, our game = C |
| Gamepad | Optional but expected | Starsector/Endless Sky ship without |

---

## Action Registry

All inputs use Godot InputMap named actions. No hardcoded `KEY_*` checks anywhere.

### Flight Actions

| Action Name | Keyboard | Mouse | Gamepad (Xbox) | Deadzone | Notes |
|-------------|----------|-------|----------------|----------|-------|
| `ship_thrust_fwd` | W | — | Left Stick Y- | 0.2 | |
| `ship_thrust_back` | S | — | Left Stick Y+ | 0.2 | |
| `ship_turn_left` | A | — | Left Stick X- | 0.2 | Tank mode only (Pattern B) |
| `ship_turn_right` | D | — | Left Stick X+ | 0.2 | Tank mode only (Pattern B) |
| `ship_strafe_left` | A | — | Left Stick X- | 0.2 | **NEW** — Pattern A default |
| `ship_strafe_right` | D | — | Left Stick X+ | 0.2 | **NEW** — Pattern A default |
| `cruise_toggle` | C | — | L3 (click) | — | **Was undocumented.** Auto-thrust toward cursor. Disengages on dock/transit. |
| `battle_stations` | **X** | — | Y | — | **Moved from B** (was colliding with ui_automation). Gyro spin-up toggle. |

### Combat Actions

| Action Name | Keyboard | Mouse | Gamepad (Xbox) | Notes |
|-------------|----------|-------|----------------|-------|
| `combat_fire_primary` | — | LMB | RT | **Needs handler.** Only fires when hostile in range (see Combat Input Layer). |
| `combat_fire_secondary` | — | RMB | LT | Shields, tractor beam, secondary weapon. |
| `combat_target_nearest` | R | — | RB | Targets nearest hostile. |
| `combat_target_cycle` | Tab | — | RB (double-tap) | **NEW** — Cycle through hostiles in range. |
| `combat_target_clear` | Escape (2nd press) | — | B | **NEW** — Deselect current target. |

### UI Actions

| Action Name | Keyboard | Mouse | Gamepad (Xbox) | Notes |
|-------------|----------|-------|----------------|-------|
| `ui_galaxy_map` | M | — | LB | Was Tab. Blocked when docked or in transit. |
| `ui_dock_confirm` | E | — | A | Dock when near station. Also triggers gate approach. |
| `ui_empire_dashboard` | Ctrl | — | Y | **Corrected**: actual binding is Ctrl, not E. |
| `ui_mission_journal` | J | — | X | |
| `ui_knowledge_web` | K | — | DPad-Down | |
| `ui_combat_log` | L | — | DPad-Right | |
| `ui_warfront_dashboard` | N | — | — | **Was undocumented.** |
| `ui_megaproject` | G | — | — | **Was undocumented.** |
| `ui_fo_panel` | F | — | — | **Was undocumented.** First officer panel. |
| `ui_automation` | B | — | — | **Was undocumented.** Was colliding with battle_stations. |
| `ui_data_overlay` | V | — | DPad-Up | Cycles: Off → Security → Trade Flow → Intel Age → Off. |
| `ui_data_log` | P | — | — | **Was undocumented.** |
| `ui_keybinds_help` | H | — | DPad-Left | |
| `ui_pause` | Escape | — | Start | |
| `ui_gate_confirm` | Enter, Space | — | A | Gate approach state only. |
| `ui_gate_cancel` | Escape | — | B | Gate approach state only. |

### Mouse Actions (not remappable)

| Input | Action | Context | Notes |
|-------|--------|---------|-------|
| Left Click | Fire primary weapon | Hostile in weapon range | **NEW behavior** — combat takes priority |
| Left Click | Autopilot to position | No hostile in range / out of combat | Click-to-fly only when safe |
| Right Click + Drag | Pan galaxy map | Galaxy map open | |
| Scroll Wheel | Zoom in/out | All flight states | |
| Mouse Movement | Pointer steering | Pattern A active, WASD not held | Ship turns toward cursor |

---

## Design Changes (v1)

### 1. Steering Mode Selection

Settings → Controls → Steering Mode:

**Pattern A: "Pointer Flight" (default)**
- Ship automatically faces mouse cursor when WASD not held
- WASD = strafe relative to heading (W forward, S back, A/D lateral)
- Pointer dead zone: 3.0 units around ship (cursor too close = no turn)
- Turn gain: 8.0 (current POINTER_TURN_GAIN_V0)
- Cruise (C): auto-thrust toward cursor direction
- Best for: exploration, trading, casual play

**Pattern B: "Manual Flight"**
- A/D rotate ship (tank controls). Ship does NOT face mouse.
- W/S = thrust forward/back relative to ship heading
- Mouse aims weapons independently of heading
- Shift + A/D = strafe while maintaining heading (Starsector pattern)
- Turrets track mouse cursor. Ship body tracks A/D.
- Best for: combat-focused players, precision flying

**Switching**: Selectable in Settings. Persisted in `user://keybinds.cfg`.
Both modes use the same InputMap actions — the flight controller reads a
`steering_mode` enum and branches behavior.

**Implementation**: `hero_ship_flight_controller.gd` already has the pointer
steering code. Pattern B adds a branch that skips `_apply_pointer_steering()`
and instead uses `ship_turn_left/right` for rotation while raycasting mouse
position only for turret aim direction (passed to SimBridge).

### 2. Combat Input Layer (LMB Context Resolution)

The core problem: LMB = click-to-fly AND combat_fire_primary. These are
mutually exclusive player intents. Resolution:

**Priority chain for LMB:**
1. If UI element under cursor → UI click (already handled by Godot input order)
2. If hostile target locked AND in weapon range → fire primary weapon
3. If hostile ship under cursor (no lock) → lock target + fire
4. Else → click-to-fly autopilot

**Implementation**:
- `hero_ship_flight_controller._unhandled_input()` checks combat state before
  setting click-to-fly nav target
- New guard: `if _bridge.call("HasHostileInRangeV0")` → consume LMB as fire,
  don't set nav target
- SimBridge exposes `HasHostileInRangeV0() → bool` (reads from CombatSystem)
- `GetLockedTargetV0() → string` for current target ID

**RMB during combat**: Fire secondary weapon (was: unused in flight). Outside
combat: pan camera (galaxy map only).

**Visual feedback**: Cursor changes when over a hostile (crosshair icon).
Cursor changes when click-to-fly is active (waypoint icon). Player always knows
what LMB will do.

### 3. Target Cycling

Current: R = target nearest. No cycling.

**New system:**
- **R** = target nearest hostile (unchanged)
- **Tab** = cycle to next hostile (clockwise by screen position)
- **Shift+Tab** = cycle previous
- **Escape** (when target locked, before pause) = clear target lock
- **Click on hostile** = lock that specific target

**Target lock HUD**: Locked target gets a bracket indicator + distance readout.
combat_hud.gd reads `GetLockedTargetV0()` and renders the indicator.

**Gamepad**: RB = target nearest. RB again within 0.5s = cycle next.
B = clear target.

### 4. B Key Collision Fix

**Problem**: `battle_stations` and `ui_automation` both on B.

**Fix**: Move `battle_stations` to X key. Rationale:
- B for automation is more discoverable (B = Build/automate mnemonic)
- X is adjacent to combat keys (C = cruise, V = overlay) and available
- Battle stations is a combat prep action, X = "eXtreme readiness" mnemonic
- Starsector uses F for ship system — X is a reasonable alternative since
  F is already taken by `ui_fo_panel`

### 5. Strafe Support

Current: A/D only turn the ship (or do nothing in pointer-steer mode since
the mouse controls heading).

**Pattern A (Pointer Flight)**: A/D become strafe left/right. Ship heading is
mouse-controlled, so A/D turning is meaningless — strafe is the useful action.
This matches Starcom: Nexus and SPAZ behavior.

**Pattern B (Manual Flight)**: A/D = turn. Shift+A/D = strafe (Starsector
pattern). Or: Q/E = strafe if Shift modifier feels awkward.

**New actions**: `ship_strafe_left`, `ship_strafe_right`. In Pattern A these
map to A/D by default. In Pattern B they map to Shift+A / Shift+D (or Q/E).

### 6. Combat Auto-Zoom

When hostiles are within engagement range, camera smoothly zooms out to show
the battlefield. Returns to normal zoom when combat ends.

- Combat zoom multiplier: 1.3x current altitude (tunable)
- Transition: 1.5s ease-in-out tween
- Player scroll wheel overrides auto-zoom (manual zoom takes priority)
- Re-engages on next combat start if player hasn't manually zoomed

Reference: Starsector's tactical zoom is the gold standard here.

---

## Key Design Decisions

### E Key Split
`ui_dock_confirm` and `ui_empire_dashboard` are separate named actions.
`ui_dock_confirm` defaults to E; `ui_empire_dashboard` defaults to Ctrl.
The game_manager dispatch logic governs priority: dock available + IN_FLIGHT →
dock; gate available + IN_FLIGHT → gate approach. Player can remap independently.

### R Key Context
R = `combat_target_nearest` in flight, restart when dead. These are mutually
exclusive states — the dead check runs first in `_unhandled_input` and returns
early. No collision.

### Tab Key — Map vs Target Cycle
Tab was the original galaxy map key (changed to M). Tab is now available for
`combat_target_cycle`. No collision with M (galaxy map).

### Analog Stick Deadzone
0.2 instead of Godot's default 0.5. The default is too coarse — ship won't
respond until stick is past halfway, which feels sluggish for flight.

**Future**: Implement scaled gradient deadzone (not binary threshold) per
industry best practice. Current binary deadzone creates a perceptible "notch"
at the edge. Gradient deadzone maps [0.2, 1.0] → [0.0, 1.0] smoothly.
Cross-shaped deadzone assists single-axis input (pure horizontal turns).

### Gamepad Philosophy
Gamepad is a secondary input device, not the primary design target. The game
is designed for keyboard+mouse. Gamepad bindings provide full gameplay access
but menus still require a cursor (no focus-based navigation yet).

**Aim assist** (gamepad only): Sticky aim reduces turn sensitivity when
crosshair is near a hostile. Lead indicators show where to aim for projectile
weapons. Both tunable in settings (Off / Low / Medium / High). Reference:
Everspace notes that gamepad combat is "significantly harder" without aim assist.

---

## State Machine — Input Context

```
                    ┌─────────────┐
                    │  MAIN_MENU  │  All gameplay input blocked
                    └──────┬──────┘
                           │ New Game / Load
                           ▼
                    ┌─────────────┐
                    │   INTRO     │  Any key/click → dismiss
                    └──────┬──────┘
                           │ Dismissed
                           ▼
              ┌────────────────────────┐
              │       IN_FLIGHT        │◄──────────────────┐
              │                        │                   │
              │  WASD/mouse: fly       │     Undock        │
              │  LMB: fire or autopilot│                   │
              │  RMB: secondary fire   │                   │
              │  R/Tab: targeting      │                   │
              │  All UI hotkeys active │                   │
              └──┬───┬───┬───┬────────┘                   │
                 │   │   │   │                             │
        E+dock   │   │   │   │  E+gate              ┌─────┴──────┐
        avail.   │   │   │   │  avail.              │   DOCKED   │
                 │   │   │   │                      │            │
                 │   │   │   └──────┐               │ Flight OFF │
                 │   │   │          ▼               │ UI hotkeys │
                 │   │   │   ┌──────────────┐       │ Trade menu │
                 │   │   │   │ GATE_APPROACH │       └────────────┘
                 │   │   │   │              │
                 │   │   │   │ Enter = go   │
                 │   │   │   │ Esc = cancel │
                 │   │   │   └──────┬───────┘
                 │   │   │          │ Confirm
                 │   │   │          ▼
                 │   │   │   ┌──────────────┐
                 │   │   │   │ IN_LANE_XSIT │  ALL input blocked
                 │   │   │   └──────────────┘
                 │   │   │
                 │   │   │  Esc
                 │   │   └──────────┐
                 │   │              ▼
                 │   │       ┌────────────┐
                 │   │       │   PAUSED   │  Esc = resume
                 │   │       └────────────┘
                 │   │
                 │   │  M key
                 │   └─────────┐
                 │             ▼
                 │      ┌──────────────┐
                 │      │ GALAXY_MAP   │  WASD = pan, scroll = zoom
                 │      │              │  M = close, click = select
                 │      └──────────────┘
                 │
                 │  Player dead
                 └─────────┐
                           ▼
                    ┌─────────────┐
                    │    DEAD     │  R only = restart
                    └─────────────┘
```

### Input Blocking by State

| State | Flight | Combat | UI Hotkeys | LMB | Special |
|-------|--------|--------|------------|-----|---------|
| IN_FLIGHT | Yes | Yes | Yes | Fire/Autopilot | Full control |
| DOCKED | No | No | Yes | UI only | Trade menu active |
| GATE_APPROACH | No | No | No | No | Enter/Esc only |
| IN_LANE_TRANSIT | No | No | No | No | Cinematic — all blocked |
| GALAXY_MAP | No | No | Yes | Select node | WASD = pan camera |
| PAUSED | No | No | No | No | Esc = resume |
| DEAD | No | No | No | No | R = restart |
| INTRO | No | No | No | Dismiss | Any key = dismiss |

---

## Architecture

### InputManager Autoload (`scripts/core/input_manager.gd`)

Singleton responsible for:
- Loading custom bindings from `user://keybinds.cfg` on startup
- Applying custom bindings over project.godot defaults via InputMap API
- Providing display labels for current bindings (`get_action_label()`)
- Saving/restoring bindings
- Emitting `bindings_changed` signal for UI refresh
- **NEW**: Storing `steering_mode` preference (Pattern A / Pattern B)

Uses Godot's `ConfigFile` with `var_to_str`/`str_to_var` for InputEvent
serialization (not JSON — InputEvents aren't JSON-serializable).

### Rebinding Flow
1. Player opens Settings → Controls tab
2. Clicks "Bind" next to an action
3. Label shows "[ Press a key... ]"
4. Next key/mouse/gamepad event is captured
5. **Conflict detection**: if key already bound, show "Key [X] is bound to
   [Action]. Swap?" dialog (swap both, cancel, or force-duplicate)
6. InputManager applies + saves the binding
7. Help overlay and HUD hint bar update automatically

### State-Dependent Input
game_manager's `_unhandled_input` handles state gating via early returns:
- Dead → only `combat_target_nearest` (restart) works
- Cinematic → all input blocked
- Gate approach → only `ui_gate_confirm` and `ui_gate_cancel`
- Docked → flight disabled, UI actions still work
- Galaxy map → flight actions route to map camera pan

### Combat Input Integration
New: `hero_ship_flight_controller.gd` queries SimBridge combat state before
processing LMB. Chain:
1. `_unhandled_input(event)` receives LMB press
2. Check `_bridge.call("HasHostileInRangeV0")` → if true, consume as fire
3. Check `_bridge.call("GetLockedTargetV0")` → if target, fire at target
4. Else → set click-to-fly nav target (existing behavior)

### Files Touched

| File | Role |
|------|------|
| `project.godot` | Action definitions (source of truth for defaults) |
| `scripts/core/input_manager.gd` | Rebinding persistence + display labels + steering mode |
| `scripts/core/game_manager.gd` | Central input dispatcher (actions, not keys) |
| `scripts/core/hero_ship_flight_controller.gd` | Flight physics input + steering modes + combat LMB |
| `scripts/view/player_follow_camera.gd` | Camera zoom + galaxy pan + combat auto-zoom |
| `scripts/view/galaxy_camera_controller.gd` | Galaxy map WASD pan |
| `scripts/ui/settings_panel.gd` | Controls tab for rebinding + steering mode selector |
| `scripts/ui/keybinds_help.gd` | Dynamic help overlay |
| `scripts/ui/hud.gd` | Dynamic hint bar + dock prompt |
| `scripts/ui/combat_hud.gd` | Target lock bracket indicator |
| `scripts/bridge/SimBridge.Combat.cs` | HasHostileInRangeV0, GetLockedTargetV0 |

---

## Implementation Priority

### Must-Fix (blocks combat feel)
1. **Wire combat fire input** — LMB/RMB must actually trigger player weapons
2. **LMB context resolution** — fire vs autopilot priority chain
3. **B key collision** — move `battle_stations` to X
4. **Add missing actions to doc** — cruise, warfront, megaproject, FO, data log

### Should-Fix (quality of life)
5. **Target cycling** — Tab to cycle, click-to-lock
6. **Steering mode B** — tank controls + independent mouse aim
7. **Strafe in Pattern A** — A/D = strafe when mouse controls heading
8. **Gradient deadzone** — replace binary threshold

### Nice-to-Have (polish)
9. **Combat auto-zoom** — camera pulls back during engagement
10. **Cursor mode indicator** — crosshair vs waypoint cursor
11. **Gamepad aim assist** — sticky aim + lead indicators
12. **Controller glyphs** — show Xbox/PS icons when gamepad active
13. **Steam Input API** — GodotSteam for Steam Deck Verified

---

## Gamepad Reference Layout

```
        ┌──LB (Galaxy Map)────────────RB (Target)──┐
        │                                           │
   ┌────┴────┐                             ┌────────┴───┐
   │   LT    │                             │     RT     │
   │ 2nd Fire│                             │  1st Fire  │
   └─────────┘                             └────────────┘

   ┌─────────────────────────────────────────────────────┐
   │                                                     │
   │  [DPad]              [Guide]              [Face]    │
   │  ↑ Overlay                                Y BtlStn │
   │  ← Help        [Back]     [Start]         X Jrnl   │
   │  ↓ Knowledge                              B Clear  │
   │  → CombatLog                              A Dock   │
   │                                                     │
   │    ┌───┐                           ┌───┐           │
   │    │ L │ Move / Strafe             │ R │ Aim       │
   │    │   │ (L3 = Cruise)             │   │ (turrets) │
   │    └───┘                           └───┘           │
   └─────────────────────────────────────────────────────┘
```

Right stick aims turrets independently of ship heading on gamepad (always
Pattern B behavior — gamepad cannot do pointer steering). Left stick controls
thrust + strafe. This is the twin-stick standard (Hades, Everspace, SPAZ).

---

## Future Considerations

- **Right-stick cursor**: Emulate mouse cursor with right analog stick for menu nav
- **Steam Input API**: GodotSteam integration for Steam Deck Verified badge
- **Context-sensitive prompts**: "Press [A] to dock" with glyph when using controller
- **ESDF alternative**: Left-hand layout for non-WASD users (just remap)
- **Weapon groups**: 1-4 keys to assign weapon groups (Starsector pattern)
- **Fleet commands**: When fleet mechanics expand, F/G keys for fleet orders
- **Radial menu (gamepad)**: Hold LB for quick-access radial (map, journal, etc.)
- **Input replay**: Record/playback input for deterministic replay debugging
