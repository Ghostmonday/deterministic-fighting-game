extends RefCounted
class_name CharacterDef

var character_id: int = 0
var name: String = ""
var archetype: int = 0
var hitbox_width: int = 100
var hitbox_height: int = 200
var walk_speed: int = 800
var run_speed: int = 1200
var jump_force: int = 1500
var gravity: int = 45
var max_fall_speed: int = 2200
var base_health: int = 100

static func get_default(archetype_id: int) -> CharacterDef:
	var def = CharacterDef.new()
	def.archetype = archetype_id
	
	match archetype_id:
		0:  # Ronin
			def.name = "Ronin"
			def.hitbox_width = 100
			def.hitbox_height = 200
			def.walk_speed = 800
			def.jump_force = 1500
			def.base_health = 100
		1:  # Knight
			def.name = "Knight"
			def.hitbox_width = 110
			def.hitbox_height = 210
			def.walk_speed = 700
			def.jump_force = 1400
			def.base_health = 110
		2:  # Guardian
			def.name = "Guardian"
			def.hitbox_width = 140
			def.hitbox_height = 220
			def.walk_speed = 500
			def.jump_force = 1100
			def.base_health = 130
		3:  # Titan
			def.name = "Titan"
			def.hitbox_width = 150
			def.hitbox_height = 250
			def.walk_speed = 450
			def.jump_force = 1000
			def.base_health = 150
		4:  # Ninja
			def.name = "Ninja"
			def.hitbox_width = 80
			def.hitbox_height = 180
			def.walk_speed = 1100
			def.jump_force = 1700
			def.base_health = 85
		5:  # Doctor
			def.name = "Plague Doctor"
			def.hitbox_width = 90
			def.hitbox_height = 190
			def.walk_speed = 900
			def.jump_force = 1500
			def.base_health = 95
		6:  # Dancer
			def.name = "Storm Dancer"
			def.hitbox_width = 90
			def.hitbox_height = 190
			def.walk_speed = 1000
			def.jump_force = 1800
			def.base_health = 90
		7:  # Gunslinger
			def.name = "Gunslinger"
			def.hitbox_width = 100
			def.hitbox_height = 200
			def.walk_speed = 950
			def.jump_force = 1500
			def.base_health = 100
		8:  # Mystic
			def.name = "Mystic"
			def.hitbox_width = 95
			def.hitbox_height = 195
			def.walk_speed = 900
			def.jump_force = 1600
			def.base_health = 95
		9:  # Reaper
			def.name = "Reaper"
			def.hitbox_width = 110
			def.hitbox_height = 210
			def.walk_speed = 850
			def.jump_force = 1450
			def.base_health = 105
	
	return def
