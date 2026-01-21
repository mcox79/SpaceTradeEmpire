extends Area3D

var speed = 80.0

func _physics_process(delta):
    # FLIPPED: Changed -= to +=
    position += transform.basis.z * speed * delta

func _on_timer_timeout():
    queue_free() # Delete self
