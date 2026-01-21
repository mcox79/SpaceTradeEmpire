extends Area3D

func _on_area_entered(area):
    # Check if the thing that hit us is a Bullet
    if area.name.begins_with('Bullet'):
        print('Asteroid Destroyed!')
        area.queue_free() # Delete the bullet
        queue_free()      # Delete the asteroid
