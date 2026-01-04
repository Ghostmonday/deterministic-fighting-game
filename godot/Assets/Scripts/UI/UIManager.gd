extends CanvasLayer

@onready var p1_health_bar: ProgressBar = $HUD/P1HealthBar
@onready var p2_health_bar: ProgressBar = $HUD/P2HealthBar
@onready var timer_label: Label = $HUD/TimerLabel
@onready var frame_label: Label = $HUD/FrameLabel
@onready var match_end_panel: Panel = $MatchEndPanel
@onready var winner_label: Label = $MatchEndPanel/WinnerLabel
@onready var restart_button: Button = $MatchEndPanel/RestartButton

var _timer: int = 99

func _ready():
	reset_match()
	if match_end_panel:
		match_end_panel.visible = false
	if restart_button:
		restart_button.pressed.connect(_on_restart)

func reset_match():
	_timer = 99
	if p1_health_bar: p1_health_bar.value = 100
	if p2_health_bar: p2_health_bar.value = 100
	if timer_label: timer_label.text = "99"

func update_hud(p1_health: int, p2_health: int, frame: int):
	if p1_health_bar: p1_health_bar.value = max(0, p1_health)
	if p2_health_bar: p2_health_bar.value = max(0, p2_health)
	if frame_label: frame_label.text = "FRAME %d" % frame

func _process(delta):
	_timer -= delta
	if _timer < 0: _timer = 99
	if timer_label: timer_label.text = str(_timer)

func show_match_end(winner: int):
	if match_end_panel:
		match_end_panel.visible = true
	if winner_label:
		if winner == -1:
			winner_label.text = "DOUBLE KO!"
		else:
			winner_label.text = "PLAYER %d WINS!" % (winner + 1)

func _on_restart():
	if match_end_panel:
		match_end_panel.visible = false
	get_tree().reload_current_scene()
