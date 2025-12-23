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

        // Validation state tracking (moved to instance level)
        // Note: These are now tracked by RollbackController or GameState

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
                    bool ignoreGravity = false;
                    if (s.players[i].currentActionHash != 0)
                    {
                        var action = ActionLibrary.GetAction(s.players[i].currentActionHash);
                        if (action != null)
                        {
                            ignoreGravity = action.ignoreGravity;
                        }
                    }
                    PhysicsSystem.ApplyGravity(ref s.players[i], defs[i], ignoreGravity);
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

                    // Apply movement input with root motion support
                    bool grounded = s.players[i].grounded > 0;
                    ActionFrame? rootMotion = null;

                    if (s.players[i].currentActionHash != 0)
                    {
                        var action = ActionLibrary.GetAction(s.players[i].currentActionHash);
                        if (action != null && s.players[i].actionFrameIndex < action.frames.Length)
                        {
                            rootMotion = action.frames[s.players[i].actionFrameIndex];
                        }
                    }

                    // Apply movement input (with root motion if available)
                    PhysicsSystem.ApplyMovementInput(ref s.players[i], defs[i], inputX, jumpPressed, grounded, rootMotion);

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
                    var action = ActionLibrary.GetAction(s.players[i].currentActionHash);
                    if (action != null)
                    {
                        // Increment frame index
                        s.players[i].actionFrameIndex++;

                        // Check if action is complete
                        if (s.players[i].actionFrameIndex >= action.totalFrames)
                        {
                            s.players[i].currentActionHash = 0;
                            s.players[i].actionFrameIndex = 0;
                        }
                    }
                    else
                    {
                        // Action not found, reset
                        s.players[i].currentActionHash = 0;
                        s.players[i].actionFrameIndex = 0;
                    }
                }
            }
        }

        private static void ResolveCombat(ref GameState s, CharacterDef[] defs)
        {
            // For each player, check their hitboxes against other players' hurtboxes
            for (int attackerIdx = 0; attackerIdx < GameState.MAX_PLAYERS; attackerIdx++)
            {
                if (s.players[attackerIdx].health <= 0) continue;

                // Get attacker's current action
                var attackerAction = ActionLibrary.GetAction(s.players[attackerIdx].currentActionHash);
                if (attackerAction == null) continue;

                // Check if current frame has hitbox events
                int currentFrame = s.players[attackerIdx].actionFrameIndex;
                foreach (var hitboxEvent in attackerAction.hitboxEvents)
                {
                    if (currentFrame >= hitboxEvent.startFrame && currentFrame <= hitboxEvent.endFrame)
                    {
                        // Create hitbox from event data
                        var hitbox = new CombatResolver.Hitbox
                        {
                            bounds = new AABB
                            {
                                minX = s.players[attackerIdx].posX + hitboxEvent.offsetX - hitboxEvent.width / 2,
                                maxX = s.players[attackerIdx].posX + hitboxEvent.offsetX + hitboxEvent.width / 2,
                                minY = s.players[attackerIdx].posY + hitboxEvent.offsetY - hitboxEvent.height / 2,
                                maxY = s.players[attackerIdx].posY + hitboxEvent.offsetY + hitboxEvent.height / 2
                            },
                            damage = hitboxEvent.damage,
                            baseKnockback = hitboxEvent.baseKnockback,
                            knockbackGrowth = hitboxEvent.knockbackGrowth,
                            hitstun = hitboxEvent.hitstun,
                            disjoint = hitboxEvent.disjoint
                        };

                        // Check against all other players
                        for (int defenderIdx = 0; defenderIdx < GameState.MAX_PLAYERS; defenderIdx++)
                        {
                            if (defenderIdx == attackerIdx) continue;
                            if (s.players[defenderIdx].health <= 0) continue;

                            // Create hurtbox from defender
                            var hurtbox = new CombatResolver.Hurtbox
                            {
                                bounds = new AABB
                                {
                                    minX = s.players[defenderIdx].posX - defs[defenderIdx].hitboxWidth / 2,
                                    maxX = s.players[defenderIdx].posX + defs[defenderIdx].hitboxWidth / 2,
                                    minY = s.players[defenderIdx].posY + defs[defenderIdx].hitboxOffsetY,
                                    maxY = s.players[defenderIdx].posY + defs[defenderIdx].hitboxOffsetY + defs[defenderIdx].hitboxHeight
                                },
                                weight = defs[defenderIdx].weight,
                                playerIndex = defenderIdx
                            };

                            // Resolve hit
                            var result = CombatResolver.ResolveHit(hitbox, hurtbox,
                                s.players[attackerIdx].posX, s.players[attackerIdx].posY,
                                defs[defenderIdx]);

                            if (result.hit)
                            {
                                // Apply hit result
                                s.players[defenderIdx].health -= result.damageDealt;
                                s.players[defenderIdx].velX += result.knockbackX;
                                s.players[defenderIdx].velY += result.knockbackY;

                                // Set hitstun
                                if (result.hitstun > 0)
                                {
                                    s.players[defenderIdx].currentActionHash = 0;
                                    s.players[defenderIdx].actionFrameIndex = 0;
                                    s.players[defenderIdx].hitstunFrames = result.hitstun;
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void ValidateState(ref GameState s, bool isDevelopment)
        {
            int hashFrequency = isDevelopment ? HASH_FREQUENCY_DEVELOPMENT : HASH_FREQUENCY_PRODUCTION;

            if (s.frameIndex % hashFrequency == 0 && s.frameIndex != s.lastValidatedFrame)
            {
                uint currentHash = StateHash.Compute(ref s);
                if (s.lastValidatedFrame != -1 && currentHash != s.lastValidatedHash)
                {
                    // Desync detected!
                    throw new System.InvalidOperationException(
                        $"DESYNC DETECTED at frame {s.frameIndex}! " +
                        $"Expected hash {s.lastValidatedHash}, got {currentHash}. " +
                        "This indicates non-deterministic behavior.");
                }
                s.lastValidatedHash = currentHash;
                s.lastValidatedFrame = s.frameIndex;
            }
        }
    }
}
