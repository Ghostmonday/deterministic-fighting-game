extends CharacterBody2D
class_name FighterController

signal hit_received(damage: int)

@export var player_index: int = 0
var _character_def: Dictionary = {}
var _input_bits: int = 0
var _facing: int = 1
var _is_attacking: bool = false
var _health: int = 100

@onready var sprite: Sprite2D = $Sprite2D
@onready var hurtbox_visual: Polygon2D = $HurtboxVisual
@onready var hitbox_visual: Polygon2D = $HitboxVisual

func _ready():
	_setup_visuals()

func setup(index: int, def: Dictionary):
	player_index = index
	_character_def = def
	_health = def.get("base_health", 100)
	position = Vector2(-200 if index == 0 else 200, 0)
	_facing = 1 if index == 0 else -1
	
	if sprite:
		sprite.modulate = Color.CYAN if index == 0 else Color.ORANGE_RED

func _setup_visuals():
	if not sprite:
		sprite = Sprite2D.new()
		sprite.name = "Sprite2D"
		add_child(sprite)
		var placeholder = PlaceholderTexture2D.new()
		placeholder.size = Vector2(64, 128)
		sprite.texture = placeholder
	
	if not hurtbox_visual:
		hurtbox_visual = Polygon2D.new()
		hurtbox_visual.name = "HurtboxVisual"
		hurtbox_visual.color = Color(0, 1, 0, 0.3)
		add_child(hurtbox_visual)
	
	if not hitbox_visual:
		hitbox_visual = Polygon2D.new()
		hitbox_visual.name = "HitboxVisual"
		hitbox_visual.color = Color(1, 0, 0, 0.5)
		hitbox_visual.visible = false
		add_child(hitbox_visual)

func _physics_process(_delta):
	_collect_input()
	_update_visuals()

func _collect_input():
	_input_bits = 0
	
	if player_index == 0:
		if Input.is_action_pressed("move_left"): _input_bits |= 4
		if Input.is_action_pressed("move_right"): _input_bits |= 8
		if Input.is_key_pressed(KEY_W): _input_bits |= 1
		if Input.is_key_pressed(KEY_S): _input_bits |= 2
		if Input.is_key_pressed(KEY_J): _input_bits |= 32
		if Input.is_key_pressed(KEY_K): _input_bits |= 64
		if Input.is_key_pressed(KEY_L): _input_bits |= 128
		if Input.is_key_pressed(KEY_SPACE): _input_bits |= 16
	else:
		if Input.is_key_pressed(KEY_LEFT): _input_bits |= 4
		if Input.is_key_pressed(KEY_RIGHT): _input_bits |= 8
		if Input.is_key_pressed(KEY_UP): _input_bits |= 1
		if Input.is_key_pressed(KEY_DOWN): _input_bits |= 2
		if Input.is_key_pressed(KEY_KP_1): _input_bits |= 32
		if Input.is_key_pressed(KEY_KP_2): _input_bits |= 64
		if Input.is_key_pressed(KEY_KP_3): _input_bits |= 128
		if Input.is_key_pressed(KEY_KP_0): _input_bits |= 16

func get_input() -> int:
	return _input_bits

func set_facing(f: int):
	_facing = f
	scale.x = f

func _update_visuals():
	var w = _character_def.get("hitbox_width", 100) / 1000.0
	var h = _character_def.get("hitbox_height", 200) / 1000.0
	
	if hurtbox_visual:
		var points = PackedVector2Array([
			Vector2(-w/2, 0), Vector2(w/2, 0),
			Vector2(w/2, h), Vector2(-w/2, h)
		])
		hurtbox_visual.polygon = points
	
	if hitbox_visual and _is_attacking:
		var hw = 100.0 / 1000.0
		var hh = 80.0 / 1000.0
		var points = PackedVector2Array([
			Vector2(-hw/2, 0), Vector2(hw/2, 0),
			Vector2(hw/2, hh), Vector2(-hw/2, hh)
		])
		hitbox_visual.polygon = points
