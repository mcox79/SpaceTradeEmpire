extends CharacterBody3D
## NPC ship controller — GATE.S16.NPC_ALIVE.SHIP_SCENE.001 + FLIGHT_CTRL.001
## Sim-driven movement: reads transit facts from SimBridge, interpolates position.
## Kinematic flight via move_and_slide + quaternion slerp rotation. XZ locked.

## Fleet ID assigned at spawn by GalaxyView.
@export var fleet_id: String = ""

## Cached transit data from SimBridge.
var _role: int = 0  # 0=Trader, 1=Hauler, 2=Patrol
var _hull_hp: int = 0
var _hull_hp_max: int = 0
var _is_hostile: bool = false
var _travel_progress: float = 0.0
var _fleet_state: String = "Idle"

## Flight controller state.
var target_position: Vector3 = Vector3.ZERO
var target_speed: float = 6.0
var stagger_remaining: float = 0.0

## Rotation smoothing (higher = snappier turn).
@export var rotation_sharpness: float = 5.0

## Stop moving when closer than this to target.
const ARRIVAL_THRESHOLD: float = 1.5

## Visual node references.
@onready var _ship_visual: Node3D = $ShipVisual
@onready var _fleet_area: Area3D = $FleetArea

## Model loaded flag.
var _model_loaded: bool = false

## GATE.S16.NPC_ALIVE.STATUS_DISPLAY.001: Status overlay nodes.
var _role_label: Label3D = null
var _hp_bar: MeshInstance3D = null
var _hp_bar_mat: StandardMaterial3D = null
const ROLE_LETTERS := ["T", "H", "P"]  # Trader, Hauler, Patrol
const LABEL_SHOW_DIST := 40.0
const HP_BAR_HEIGHT := 3.5  # Above ship center


func _ready() -> void:
	_fleet_area.set_meta("fleet_id", fleet_id)
	add_to_group("NpcShip")
	collision_layer = 4
	collision_mask = 0
	_create_status_display()


## GATE.S16.NPC_ALIVE.STATUS_DISPLAY.001: Create role label + HP bar overlay.
func _create_status_display() -> void:
	# Role label (T/H/P) — billboard facing camera.
	_role_label = Label3D.new()
	_role_label.name = "RoleLabel"
	_role_label.pixel_size = 0.02
	_role_label.font_size = 32
	_role_label.outline_size = 8
	_role_label.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	_role_label.position = Vector3(0, HP_BAR_HEIGHT + 0.5, 0)
	_role_label.modulate = Color(0.8, 0.8, 0.8)
	_role_label.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	add_child(_role_label)

	# HP bar — thin box mesh scaled by HP ratio.
	_hp_bar = MeshInstance3D.new()
	_hp_bar.name = "HpBar"
	var box := BoxMesh.new()
	box.size = Vector3(2.0, 0.15, 0.05)
	_hp_bar.mesh = box
	_hp_bar_mat = StandardMaterial3D.new()
	_hp_bar_mat.albedo_color = Color(0.2, 0.8, 0.2)
	_hp_bar_mat.emission_enabled = true
	_hp_bar_mat.emission = Color(0.2, 0.8, 0.2)
	_hp_bar_mat.emission_energy_multiplier = 1.5
	_hp_bar_mat.billboard_mode = BaseMaterial3D.BILLBOARD_ENABLED
	_hp_bar.material_override = _hp_bar_mat
	_hp_bar.position = Vector3(0, HP_BAR_HEIGHT, 0)
	_hp_bar.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	_hp_bar.visible = false  # Only show when damaged
	add_child(_hp_bar)

	_update_status_display()


func _update_status_display() -> void:
	if _role_label:
		var letter: String = ROLE_LETTERS[clampi(_role, 0, 2)]
		if _is_hostile:
			_role_label.text = letter + " [!]"
			_role_label.modulate = Color(1.0, 0.3, 0.3)
		else:
			_role_label.text = letter
			_role_label.modulate = Color(0.8, 0.9, 1.0)

	if _hp_bar and _hull_hp_max > 0:
		var ratio := clampf(float(_hull_hp) / float(_hull_hp_max), 0.0, 1.0)
		_hp_bar.scale = Vector3(ratio, 1.0, 1.0)
		# Color: green > yellow > red
		if ratio > 0.5:
			_hp_bar_mat.albedo_color = Color(0.2, 0.8, 0.2)
			_hp_bar_mat.emission = Color(0.2, 0.8, 0.2)
		elif ratio > 0.25:
			_hp_bar_mat.albedo_color = Color(0.9, 0.8, 0.1)
			_hp_bar_mat.emission = Color(0.9, 0.8, 0.1)
		else:
			_hp_bar_mat.albedo_color = Color(0.9, 0.2, 0.1)
			_hp_bar_mat.emission = Color(0.9, 0.2, 0.1)
		_hp_bar.visible = ratio < 1.0  # Only show when damaged

	# Distance-based visibility.
	var players := get_tree().get_nodes_in_group("Player") if get_tree() else []
	var show_label := false
	for p in players:
		if p is Node3D and global_position.distance_to(p.global_position) < LABEL_SHOW_DIST:
			show_label = true
			break
	if _role_label:
		_role_label.visible = show_label


## GATE.S16.NPC_ALIVE.BT_ROLES.001: Attach behavior tree for this ship's role.
## Called by spawn system after role is known.
## Guarded: requires LimboAI addon. Without it, NPC uses sim-driven kinematic flight.
func attach_behavior_tree(role: int) -> void:
	_role = role
	if not ClassDB.class_exists(&"BTPlayer"):
		return
	var bt_player = ClassDB.instantiate(&"BTPlayer")
	if bt_player == null:
		return
	bt_player.name = "BTPlayer"
	var builder_script = load("res://scripts/npc/npc_bt_builder.gd")
	if builder_script:
		bt_player.set("behavior_tree", builder_script.call("build_for_role", role))
	var bb = bt_player.get("blackboard")
	if bb:
		bb.call("set_var", "fleet_id", fleet_id)
		bb.call("set_var", "move_speed", target_speed)
		bb.call("set_var", "target_pos", global_position)
	add_child(bt_player)


## Called by the spawn system to load the correct Quaternius model.
## May be called before _ready() — resolves ShipVisual lazily.
func load_model(model_scene: PackedScene) -> void:
	if model_scene == null:
		return
	if _ship_visual == null:
		_ship_visual = get_node_or_null("ShipVisual")
	if _ship_visual == null:
		return
	for child in _ship_visual.get_children():
		child.queue_free()
	var instance := model_scene.instantiate()
	instance.name = "FleetModel"
	_ship_visual.add_child(instance)
	_model_loaded = true


## Called by the spawn system each poll to update sim-driven state.
func update_transit(data: Dictionary) -> void:
	_role = data.get("role", 0)
	_hull_hp = data.get("hull_hp", 0)
	_hull_hp_max = data.get("hull_hp_max", 0)
	_is_hostile = data.get("is_hostile", false)
	_travel_progress = data.get("travel_progress", 0.0)
	_fleet_state = data.get("state", "Idle")
	_update_status_display()


func _physics_process(delta: float) -> void:
	# Combat stagger — freeze movement.
	if stagger_remaining > 0.0:
		stagger_remaining -= delta
		velocity = Vector3.ZERO
		move_and_slide()
		return

	# Direction to target (XZ only).
	var to_target := target_position - global_position
	to_target.y = 0.0
	var dist := to_target.length()

	if dist < ARRIVAL_THRESHOLD:
		velocity = Vector3.ZERO
		move_and_slide()
		return

	var dir := to_target / dist

	# Smooth rotation toward movement direction (ship forward = -Z).
	if dir.length_squared() > 0.001:
		var target_basis := Basis.looking_at(-dir, Vector3.UP)
		var current_quat := global_transform.basis.get_rotation_quaternion()
		var target_quat := target_basis.get_rotation_quaternion()
		var weight := clampf(rotation_sharpness * delta, 0.0, 1.0)
		global_transform.basis = Basis(current_quat.slerp(target_quat, weight))

	velocity = dir * target_speed
	velocity.y = 0.0
	move_and_slide()
	global_position.y = 0.0


## Apply combat stagger (seconds of movement freeze).
func apply_stagger(duration: float) -> void:
	stagger_remaining += duration


## Set movement target from world position.
func set_target(pos: Vector3, spd: float) -> void:
	target_position = Vector3(pos.x, 0.0, pos.z)
	target_speed = spd


## GATE.S16.NPC_ALIVE.COMBAT_BRIDGE.001: Called when this ship takes a hit.
## Applies stagger and routes damage through SimBridge command queue.
func on_hit(damage: int) -> void:
	apply_stagger(0.3)
	var bridge := get_node_or_null("/root/SimBridge")
	if bridge and bridge.has_method("DamageNpcFleetV0") and not fleet_id.is_empty():
		var result: Dictionary = bridge.call("DamageNpcFleetV0", fleet_id, damage)
		if result.get("destroyed", false):
			queue_free()
