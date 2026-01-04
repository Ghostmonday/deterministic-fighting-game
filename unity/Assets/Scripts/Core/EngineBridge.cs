using UnityEngine;
using System.Collections.Generic;

namespace NeuralDraft.Unity {
    /// <summary>
    /// Bridge between Unity and Deterministic Fighting Engine
    /// Handles the connection between visual representation and simulation
    /// </summary>
    public class EngineBridge : MonoBehaviour {
        public static EngineBridge Instance { get; private set; }
        
        [Header("Engine Settings")]
        public bool runEngine = true;
        public int targetFPS = 60;
        public bool useFixedUpdate = true;
        
        [Header("Simulation")]
        private GameState _currentState;
        private MapData _stage;
        private CharacterDef[] _characterDefs;
        private RollbackController _rollback;
        
        [Header("References")]
        public FighterController player1;
        public FighterController player2;
        public StageController stage;
        public UIManager ui;
        
        // Frame tracking
        private int _frameCount;
        private float _lastFrameTime;
        private float _frameInterval;
        
        // Input buffers
        private ushort _p1Input;
        private ushort _p2Input;
        
        void Awake() {
            Instance = this;
            _frameInterval = 1f / targetFPS;
        }
        
        void Start() {
            InitializeEngine();
            _lastFrameTime = Time.time;
        }
        
        void InitializeEngine() {
            // Create stage data
            _stage = new MapData {
                floorY = 0,
                leftWall = -500,
                rightWall = 500,
                width = 1000,
                height = 500
            };
            
            // Load character definitions
            _characterDefs = new CharacterDef[2];
            _characterDefs[0] = CharacterDef.GetDefault(0); // Ronin
            _characterDefs[1] = CharacterDef.GetDefault(1); // Knight
            
            // Initialize rollback controller
            _rollback = new RollbackController(_stage, _characterDefs, isDevelopment: true);
            
            // Initialize game state
            ResetGame();
            
            Debug.Log("[EngineBridge] Deterministic engine initialized");
        }
        
        public void ResetGame() {
            _currentState = new GameState();
            _frameCount = 0;
            
            // Sync Unity characters to engine state
            if (player1 != null) {
                player1.Initialize(0, _characterDefs[0]);
                _currentState.players[0].posX = player1.transform.position.x * Fx.SCALE;
                _currentState.players[0].posY = player1.transform.position.y * Fx.SCALE;
                _currentState.players[0].health = _characterDefs[0].baseHealth;
            }
            
            if (player2 != null) {
                player2.Initialize(1, _characterDefs[1]);
                _currentState.players[1].posX = player2.transform.position.x * Fx.SCALE;
                _currentState.players[1].posY = player2.transform.position.y * Fx.SCALE;
                _currentState.players[1].health = _characterDefs[1].baseHealth;
            }
            
            _rollback.SaveState(0);
            _rollback.SaveInputs(0, InputFrame.Empty(0));
        }
        
        void Update() {
            if (!runEngine) return;
            
            float now = Time.time;
            float delta = now - _lastFrameTime;
            
            if (delta >= _frameInterval) {
                _lastFrameTime = now - (delta % _frameInterval);
                Tick();
            }
        }
        
        void Tick() {
            _frameCount++;
            
            // Collect inputs from Unity controllers
            _p1Input = player1 != null ? player1.GetInputBits() : (ushort)0;
            _p2Input = player2 != null ? player2.GetInputBits() : (ushort)0;
            
            // Run deterministic simulation
            _rollback.TickPrediction(_p1Input, _p2Input);
            
            // Get updated state
            _currentState = _rollback.GetState(_rollback.GetCurrentFrame());
            
            // Sync Unity objects to engine state
            SyncToUnity();
            
            // Update UI
            if (ui != null) {
                ui.UpdateHUD(_currentState.players[0].health, _currentState.players[1].health, _frameCount);
            }
            
            // Check for match end
            if (_currentState.players[0].health <= 0 || _currentState.players[1].health <= 0) {
                EndMatch();
            }
        }
        
        void SyncToUnity() {
            // Sync player 1
            if (player1 != null) {
                player1.transform.position = new Vector3(
                    _currentState.players[0].posX / (float)Fx.SCALE,
                    _currentState.players[0].posY / (float)Fx.SCALE,
                    0
                );
                player1.SetFacing(_currentState.players[0].facing == Facing.RIGHT ? 1 : -1);
                player1.SetState(_currentState.players[0]);
                player1.UpdateVisuals();
            }
            
            // Sync player 2
            if (player2 != null) {
                player2.transform.position = new Vector3(
                    _currentState.players[1].posX / (float)Fx.SCALE,
                    _currentState.players[1].posY / (float)Fx.SCALE,
                    0
                );
                player2.SetFacing(_currentState.players[1].facing == Facing.RIGHT ? 1 : -1);
                player2.SetState(_currentState.players[1]);
                player2.UpdateVisuals();
            }
        }
        
        void EndMatch() {
            runEngine = false;
            
            int winner = -1;
            if (_currentState.players[0].health <= 0 && _currentState.players[1].health <= 0) {
                winner = -1; // Double KO
            } else if (_currentState.players[0].health <= 0) {
                winner = 1;
            } else if (_currentState.players[1].health <= 0) {
                winner = 0;
            }
            
            if (ui != null) {
                ui.ShowMatchEnd(winner);
            }
            
            Debug.Log($"[EngineBridge] Match ended. Winner: P{winner + 1}");
        }
        
        // Public API for external access
        public GameState GetCurrentState() => _currentState;
        public int GetFrameCount() => _frameCount;
        public ushort GetP1Input() => _p1Input;
        public ushort GetP2Input() => _p2Input;
    }
}
