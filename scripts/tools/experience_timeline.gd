## EPIC.X.EXPERIENCE_PROOF.V0 — Component 2
## Multi-snapshot trajectory analysis over time.
## Records observer snapshots tagged with phase labels,
## then provides trajectory extraction and analysis.

var _observer = null  # ExperienceObserver instance (untyped for headless compat)
var _snapshots: Array = []  # [{phase, tick, report}]


func init_v0(observer) -> void:
	_observer = observer


func record_snapshot_v0(phase_label: String) -> void:
	if _observer == null:
		return
	var report = _observer.capture_full_report_v0()
	_snapshots.append({
		"phase": phase_label,
		"tick": report.get("tick", -1),
		"report": report,
	})


func get_snapshot_count() -> int:
	return _snapshots.size()


## Extract a single field across all snapshots.
## field_path uses dot notation: "player.credits", "hud.labels", etc.
func get_trajectory_v0(field_path: String) -> Array:
	var values: Array = []
	for snap in _snapshots:
		values.append(_resolve_path(snap.get("report", {}), field_path))
	return values


## Did the value grow (last > first)?
func is_increasing_v0(field_path: String) -> bool:
	var vals = get_trajectory_v0(field_path)
	if vals.size() < 2:
		return false
	var first = vals[0]
	var last = vals[vals.size() - 1]
	if first is int and last is int:
		return last > first
	if first is float and last is float:
		return last > first
	return false


## After trigger_phase, did the value change by response_phase?
func is_responsive_v0(field_path: String, trigger_phase: String, response_phase: String) -> bool:
	var trigger_val = null
	var response_val = null
	for snap in _snapshots:
		if snap.get("phase", "") == trigger_phase:
			trigger_val = _resolve_path(snap.get("report", {}), field_path)
		elif snap.get("phase", "") == response_phase and trigger_val != null:
			response_val = _resolve_path(snap.get("report", {}), field_path)
			break
	if trigger_val == null or response_val == null:
		return false
	return str(trigger_val) != str(response_val)


## Longest run of identical consecutive values (detects "nothing happening").
func max_stale_window_v0(field_path: String) -> int:
	var vals = get_trajectory_v0(field_path)
	if vals.size() < 2:
		return 0
	var max_run := 1
	var cur_run := 1
	for i in range(1, vals.size()):
		if str(vals[i]) == str(vals[i - 1]):
			cur_run += 1
			if cur_run > max_run:
				max_run = cur_run
		else:
			cur_run = 1
	return max_run


## Unique ship_state values seen across all snapshots.
func states_visited_v0() -> Array:
	var seen := {}
	for snap in _snapshots:
		var report: Dictionary = snap.get("report", {})
		var player: Dictionary = report.get("player", {})
		var state: String = str(player.get("ship_state", ""))
		if not state.is_empty():
			seen[state] = true
	return seen.keys()


## Events per window across timeline.
func event_rate_v0(event_field: String, window_size: int) -> Array:
	var vals = get_trajectory_v0(event_field)
	if vals.size() < 2 or window_size < 1:
		return []
	var rates: Array = []
	var i := 0
	while i + window_size <= vals.size():
		var changes := 0
		for j in range(i + 1, i + window_size):
			if str(vals[j]) != str(vals[j - 1]):
				changes += 1
		rates.append(changes)
		i += window_size
	return rates


## Phase labels in order.
func get_phases_v0() -> Array:
	var phases: Array = []
	for snap in _snapshots:
		phases.append(snap.get("phase", ""))
	return phases


## Full timeline report for JSON output.
func get_timeline_report_v0() -> Dictionary:
	var phases = get_phases_v0()
	var credit_traj = get_trajectory_v0("player.credits")
	var state_traj = get_trajectory_v0("player.ship_state")
	var hull_traj = get_trajectory_v0("player.hull")
	var cargo_traj = get_trajectory_v0("player.cargo_count")
	return {
		"timeline_version": 1,
		"snapshot_count": _snapshots.size(),
		"phases": phases,
		"trajectories": {
			"credits": credit_traj,
			"ship_state": state_traj,
			"hull": hull_traj,
			"cargo_count": cargo_traj,
		},
		"analysis": {
			"credits_increasing": is_increasing_v0("player.credits"),
			"states_visited": states_visited_v0(),
			"states_visited_count": states_visited_v0().size(),
			"max_stale_credits": max_stale_window_v0("player.credits"),
			"max_stale_hull": max_stale_window_v0("player.hull"),
		},
		"snapshots": _snapshots,
	}


# --- Helpers ---

## Resolve dot-separated path in a nested Dictionary.
func _resolve_path(data: Dictionary, path: String):
	var parts := path.split(".")
	var current = data
	for part in parts:
		if current is Dictionary:
			current = current.get(part, null)
		else:
			return null
	return current
