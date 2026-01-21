extends StaticBody3D

@export var resource_type: String = "ore_iron"
@export var resource_amount: int = 1
@export var health: int = 3

func take_damage(amount: int):
    health -= amount
    print("[TARGET] Asteroid Hit! Health: %s" % health)
    
    # Flash Effect (Optional - just creates a visual pop)
    var mesh = $MeshInstance3D
    if mesh:
        var tween = create_tween()
        tween.tween_property(mesh, "scale", Vector3(1.2, 1.2, 1.2), 0.05)
        tween.tween_property(mesh, "scale", Vector3(1.0, 1.0, 1.0), 0.05)

    if health <= 0:
        explode()

func explode():
    print("[TARGET] Asteroid Destroyed! Yield: %s x%s" % [resource_type, resource_amount])
    
    # Give Loot to Player (Simplification: Auto-collect)
    # In a full game, we would spawn floating items.
    var player = get_tree().get_first_node_in_group("Player")
    if player:
        # Check if player has space (using the API we built earlier)
        # Note: We need to ensure 'add_cargo' exists in player.gd or use direct dict access
        if player.has_method("add_cargo"):
            player.add_cargo(resource_type, resource_amount)
        else:
            # Fallback if add_cargo helper is missing
            if not player.cargo.has(resource_type): player.cargo[resource_type] = 0
            player.cargo[resource_type] += resource_amount
            # Force UI Update
            player._update_ui_state() 
            
    queue_free() # Delete the rock
