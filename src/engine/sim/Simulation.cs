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
            // 3a. Update Action State (Animation progress)
            UpdateActions(ref s, defs);

            // 3b. Resolve Hits
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

                    // Only allow movement if not in an action or action is cancelable
                    // For now, we assume simple state: if in action, no movement control (unless action allows it)
                    // This is a simplification. Real fighting games allow some control.
                    if (s.players[i].currentActionHash == 0)
                    {
                        PhysicsSystem.ApplyMovementInput(ref s.players[i], defs[i], inputX, jumpPressed, grounded);
                    }

                    // Apply combat inputs (Attack > Special > Defend priority)
                    if (s.players[i].currentActionHash == 0)
                    {
                        ActionDef newAction = null;

                        if (attackPressed)
                        {
                            newAction = ActionLibrary.GetAction(defs[i].archetype, InputBits.ATTACK);
                        }
                        else if (specialPressed)
                        {
                            newAction = ActionLibrary.GetAction(defs[i].archetype, InputBits.SPECIAL);
                        }
                        else if (defendPressed)
                        {
                             newAction = ActionLibrary.GetAction(defs[i].archetype, InputBits.DEFEND);
                        }

                        if (newAction != null)
                        {
                            s.players[i].currentActionHash = newAction.actionId;
                            s.players[i].actionFrameIndex = 0;
                            // Reset velocity if starting an attack on ground usually?
                            // Keeping momentum for now unless action specifies otherwise (via frame data)
                        }
                    }
                }
            }
        }

        private static void UpdateActions(ref GameState s, CharacterDef[] defs)
        {
            for (int i = 0; i < GameState.MAX_PLAYERS; i++)
            {
                if (s.players[i].currentActionHash != 0)
                {
                    var action = ActionLibrary.GetActionByHash(defs[i].archetype, s.players[i].currentActionHash);
                    if (action != null)
                    {
                        // Apply frame data (velocity)
                        if (s.players[i].actionFrameIndex < action.frames.Length)
                        {
                            var frame = action.frames[s.players[i].actionFrameIndex];

                            // Apply velocity override if non-zero (simplified)
                            // In real engine, we might add to velocity or set it.
                            if (frame.velX != 0)
                            {
                                int dir = s.players[i].facing == Facing.RIGHT ? 1 : -1;
                                s.players[i].velX = frame.velX * dir;
                            }
                            else
                            {
                                // Apply friction if no velocity override
                                PhysicsSystem.ApplyFriction(ref s.players[i], defs[i], s.players[i].grounded > 0);
                            }

                            if (frame.velY != 0) s.players[i].velY = frame.velY;
                        }

                        // Advance frame
                        s.players[i].actionFrameIndex++;

                        // Check completion
                        if (s.players[i].actionFrameIndex >= action.totalFrames)
                        {
                            s.players[i].currentActionHash = 0;
                            s.players[i].actionFrameIndex = 0;
                        }
                    }
                    else
                    {
                        // Action not found (should not happen), reset
                        s.players[i].currentActionHash = 0;
                        s.players[i].actionFrameIndex = 0;
                    }
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

        // ThreadStatic buffers to avoid allocations per frame while remaining thread-safe
        [System.ThreadStatic]
        private static CombatResolver.Hitbox[] _hitboxBuffer;
        [System.ThreadStatic]
        private static int[] _attackerXBuffer;
        [System.ThreadStatic]
        private static int[] _attackerYBuffer;
        [System.ThreadStatic]
        private static CombatResolver.Hurtbox[] _hurtboxBuffer;
        [System.ThreadStatic]
        private static CombatResolver.HitResult[] _resultsBuffer;

        private static void EnsureBuffers()
        {
            if (_hitboxBuffer == null)
            {
                _hitboxBuffer = new CombatResolver.Hitbox[GameState.MAX_PLAYERS * 4];
                _attackerXBuffer = new int[GameState.MAX_PLAYERS * 4];
                _attackerYBuffer = new int[GameState.MAX_PLAYERS * 4];
                _hurtboxBuffer = new CombatResolver.Hurtbox[GameState.MAX_PLAYERS];
                _resultsBuffer = new CombatResolver.HitResult[GameState.MAX_PLAYERS * 4];
            }
        }

        /// <summary>
        /// Resolve combat using Hitboxes and Hurtboxes from ActionDefs.
        /// </summary>
        private static void ResolveCombat(ref GameState s, CharacterDef[] defs)
        {
            EnsureBuffers();

            // 1. Collect Active Hitboxes
            int hitboxCount = 0;

            for (int i = 0; i < GameState.MAX_PLAYERS; i++)
            {
                if (s.players[i].health <= 0) continue;

                if (s.players[i].currentActionHash != 0)
                {
                    var action = ActionLibrary.GetActionByHash(defs[i].archetype, s.players[i].currentActionHash);
                    if (action != null && action.hitboxEvents != null)
                    {
                        foreach (var hbEvent in action.hitboxEvents)
                        {
                            if (s.players[i].actionFrameIndex >= hbEvent.startFrame &&
                                s.players[i].actionFrameIndex <= hbEvent.endFrame)
                            {
                                // Check buffer overflow protection
                                if (hitboxCount >= _hitboxBuffer.Length) break;

                                // Generate Hitbox
                                int dir = s.players[i].facing == Facing.RIGHT ? 1 : -1;
                                int boxCenterX = s.players[i].posX + (hbEvent.offsetX * dir);
                                int boxCenterY = s.players[i].posY + hbEvent.offsetY;
                                int boxHalfWidth = hbEvent.width / 2;

                                var bounds = new AABB
                                {
                                    minX = boxCenterX - boxHalfWidth,
                                    maxX = boxCenterX + boxHalfWidth,
                                    minY = boxCenterY,
                                    maxY = boxCenterY + hbEvent.height
                                };

                                _hitboxBuffer[hitboxCount] = new CombatResolver.Hitbox
                                {
                                    bounds = bounds,
                                    damage = hbEvent.damage,
                                    baseKnockback = hbEvent.baseKnockback,
                                    knockbackGrowth = hbEvent.knockbackGrowth,
                                    hitstun = hbEvent.hitstun,
                                    disjoint = hbEvent.disjoint,
                                    ownerIndex = i // Set owner index for self-hit check
                                };
                                _attackerXBuffer[hitboxCount] = s.players[i].posX;
                                _attackerYBuffer[hitboxCount] = s.players[i].posY;
                                hitboxCount++;
                            }
                        }
                    }
                }
            }

            // 2. Collect Hurtboxes
            int hurtboxCount = 0;
            for (int i = 0; i < GameState.MAX_PLAYERS; i++)
            {
                if (s.players[i].health <= 0) continue;
                if (hurtboxCount >= _hurtboxBuffer.Length) break;

                int halfWidth = defs[i].hitboxWidth / 2;
                _hurtboxBuffer[hurtboxCount] = new CombatResolver.Hurtbox
                {
                    bounds = new AABB
                    {
                        minX = s.players[i].posX - halfWidth,
                        maxX = s.players[i].posX + halfWidth,
                        minY = s.players[i].posY + defs[i].hitboxOffsetY,
                        maxY = s.players[i].posY + defs[i].hitboxOffsetY + defs[i].hitboxHeight
                    },
                    weight = defs[i].weight,
                    playerIndex = i
                };
                hurtboxCount++;
            }

            // 3. Resolve (Zero Allocations)
            int resultCount = CombatResolver.ResolveCombatNonAlloc(
                _hitboxBuffer, hitboxCount,
                _hurtboxBuffer, hurtboxCount,
                _attackerXBuffer, _attackerYBuffer,
                defs,
                _resultsBuffer);

            // 4. Apply Results
            for (int i = 0; i < resultCount; i++)
            {
                var result = _resultsBuffer[i];
                if (result.hit)
                {
                    int pIdx = result.hitPlayerIndex;
                    s.players[pIdx].health = (short)System.Math.Max(0, s.players[pIdx].health - result.damageDealt);
                    s.players[pIdx].velX += result.knockbackX;
                    s.players[pIdx].velY += result.knockbackY;
                    s.players[pIdx].hitstunRemaining = (short)result.hitstun;

                    // Interrupt action on hit
                    s.players[pIdx].currentActionHash = 0;
                    s.players[pIdx].actionFrameIndex = 0;
                }
            }
        }
    }
}
