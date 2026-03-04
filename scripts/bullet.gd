extends Area3D

@export var speed: float = 60.0
@export var max_lifetime_sec: float = 5.0

var _velocity: Vector3 = Vector3.ZERO
var _age: float = 0.0

# Set by spawner. Damage applied on collision via SimBridge.
var source_is_player: bool = true
var source_fleet_id: String = ""  # AI fleet ID (for AI bullets hitting player)

func _ready() -> void:
	monitoring = true
	monitorable = false

	# Default direction: forward (negative Z) in 3D
	if _velocity == Vector3.ZERO:
		_velocity = -global_transform.basis.z * speed

	body_entered.connect(_on_body_entered)
	area_entered.connect(_on_area_entered)

func set_direction(direction: Vector3) -> void:
	if direction.length() > 0.0001:
		_velocity = direction.normalized() * speed

func _physics_process(delta: float) -> void:
	_age += delta
	if _age >= max_lifetime_sec:
		queue_free()
		return

	global_position += _velocity * delta

func _on_body_entered(body: Node) -> void:
	# AI bullet hitting the player
	if not source_is_player and body.is_in_group("Player"):
		var bridge = get_node_or_null("/root/SimBridge")
		if bridge and bridge.has_method("ApplyAiShotAtPlayerV0") and not source_fleet_id.is_empty():
			bridge.call("ApplyAiShotAtPlayerV0", source_fleet_id)
	queue_free()

func _on_area_entered(area: Area3D) -> void:
	# Player bullet hitting a fleet marker
	if source_is_player and area.has_meta("fleet_id"):
		var fleet_id: String = str(area.get_meta("fleet_id"))
		if not fleet_id.is_empty():
			var bridge = get_node_or_null("/root/SimBridge")
			if bridge and bridge.has_method("ApplyTurretShotV0"):
				var result: Dictionary = bridge.call("ApplyTurretShotV0", fleet_id)
				if result.get("killed", false):
					var gm = get_node_or_null("/root/GameManager")
					if gm and gm.has_method("despawn_fleet_v0"):
						gm.call("despawn_fleet_v0", fleet_id)
	queue_free()
