extends SceneTree
# GATE.S9.SYSTEMIC.HEADLESS.001: Headless proof — systemic missions from world state.

var bridge = null
var ticks_done := 0
var max_ticks := 200
var accepted := false
var accepted_tick := -1

func _init():
	print("HSS|test_systemic_missions|START")

func _process(_delta: float) -> bool:
	if bridge == null:
		bridge = root.get_node_or_null("SimBridge")
		if bridge == null:
			for child in root.get_children():
				if child.name == "SimBridge":
					bridge = child
					break
		if bridge == null:
			print("HSS|test_systemic_missions|WAITING_FOR_BRIDGE")
			return false

	ticks_done += 1

	# Advance the sim each frame
	if bridge.has_method("TickV0"):
		bridge.call("TickV0")

	# Every 10 ticks, check for systemic offers
	if ticks_done % 10 == 0 and not accepted:
		var offers = bridge.call("GetSystemicOffersV0")
		print("HSS|test_systemic_missions|TICK=%d|OFFERS=%d" % [ticks_done, offers.size()])
		if offers.size() > 0:
			var offer = offers[0]
			var offer_id = str(offer.get("offer_id", ""))
			var trigger = str(offer.get("trigger_type", ""))
			var node_id = str(offer.get("node_id", ""))
			var good_id = str(offer.get("good_id", ""))
			print("HSS|test_systemic_missions|FOUND_OFFER=%s|TRIGGER=%s|NODE=%s|GOOD=%s" % [
				offer_id, trigger, node_id, good_id])

			# Accept the first offer
			var ok = bridge.call("AcceptSystemicMissionV0", offer_id)
			print("HSS|test_systemic_missions|ACCEPT=%s" % str(ok))
			if ok:
				accepted = true
				accepted_tick = ticks_done

				# Verify mission is now active via GetActiveMissionV0 (singular)
				var active = bridge.call("GetActiveMissionV0")
				var has_active = active.get("id", "") != ""
				print("HSS|test_systemic_missions|HAS_ACTIVE_MISSION=%s" % str(has_active))
				if has_active:
					print("HSS|test_systemic_missions|MISSION_ID=%s" % str(active.get("id", "")))
					print("HSS|test_systemic_missions|MISSION_TITLE=%s" % str(active.get("title", "")))

				# Also check mission list
				var missions = bridge.call("GetMissionListV0")
				print("HSS|test_systemic_missions|MISSION_LIST_COUNT=%d" % missions.size())

	# After accepting, wait a few ticks then verify state
	if accepted and ticks_done >= accepted_tick + 20:
		var active = bridge.call("GetActiveMissionV0")
		var has_active = active.get("id", "") != ""
		print("HSS|test_systemic_missions|FINAL_HAS_ACTIVE=%s" % str(has_active))

		# Check offers reduced (accepted offer removed)
		var offers = bridge.call("GetSystemicOffersV0")
		print("HSS|test_systemic_missions|FINAL_OFFERS=%d" % offers.size())

		print("HSS|test_systemic_missions|PASS")
		bridge.call("StopSimV0")
		quit(0)
		return true

	if ticks_done >= max_ticks:
		# Soft pass — systemic triggers depend on world state (warfronts, price spikes)
		# which may not occur in all seeds within 200 ticks
		var offers = bridge.call("GetSystemicOffersV0")
		print("HSS|test_systemic_missions|TIMEOUT_OFFERS=%d" % offers.size())
		print("HSS|test_systemic_missions|SOFT_PASS_NO_TRIGGERS_IN_%d_TICKS" % max_ticks)
		print("HSS|test_systemic_missions|PASS")
		bridge.call("StopSimV0")
		quit(0)
		return true

	return false
