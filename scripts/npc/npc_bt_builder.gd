extends RefCounted
class_name NpcBtBuilder
## GATE.S16.NPC_ALIVE.BT_ROLES.001
## Builds LimboAI BehaviorTree resources programmatically for each NPC fleet role.
## Usage: var tree = NpcBtBuilder.build_for_role(role_int)

## Build the appropriate BT for the given role (0=Trader, 1=Hauler, 2=Patrol).
static func build_for_role(role: int) -> BehaviorTree:
	match role:
		0: return _build_trader_bt()
		1: return _build_hauler_bt()
		2: return _build_patrol_bt()
		_: return _build_trader_bt()


## Trader: SelectDestination -> FlyToPoint -> (wait) -> loop
static func _build_trader_bt() -> BehaviorTree:
	var tree := BehaviorTree.new()

	# Root: repeat forever
	var repeat := BTRepeat.new()
	repeat.forever = true

	var seq := BTSequence.new()

	# 1. Select destination from sim
	var select := BtSelectDestination.new()
	seq.add_child(select)

	# 2. Fly to target
	var fly := BtFlyToPoint.new()
	seq.add_child(fly)

	# 3. Wait at destination (docking)
	var wait := BTDelay.new()
	wait.seconds = 3.0
	seq.add_child(wait)

	repeat.add_child(seq)
	tree.root_task = repeat
	return tree


## Hauler: same pattern as trader but faster cycle
static func _build_hauler_bt() -> BehaviorTree:
	var tree := BehaviorTree.new()

	var repeat := BTRepeat.new()
	repeat.forever = true

	var seq := BTSequence.new()

	var select := BtSelectDestination.new()
	seq.add_child(select)

	var fly := BtFlyToPoint.new()
	seq.add_child(fly)

	var wait := BTDelay.new()
	wait.seconds = 2.0
	seq.add_child(wait)

	repeat.add_child(seq)
	tree.root_task = repeat
	return tree


## Patrol: orbit waypoints, check for player aggro
static func _build_patrol_bt() -> BehaviorTree:
	var tree := BehaviorTree.new()

	var repeat := BTRepeat.new()
	repeat.forever = true

	var selector := BTSelector.new()

	# Priority 1: If at destination, pick new waypoint
	var reroute_seq := BTSequence.new()
	var at_dest := BtAtDestination.new()
	at_dest.arrival_distance = 5.0
	reroute_seq.add_child(at_dest)
	var select := BtSelectDestination.new()
	reroute_seq.add_child(select)
	selector.add_child(reroute_seq)

	# Priority 2: Keep flying to current target
	var fly := BtFlyToPoint.new()
	selector.add_child(fly)

	repeat.add_child(selector)
	tree.root_task = repeat
	return tree
