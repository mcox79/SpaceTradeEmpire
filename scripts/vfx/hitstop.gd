extends Node
class_name Hitstop
## GATE.T52.COMBAT.HITSTOP.001
## Screen-freeze (hitstop) on shield break and kill events.
## Usage:
##   Hitstop.trigger(tree, duration_sec)
## Pauses the tree for the given duration, then unpauses.
## UI nodes with process_mode = PROCESS_MODE_ALWAYS remain responsive.
## Re-entrant safe: if already frozen, extends the timer to the longer duration.

const SHIELD_BREAK_DURATION: float = 0.08   # 80ms
const KILL_DURATION: float = 0.12            # 120ms

## Active hitstop node name (singleton pattern in tree).
const HITSTOP_NODE_NAME := "HitstopController"

## True while a hitstop freeze is active.
static var _is_frozen: bool = false


## Trigger a hitstop freeze. Safe to call from anywhere.
## tree: the SceneTree to pause.
## duration: freeze duration in seconds.
static func trigger(tree: SceneTree, duration: float) -> void:
	if tree == null:
		return
	# If already frozen and a longer freeze is requested, we extend.
	# Otherwise skip (don't shorten an active freeze).
	if _is_frozen:
		# Find existing controller and extend if needed.
		var existing = tree.root.get_node_or_null(HITSTOP_NODE_NAME)
		if existing and is_instance_valid(existing) and existing.has_method("_extend"):
			existing.call("_extend", duration)
		return

	_is_frozen = true
	tree.paused = true

	# Create a temporary node that lives above the pause to run the unfreeze timer.
	var controller := Hitstop.new()
	controller.name = HITSTOP_NODE_NAME
	controller.process_mode = Node.PROCESS_MODE_ALWAYS
	tree.root.add_child(controller)
	controller._start_unfreeze(tree, duration)


## Convenience: trigger shield-break hitstop.
static func on_shield_break(tree: SceneTree) -> void:
	trigger(tree, SHIELD_BREAK_DURATION)


## Convenience: trigger kill hitstop.
static func on_kill(tree: SceneTree) -> void:
	trigger(tree, KILL_DURATION)


## --- Instance methods (used by the temporary controller node) ---

var _remaining: float = 0.0
var _tree_ref: SceneTree = null


func _start_unfreeze(tree: SceneTree, duration: float) -> void:
	_tree_ref = tree
	_remaining = duration


func _extend(new_duration: float) -> void:
	if new_duration > _remaining:
		_remaining = new_duration


func _process(delta: float) -> void:
	if _remaining <= 0.0:
		return
	_remaining -= delta
	if _remaining <= 0.0:
		_unfreeze()


func _unfreeze() -> void:
	if _tree_ref and is_instance_valid(_tree_ref.root):
		_tree_ref.paused = false
	_is_frozen = false
	queue_free()
