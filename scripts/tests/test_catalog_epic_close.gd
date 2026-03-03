extends SceneTree

# GATE.S4.CATALOG.EPIC_CLOSE.001
# Verifies: GetCatalogGoodsV0() returns all 4 catalog goods (food, fuel, metal, ore).
# This tests the content registry, NOT market inventory. food is production-only and not
# universally stocked at genesis — catalog completeness must be proven via GetCatalogGoodsV0().
# Emits: CAT_CLOSE|PASS

const PREFIX := "CATV0|"
const MAX_POLLS := 400

enum Phase { WAIT_BRIDGE, CHECK_CATALOG, DONE }

var _phase := Phase.WAIT_BRIDGE
var _polls := 0
var _bridge = null


func _initialize() -> void:
	print(PREFIX + "CAT_CLOSE_START")


func _process(_delta: float) -> bool:
	match _phase:
		Phase.WAIT_BRIDGE:
			_bridge = root.get_node_or_null("SimBridge")
			if _bridge != null:
				_polls = 0
				_phase = Phase.CHECK_CATALOG
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					print(PREFIX + "ERROR: SimBridge not found")
					_quit()

		Phase.CHECK_CATALOG:
			# GetCatalogGoodsV0 is a pure registry read — no sim state required.
			var goods = _bridge.call("GetCatalogGoodsV0")
			if typeof(goods) != TYPE_ARRAY or goods.size() == 0:
				_polls += 1
				if _polls >= MAX_POLLS:
					print(PREFIX + "FAIL: catalog empty or not array")
					_quit()
				return false

			var required := ["food", "fuel", "metal", "ore"]
			var found := {}
			for gid in goods:
				var s := str(gid)
				if s in required:
					found[s] = true

			var missing: Array = []
			for req in required:
				if not found.has(req):
					missing.append(req)

			if missing.size() > 0:
				print(PREFIX + "FAIL: missing_goods=%s" % str(missing))
				_quit()
				return false

			print(PREFIX + "CATALOG|PASS|goods=food,fuel,metal,ore|count=%d" % goods.size())
			print(PREFIX + "CAT_CLOSE|PASS")
			_phase = Phase.DONE
			_quit()

		Phase.DONE:
			pass

	return false


func _quit() -> void:
	_phase = Phase.DONE
	if _bridge and _bridge.has_method("StopSimV0"):
		_bridge.call("StopSimV0")
	quit(0)
