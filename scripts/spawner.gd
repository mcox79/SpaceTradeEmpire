extends Node3D

@export var asteroid_scene: PackedScene
@export var field_radius: float = 100.0
@export var asteroid_count: int = 30

func _ready():
    if not asteroid_scene:
        print("ERROR: No Asteroid Scene assigned to Spawner!")
        return
        
    print("[SYSTEM] GENERATING ASTEROID FIELD...")
    _generate_field()

func _generate_field():
    var rng = RandomNumberGenerator.new()
    rng.randomize()
    
    for i in range(asteroid_count):
        var rock = asteroid_scene.instantiate()
        
        # Random Position within Radius
        var x = rng.randf_range(-field_radius, field_radius)
        var z = rng.randf_range(-field_radius, field_radius)
        
        # Keep the center clear (Station Zone)
        if abs(x) < 20 and abs(z) < 20:
            x += 30 # Push it out
        
        rock.position = Vector3(x, 0, z)
        
        # Random Rotation
        rock.rotation.x = rng.randf_range(0, 6.28)
        rock.rotation.y = rng.randf_range(0, 6.28)
        
        add_child(rock)
        
    print("[SYSTEM] FIELD DEPLOYED: %s Objects." % asteroid_count)
