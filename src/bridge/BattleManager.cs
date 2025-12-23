/* ================================================================================
   NEURAL DRAFT LLC | DEEPSEEK INSTRUCTION HEADER
================================================================================
   FILE:    BattleManager.cs
   CONTEXT: Unity glue code.

   TASK:
   Write a MonoBehaviour. FixedUpdate runs Sim. Update runs Render. Capture Input. Manage 'View' objects (Transforms). Do NOT use Unity Physics.

   CONSTRAINTS:
   - Use Fixed-Point Math (Fx.SCALE = 1000) for all physics.
   - No Unity Engine references in this file (unless specified in Bridge).
   - Strict Determinism: No floats, no random execution order.
================================================================================

*/
namespace NeuralDraft
{
    using UnityEngine;
    using System;
    using System.Net;
    using System.Text;
    using System.Threading;

    public class BattleManager : MonoBehaviour
    {
        private GameState currentState;
        private GameState renderState;
        private RollbackController rollbackController;
        private MapData mapData;
        private CharacterDef[] characterDefs;

        // --- Signal endpoint (local HTTP) ---
        private HttpListener _listener;
        private Thread _listenerThread;
        private readonly object _signalLock = new object();
        private SignalSnapshot _currentSignalSnapshot;
        private const string SIGNAL_PREFIX = "http://localhost:7777/v1/signal/";

        // Signal snapshot struct for atomic updates
        private struct SignalSnapshot
        {
            public int Frame;
            public short P1Hp;
            public short P2Hp;
            public int StateHash;
        }

        // View objects
        public Transform[] playerTransforms;
        public Transform[] projectileTransforms;

        // Network
        private UdpInputTransport udpTransport;
        private int localPlayerIndex = 0;

        // Timing
        private const int FIXED_FRAME_RATE = 60;
        private float accumulatedTime = 0f;

        void Start()
        {
            InitializeGame();
            StartSignalServer();
        }

        void InitializeGame()
        {
            // Initialize game state
            currentState = new GameState();
            renderState = new GameState();

            // Initialize rollback controller with new API
            mapData = CreateTestMap();
            characterDefs = new CharacterDef[GameState.MAX_PLAYERS];
            // TEST MATCHUP: Guardian (High Friction) vs Plague Doctor (Low Friction)
            characterDefs[0] = CharacterDef.GetDefault(2);     // Earth East (Guardian)
            characterDefs[1] = CharacterDef.GetDefault(5);     // Venom West (Plague Doctor)

            rollbackController = new RollbackController(mapData, characterDefs, isDevelopment: true);

            // Initialize player positions and health
            for (int i = 0; i < GameState.MAX_PLAYERS; i++)
            {
                currentState.players[i].posX = (i == 0 ? -2000 : 2000) * Fx.SCALE / 1000;
                currentState.players[i].posY = 1000 * Fx.SCALE / 1000; // Start above floor
                currentState.players[i].health = (short)characterDefs[i].baseHealth;
                currentState.players[i].grounded = 1;
                currentState.players[i].facing = i == 0 ? Facing.RIGHT : Facing.LEFT;
            }

            // Initialize network (simplified - would need proper configuration)
            // udpTransport = new UdpInputTransport(7777, "127.0.0.1", 7778);
        }

        private MapData CreateTestMap()
        {
            // Create a simple flat stage with platforms
            AABB[] solidBlocks = new AABB[3];

            // Main ground platform
            solidBlocks[0] = new AABB
            {
                minX = -5000 * Fx.SCALE / 1000,
                maxX = 5000 * Fx.SCALE / 1000,
                minY = 0,
                maxY = 100 * Fx.SCALE / 1000  // 100 units thick
            };

            // Left platform
            solidBlocks[1] = new AABB
            {
                minX = -3000 * Fx.SCALE / 1000,
                maxX = -2000 * Fx.SCALE / 1000,
                minY = 500 * Fx.SCALE / 1000,
                maxY = 600 * Fx.SCALE / 1000
            };

            // Right platform
            solidBlocks[2] = new AABB
            {
                minX = 2000 * Fx.SCALE / 1000,
                maxX = 3000 * Fx.SCALE / 1000,
                minY = 500 * Fx.SCALE / 1000,
                maxY = 600 * Fx.SCALE / 1000
            };

            return new MapData
            {
                SolidBlocks = solidBlocks,
                KillFloorY = -10000 * Fx.SCALE / 1000  // Very low kill floor
            };
        }

        private void StartSignalServer()
        {
            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add(SIGNAL_PREFIX);
                _listener.Start();

                _listenerThread = new Thread(SignalServerLoop);
                _listenerThread.IsBackground = true;
                _listenerThread.Start();

                Debug.Log($"Signal endpoint listening: {SIGNAL_PREFIX}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to start signal server: {e}");
            }
        }

        private void SignalServerLoop()
        {
            while (_listener != null && _listener.IsListening)
            {
                try
                {
                    var ctx = _listener.GetContext();
                    var resp = ctx.Response;

                    // Get atomic snapshot (thread-safe)
                    SignalSnapshot snapshot;
                    lock (_signalLock)
                    {
                        snapshot = _currentSignalSnapshot;
                    }

                    // Deterministic mapping (simple for Phase 1)
                    // sentiment in [-1, +1] based on advantage
                    // Avoid floats if you want: send milli-units instead.
                    int diff = snapshot.P1Hp - snapshot.P2Hp;                 // health diff
                    int sentimentMilli = Math.Clamp(diff * 5, -1000, 1000); // 5 milli per HP

                    // JSON (keep tiny)
                    string json =
                        $"{{\"symbol\":\"SDNA\",\"frame\":{snapshot.Frame},\"p1Hp\":{snapshot.P1Hp},\"p2Hp\":{snapshot.P2Hp},\"sentimentMilli\":{sentimentMilli},\"stateHash\":{snapshot.StateHash}}}";

                    byte[] buf = Encoding.UTF8.GetBytes(json);
                    resp.StatusCode = 200;
                    resp.ContentType = "application/json";
                    resp.ContentLength64 = buf.Length;
                    resp.OutputStream.Write(buf, 0, buf.Length);
                    resp.OutputStream.Close();
                }
                catch
                {
                    // swallow to keep endpoint alive during development
                }
            }
        }

        void FixedUpdate()
        {
            // Fixed timestep for deterministic simulation
            accumulatedTime += Time.fixedDeltaTime;
            float targetFrameTime = 1f / FIXED_FRAME_RATE;

            while (accumulatedTime >= targetFrameTime)
            {
                accumulatedTime -= targetFrameTime;
                SimulateFrame();
            }
        }

        void SimulateFrame()
        {
            // Capture local input
            ushort p0Inputs = CaptureLocalInputs(0);
            ushort p1Inputs = CaptureLocalInputs(1);

            // Create input frame
            InputFrame inputs = new InputFrame(rollbackController.GetCurrentFrame(), p0Inputs, p1Inputs);

            // Save inputs and run prediction
            rollbackController.TickPrediction(inputs);

            // Get current state for rendering
            rollbackController.GetState(rollbackController.GetCurrentFrame()).CopyTo(ref renderState);

            // Update signal endpoint snapshot (atomic update)
            var snapshot = new SignalSnapshot
            {
                Frame = rollbackController.GetCurrentFrame(),
                P1Hp = renderState.players[0].health,
                P2Hp = renderState.players[1].health,
                StateHash = (int)StateHash.Compute(ref renderState)
            };

            lock (_signalLock)
            {
                _currentSignalSnapshot = snapshot;
            }
        }

        ushort CaptureLocalInputs(int playerIndex)
        {
            ushort inputs = 0;

            // Player 1 uses arrow keys, Player 2 uses WASD (for testing)
            string horizontalAxis = playerIndex == 0 ? "Horizontal" : "Horizontal2";
            string verticalAxis = playerIndex == 0 ? "Vertical" : "Vertical2";
            string jumpButton = playerIndex == 0 ? "Jump" : "Jump2";
            string attackButton = playerIndex == 0 ? "Fire1" : "Fire2";

            // Horizontal movement
            float horizontal = Input.GetAxis(horizontalAxis);
            if (horizontal < -0.5f) inputs = (ushort)(inputs | (ushort)InputBits.LEFT);
            if (horizontal > 0.5f) inputs = (ushort)(inputs | (ushort)InputBits.RIGHT);

            // Vertical movement
            float vertical = Input.GetAxis(verticalAxis);
            if (vertical > 0.5f) inputs = (ushort)(inputs | (ushort)InputBits.UP);
            if (vertical < -0.5f) inputs = (ushort)(inputs | (ushort)InputBits.DOWN);

            // Jump button
            if (Input.GetButton(jumpButton)) inputs = (ushort)(inputs | (ushort)InputBits.JUMP);

            // Attack button
            if (Input.GetButton(attackButton)) inputs = (ushort)(inputs | (ushort)InputBits.ATTACK);

            return inputs;
        }

        void Update()
        {
            // Render current state
            RenderGameState();
        }

        void RenderGameState()
        {
            // Update player transforms
            for (int i = 0; i < GameState.MAX_PLAYERS; i++)
            {
                if (playerTransforms != null && i < playerTransforms.Length && playerTransforms[i] != null)
                {
                    float posX = renderState.players[i].posX / (float)Fx.SCALE;
                    float posY = renderState.players[i].posY / (float)Fx.SCALE;

                    playerTransforms[i].position = new Vector3(posX, posY, 0);

                    // Set facing direction
                    if (renderState.players[i].facing == Facing.RIGHT)
                    {
                        playerTransforms[i].localScale = new Vector3(1, 1, 1);
                    }
                    else
                    {
                        playerTransforms[i].localScale = new Vector3(-1, 1, 1);
                    }
                }
            }

            // Update projectile transforms
            for (int i = 0; i < GameState.MAX_PROJECTILES; i++)
            {
                if (projectileTransforms != null && i < projectileTransforms.Length && projectileTransforms[i] != null)
                {
                    if (renderState.projectiles[i].active == 1)
                    {
                        float posX = renderState.projectiles[i].posX / (float)Fx.SCALE;
                        float posY = renderState.projectiles[i].posY / (float)Fx.SCALE;

                        projectileTransforms[i].position = new Vector3(posX, posY, 0);
                        projectileTransforms[i].gameObject.SetActive(true);
                    }
                    else
                    {
                        projectileTransforms[i].gameObject.SetActive(false);
                    }
                }
            }
        }

        void OnDestroy()
        {
            // Clean up signal server
            if (_listener != null)
            {
                try
                {
                    _listener.Stop();
                    _listener.Close();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Error stopping HTTP listener: {ex.Message}");
                }
                finally
                {
                    _listener = null;
                }
            }

            // Clean up listener thread
            if (_listenerThread != null && _listenerThread.IsAlive)
            {
                try
                {
                    // Give the thread 2 seconds to finish gracefully
                    if (!_listenerThread.Join(TimeSpan.FromSeconds(2)))
                    {
                        Debug.LogWarning("HTTP listener thread did not stop gracefully, aborting...");
                        _listenerThread.Abort(); // Last resort
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Error joining listener thread: {ex.Message}");
                }
                finally
                {
                    _listenerThread = null;
                }
            }

            // Clean up network resources
            if (udpTransport != null)
            {
                udpTransport.Dispose();
            }
        }
    }
}
