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
        private volatile int _lastFrame;
        private volatile short _p1Hp;
        private volatile short _p2Hp;
        private volatile int _stateHash; // optional if you compute it
        private const string SIGNAL_PREFIX = "http://localhost:7777/v1/signal/";

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

            // Initialize rollback controller
            rollbackController = new RollbackController();

            // Initialize map data (simplified)
            mapData = new MapData();
            mapData.KillFloorY = -2000 * Fx.SCALE / 1000; // -2000 units below origin

            // Initialize character definitions
            characterDefs = new CharacterDef[GameState.MAX_PLAYERS];
            characterDefs[0] = CharacterDef.CreateTitan();     // Player 1: Titan
            characterDefs[1] = CharacterDef.CreateNinja();     // Player 2: Ninja

            // Initialize player positions and health
            for (int i = 0; i < GameState.MAX_PLAYERS; i++)
            {
                currentState.players[i].posX = (i == 0 ? -500 : 500) * Fx.SCALE / 1000;
                currentState.players[i].posY = 1000 * Fx.SCALE / 1000; // Start above floor
                currentState.players[i].health = (short)characterDefs[i].baseHealth;
                currentState.players[i].grounded = 1;
            }

            // Initialize network (simplified - would need proper configuration)
            // udpTransport = new UdpInputTransport(7777, "127.0.0.1", 7778);
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

                    // Snapshot the latest values (volatile reads)
                    int frame = _lastFrame;
                    short p1 = _p1Hp;
                    short p2 = _p2Hp;
                    int hash = _stateHash;

                    // Deterministic mapping (simple for Phase 1)
                    // sentiment in [-1, +1] based on advantage
                    // Avoid floats if you want: send milli-units instead.
                    int diff = p1 - p2;                 // health diff
                    int sentimentMilli = Math.Clamp(diff * 5, -1000, 1000); // 5 milli per HP

                    // JSON (keep tiny)
                    string json =
                        $"{{\"symbol\":\"SDNA\",\"frame\":{frame},\"p1Hp\":{p1},\"p2Hp\":{p2},\"sentimentMilli\":{sentimentMilli},\"stateHash\":{hash}}}";

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
            byte[] localInputs = CaptureLocalInput();
            rollbackController.SaveInputs(rollbackController.GetCurrentFrame(), localInputs, localPlayerIndex);

            // Send inputs over network (in real implementation)
            // if (udpTransport != null)
            // {
            //     udpTransport.SendInputs(localInputs, rollbackController.GetCurrentFrame());
            // }

            // Check for remote inputs
            // if (udpTransport != null)
            // {
            //     byte[] remoteInputs;
            //     int frame;
            //     int playerIndex;
            //     if (udpTransport.TryReceive(out remoteInputs, out frame, out playerIndex))
            //     {
            //         rollbackController.OnRemoteInput(frame, remoteInputs, playerIndex);
            //     }
            // }

            // Run prediction
            rollbackController.TickPrediction();

            // Get current state for rendering
            rollbackController.GetState(rollbackController.GetCurrentFrame()).CopyTo(renderState);

            // Update signal endpoint snapshot
            _lastFrame = rollbackController.GetCurrentFrame();
            _p1Hp = renderState.players[0].health;
            _p2Hp = renderState.players[1].health;

            // Optional: if you have a hash function available
            // _stateHash = StateHash.Hash(renderState); // if exists in your repo
            _stateHash = 0;
        }

        byte[] CaptureLocalInput()
        {
            // Simplified input capture
            byte[] inputs = new byte[8];

            // Horizontal input
            float horizontal = Input.GetAxis("Horizontal");
            inputs[0] = (byte)(Mathf.Clamp(horizontal * 127 + 127, 0, 255));

            // Jump button
            inputs[1] = Input.GetButton("Jump") ? (byte)1 : (byte)0;

            // Attack buttons
            inputs[2] = Input.GetButton("Fire1") ? (byte)1 : (byte)0;
            inputs[3] = Input.GetButton("Fire2") ? (byte)1 : (byte)0;
            inputs[4] = Input.GetButton("Fire3") ? (byte)1 : (byte)0;

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
            try
            {
                if (_listener != null)
                {
                    _listener.Stop();
                    _listener.Close();
                    _listener = null;
                }
            }
            catch { }

            // Clean up network resources
            if (udpTransport != null)
            {
                udpTransport.Dispose();
            }
        }
    }
}
