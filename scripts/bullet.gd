extends Area3D

var speed = 50.0
var damage = 1
var lifetime = 2.0

func _ready():
    # FIX 2: PHYSICS LAYERS
    # We force the bullet to scan Layer 1 (World) and Layer 2 (Asteroids/Enemies).
    collision_mask = 1 | 2 
    
    body_entered.connect(_on_body_entered)
    
    # Self-destruct
    await get_tree().create_timer(lifetime).timeout
    queue_free()

func _physics_process(delta):
    # Move "Forward" (Negative Z)
    position -= transform.basis.z * speed * delta

func _on_body_entered(body):
    print("BULLET HIT: " + body.name)
    
    if body.has_method("take_damage"):
        body.take_damage(damage)
        queue_free() # Destroy bullet
    elif body.name != "Player":
        # Destroy bullet if it hits something that isn't the player (like a wall)
        queue_free()
