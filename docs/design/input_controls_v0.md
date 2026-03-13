# Input Controls Design — v0

## Overview

Industry-standard input system with full remapping, gamepad support, and named
actions for all inputs. Replaces hardcoded `KEY_*` checks throughout the codebase.

## Industry Research

Source: 6 top-down space games (Starsector, Starcom, Endless Sky, SPAZ, Cosmoteer, Everspace 2).

| Function | Industry Standard | Notes |
|----------|------------------|-------|
| Primary fire | Left Mouse (5/6) | Endless Sky's Tab = most criticized design choice |
| Secondary fire | Right Mouse | Shields/ability in SPAZ |
| Ship ability | F | Starsector ship system |
| Map | M (4/6) | Starsector uses Tab for tactical map |
| Dock/interact | E or F | Starcom uses X (unusual) |
| Target enemy | R | Starsector, universal |
| Zoom | Scroll wheel | Universal |
| Pause | Space or P | Starsector: Space |
| Fleet commands | F, G, numbered | Starsector, Endless Sky |
| Gamepad | Optional | Starsector/Endless Sky ship without |

## Action Registry

All inputs use Godot InputMap named actions. No hardcoded `KEY_*` checks anywhere.

### Flight Actions

| Action Name | Keyboard | Mouse | Gamepad (Xbox) | Deadzone |
|-------------|----------|-------|----------------|----------|
| `ship_thrust_fwd` | W | — | Left Stick Y- | 0.2 |
| `ship_thrust_back` | S | — | Left Stick Y+ | 0.2 |
| `ship_turn_left` | A | — | Left Stick X- | 0.2 |
| `ship_turn_right` | D | — | Left Stick X+ | 0.2 |

### Combat Actions

| Action Name | Keyboard | Mouse | Gamepad (Xbox) | Notes |
|-------------|----------|-------|----------------|-------|
| `combat_fire_primary` | — | LMB | RT | Was G key (industry outlier) |
| `combat_fire_secondary` | — | RMB | LT | Future: shields, tractor beam |
| `combat_target_nearest` | R | — | RB | Also restart when dead (mutually exclusive states) |

### UI Actions

| Action Name | Keyboard | Mouse | Gamepad (Xbox) | Notes |
|-------------|----------|-------|----------------|-------|
| `ui_galaxy_map` | M | — | Select/Back | Was Tab |
| `ui_dock_confirm` | E | — | A | Dock when near station |
| `ui_empire_dashboard` | E | — | Y | Empire when no dock available |
| `ui_mission_journal` | J | — | X | |
| `ui_knowledge_web` | K | — | DPad-Down | |
| `ui_combat_log` | L | — | DPad-Right | |
| `ui_data_overlay` | V | — | DPad-Up | Cycles: Off→Security→Trade→Intel→Off |
| `ui_keybinds_help` | H | — | DPad-Left | |
| `ui_pause` | Escape | — | Start | |
| `ui_gate_confirm` | Enter, Space | — | A | Gate approach state only |
| `ui_gate_cancel` | Escape | — | B | Gate approach state only |

### Mouse Actions (not remappable)

| Input | Action | Notes |
|-------|--------|-------|
| Left Click | Autopilot to position / UI select | Handled by hero_ship_flight_controller.gd |
| Right Click + Drag | Pan galaxy map | Handled by player_follow_camera.gd |
| Scroll Wheel | Zoom in/out | Handled by player_follow_camera.gd |

## Key Design Decisions

### E Key Split
`ui_dock_confirm` and `ui_empire_dashboard` are separate named actions, both
defaulting to E. The game_manager dispatch logic governs which fires based on
state (dock available + IN_FLIGHT → dock, else → empire). Player can remap
independently: dock to F while keeping empire on E.

### R Key Context
R = `combat_target_nearest` in flight, restart when dead. These are mutually
exclusive states — the dead check runs first in `_unhandled_input` and returns
early. No collision.

### Analog Stick Deadzone
0.2 instead of Godot's default 0.5. The default is too coarse — ship won't
respond until stick is past halfway, which feels sluggish for flight.

### Gamepad Philosophy
Gamepad is a secondary input device, not the primary design target. The game
is designed for keyboard+mouse. Gamepad bindings provide full gameplay access
but menus still require a cursor (no focus-based navigation yet).

## Architecture

### InputManager Autoload (`scripts/core/input_manager.gd`)

Singleton responsible for:
- Loading custom bindings from `user://keybinds.cfg` on startup
- Applying custom bindings over project.godot defaults via InputMap API
- Providing display labels for current bindings (`get_action_label()`)
- Saving/restoring bindings
- Emitting `bindings_changed` signal for UI refresh

Uses Godot's `ConfigFile` with `var_to_str`/`str_to_var` for InputEvent
serialization (not JSON — InputEvents aren't JSON-serializable).

### Rebinding Flow
1. Player opens Settings → Controls tab
2. Clicks "Bind" next to an action
3. Label shows "[ Press a key... ]"
4. Next key/mouse/gamepad event is captured
5. InputManager applies + saves the binding
6. Help overlay and HUD hint bar update automatically

### State-Dependent Input
No framework needed. game_manager's existing `_unhandled_input` handles state
gating via early returns:
- Dead → only `combat_target_nearest` (restart) works
- Cinematic → all input blocked
- Gate approach → only `ui_gate_confirm` and `ui_gate_cancel`
- Docked → flight disabled, UI actions still work
- Galaxy map → flight actions route to map camera pan

### Files Touched

| File | Role |
|------|------|
| `project.godot` | Action definitions (source of truth for defaults) |
| `scripts/core/input_manager.gd` | Rebinding persistence + display labels |
| `scripts/core/game_manager.gd` | Central input dispatcher (actions, not keys) |
| `scripts/core/hero_ship_flight_controller.gd` | Flight physics input |
| `scripts/view/player_follow_camera.gd` | Camera zoom + galaxy pan |
| `scripts/view/galaxy_camera_controller.gd` | Galaxy map WASD pan |
| `scripts/ui/settings_panel.gd` | Controls tab for rebinding |
| `scripts/ui/keybinds_help.gd` | Dynamic help overlay |
| `scripts/ui/hud.gd` | Dynamic hint bar + dock prompt |

## Future Considerations

- **Controller glyphs**: Show Xbox/PS button icons when gamepad is active input
- **Right-stick cursor**: Emulate mouse cursor with right analog stick for menu nav
- **Steam Input API**: GodotSteam integration for Steam Deck Verified badge
- **Context-sensitive prompts**: "Press [A] to dock" with glyph when using controller
- **ESDF alternative**: Left-hand layout for non-WASD users (just remap)
