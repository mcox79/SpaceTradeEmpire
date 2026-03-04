## EPIC.X.EXPERIENCE_PROOF.V0 — Component 4
## Assertion engine operating on ExperienceTimeline data.
## Returns [{check_name, passed, detail, diagnostic, investigate}].

const AuditScript = preload("res://scripts/tools/aesthetic_audit.gd")

var _timeline = null  # ExperienceTimeline instance (untyped for headless compat)
var _audit = null  # AestheticAudit instance


func init_v0(timeline) -> void:
	_timeline = timeline
	_audit = AuditScript.new()


## Run all metric checks. Returns array of check results.
func run_all_checks_v0(latest_report: Dictionary = {}) -> Array:
	var results: Array = []
	results.append_array(_functional_checks_v0())
	results.append_array(_pacing_checks_v0())
	if not latest_report.is_empty():
		results.append_array(_aesthetic_checks_v0(latest_report))
	return results


# ── Functional checks (proxy for "is the game working?") ──

func _functional_checks_v0() -> Array:
	var results: Array = []
	if _timeline == null:
		return results

	# 1. credits_change_after_trade: credits must change during a trade cycle
	var credit_traj = _timeline.get_trajectory_v0("player.credits")
	var credits_changed := false
	for i in range(1, credit_traj.size()):
		if str(credit_traj[i]) != str(credit_traj[i - 1]):
			credits_changed = true
			break
	results.append(_check("credits_change_after_trade", credits_changed,
		"credits=%s across phases %s" % [str(credit_traj), str(_timeline.get_phases_v0())],
		"Credits unchanged across session. Likely: TradeCommand not executing, or MarketSystem not processing, or PlayerCredits field not wired.",
		["scripts/bridge/SimBridge.Market.cs", "SimCore/Systems/MarketSystem.cs"]))

	# 2. hp_change_after_combat: hull must change if combat occurred
	var hull_traj = _timeline.get_trajectory_v0("player.hull")
	var states = _timeline.states_visited_v0()
	var had_combat = "IN_COMBAT" in states
	var hull_changed := false
	for i in range(1, hull_traj.size()):
		if str(hull_traj[i]) != str(hull_traj[i - 1]):
			hull_changed = true
			break
	var hp_pass = hull_changed or not had_combat
	results.append(_check("hp_change_after_combat", hp_pass,
		"hull=%s, had_combat=%s" % [str(hull_traj), str(had_combat)],
		"HP unchanged despite combat. Likely: ApplyTurretShot/ApplyAiShot not reducing hull, or combat state not reached.",
		["scripts/bridge/SimBridge.Combat.cs", "SimCore/Systems/CombatSystem.cs"]))

	# 3. hud_reflects_state: CreditsLabel text must contain actual credit value
	var phases = _timeline.get_phases_v0()
	var hud_coherent := true
	var hud_detail := ""
	for i in range(_timeline.get_snapshot_count()):
		var snap = _timeline._snapshots[i]
		var report: Dictionary = snap.get("report", {})
		var player: Dictionary = report.get("player", {})
		var hud: Dictionary = report.get("hud", {})
		var credits: int = int(player.get("credits", 0))
		var labels: Array = hud.get("labels", [])
		for lbl in labels:
			if str(lbl.get("name", "")) == "CreditsLabel":
				var text: String = str(lbl.get("text", ""))
				if str(credits) not in text and credits > 0:
					hud_coherent = false
					hud_detail = "phase=%s credits=%d label='%s'" % [str(phases[i]), credits, text]
					break
		if not hud_coherent:
			break
	results.append(_check("hud_reflects_state", hud_coherent,
		hud_detail if not hud_coherent else "HUD labels consistent with state",
		"CreditsLabel text does not contain actual credit value. Display is disconnected from data.",
		["scripts/ui/hud.gd", "scripts/bridge/SimBridge.cs"]))

	# 4. first_reward_within_bounds: first credit increase within 120 ticks
	var first_reward_tick := -1
	var initial_credits = credit_traj[0] if credit_traj.size() > 0 else 0
	for i in range(1, credit_traj.size()):
		var c = credit_traj[i]
		if c is int and initial_credits is int and c > initial_credits:
			var snap = _timeline._snapshots[i]
			first_reward_tick = int(snap.get("tick", -1))
			break
	var start_tick: int = int(_timeline._snapshots[0].get("tick", 0)) if _timeline.get_snapshot_count() > 0 else 0
	var reward_elapsed := first_reward_tick - start_tick if first_reward_tick >= 0 else 9999
	results.append(_check("first_reward_within_bounds", reward_elapsed <= 120,
		"first_reward_at_tick=%d (elapsed=%d)" % [first_reward_tick, reward_elapsed],
		"First profit took >120 ticks. Onboarding is too slow or economy is disconnected.",
		["SimCore/Systems/MarketSystem.cs", "SimCore/Gen/GalaxyGenerator.cs"]))

	return results


# ── Pacing checks (proxy for "is the game fun?") ──

func _pacing_checks_v0() -> Array:
	var results: Array = []
	if _timeline == null:
		return results

	# 1. state_coverage: >= 3 unique ship states visited
	var states = _timeline.states_visited_v0()
	results.append(_check("state_coverage", states.size() >= 3,
		"states_visited=%s (count=%d)" % [str(states), states.size()],
		"Player visited <3 ship states. Major game systems may be unreachable.",
		["scripts/bridge/SimBridge.cs", "SimCore/SimKernel.cs"]))

	# 2. max_stale_window: no metric unchanged for > half of snapshots
	var max_stale = _timeline.max_stale_window_v0("player.credits")
	var snap_count = _timeline.get_snapshot_count()
	var stale_limit: int = maxi(snap_count / 2, 3)
	results.append(_check("max_stale_window", max_stale < stale_limit,
		"max_stale_credits=%d (limit=%d, snapshots=%d)" % [max_stale, stale_limit, snap_count],
		"Credits unchanged for %d consecutive snapshots. Game may have dead periods." % max_stale,
		["SimCore/SimKernel.cs", "SimCore/Systems/MarketSystem.cs"]))

	# 3. progression_trajectory: credits should trend upward over session
	var credits_up = _timeline.is_increasing_v0("player.credits")
	results.append(_check("progression_trajectory", credits_up,
		"credits_increasing=%s" % str(credits_up),
		"Credits did not increase over the session. Economy may not produce net-positive trades.",
		["SimCore/Systems/MarketSystem.cs", "SimCore/Gen/GalaxyGenerator.cs"]))

	return results


# ── Aesthetic checks (delegates to AestheticAudit) ──

func _aesthetic_checks_v0(report: Dictionary) -> Array:
	var results: Array = []
	if _audit == null:
		return results
	var audit_results = _audit.run_audit_v0(report)
	for ar in audit_results:
		results.append(_check(
			"aesthetic_%s" % str(ar.get("flag", "")),
			ar.get("passed", true),
			str(ar.get("detail", "")),
			"Aesthetic flag %s (%s) failed." % [str(ar.get("flag", "")), ar.get("severity", "")],
			[]
		))
	return results


# ── Output ──

## Print results summary to stdout.
func print_results_v0(results: Array, prefix: String = "EXPV0") -> void:
	var pass_count := 0
	var fail_count := 0
	for r in results:
		var status := "PASS" if r.get("passed", false) else "FAIL"
		if r.get("passed", false):
			pass_count += 1
		else:
			fail_count += 1
		print("%s|%s|%s|%s" % [prefix, status, str(r.get("check", "")), str(r.get("detail", ""))])
		if not r.get("passed", false):
			print("%s|DIAGNOSTIC|%s" % [prefix, str(r.get("diagnostic", ""))])
			var inv: Array = r.get("investigate", [])
			if inv.size() > 0:
				print("%s|INVESTIGATE|%s" % [prefix, str(inv)])
	print("%s|SUMMARY|passed=%d failed=%d total=%d" % [prefix, pass_count, fail_count, pass_count + fail_count])


func _check(name: String, passed: bool, detail: String, diagnostic: String, investigate: Array) -> Dictionary:
	return {
		"check": name,
		"passed": passed,
		"detail": detail,
		"diagnostic": diagnostic,
		"investigate": investigate,
	}
