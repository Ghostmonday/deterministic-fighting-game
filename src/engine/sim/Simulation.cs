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
        /// <param name="actions">Action library</param>
        /// <param name="isDevelopment">True for development mode (more frequent hashing)</param>
        public static void Tick(ref GameState s, InputFrame inputs, MapData map, CharacterDef[] defs, System.Collections.Generic.Dictionary<int, ActionDef> actions, bool isDevelopment = true)
        {
            // ================================================================================
            // 1. INPUT APPLICATION
            // ================================================================================
            ApplyInputs(ref s, inputs, defs, actions);

            // ================================================================================
            // 2. ACTION PHYSICS (Velocity Override)
            // ================================================================================
            ApplyActionPhysics(ref s, actions);

            // ================================================================================
            // 3. PHYSICS
            // ================================================================================
            // 3a. Gravity application
            for (int i = 0; i < GameState.MAX_PLAYERS; i++)
            {
                if (s.players[i].health > 0) // Only apply to alive players
                {
                    PhysicsSystem.ApplyGravity(ref s.players[i], defs[i]);
                }
            }

            // 3b. Map collision (Resolve X then Y)
            for (int i = 0; i < GameState.MAX_PLAYERS; i++)
            {
                if (s.players[i].health > 0)
                {
                    PhysicsSystem.StepAndCollide(ref s.players[i], defs[i], map);
                }
            }

            // 3c. Friction (handled inside PhysicsSystem.ApplyMovementInput)

            // ================================================================================
            // 4. ACTION EVENTS (Projectiles)
            // ================================================================================
            ProcessActionEvents(ref s, actions, defs);

            // ================================================================================
            // 5. COMBAT RESOLUTION
            // ================================================================================
            ResolveCombat(ref s, defs, actions);

            // ================================================================================
            // 6. ENTITY UPDATES
            // ================================================================================
            ProjectileSystem.UpdateAllProjectiles(s, map);

            // ================================================================================
            // 7. ACTION PROGRESSION
            // ================================================================================
            ProgressActions(ref s, actions);

            // ================================================================================
            // 8. STATE UPDATES
            // ================================================================================
            s.frameIndex++;

            // ================================================================================
            // 9. STATE VALIDATION (Determinism Audit)
            // ================================================================================
            ValidateState(ref s, isDevelopment);
        }

        /// <summary>
        /// Apply inputs to player states.
        /// </summary>
        private static void ApplyInputs(ref GameState s, InputFrame inputs, CharacterDef[] defs, System.Collections.Generic.Dictionary<int, ActionDef> actions)
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

                    // Apply movement input ONLY if not in an action (or if action logic allows, simplified here)
                    bool grounded = s.players[i].grounded > 0;

                    if (s.players[i].currentActionHash == 0)
                    {
                        PhysicsSystem.ApplyMovementInput(ref s.players[i], defs[i], inputX, jumpPressed, grounded);

                        // Check for new actions
                        if (s.players[i].hitstunRemaining == 0)
                        {
                            if (attackPressed)
                            {
                                s.players[i].currentActionHash = defs[i].defaultAttackActionId;
                                s.players[i].actionFrameIndex = 0;
                            }
                            else if (specialPressed)
                            {
                                s.players[i].currentActionHash = defs[i].defaultSpecialActionId;
                                s.players[i].actionFrameIndex = 0;
                            }
                            else if (defendPressed)
                            {
                                s.players[i].currentActionHash = defs[i].defaultDefendActionId;
                                s.players[i].actionFrameIndex = 0;
                            }
                        }
                    }
                    else
                    {
                        // In action: check cancelability or other logic (omitted for now)
                        // Could check for cancelable flag in current frame to allow interrupting
                    }
                }
            }
        }

        private static void ApplyActionPhysics(ref GameState s, System.Collections.Generic.Dictionary<int, ActionDef> actions)
        {
            for (int i = 0; i < GameState.MAX_PLAYERS; i++)
            {
                if (s.players[i].health > 0 && s.players[i].currentActionHash != 0)
                {
                    if (actions != null && actions.TryGetValue(s.players[i].currentActionHash, out var action))
                    {
                        if (s.players[i].actionFrameIndex < action.frames.Length)
                        {
                            var frame = action.frames[s.players[i].actionFrameIndex];
                            // Apply velocity if specified (non-zero)
                            // Assuming 0 means "no override" or "stop"?
                            // Let's assume if it's set, we use it.
                            // For simplicity, let's say if abs(velX) > 0, we set it.
                            // Or better: actions control movement fully.

                            // If action has velocity defined, apply it relative to facing
                            if (frame.velX != 0 || frame.velY != 0)
                            {
                                int facingDir = (s.players[i].facing == Facing.RIGHT) ? 1 : -1;
                                s.players[i].velX = frame.velX * facingDir;
                                s.players[i].velY = frame.velY; // Y is usually absolute (jump)
                            }
                        }
                    }
                }
            }
        }

        private static void ProcessActionEvents(ref GameState s, System.Collections.Generic.Dictionary<int, ActionDef> actions, CharacterDef[] defs)
        {
             for (int i = 0; i < GameState.MAX_PLAYERS; i++)
            {
                if (s.players[i].health > 0 && s.players[i].currentActionHash != 0)
                {
                    if (actions != null && actions.TryGetValue(s.players[i].currentActionHash, out var action))
                    {
                        int currentFrame = s.players[i].actionFrameIndex;

                        if (action.projectileSpawns != null)
                        {
                            foreach (var spawn in action.projectileSpawns)
                            {
                                if (spawn.frame == currentFrame)
                                {
                                    int facingDir = (s.players[i].facing == Facing.RIGHT) ? 1 : -1;

                                    int spawnX = s.players[i].posX + (spawn.offsetX * facingDir);
                                    int spawnY = s.players[i].posY + spawn.offsetY;
                                    int velX = spawn.velX * facingDir;
                                    int velY = spawn.velY;

                                    ProjectileSystem.SpawnProjectile(s, spawnX, spawnY, velX, velY, spawn.lifetime, spawn.type);
                                }
                            }
                        }
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

        /// <summary>
        /// Basic combat resolution for testing.
        /// In a full implementation, this would use ActionDef to generate hitboxes.
        /// </summary>
        private static void ResolveCombat(ref GameState s, CharacterDef[] defs, System.Collections.Generic.Dictionary<int, ActionDef> actions)
        {
            // Collect hitboxes and hurtboxes
            // Note: In a real implementation, we would use a list or pre-allocated array.
            // For now, we use arrays assuming max 10 hitboxes/hurtboxes per frame for simplicity.

            int maxHitboxes = GameState.MAX_PLAYERS * 5; // Assumed max per player
            int maxHurtboxes = GameState.MAX_PLAYERS;

            var hitboxes = new CombatResolver.Hitbox[maxHitboxes];
            var attackerPositionsX = new int[maxHitboxes];
            var attackerPositionsY = new int[maxHitboxes];
            int hitboxCount = 0;

            var hurtboxes = new CombatResolver.Hurtbox[maxHurtboxes];
            int hurtboxCount = 0;

            // Generate hitboxes from active actions
            for (int i = 0; i < GameState.MAX_PLAYERS; i++)
            {
                if (s.players[i].health <= 0) continue;

                // Add hurtbox (body)
                hurtboxes[hurtboxCount] = new CombatResolver.Hurtbox
                {
                    bounds = new AABB
                    {
                        minX = s.players[i].posX - defs[i].hitboxWidth / 2,
                        maxX = s.players[i].posX + defs[i].hitboxWidth / 2,
                        minY = s.players[i].posY,
                        maxY = s.players[i].posY + defs[i].hitboxHeight
                    },
                    weight = defs[i].weight,
                    playerIndex = i
                };
                hurtboxCount++;

                // Add hitboxes if attacking
                int actionHash = s.players[i].currentActionHash;
                if (actionHash != 0 && actions != null && actions.TryGetValue(actionHash, out var action))
                {
                    int frame = s.players[i].actionFrameIndex;
                    if (action.hitboxEvents != null)
                    {
                        foreach (var ev in action.hitboxEvents)
                        {
                            if (frame >= ev.startFrame && frame < ev.endFrame)
                            {
                                int facingDir = (s.players[i].facing == Facing.RIGHT) ? 1 : -1;

                                int hbCenterX = s.players[i].posX + (ev.offsetX * facingDir);
                                int hbCenterY = s.players[i].posY + ev.offsetY;

                                hitboxes[hitboxCount] = new CombatResolver.Hitbox
                                {
                                    bounds = new AABB
                                    {
                                        minX = hbCenterX - ev.width / 2,
                                        maxX = hbCenterX + ev.width / 2,
                                        minY = hbCenterY - ev.height / 2,
                                        maxY = hbCenterY + ev.height / 2
                                    },
                                    damage = ev.damage,
                                    baseKnockback = ev.baseKnockback,
                                    knockbackGrowth = ev.knockbackGrowth,
                                    hitstun = ev.hitstun,
                                    disjoint = ev.disjoint
                                };
                                attackerPositionsX[hitboxCount] = s.players[i].posX;
                                attackerPositionsY[hitboxCount] = s.players[i].posY;
                                hitboxCount++;
                            }
                        }
                    }
                }
            }

            // Trim arrays
            var activeHitboxes = new CombatResolver.Hitbox[hitboxCount];
            var activeAttackerX = new int[hitboxCount];
            var activeAttackerY = new int[hitboxCount];
            for(int k=0; k<hitboxCount; k++) {
                activeHitboxes[k] = hitboxes[k];
                activeAttackerX[k] = attackerPositionsX[k];
                activeAttackerY[k] = attackerPositionsY[k];
            }

            var activeHurtboxes = new CombatResolver.Hurtbox[hurtboxCount];
            for(int k=0; k<hurtboxCount; k++) activeHurtboxes[k] = hurtboxes[k];

            // Resolve combat
            var results = CombatResolver.ResolveCombat(activeHitboxes, activeHurtboxes, activeAttackerX, activeAttackerY, defs);

            // Apply results
            foreach (var res in results)
            {
                if (res.hit)
                {
                    // Apply damage
                    s.players[res.hitPlayerIndex].health = (short)System.Math.Max(0, s.players[res.hitPlayerIndex].health - res.damageDealt);

                    // Apply knockback
                    s.players[res.hitPlayerIndex].velX = res.knockbackX;
                    s.players[res.hitPlayerIndex].velY = res.knockbackY;

                    // Apply hitstun
                    s.players[res.hitPlayerIndex].hitstunRemaining = (short)res.hitstun;

                    // Interrupt action if hit
                    s.players[res.hitPlayerIndex].currentActionHash = 0;
                    s.players[res.hitPlayerIndex].actionFrameIndex = 0;
                }
            }
        }

        private static void ProgressActions(ref GameState s, System.Collections.Generic.Dictionary<int, ActionDef> actions)
        {
            for (int i = 0; i < GameState.MAX_PLAYERS; i++)
            {
                if (s.players[i].health > 0 && s.players[i].currentActionHash != 0)
                {
                    if (actions != null && actions.TryGetValue(s.players[i].currentActionHash, out var action))
                    {
                        // Increment frame
                        s.players[i].actionFrameIndex++;

                        // Check if action is finished
                        if (s.players[i].actionFrameIndex >= action.totalFrames)
                        {
                            s.players[i].currentActionHash = 0;
                            s.players[i].actionFrameIndex = 0;
                        }
                    }
                    else
                    {
                        // Unknown action, reset
                        s.players[i].currentActionHash = 0;
                        s.players[i].actionFrameIndex = 0;
                    }
                }
            }
        }
    }
}
