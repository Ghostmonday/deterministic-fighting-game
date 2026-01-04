extends RefCounted
class_name FightingEngine

var _state: Dictionary = {}
var _character_defs: Array = []

func setup_character(player_id: int, archetype: int):
	var def = CharacterDef.get_default(archetype)
	_state["player_%d" % player_id] = {
		"pos_x": -200 if player_id == 0 else 200,
		"pos_y": 0,
		"vel_x": 0,
		"vel_y": 0,
		"facing": 1 if player_id == 0 else -1,
		"grounded": 1,
		"health": def.base_health,
		"action_hash": 0,
		"action_frame": 0,
		"hitstun": 0
	}
	_character_defs.append(def)

func reset_state():
	for i in range(2):
		if _state.has("player_%d" % i):
			var p = _state["player_%d" % i]
			p.pos_x = -200 if i == 0 else 200
			p.pos_y = 0
			p.vel_x = 0
			p.vel_y = 0
			p.health = _character_defs[i].base_health
			p.hitstun = 0

func tick(p1_input: int, p2_input: int):
	_apply_input(0, p1_input)
	_apply_input(1, p2_input)
	_apply_physics(0)
	_apply_physics(1)
	_resolve_collisions()
	_update_actions()
	_check_combat()
	_increment_frame()

func _apply_input(player_id: int, input_bits: int):
	var p = _state["player_%d" % player_id]
	if p.hitstun > 0:
		return
	
	var speed = _character_defs[player_id].walk_speed
	var ix = 0
	if input_bits & 4: ix = -1
	if input_bits & 8: ix = 1
	
	if ix != 0:
		p.vel_x = ix * speed
		p.facing = ix
	
	# Jump
	if input_bits & 16 and p.grounded == 1:
		p.vel_y = _character_defs[player_id].jump_force
		p.grounded = 0

func _apply_physics(player_id: int):
	var p = _state["player_%d" % player_id]
	var def = _character_defs[player_id]
	
	if p.grounded == 0:
		p.vel_y -= def.gravity
		if p.vel_y < -def.max_fall_speed:
			p.vel_y = -def.max_fall_speed
	
	p.pos_x += p.vel_x
	p.pos_y += p.vel_y
	
	# Floor collision
	if p.pos_y < 0:
		p.pos_y = 0
		p.vel_y = 0
		p.grounded = 1
	
	# Wall collision
	if p.pos_x < -500:
		p.pos_x = -500
		p.vel_x = 0
	if p.pos_x > 500:
		p.pos_x = 500
		p.vel_x = 0

func _resolve_collisions():
	pass

func _update_actions():
	for i in range(2):
		var p = _state["player_%d" % i]
		if p.hitstun > 0:
			p.hitstun -= 1
			if p.hitstun == 0:
				p.action_hash = 0
				p.action_frame = 0

func _check_combat():
	var p0 = _state["player_0"]
	var p1 = _state["player_1"]
	
	# Simple hit detection
	var dist = abs(p0.pos_x - p1.pos_x)
	if dist < 100 and p0.health > 0 and p1.health > 0:
		pass

func _increment_frame():
	pass

func get_position(player_id: int) -> Vector2:
	var p = _state["player_%d" % player_id]
	return Vector2(p.pos_x, p.pos_y)

func get_facing(player_id: int) -> int:
	var p = _state["player_%d" % player_id]
	return p.facing

func get_health(player_id: int) -> int:
	var p = _state["player_%d" % player_id]
	return p.health

func get_character_def(player_id: int):
	return _character_defs[player_id]

func is_match_over() -> bool:
	var p0 = _state["player_0"]
	var p1 = _state["player_1"]
	return p0.health <= 0 or p1.health <= 0

func get_winner() -> int:
	var p0 = _state["player_0"]
	var p1 = _state["player_1"]
	if p0.health <= 0 and p1.health <= 0:
		return -1
	elif p0.health <= 0:
		return 1
	elif p1.health <= 0:
		return 0
	return -1

func get_state_dict() -> Dictionary:
	return _state.duplicate(true)
