extends CharacterBody3D

@export var speed: float = 15.0
@export var health: int = 3
@export var detection_range: float = 100.0
@export var attack_range: float = 30.0 
@export var loot_table: String = "ore_gold"

var target: Node3D = null
var bullet_scene = preload("res://scenes/bullet.tscn")
var can_shoot: bool = true
var cooldown: float = 1.5

func _ready():
	target = get_tree().get_first_node_in_group("Player")
	add_to_group("Hostile")

func _physics_process(_delta):
	if not target: return
	
	var dist = global_position.distance_to(target.global_position)
	
	if dist < detection_range:
		# POINT NEGATIVE Z AT PLAYER
		look_at(target.global_position, Vector3.UP)
		
		if dist > attack_range:
			# Move along -Z (Forward)
			velocity = -transform.basis.z * speed
		else:
			velocity = Vector3.ZERO
			_attempt_fire()
			
		move_and_slide()

func _attempt_fire():
	if can_shoot:
		can_shoot = false
		var b = bullet_scene.instantiate()
		
		# SPAWN IN FRONT (-Z)
		b.position = position - (transform.basis.z * 2.5) 
		b.rotation = rotation
		b.shooter = self
		
		get_parent().add_child(b)
		
		await get_tree().create_timer(cooldown).timeout
		can_shoot = true

func take_damage(amount: int):
	health -= amount
	print("[ENEMY] Integrity: %s" % health)
	if health <= 0: _die()

func _die():
	if target and target.has_method("add_cargo"):
		target.add_cargo(loot_table, 2)
	queue_free()
