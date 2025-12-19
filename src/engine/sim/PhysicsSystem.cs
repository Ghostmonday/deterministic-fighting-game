/* ================================================================================
   NEURAL DRAFT LLC | DEEPSEEK INSTRUCTION HEADER
================================================================================
   FILE:    PhysicsSystem.cs
   CONTEXT: Player movement + gravity.

   TASK:
   Implement static 'PhysicsSystem'. Methods: ApplyMovementInput, ApplyGravity (45), StepAndCollide. Use AABB collision. Do NOT use Unity Physics.

   CONSTRAINTS:
   - Use Fixed-Point Math (Fx.SCALE = 1000) for all physics.
   - No Unity Engine references in this file (unless specified in Bridge).
   - Strict Determinism: No floats, no random execution order.
================================================================================

*/
namespace NeuralDraft
{
    public static class PhysicsSystem
    {
        private const int GRAVITY = 45; // Fixed-point gravity value
        private const int MAX_FALL_SPEED = 3000; // Fixed-point max fall speed
        private const int WALK_SPEED = 800; // Fixed-point walk speed
        private const int JUMP_FORCE = 1500; // Fixed-point jump force
        private const int GROUND_FRICTION = 200; // Fixed-point ground friction

        public static void ApplyMovementInput(ref PlayerState player, int inputX, bool jumpPressed, bool grounded)
        {
            // Apply horizontal movement
            if (inputX != 0)
            {
                player.velX = inputX * WALK_SPEED;
                player.facing = inputX > 0 ? Facing.RIGHT : Facing.LEFT;
            }
            else
            {
                // Apply friction when no input
                if (player.velX > 0)
                {
                    player.velX = System.Math.Max(0, player.velX - GROUND_FRICTION);
                }
                else if (player.velX < 0)
                {
                    player.velX = System.Math.Min(0, player.velX + GROUND_FRICTION);
                }
            }

            // Apply jump
            if (jumpPressed && grounded)
            {
                player.velY = -JUMP_FORCE;
                player.grounded = 0;
            }
        }

        public static void ApplyGravity(ref PlayerState player)
        {
            if (player.grounded == 0)
            {
                player.velY += GRAVITY;

                // Cap fall speed
                if (player.velY > MAX_FALL_SPEED)
                {
                    player.velY = MAX_FALL_SPEED;
                }
            }
            else
            {
                player.velY = 0;
            }
        }

        public static void StepAndCollide(ref PlayerState player, MapData map, int deltaTime)
        {
            // Apply velocity to position
            int newPosX = player.posX + player.velX * deltaTime / Fx.SCALE;
            int newPosY = player.posY + player.velY * deltaTime / Fx.SCALE;

            // Create player AABB (simplified - assuming player is 100x200 units)
            AABB playerBounds = new AABB
            {
                minX = newPosX - 50 * Fx.SCALE / 1000,
                maxX = newPosX + 50 * Fx.SCALE / 1000,
                minY = newPosY,
                maxY = newPosY + 200 * Fx.SCALE / 1000
            };

            // Check collision with solid blocks
            bool collided = false;
            player.grounded = 0;

            if (map.SolidBlocks != null)
            {
                foreach (var block in map.SolidBlocks)
                {
                    if (AABB.Overlaps(playerBounds, block))
                    {
                        collided = true;

                        // Simple collision resolution - push player out
                        // Calculate overlap on each axis
                        int overlapX = System.Math.Min(
                            playerBounds.maxX - block.minX,
                            block.maxX - playerBounds.minX
                        );

                        int overlapY = System.Math.Min(
                            playerBounds.maxY - block.minY,
                            block.maxY - playerBounds.minY
                        );

                        // Resolve on the axis with smallest overlap
                        if (overlapX < overlapY)
                        {
                            // Horizontal collision
                            if (playerBounds.minX < block.minX)
                            {
                                newPosX = block.minX - 50 * Fx.SCALE / 1000;
                            }
                            else
                            {
                                newPosX = block.maxX + 50 * Fx.SCALE / 1000;
                            }
                            player.velX = 0;
                        }
                        else
                        {
                            // Vertical collision
                            if (playerBounds.minY < block.minY)
                            {
                                newPosY = block.minY - 200 * Fx.SCALE / 1000;
                                player.velY = 0;
                            }
                            else
                            {
                                newPosY = block.maxY;
                                player.velY = 0;
                                player.grounded = 1;
                            }
                        }

                        // Update player bounds after adjustment
                        playerBounds = new AABB
                        {
                            minX = newPosX - 50 * Fx.SCALE / 1000,
                            maxX = newPosX + 50 * Fx.SCALE / 1000,
                            minY = newPosY,
                            maxY = newPosY + 200 * Fx.SCALE / 1000
                        };
                    }
                }
            }

            // Check kill floor
            if (newPosY > map.KillFloorY)
            {
                // Player fell off the map - reset to safe position
                newPosX = 0;
                newPosY = 0;
                player.velX = 0;
                player.velY = 0;
                player.grounded = 1;
            }

            // Update player position
            player.posX = newPosX;
            player.posY = newPosY;
        }
    }
}
