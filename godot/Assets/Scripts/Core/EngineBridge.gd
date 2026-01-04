extends Node
class_name EngineBridge

signal frame_advanced(frame: int)
signal match_ended(winner: int)

var _engine: FightingEngine
var _current_frame: int = 0
var _is_running: bool = false

@onready var player1: FighterController = $Player1
@onready var player2: FighterController = $Player2
@onready var camera: Camera2D = $Camera2D
@onready var ui: CanvasLayer = $CanvasLayer

func _ready():
	_setup_engine()

func _setup_engine():
	_engine = FightingEngine.new()
	_engine.setup_character(0, 0)  # Ronin
	_engine.setup_character(1, 1)  # Knight
	reset_match()
	print("[EngineBridge] Godot engine initialized")

func reset_match():
	_current_frame = 0
	_engine.reset_state()
	
	if player1:
		player1.setup(0, _engine.get_character_def(0))
		player1.position = Vector2(-200, 0)
	
	if player2:
		player2.setup(1, _engine.get_character_def(1))
		player2.position = Vector2(200, 0)
	
	_is_running = true
	
	if ui and ui.has_method("reset_match"):
		ui.reset_match()

func _physics_process(_delta):
	if not _is_running:
		return
	
	var p1_input = player1.get_input() if player1 else 0
	var p2_input = player2.get_input() if player2 else 0
	
	_engine.tick(p1_input, p2_input)
	_current_frame += 1
	_sync_to_godot()
	
	if _engine.is_match_over():
		_end_match()
	
	frame_advanced.emit(_current_frame)
	
	if ui:
		ui.update_hud(_engine.get_health(0), _engine.get_health(1), _current_frame)

func _sync_to_godot():
	if player1:
		var pos = _engine.get_position(0)
		player1.position = Vector2(pos.x / 1000.0, pos.y / 1000.0)
		player1.set_facing(_engine.get_facing(0))
	
	if player2:
		var pos = _engine.get_position(1)
		player2.position = Vector2(pos.x / 1000.0, pos.y / 1000.0)
		player2.set_facing(_engine.get_facing(1))
	
	if camera and player1 and player2:
		var mid_point = (player1.position + player2.position) / 2.0
		camera.position = camera.position.lerp(mid_point + Vector2(0, 100), 0.1)

func _end_match():
	_is_running = false
	var winner = _engine.get_winner()
	match_ended.emit(winner)
	if ui and ui.has_method("show_match_end"):
		ui.show_match_end(winner)
	print("[EngineBridge] Match ended. Winner: P%d" % (winner + 1))
