/* ================================================================================
   NEURAL DRAFT LLC | DEEPSEEK INSTRUCTION HEADER
================================================================================
   FILE:    SimRunner.cs
   CONTEXT: Headless simulation harness for deterministic testing.

   TASK:
   Create a console application that:
   1. Initializes GameState and MapData
   2. Spawns 2 characters (Titan vs Ninja)
   3. Simulates 10,000 frames with randomized inputs
   4. Verifies determinism by comparing state hashes across runs

   CONSTRAINTS:
   - Must be deterministic with same random seed
   - No Unity dependencies
   - Must run without crashing
   - Final state hash must be identical across runs
================================================================================

*/
using System;

namespace NeuralDraft.SimRunner
{
    class Program
    {
        private const int TOTAL_FRAMES = 10000;
        private const int RANDOM_SEED = 123456789;

        static void Main(string[] args)
        {
            Console.WriteLine("================================================");
            Console.WriteLine("NEURAL DRAFT - Deterministic Simulation Runner");
            Console.WriteLine("================================================");
            Console.WriteLine("Running " + TOTAL_FRAMES + " frames with seed " + RANDOM_SEED);
            Console.WriteLine();

            try
            {
                // Run simulation twice with same seed
                uint hash1 = RunSimulation(RANDOM_SEED);
                uint hash2 = RunSimulation(RANDOM_SEED);

                Console.WriteLine();
                Console.WriteLine("================================================");
                Console.WriteLine("RESULTS:");
                Console.WriteLine("  Run 1 Final Hash: 0x" + hash1.ToString("X8"));
                Console.WriteLine("  Run 2 Final Hash: 0x" + hash2.ToString("X8"));
                Console.WriteLine("  Hashes Match: " + (hash1 == hash2));
                Console.WriteLine("================================================");

                if (hash1 == hash2)
                {
                    Console.WriteLine("✅ SUCCESS: Determinism verified!");
                    Environment.Exit(0);
                }
                else
                {
                    Console.WriteLine("❌ FAILURE: Determinism violation detected!");
                    Environment.Exit(1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ CRASH: Simulation failed with exception:");
                Console.WriteLine(ex.ToString());
                Environment.Exit(1);
            }
        }

        private static uint RunSimulation(int seed)
        {
            // Initialize random number generator with deterministic seed
            Random random = new Random(seed);

            // Create map data (simple flat stage)
            MapData map = CreateTestMap();

            // Create character definitions (Titan vs Ninja)
            CharacterDef[] characterDefs = new CharacterDef[GameState.MAX_PLAYERS];
            characterDefs[0] = CharacterDef.CreateTitan();
            characterDefs[1] = CharacterDef.CreateNinja();

            // Initialize game state
            GameState state = new GameState();

            // Position players on opposite sides
            state.players[0].posX = -2000 * Fx.SCALE / 1000;  // Left side
            state.players[0].posY = 1000 * Fx.SCALE / 1000;   // Above ground
            state.players[0].health = characterDefs[0].baseHealth;
            state.players[0].facing = Facing.RIGHT;

            state.players[1].posX = 2000 * Fx.SCALE / 1000;   // Right side
            state.players[1].posY = 1000 * Fx.SCALE / 1000;   // Above ground
            state.players[1].health = characterDefs[1].baseHealth;
            state.players[1].facing = Facing.LEFT;

            // Create rollback controller (development mode for full hashing)
            RollbackController controller = new RollbackController(map, characterDefs, isDevelopment: true);

            Console.WriteLine("Starting simulation with " + TOTAL_FRAMES + " frames...");

            for (int frame = 0; frame < TOTAL_FRAMES; frame++)
            {
                // Generate deterministic random inputs
                ushort p0Inputs = GenerateRandomInputs(random, frame, 0);
                ushort p1Inputs = GenerateRandomInputs(random, frame, 1);

                // Create input frame
                InputFrame inputs = new InputFrame(frame, p0Inputs, p1Inputs);

                // Save inputs and simulate
                controller.SaveInputs(frame, inputs);
                controller.TickPrediction(p0Inputs, p1Inputs);

                // Progress reporting
                if (frame % 1000 == 0)
                {
                    Console.WriteLine("  Frame " + frame + "/" + TOTAL_FRAMES + " - Player 0 Health: " + state.players[0].health + ", Player 1 Health: " + state.players[1].health);
                }

                // Get updated state
                state = controller.GetState(frame);
            }

            Console.WriteLine("Simulation completed. Final frame: " + state.frameIndex);

            // Compute final state hash
            uint finalHash = StateHash.Compute(state);
            return finalHash;
        }

        private static MapData CreateTestMap()
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

        private static ushort GenerateRandomInputs(Random random, int frame, int playerIndex)
        {
            ushort inputs = 0;

            // Deterministic input generation based on frame and player
            int randomValue = random.Next(0, 100);

            // Movement inputs (mutually exclusive)
            if (randomValue < 20)
                inputs |= InputBits.LEFT;
            else if (randomValue < 40)
                inputs |= InputBits.RIGHT;

            // Jump input (less frequent)
            if (random.Next(0, 100) < 10)
                inputs |= InputBits.JUMP;

            // Attack input (frequency depends on frame)
            if (frame % 30 == playerIndex * 15)  // Different timing for each player
                inputs |= InputBits.ATTACK;

            // Special input (rare)
            if (random.Next(0, 100) < 5)
                inputs |= InputBits.SPECIAL;

            // Defend input (when not attacking)
            if ((inputs & InputBits.ATTACK) == 0 && random.Next(0, 100) < 15)
                inputs |= InputBits.DEFEND;

            return inputs;
        }
    }
}
