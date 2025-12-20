/* ================================================================================
   NEURAL DRAFT LLC | DEEPSEEK INSTRUCTION HEADER
================================================================================
   FILE:    Simulation.cs
   CONTEXT: Pure, deterministic simulation loop extracted from RollbackController.

   TASK:
   Create static class Simulation with Tick method implementing strict execution order:
   1. Input Application
   2. Physics (Gravity -> Map Collision -> Friction)
   3. Combat (Hitbox/Hurtbox interaction)
   4. Entities (ProjectileSystem.StepProjectiles)
   5. State updates (frameIndex increment)

   CONSTRAINTS:
   - Use Fixed-Point Math (Fx.SCALE = 1000) for all physics.
   - No Unity Engine references in this file.
   - Strict Determinism: No floats, no random execution order.
   - Must be pure static method with no side effects.
================================================================================

*/
namespace NeuralDraft
{
    public static class Simulation
    {
        // State validation frequency
        private const int HASH_FREQUENCY_DEVELOPMENT = 1;   // Every frame in development
        private const int HASH_FREQUENCY_PRODUCTION = 10;   // Every 10 frames in production

        // Last computed hash for desync detection
        private static uint lastComputedHash = 0;
        private static int lastHashedFrame = -1;

        /// <summary>
        /// Executes one deterministic simulation tick.
        /// Strict execution order is CRITICAL for determinism.
        /// </summary>
        /// <param name="s">Game state to simulate (modified in-place)</param>
        /// <param name="inputs">Input frame for this tick</param>
        /// <param name="map">Map collision data</param>
        /// <param name="defs">Character definitions for all players</param>
        /// <param name="isDevelopment">True for development mode (more frequent hashing)</param>
        public static void Tick(ref GameState s, InputFrame inputs, MapData map, CharacterDef[] defs, bool isDevelopment = true)
        {
            // ================================================================================
            // 1. INPUT APPLICATION
            // ================================================================================
            ApplyInputs(ref s, inputs, defs);

            // ================================================================================
            // 2. PHYSICS
            // ================================================================================
            // 2a. Gravity application
            for (int i = 0; i < GameState.MAX_PLAYERS; i++)
            {
                if (s.players[i].health > 0) // Only apply to alive players
                {
                    PhysicsSystem.ApplyGravity(ref s.players[i], defs[i]);
                }
            }

            // 2b. Map collision (Resolve X then Y)
            for (int i = 0; i < GameState.MAX_PLAYERS; i++)
            {
                if (s.players[i].health > 0)
                {
                    PhysicsSystem.StepAndCollide(ref s.players[i], defs[i], map);
                }
            }

            // 2c. Friction (handled inside PhysicsSystem.ApplyMovementInput)

            // ================================================================================
            // 3. COMBAT RESOLUTION
            // ================================================================================
            ResolveCombat(ref s, defs);

            // ================================================================================
            // 4. ENTITY UPDATES
            // ================================================================================
            ProjectileSystem.UpdateAllProjectiles(s, map);

            // ================================================================================
            // 5. STATE UPDATES
            // ================================================================================
            s.frameIndex++;

            // ================================================================================
            // 6. STATE VALIDATION (Determinism Audit)
            // ================================================================================
            ValidateState(ref s, isDevelopment);
        }

        /// <summary>
        /// Apply inputs to player states.
        /// </summary>
        private static void ApplyInputs(ref GameState s, InputFrame inputs, CharacterDef[] defs)
        {
            for (int i = 0; i < GameState.MAX_PLAYERS; i++)
            {
                if (s.players[i].health > 0)
                {
                    // Extract input bits for this player
                    ushort playerInputs = inputs.GetPlayerInputs(i);

                    // Parse input bits
                    int inputX = 0;
                    if ((playerInputs & (ushort)InputBits.LEFT) != 0) inputX = -1;
                    if ((playerInputs & (ushort)InputBits.RIGHT) != 0) inputX = 1;

                    bool jumpPressed = (playerInputs & (ushort)InputBits.JUMP) != 0;
                    bool attackPressed = (playerInputs & (ushort)InputBits.ATTACK) != 0;
                    bool specialPressed = (playerInputs & (ushort)InputBits.SPECIAL) != 0;
                    bool defendPressed = (playerInputs & (ushort)InputBits.DEFEND) != 0;

                    // Apply movement input
                    bool grounded = s.players[i].grounded > 0;
                    PhysicsSystem.ApplyMovementInput(ref s.players[i], defs[i], inputX, jumpPressed, grounded);

                    // TODO: Apply combat inputs (attack, special, defend)
                    // This requires ActionDef system to be fully implemented
                }
            }
        }

        /// <summary>
        /// Validate state determinism by computing and checking hash.
        /// </summary>
        private static void ValidateState(ref GameState s, bool isDevelopment)
        {
            int hashFrequency = isDevelopment ? HASH_FREQUENCY_DEVELOPMENT : HASH_FREQUENCY_PRODUCTION;

            // Only compute hash at specified frequency
            if (s.frameIndex % hashFrequency == 0)
            {
                uint currentHash = StateHash.Compute(s);

                // Check for desync if we have a previous hash for this frame
                if (lastHashedFrame == s.frameIndex && lastComputedHash != currentHash)
                {
                    // CRITICAL DESYNC DETECTED
                    System.Console.WriteLine("CRITICAL DESYNC at frame " + s.frameIndex + ":");
                    System.Console.WriteLine("  Local Hash: " + lastComputedHash.ToString("X8"));
                    System.Console.WriteLine("  Remote Hash: " + currentHash.ToString("X8"));
                    System.Console.WriteLine("  State dump:");
                    DumpStateDifferences(s);

                    // In a real implementation, you would trigger rollback recovery here
                    throw new System.Exception("Determinism violation at frame " + s.frameIndex);
                }

                // Store hash for future comparison
                lastComputedHash = currentHash;
                lastHashedFrame = s.frameIndex;
            }
        }

        /// <summary>
        /// Dump state differences for debugging desyncs.
        /// </summary>
        private static void DumpStateDifferences(GameState state)
        {
            System.Console.WriteLine("  Frame: " + state.frameIndex);

            for (int i = 0; i < GameState.MAX_PLAYERS; i++)
            {
                var player = state.players[i];
                System.Console.WriteLine("  Player " + i + ":");
                System.Console.WriteLine("    Position: (" + player.posX + ", " + player.posY + ")");
                System.Console.WriteLine("    Velocity: (" + player.velX + ", " + player.velY + ")");
                System.Console.WriteLine("    Health: " + player.health);
                System.Console.WriteLine("    Grounded: " + player.grounded);
                System.Console.WriteLine("    Hitstun: " + player.hitstunRemaining);
            }

            System.Console.WriteLine("  Active Projectiles: " + state.activeProjectileCount);
        }

        /// <summary>
        /// Basic combat resolution for testing.
        /// In a full implementation, this would use ActionDef to generate hitboxes.
        /// </summary>
        private static void ResolveCombat(ref GameState s, CharacterDef[] defs)
        {
            // Simple test: if players are close and attacking, deal damage
            for (int i = 0; i < GameState.MAX_PLAYERS; i++)
            {
                for (int j = 0; j < GameState.MAX_PLAYERS; j++)
                {
                    if (i == j || s.players[i].health <= 0 || s.players[j].health <= 0)
                        continue;

                    // Calculate distance between players
                    int deltaX = s.players[i].posX - s.players[j].posX;
                    int deltaY = s.players[i].posY - s.players[j].posY;
                    int distanceSquared = deltaX * deltaX + deltaY * deltaY;

                    // Simple attack range check (500 units squared)
                    int attackRange = 500 * Fx.SCALE / 1000;
                    if (distanceSquared < attackRange * attackRange)
                    {
                        // Simple damage calculation
                        int damage = 10;
                        s.players[j].health = (short)System.Math.Max(0, s.players[j].health - damage);

                        // Simple knockback
                        if (deltaX != 0)
                        {
                            int knockbackDirection = deltaX > 0 ? 1 : -1;
                            s.players[j].velX += knockbackDirection * 500 * Fx.SCALE / 1000;
                        }

                        // Simple hitstun
                        s.players[j].hitstunRemaining = 10;
                    }
                }
            }
        }
    }
}
