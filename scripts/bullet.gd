extends Area3D

# --- PHYSICS CONSTANTS ---
var speed = 60.0
var damage = 1
var lifetime = 2.0
var shooter = null # IFF TAG

func _ready():
    # Layer 1 (World) + Layer 2 (Enemies/Player)
    collision_mask = 1 | 2 
    body_entered.connect(_on_body_entered)
    
    # Failsafe: Die after 2 seconds to prevent memory leaks
    await get_tree().create_timer(lifetime).timeout
    queue_free()

func _physics_process(delta):
    # LAW: Bullets ALWAYS travel Negative Z (Local Forward)
    position -= transform.basis.z * speed * delta

func _on_body_entered(body):
    # LAW: Do not hit the shooter
    if body == shooter: return

    print("IMPACT: %s -> %s" % [shooter.name if shooter else "Unknown", body.name])
    
    if body.has_method("take_damage"):
        body.take_damage(damage)
        queue_free()
    elif body.name != "Player" and body.name != "EnemyDrone":
        # Destroy on static objects (Station, Walls)
        queue_free()
