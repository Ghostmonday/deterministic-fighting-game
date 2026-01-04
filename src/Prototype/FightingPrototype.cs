/* DETERMINISTIC FIGHTING GAME PROTOTYPE */
namespace NeuralDraft.Prototype {
    public class FightingPrototype {
        private GameState _currentState;
        private RollbackController _rollback;
        private MapData _stage;
        private CharacterDef[] _characterDefs;
        private List<uint> _stateHashes = new List<uint>();
        private int _totalFrames;
        private const int TEST_FRAMES = 10000;
        public FightingPrototype() {
            _stage = new MapData { width = 1000, height = 500, floorY = 0, leftWall = -500, rightWall = 500 };
            _characterDefs = new CharacterDef[2];
            _characterDefs[0] = CharacterDef.GetDefault(0); _characterDefs[1] = CharacterDef.GetDefault(1);
            _rollback = new RollbackController(_stage, _characterDefs, isDevelopment: true);
            ResetGame();
        }
        public void ResetGame() {
            _currentState = new GameState(); _stateHashes.Clear(); _totalFrames = 0;
            _currentState.players[0].posX = -200; _currentState.players[0].posY = 0;
            _currentState.players[0].facing = Facing.Right; _currentState.players[0].grounded = 1;
            _currentState.players[0].health = _characterDefs[0].baseHealth;
            _currentState.players[1].posX = 200; _currentState.players[1].posY = 0;
            _currentState.players[1].facing = Facing.Left; _currentState.players[1].grounded = 1;
            _currentState.players[1].health = _characterDefs[1].baseHealth;
            _rollback.SaveState(0); _rollback.SaveInputs(0, InputFrame.Empty(0));
            _stateHashes.Add(StateHash.Compute(ref _currentState));
        }
        public void Tick(ushort localInputs, ushort remoteInputs) {
            _totalFrames++;
            _rollback.TickPrediction(localInputs, remoteInputs);
            _currentState = _rollback.GetState(_totalFrames);
            _stateHashes.Add(StateHash.Compute(ref _currentState));
        }
        public DeterminismTestResult RunDeterminismTest() {
            var result = new DeterminismTestResult { StartedAt = DateTime.UtcNow, Passed = true, TotalFrames = TEST_FRAMES };
            uint initialHash = StateHash.Compute(ref _currentState); result.InitialHash = initialHash;
            for (int frame = 1; frame <= TEST_FRAMES; frame++) {
                ushort p0 = GenerateDeterministicInput(frame, 0); ushort p1 = GenerateDeterministicInput(frame, 1);
                Tick(p0, p1);
                if (frame % 1000 == 0) { uint h = StateHash.Compute(ref _currentState); Console.WriteLine($"Frame {frame}: Hash={h}"); }
            }
            uint finalHash = StateHash.Compute(ref _currentState); result.FinalHash = finalHash;
            result.Passed = (initialHash == finalHash); result.CompletedAt = DateTime.UtcNow; result.Duration = result.CompletedAt - result.StartedAt;
            Console.WriteLine(result.Passed ? $"PASSED! Initial={initialHash} Final={finalHash}" : $"FAILED! Initial={initialHash} Final={finalHash}");
            return result;
        }
        private ushort GenerateDeterministicInput(int frame, int playerIndex) {
            ushort inputs = 0; int cycle = frame % 120;
            if (playerIndex == 0) { if (cycle < 60) inputs |= (ushort)InputBits.RIGHT;
                if (cycle == 30 || cycle == 90) inputs |= (ushort)InputBits.ATTACK;
                if (cycle == 45) inputs |= (ushort)InputBits.JUMP; }
            else { if (cycle < 60) inputs |= (ushort)InputBits.LEFT;
                if (cycle == 30 || cycle == 90) inputs |= (ushort)InputBits.DEFEND;
                if (cycle == 45) inputs |= (ushort)InputBits.SPECIAL; }
            return inputs;
        }
        public GameState GetCurrentState() => _currentState;
        public bool IsMatchOver() => _currentState.players[0].health <= 0 || _currentState.players[1].health <= 0;
    }
    [Flags] public enum InputBits : ushort { NONE = 0, UP = 1, DOWN = 2, LEFT = 4, RIGHT = 8, JUMP = 16, ATTACK = 32, SPECIAL = 64, DEFEND = 128 }
    public enum Facing { Left = -1, Right = 1 }
    public class DeterminismTestResult { public DateTime StartedAt; public DateTime CompletedAt; public TimeSpan Duration; public bool Passed; public int TotalFrames; public uint InitialHash; public uint FinalHash; }
}
