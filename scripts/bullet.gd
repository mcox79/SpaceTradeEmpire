extends Area3D

var speed = 50.0
var damage = 1
var lifetime = 2.0

func _ready():
    collision_mask = 1 | 2 
    body_entered.connect(_on_body_entered)
    await get_tree().create_timer(lifetime).timeout
    queue_free()

func _physics_process(delta):
    # --- CALIBRATION FIX ---
    # Moving along Positive Z to match the ship's inverted mesh
    position += transform.basis.z * speed * delta

func _on_body_entered(body):
    if body.has_method("take_damage"):
        body.take_damage(damage)
        queue_free()
    elif body.name != "Player":
        queue_free()
