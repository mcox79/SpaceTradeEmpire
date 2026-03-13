## InputManager — Rebindable input system autoload
## Loads custom keybindings from user://keybinds.cfg on startup,
## provides display labels for current bindings, and persists remaps.
extends Node

signal bindings_changed(action: String)

const SAVE_PATH := "user://keybinds.cfg"

## Human-readable labels for each rebindable action (shown in Controls tab + help overlay)
const ACTION_LABELS := {
	"ship_thrust_fwd":       "Thrust Forward",
	"ship_thrust_back":      "Thrust Back",
	"ship_turn_left":        "Turn Left",
	"ship_turn_right":       "Turn Right",
	"combat_fire_primary":   "Fire (Primary)",
	"combat_fire_secondary": "Fire (Secondary)",
	"combat_target_nearest": "Target Nearest / Restart",
	"ui_galaxy_map":         "Galaxy Map",
	"ui_dock_confirm":       "Dock",
	"ui_empire_dashboard":   "Empire Dashboard",
	"ui_mission_journal":    "Mission Journal",
	"ui_knowledge_web":      "Knowledge Web",
	"ui_combat_log":         "Combat Log",
	"ui_data_overlay":       "Data Overlay",
	"ui_keybinds_help":      "Controls Help",
	"ui_pause":              "Pause",
	"ui_gate_confirm":       "Confirm Gate Transit",
	"ui_gate_cancel":        "Cancel Gate Transit",
}

## Cache of project.godot defaults for reset functionality
var _defaults: Dictionary = {}  # action -> Array[InputEvent]


func _ready() -> void:
	_cache_defaults()
	_load_custom_bindings()


## Cache the project.godot default events for every managed action
func _cache_defaults() -> void:
	for action in ACTION_LABELS:
		if InputMap.has_action(action):
			_defaults[action] = InputMap.action_get_events(action).duplicate()


## Load custom bindings from keybinds.cfg and apply over defaults
func _load_custom_bindings() -> void:
	var cfg := ConfigFile.new()
	var err := cfg.load(SAVE_PATH)
	if err != OK:
		return  # No custom bindings — use project defaults
	for action in cfg.get_section_keys("bindings"):
		if not InputMap.has_action(action):
			continue
		var event_count: int = cfg.get_value("bindings", action + "_count", 0)
		if event_count <= 0:
			continue
		InputMap.action_erase_events(action)
		for i in range(event_count):
			var event_str: String = cfg.get_value("bindings", action + "_" + str(i), "")
			if event_str.is_empty():
				continue
			var event = str_to_var(event_str)
			if event is InputEvent:
				InputMap.action_add_event(action, event)


## Save all current bindings that differ from defaults
func _save_bindings() -> void:
	var cfg := ConfigFile.new()
	for action in ACTION_LABELS:
		if not InputMap.has_action(action):
			continue
		var events := InputMap.action_get_events(action)
		cfg.set_value("bindings", action + "_count", events.size())
		for i in range(events.size()):
			cfg.set_value("bindings", action + "_" + str(i), var_to_str(events[i]))
	cfg.save(SAVE_PATH)


## Rebind the first keyboard/mouse event for an action (preserves gamepad as secondary)
func rebind_action(action: String, event: InputEvent) -> void:
	if not InputMap.has_action(action):
		return
	var events := InputMap.action_get_events(action)
	# Separate keyboard/mouse events from gamepad events
	var gamepad_events: Array[InputEvent] = []
	for ev in events:
		if ev is InputEventJoypadButton or ev is InputEventJoypadMotion:
			gamepad_events.append(ev)
	# Clear and re-add: new event first, then gamepad
	InputMap.action_erase_events(action)
	InputMap.action_add_event(action, event)
	for gev in gamepad_events:
		InputMap.action_add_event(action, gev)
	_save_bindings()
	bindings_changed.emit(action)


## Rebind the gamepad event for an action (preserves keyboard/mouse as primary)
func rebind_action_gamepad(action: String, event: InputEvent) -> void:
	if not InputMap.has_action(action):
		return
	var events := InputMap.action_get_events(action)
	var kb_events: Array[InputEvent] = []
	for ev in events:
		if not (ev is InputEventJoypadButton or ev is InputEventJoypadMotion):
			kb_events.append(ev)
	InputMap.action_erase_events(action)
	for kev in kb_events:
		InputMap.action_add_event(action, kev)
	InputMap.action_add_event(action, event)
	_save_bindings()
	bindings_changed.emit(action)


## Reset a single action to its project.godot default
func reset_action(action: String) -> void:
	if not _defaults.has(action):
		return
	InputMap.action_erase_events(action)
	for ev in _defaults[action]:
		InputMap.action_add_event(action, ev)
	_save_bindings()
	bindings_changed.emit(action)


## Reset ALL bindings to project defaults and delete the cfg file
func reset_all_bindings() -> void:
	for action in _defaults:
		InputMap.action_erase_events(action)
		for ev in _defaults[action]:
			InputMap.action_add_event(action, ev)
	DirAccess.remove_absolute(SAVE_PATH)
	bindings_changed.emit("")


## Get display label for the first keyboard/mouse event of an action
func get_action_label(action: String) -> String:
	if not InputMap.has_action(action):
		return "---"
	var events := InputMap.action_get_events(action)
	for ev in events:
		if ev is InputEventKey or ev is InputEventMouseButton:
			return event_to_display_string(ev)
	# Fallback to first event of any type
	if events.size() > 0:
		return event_to_display_string(events[0])
	return "---"


## Get display label for the first gamepad event of an action
func get_action_gamepad_label(action: String) -> String:
	if not InputMap.has_action(action):
		return "---"
	var events := InputMap.action_get_events(action)
	for ev in events:
		if ev is InputEventJoypadButton or ev is InputEventJoypadMotion:
			return event_to_display_string(ev)
	return "---"


## Convert an InputEvent to a human-readable display string
func event_to_display_string(event: InputEvent) -> String:
	if event is InputEventKey:
		var kc: int = event.physical_keycode if event.physical_keycode != 0 else event.keycode
		if kc == KEY_ESCAPE:
			return "Esc"
		if kc == KEY_ENTER:
			return "Enter"
		if kc == KEY_SPACE:
			return "Space"
		if kc == KEY_TAB:
			return "Tab"
		return OS.get_keycode_string(kc)
	if event is InputEventMouseButton:
		match event.button_index:
			MOUSE_BUTTON_LEFT:   return "LMB"
			MOUSE_BUTTON_RIGHT:  return "RMB"
			MOUSE_BUTTON_MIDDLE: return "MMB"
			_: return "Mouse%d" % event.button_index
	if event is InputEventJoypadButton:
		match event.button_index:
			JOY_BUTTON_A:             return "A"
			JOY_BUTTON_B:             return "B"
			JOY_BUTTON_X:             return "X"
			JOY_BUTTON_Y:             return "Y"
			JOY_BUTTON_LEFT_SHOULDER: return "LB"
			JOY_BUTTON_RIGHT_SHOULDER:return "RB"
			JOY_BUTTON_LEFT_STICK:    return "LS"
			JOY_BUTTON_RIGHT_STICK:   return "RS"
			JOY_BUTTON_BACK:          return "Select"
			JOY_BUTTON_START:         return "Start"
			JOY_BUTTON_DPAD_UP:       return "DPad-Up"
			JOY_BUTTON_DPAD_DOWN:     return "DPad-Down"
			JOY_BUTTON_DPAD_LEFT:     return "DPad-Left"
			JOY_BUTTON_DPAD_RIGHT:    return "DPad-Right"
			_: return "Pad%d" % event.button_index
	if event is InputEventJoypadMotion:
		var axis_name := ""
		match event.axis:
			JOY_AXIS_LEFT_X:       axis_name = "LStick-X"
			JOY_AXIS_LEFT_Y:       axis_name = "LStick-Y"
			JOY_AXIS_RIGHT_X:      axis_name = "RStick-X"
			JOY_AXIS_RIGHT_Y:      axis_name = "RStick-Y"
			JOY_AXIS_TRIGGER_LEFT: return "LT"
			JOY_AXIS_TRIGGER_RIGHT:return "RT"
			_: axis_name = "Axis%d" % event.axis
		var dir := "+" if event.axis_value > 0 else "-"
		return axis_name + dir
	return event.as_text()


## Check if a given event conflicts with another action's binding
## Returns the conflicting action name, or "" if no conflict
func find_conflict(action: String, event: InputEvent) -> String:
	for other_action in ACTION_LABELS:
		if other_action == action:
			continue
		if not InputMap.has_action(other_action):
			continue
		for ev in InputMap.action_get_events(other_action):
			if _events_match(ev, event):
				return other_action
	return ""


## Check if two events match (same type + same key/button/axis)
func _events_match(a: InputEvent, b: InputEvent) -> bool:
	if a is InputEventKey and b is InputEventKey:
		var ak: int = a.physical_keycode if a.physical_keycode != 0 else a.keycode
		var bk: int = b.physical_keycode if b.physical_keycode != 0 else b.keycode
		return ak == bk
	if a is InputEventMouseButton and b is InputEventMouseButton:
		return a.button_index == b.button_index
	if a is InputEventJoypadButton and b is InputEventJoypadButton:
		return a.button_index == b.button_index
	if a is InputEventJoypadMotion and b is InputEventJoypadMotion:
		return a.axis == b.axis and sign(a.axis_value) == sign(b.axis_value)
	return false
