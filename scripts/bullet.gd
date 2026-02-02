extends Area3D

@export var speed: float = 60.0
@export var max_lifetime_sec: float = 5.0

var _velocity: Vector3 = Vector3.ZERO
var _age: float = 0.0

func _ready() -> void:
	monitoring = true
	monitorable = true

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

func _on_body_entered(_body: Node) -> void:
	queue_free()

func _on_area_entered(_area: Area3D) -> void:
	queue_free()
