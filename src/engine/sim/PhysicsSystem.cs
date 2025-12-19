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
        private const int GRAVITY = 45;
        private const int MAX_FALL_SPEED = 3000;
        private const int WALK_SPEED = 800;
        private const int JUMP_FORCE = 1500;
        private const int GROUND_FRICTION = 200;

        public static void ApplyMovementInput(ref PlayerState player, int inputX, bool jumpPressed, bool grounded)
        {
            if (inputX != 0) {
                player.velX = inputX * WALK_SPEED;
                player.facing = inputX > 0 ? Facing.RIGHT : Facing.LEFT;
            } else {
                if (player.velX > 0) player.velX = System.Math.Max(0, player.velX - GROUND_FRICTION);
                else if (player.velX < 0) player.velX = System.Math.Min(0, player.velX + GROUND_FRICTION);
            }

            if (jumpPressed && grounded) {
                player.velY = JUMP_FORCE; // Positive is UP
                player.grounded = 0;
            }
        }

        public static void ApplyGravity(ref PlayerState player)
        {
            if (player.grounded == 0) {
                player.velY -= GRAVITY; // Subtract for Y-Up
                if (player.velY < -MAX_FALL_SPEED) player.velY = -MAX_FALL_SPEED;
            } else if (player.velY < 0) {
                player.velY = 0;
            }
        }

        public static void StepAndCollide(ref PlayerState player, MapData map, int deltaTime)
        {
            int newPosX = player.posX + player.velX * deltaTime / Fx.SCALE;
            int newPosY = player.posY + player.velY * deltaTime / Fx.SCALE;

            AABB playerBounds = new AABB {
                minX = newPosX - 50 * Fx.SCALE / 1000,
                maxX = newPosX + 50 * Fx.SCALE / 1000,
                minY = newPosY,
                maxY = newPosY + 200 * Fx.SCALE / 1000
            };

            player.grounded = 0;
            if (map.SolidBlocks != null) {
                foreach (var block in map.SolidBlocks) {
                    if (AABB.Overlaps(playerBounds, block)) {
                        int overlapX = System.Math.Min(playerBounds.maxX - block.minX, block.maxX - playerBounds.minX);
                        int overlapY = System.Math.Min(playerBounds.maxY - block.minY, block.maxY - playerBounds.minY);

                        if (overlapX < overlapY) {
                            if (playerBounds.minX < block.minX) newPosX = block.minX - 50 * Fx.SCALE / 1000;
                            else newPosX = block.maxX + 50 * Fx.SCALE / 1000;
                            player.velX = 0;
                        } else {
                            if (playerBounds.minY < block.minY) { // Hit Ceiling
                                newPosY = block.minY - 200 * Fx.SCALE / 1000;
                                player.velY = 0;
                            } else { // Land on Floor
                                newPosY = block.maxY;
                                player.velY = 0;
                                player.grounded = 1;
                            }
                        }
                        // Update bounds for next check
                        playerBounds.minX = newPosX - 50 * Fx.SCALE / 1000;
                        playerBounds.maxX = newPosX + 50 * Fx.SCALE / 1000;
                        playerBounds.minY = newPosY;
                        playerBounds.maxY = newPosY + 200 * Fx.SCALE / 1000;
                    }
                }
            }

            if (newPosY < map.KillFloorY) { // Check Y < Floor
                newPosX = 0;
                newPosY = 2000 * Fx.SCALE / 1000; // Reset to sky
                player.velX = 0;
                player.velY = 0;
            }

            player.posX = newPosX;
            player.posY = newPosY;
        }
    }
}
