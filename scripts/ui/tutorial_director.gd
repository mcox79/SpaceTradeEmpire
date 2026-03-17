# scripts/ui/tutorial_director.gd
# Tutorial orchestrator: polls SimBridge for tutorial phase, drives UI (dialogue box,
# selection overlay, input blocking), and calls bridge to advance phases.
# Rotating FO auditions: pre-selection, each FO takes turns advising. Post-trade, player chooses.
extends Node

const FODialogueBox = preload("res://scripts/ui/fo_dialogue_box.gd")
const FOSelectionOverlay = preload("res://scripts/ui/fo_selection_overlay.gd")

const POLL_INTERVAL := 0.5  # Seconds between bridge polls
const STALL_NUDGE_SECONDS := 60.0  # Show nudge dialogue after this long

var _bridge: Node = null
var _gm: Node = null
var _dialogue_box = null  # FODialogueBox instance
var _selection_overlay = null  # FOSelectionOverlay instance

var _poll_timer := 0.0
var _last_phase := -1  # Track phase changes to trigger dialogue
var _last_phase_name := ""
var _dialogue_shown_for_phase := -1  # Prevent re-showing dialogue for same phase
var _is_headless := false
var _in_hail_sequence := false  # Guard: prevents line_dismissed from advancing tutorial during FO hails.

# FO info (cached after selection).
var _fo_type := ""
var _fo_name := ""

# Trade guidance: tutorial sell target node ID for edgedar waypoint.
var _tutorial_target_node_id := ""

# Candidate name lookup.
const FO_NAMES := {
	"Analyst": "Maren",
	"Veteran": "Dask",
	"Pathfinder": "Lira",
}

# Candidate color lookup.
const FO_COLORS := {
	"Analyst": Color(0.4, 0.6, 1.0),
	"Veteran": Color(1.0, 0.8, 0.3),
	"Pathfinder": Color(0.3, 0.9, 0.5),
}


func _ready() -> void:
	_is_headless = DisplayServer.get_name() == "headless"
	# Find bridge and game manager.
	_bridge = get_node_or_null("/root/SimBridge")
	_gm = get_parent()  # Assumed to be GameManager
	if _bridge == null:
		# Deferred: try again next frame.
		await get_tree().process_frame
		_bridge = get_node_or_null("/root/SimBridge")

	# Create dialogue box (persistent, reused across phases).
	_dialogue_box = FODialogueBox.new()
	_dialogue_box.name = "TutorialDialogueBox"
	add_child(_dialogue_box)
	_dialogue_box.line_dismissed.connect(_on_dialogue_dismissed)


func _process(delta: float) -> void:
	if _bridge == null:
		_bridge = get_node_or_null("/root/SimBridge")
		if _bridge == null:
			return

	_poll_timer += delta
	if _poll_timer < POLL_INTERVAL and not _is_headless:
		return
	_poll_timer = 0.0

	_poll_tutorial_state()


func _poll_tutorial_state() -> void:
	if not _bridge.has_method("GetTutorialStateV0"):
		return

	var state: Dictionary = _bridge.call("GetTutorialStateV0")
	if state.is_empty():
		return  # Tutorial not initialized or complete

	var phase: int = int(state.get("phase", 0))
	var phase_name: String = str(state.get("phase_name", ""))

	# Phase changed — trigger new dialogue or UI action.
	if phase != _last_phase:
		_last_phase = phase
		_last_phase_name = phase_name
		_on_phase_changed(phase, phase_name, state)

	# Update HUD objective from tutorial.
	var objective: String = str(state.get("objective", ""))
	if _gm:
		_update_hud_objective(objective)


func _on_phase_changed(phase: int, phase_name: String, state: Dictionary) -> void:
	var pre_selection: bool = bool(state.get("pre_selection", false))

	match phase_name:
		"FO_Selection":
			_show_fo_hails()
		"FO_Selection_PostTrade":
			_show_post_trade_selection()
		"Flight_Intro", "Docked_First", "Market_Explain", \
		"Buy_Complete", "Sell_Prompt", "Sell_Complete":
			if pre_selection:
				_show_rotating_fo_dialogue(phase, phase_name)
			else:
				_show_phase_dialogue(phase, phase_name)
		"FO_Selected_Settle", "Faction_Explain", \
		"Explore_Prompt", "Explore_Complete", "Automation_Explain", \
		"Automation_Complete", "Mystery_Tease", "Tutorial_Complete":
			_show_phase_dialogue(phase, phase_name)
		"Dock_Prompt", "Buy_Prompt", "Travel_Prompt", "Automation_Prompt":
			# Action phases — no dialogue on entry (player must act).
			pass
		_:
			pass

	# On Flight_Intro: brief silence after FO greeting, then unlock controls.
	if phase_name == "Flight_Intro":
		if not _is_headless:
			await get_tree().create_timer(1.5).timeout
		_unlock_controls()

	# On Buy_Complete: set up trade guidance waypoint.
	if phase_name == "Buy_Complete":
		_setup_trade_waypoint()

	# On Tutorial_Complete: clean up.
	if phase_name == "Tutorial_Complete":
		_clear_trade_waypoint()


# ── FO Hails (Phase 1: FO_Selection) ────────────────────────────────

func _show_fo_hails() -> void:
	if not _bridge.has_method("GetTutorialFOHailsV0"):
		return

	# Guard: prevent line_dismissed from calling DismissTutorialDialogueV0 during hails.
	_in_hail_sequence = true

	var hails: Array = _bridge.call("GetTutorialFOHailsV0")

	# Brief pause to drain stale input from captain name confirm.
	if not _is_headless:
		await get_tree().create_timer(0.3).timeout

	# Ship Computer preamble.
	_dialogue_box.show_line("SHIP COMPUTER", Color(0.5, 0.5, 0.6),
		"Three officers responded to your posting. Incoming transmissions.")
	print("UUIR|TUTORIAL|HAIL|SHIP_COMPUTER")
	if _is_headless:
		await get_tree().process_frame
		_dialogue_box.advance_dialogue()
	else:
		await _dialogue_box.line_dismissed

	# Each FO hails the captain.
	for hail in hails:
		var col := Color(
			float(hail.get("color_r", 0.5)),
			float(hail.get("color_g", 0.5)),
			float(hail.get("color_b", 0.6))
		)
		var hail_name: String = str(hail.get("name", ""))
		var hail_text: String = str(hail.get("text", ""))
		_dialogue_box.show_line(hail_name, col, hail_text)
		print("UUIR|TUTORIAL|HAIL|%s|%s" % [hail_name, hail_text.left(60)])
		if _is_headless:
			await get_tree().process_frame
			_dialogue_box.advance_dialogue()
		else:
			await _dialogue_box.line_dismissed

	# Ship Computer handoff: "I'll patch them through one at a time."
	_dialogue_box.show_line("SHIP COMPUTER", Color(0.5, 0.5, 0.6),
		"I'll patch them through one at a time for a trial run. See who fits.")
	print("UUIR|TUTORIAL|HAIL|SHIP_COMPUTER_HANDOFF")
	if _is_headless:
		await get_tree().process_frame
		_dialogue_box.advance_dialogue()
	else:
		await _dialogue_box.line_dismissed

	_in_hail_sequence = false

	# Dismiss to advance phase (FO_Selection → Flight_Intro).
	if _bridge.has_method("DismissTutorialDialogueV0"):
		_bridge.call("DismissTutorialDialogueV0")


# ── Rotating FO Dialogue (Pre-Selection) ────────────────────────────

func _show_rotating_fo_dialogue(phase: int, phase_name: String) -> void:
	if _dialogue_shown_for_phase == phase:
		return
	_dialogue_shown_for_phase = phase

	if _dialogue_box == null or _bridge == null:
		return
	if not _bridge.has_method("GetRotatingFODialogueV0"):
		return

	var fo_data: Dictionary = _bridge.call("GetRotatingFODialogueV0")
	if fo_data.is_empty():
		# No dialogue for this phase — auto-dismiss.
		_bridge.call("DismissTutorialDialogueV0")
		return

	var fo_type: String = str(fo_data.get("type", ""))
	var fo_name: String = str(fo_data.get("name", ""))
	var text: String = str(fo_data.get("text", ""))
	var col := Color(
		float(fo_data.get("color_r", 0.5)),
		float(fo_data.get("color_g", 0.5)),
		float(fo_data.get("color_b", 0.6))
	)

	_dialogue_box.show_line(fo_name, col, text)
	print("UUIR|TUTORIAL|ROTATING_FO|%s|%s|%s" % [phase_name, fo_name, text.left(60)])

	# In headless: auto-advance after showing.
	if _is_headless:
		await get_tree().process_frame
		if _dialogue_box.is_waiting_for_advance():
			_dialogue_box.advance_dialogue()


# ── Post-Trade FO Selection (Phase 20) ──────────────────────────────

func _show_post_trade_selection() -> void:
	if _selection_overlay != null:
		return  # Already showing

	_go_to_selection_overlay()


func _go_to_selection_overlay() -> void:
	var candidates: Array = _bridge.call("GetTutorialCandidatesV0")
	var narrator: String = _bridge.call("GetTutorialNarratorPromptV0")

	_selection_overlay = FOSelectionOverlay.new()
	_selection_overlay.name = "FOSelectionOverlay"
	add_child(_selection_overlay)
	_selection_overlay.populate(candidates, narrator)
	_selection_overlay.candidate_selected.connect(_on_fo_selected)

	# In headless, auto-select the first candidate after a brief delay.
	if _is_headless:
		await get_tree().process_frame
		if candidates.size() > 0:
			var first_type: String = str(candidates[0].get("type", "Analyst"))
			_selection_overlay.select_candidate(first_type)


func _on_fo_selected(candidate_type: String) -> void:
	if _bridge == null or not _bridge.has_method("SelectTutorialFOV0"):
		return

	var success: bool = _bridge.call("SelectTutorialFOV0", candidate_type)
	if success:
		_fo_type = candidate_type
		_fo_name = FO_NAMES.get(candidate_type, candidate_type)
		print("UUIR|TUTORIAL|FO_SELECTED|%s" % candidate_type)

	_selection_overlay = null  # Overlay frees itself after selection animation.
	_clear_trade_waypoint()


# ── Single-FO Phase Dialogue (Post-Selection) ──────────────────────

func _show_phase_dialogue(phase: int, phase_name: String) -> void:
	if _dialogue_shown_for_phase == phase:
		return  # Already shown for this phase
	_dialogue_shown_for_phase = phase

	if _dialogue_box == null or _bridge == null:
		return
	if not _bridge.has_method("GetTutorialDialogueV0"):
		return

	var text: String = _bridge.call("GetTutorialDialogueV0")
	if text.is_empty():
		# No dialogue for this phase — auto-dismiss.
		_bridge.call("DismissTutorialDialogueV0")
		return

	_dialogue_box.show_line_by_type(_fo_type, _fo_name, text)
	print("UUIR|TUTORIAL|DIALOGUE|%s|%s" % [phase_name, text.left(60)])

	# In headless: auto-advance after showing.
	if _is_headless:
		await get_tree().process_frame
		if _dialogue_box.is_waiting_for_advance():
			_dialogue_box.advance_dialogue()


# ── Dialogue Dismiss Handler ────────────────────────────────────────

func _on_dialogue_dismissed() -> void:
	# During FO hail sequence, line_dismissed is used locally (awaited) — don't advance tutorial.
	if _in_hail_sequence:
		return
	if _bridge == null or not _bridge.has_method("DismissTutorialDialogueV0"):
		return
	_bridge.call("DismissTutorialDialogueV0")
	print("UUIR|TUTORIAL|DISMISS|phase=%s" % _last_phase_name)

	# Check if the bridge set DialogueDismissed (phase done) or incremented sequence (next beat).
	# Only re-fetch if the phase is NOT dismissed yet — otherwise we'd re-show the same line.
	var state: Dictionary = _bridge.call("GetTutorialStateV0")
	if bool(state.get("dialogue_dismissed", false)):
		return  # Phase is done, TutorialSystem will advance on next tick.

	# Multi-sequence support: DialogueSequence was incremented, show next beat.
	_show_next_beat_if_exists()


func _show_next_beat_if_exists() -> void:
	# Check for next beat via rotating FO (pre-selection) or single FO (post-selection).
	if _bridge.has_method("GetRotatingFODialogueV0"):
		var fo_data: Dictionary = _bridge.call("GetRotatingFODialogueV0")
		if not fo_data.is_empty():
			var fo_name: String = str(fo_data.get("name", ""))
			var text: String = str(fo_data.get("text", ""))
			var col := Color(
				float(fo_data.get("color_r", 0.5)),
				float(fo_data.get("color_g", 0.5)),
				float(fo_data.get("color_b", 0.6))
			)
			print("UUIR|TUTORIAL|NEXT_BEAT|%s|%s|%s" % [_last_phase_name, fo_name, text.left(60)])
			_dialogue_box.show_line(fo_name, col, text)
			if _is_headless:
				await get_tree().process_frame
				if _dialogue_box.is_waiting_for_advance():
					_dialogue_box.advance_dialogue()
			return

	# Fallback: check single-FO dialogue (post-selection).
	if _bridge.has_method("GetTutorialDialogueV0"):
		var next_text: String = _bridge.call("GetTutorialDialogueV0")
		if not next_text.is_empty():
			print("UUIR|TUTORIAL|NEXT_BEAT|%s|%s" % [_last_phase_name, next_text.left(60)])
			_dialogue_box.show_line_by_type(_fo_type, _fo_name, next_text)
			if _is_headless:
				await get_tree().process_frame
				if _dialogue_box.is_waiting_for_advance():
					_dialogue_box.advance_dialogue()
			return


# ── Controls Unlock ─────────────────────────────────────────────────

func _unlock_controls() -> void:
	if _gm == null:
		print("UUIR|TUTORIAL|UNLOCK_CONTROLS|SKIP_NO_GM")
		return
	# Unlock camera.
	var cam = _gm.get_node_or_null("PlayerFollowCamera")
	if cam == null:
		cam = get_node_or_null("/root/Main/PlayerFollowCamera")
	if cam:
		cam.set("input_locked", false)
	# Unfreeze hero ship.
	var hero = _gm.get("_hero_body")
	if hero and hero is RigidBody3D:
		hero.freeze = false
	# End intro mode.
	_gm.set("intro_active", false)
	print("UUIR|TUTORIAL|UNLOCK_CONTROLS|intro_active=false|hero_freeze=false")
	# Deferred dock proximity check.
	_check_dock_proximity_async()


func _check_dock_proximity_async() -> void:
	await get_tree().physics_frame
	await get_tree().physics_frame
	if _gm == null:
		return
	var hero = _gm.get("_hero_body")
	if hero == null:
		print("UUIR|TUTORIAL|DOCK_PROXIMITY_CHECK|NO_HERO")
		return
	var stations = get_tree().get_nodes_in_group("Station")
	print("UUIR|TUTORIAL|DOCK_PROXIMITY_CHECK|stations=%d" % stations.size())
	for station in stations:
		if station is Area3D:
			var overlapping = station.get_overlapping_bodies()
			if hero in overlapping:
				print("UUIR|TUTORIAL|DOCK_PROXIMITY_RESTORE|%s" % station.name)
				_gm.call("on_dock_proximity_v0", station)
				return
	print("UUIR|TUTORIAL|DOCK_PROXIMITY_CHECK|NOT_OVERLAPPING")


# ── Trade Guidance ──────────────────────────────────────────────────

func _setup_trade_waypoint() -> void:
	if _bridge == null or not _bridge.has_method("GetTutorialSellTargetV0"):
		return

	var target: Dictionary = _bridge.call("GetTutorialSellTargetV0")
	if target.is_empty():
		return

	_tutorial_target_node_id = str(target.get("node_id", ""))
	var node_name: String = str(target.get("node_name", ""))
	print("UUIR|TUTORIAL|SELL_TARGET|%s|%s" % [_tutorial_target_node_id, node_name])

	# Set edgedar waypoint.
	var edgedar = get_node_or_null("/root/Main/HUD/EdgedarOverlay")
	if edgedar == null:
		edgedar = get_tree().root.find_child("EdgedarOverlay", true, false)
	if edgedar:
		edgedar.set("tutorial_target_node_id", _tutorial_target_node_id)

	# Update objective text with station name.
	if not node_name.is_empty():
		_update_hud_objective("\u25b8 Sell at %s for profit" % node_name)


func _clear_trade_waypoint() -> void:
	_tutorial_target_node_id = ""
	var edgedar = get_node_or_null("/root/Main/HUD/EdgedarOverlay")
	if edgedar == null:
		edgedar = get_tree().root.find_child("EdgedarOverlay", true, false)
	if edgedar:
		edgedar.set("tutorial_target_node_id", "")


## Called by game_manager on dock. If the tutorial is stuck on an action phase,
## the FO re-nudges the player so they know what to do.
func on_dock_nudge() -> void:
	if _bridge == null or not _bridge.has_method("GetTutorialStateV0"):
		return
	var state: Dictionary = _bridge.call("GetTutorialStateV0")
	if state.is_empty():
		return
	var phase_name: String = str(state.get("phase_name", ""))
	var nudge_text := ""
	match phase_name:
		"Buy_Prompt":
			nudge_text = "Open the Market tab and buy a surplus good — look for the BEST BUY tag."
		"Travel_Prompt":
			nudge_text = "Undock and fly to a lane gate to travel to another system."
		"Sell_Prompt":
			nudge_text = "Check the Market here — sell your cargo where the price is higher."
		"Automation_Prompt":
			nudge_text = "Open the Programs tab and create an automated trade route."
		_:
			return  # Not an action phase or already handled.
	if nudge_text.is_empty():
		return
	# Show nudge via the current rotating FO or ship computer.
	if _bridge.has_method("GetRotatingFODialogueV0"):
		var fo_data: Dictionary = _bridge.call("GetRotatingFODialogueV0")
		if not fo_data.is_empty():
			var col := Color(
				float(fo_data.get("color_r", 0.5)),
				float(fo_data.get("color_g", 0.5)),
				float(fo_data.get("color_b", 0.6))
			)
			_dialogue_box.show_line(str(fo_data.get("name", "")), col, nudge_text)
			print("UUIR|TUTORIAL|NUDGE|%s|%s" % [phase_name, nudge_text.left(60)])
			return
	if not _fo_name.is_empty():
		_dialogue_box.show_line_by_type(_fo_type, _fo_name, nudge_text)
	else:
		_dialogue_box.show_line("SHIP COMPUTER", Color(0.5, 0.5, 0.6), nudge_text)
	print("UUIR|TUTORIAL|NUDGE|%s|%s" % [phase_name, nudge_text.left(60)])


## Check if current station is a bad sell location. Called externally by game_manager on dock.
func check_wrong_station_warning() -> void:
	if _bridge == null or not _bridge.has_method("IsBadSellStationV0"):
		return

	var result: Dictionary = _bridge.call("IsBadSellStationV0")
	if result.is_empty():
		return
	if not bool(result.get("is_bad", false)):
		return

	var better_name: String = str(result.get("better_node_name", ""))
	if better_name.is_empty():
		return

	# Get warning text from bridge.
	var warning: String = ""
	if _bridge.has_method("GetWrongStationWarningV0"):
		warning = _bridge.call("GetWrongStationWarningV0", better_name)
	if warning.is_empty():
		return

	# Show warning via dialogue box using the active rotating FO.
	var fo_data: Dictionary = {}
	if _bridge.has_method("GetRotatingFODialogueV0"):
		fo_data = _bridge.call("GetRotatingFODialogueV0")

	if not fo_data.is_empty():
		var col := Color(
			float(fo_data.get("color_r", 0.5)),
			float(fo_data.get("color_g", 0.5)),
			float(fo_data.get("color_b", 0.6))
		)
		_dialogue_box.show_line(str(fo_data.get("name", "")), col, warning)
	elif not _fo_name.is_empty():
		_dialogue_box.show_line_by_type(_fo_type, _fo_name, warning)
	else:
		_dialogue_box.show_line("SHIP COMPUTER", Color(0.5, 0.5, 0.6), warning)

	print("UUIR|TUTORIAL|WRONG_STATION|%s" % better_name)


# ── HUD Objective ───────────────────────────────────────────────────

func _update_hud_objective(text: String) -> void:
	var hud = get_node_or_null("/root/Main/HUD")
	if hud == null:
		hud = get_tree().root.find_child("HUD", true, false)
	if hud == null:
		return
	var label = hud.get("_guide_objective_label")
	if label != null and label is Label:
		label.text = text
		label.visible = not text.is_empty()
