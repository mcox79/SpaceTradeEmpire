extends Area3D
class_name GameStation

# SIGNAL: Tells the UI/GameManager that we are docked
signal player_docked(station)
signal player_undocked()

func _ready():
    # Monitor for the player entering this zone
    body_entered.connect(_on_body_entered)
    body_exited.connect(_on_body_exited)

func _on_body_entered(body):
    if body.name == "Player":
        print("--- DOCKING REQUEST ACCEPTED ---")
        emit_signal("player_docked", self)

func _on_body_exited(body):
    if body.name == "Player":
        print("--- DEPARTING STATION ---")
        emit_signal("player_undocked")
