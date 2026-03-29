extends Node
class_name EconomyAlertPoller
## GATE.T47.DIGEST.MARKET_ALERTS.001: Polls GetMarketAlertsV0 and pushes colored toasts.
## Economy digest: market price spikes, drops, stockouts — with type-specific styling.

var _bridge: Node = null
var _toast_manager: Node = null
var _poll_timer: float = 0.0

const POLL_INTERVAL: float = 30.0  # seconds (~30 sim ticks at 1 tick/sec)
const MAX_TOASTS_PER_POLL: int = 3


func _ready() -> void:
	_bridge = get_node_or_null("/root/SimBridge")
	_toast_manager = get_node_or_null("/root/ToastManager")


func _process(delta: float) -> void:
	_poll_timer += delta
	if _poll_timer >= POLL_INTERVAL:
		_poll_timer = 0.0
		_poll_alerts()


func _poll_alerts() -> void:
	if not _bridge:
		_bridge = get_node_or_null("/root/SimBridge")
	if not _bridge:
		return
	if not _bridge.has_method("GetMarketAlertsV0"):
		return
	var alerts = _bridge.call("GetMarketAlertsV0", 10)
	if not alerts or not alerts is Array or alerts.size() == 0:
		return

	if not _toast_manager:
		_toast_manager = get_node_or_null("/root/ToastManager")
	if not _toast_manager:
		return

	var count: int = 0
	for alert in alerts:
		if count >= MAX_TOASTS_PER_POLL:
			break
		var text: String = _format_alert(alert)
		if text.is_empty():
			continue
		var color: String = _get_alert_color(alert)
		# Use colored toast if available, otherwise fallback to trade priority toast.
		if _toast_manager.has_method("show_toast_colored"):
			_toast_manager.call("show_toast_colored", text, 5.0, color)
		elif _toast_manager.has_method("show_priority_toast"):
			_toast_manager.call("show_priority_toast", text, "trade")
		elif _toast_manager.has_method("show_toast"):
			_toast_manager.call("show_toast", text, 5.0)
		count += 1
		print("UUIR|MARKET_ALERT|%s|%s" % [str(alert.get("type", "")), str(alert.get("good_id", ""))])


func _format_alert(alert: Dictionary) -> String:
	var good_id: String = str(alert.get("good_id", "Unknown"))
	var good_name: String = good_id.replace("_", " ").capitalize()
	var node_id: String = str(alert.get("node_id", ""))
	var station: String = _get_node_display_name(node_id)
	var pct: int = int(alert.get("change_pct", 0))
	var alert_type: String = str(alert.get("type", ""))

	match alert_type:
		"stockout":
			return "Shortage: %s at %s" % [good_name, station]
		"price_spike":
			return "%s price up %d%% at %s" % [good_name, pct, station]
		"price_drop":
			return "%s price down %d%% at %s" % [good_name, absi(pct), station]
		_:
			return ""


## Returns hex color string by alert type for toast styling.
func _get_alert_color(alert: Dictionary) -> String:
	var alert_type: String = str(alert.get("type", ""))
	match alert_type:
		"stockout":
			return "#FF9933"   # orange — shortage / supply dropped
		"price_spike":
			return "#FFD933"   # yellow — price increase
		"price_drop":
			return "#33DDFF"   # cyan — price decrease / surplus
		_:
			return "#CCCCCC"


## Get station display name from bridge, with fallback formatting.
func _get_node_display_name(node_id: String) -> String:
	if node_id.is_empty():
		return "Unknown"
	if _bridge and _bridge.has_method("GetNodeDisplayNameV0"):
		var display_name: String = str(_bridge.call("GetNodeDisplayNameV0", node_id))
		if not display_name.is_empty():
			# Strip parenthesized production tags for clean display.
			var paren_idx: int = display_name.find("(")
			if paren_idx > 0:
				display_name = display_name.substr(0, paren_idx).strip_edges()
			return display_name
	return node_id.replace("_", " ").capitalize()
