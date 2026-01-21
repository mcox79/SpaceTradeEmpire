extends Area3D
class_name GameStation

func _ready():
    # Ensure this area scans for physics bodies
    monitoring = true
    monitorable = true
    body_entered.connect(_on_body_entered)
    print("STATION ONLINE: Scanning for interactions...")

func _on_body_entered(body):
    # CRITICAL DEBUG: Print exactly what hit us.
    print("!!! CONTACT DETECTED !!! Object Name: " + body.name)
    print("--- DOCKING REQUEST ACCEPTED ---")
