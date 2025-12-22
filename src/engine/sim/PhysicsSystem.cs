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
        public static void ApplyMovementInput(ref PlayerState player, CharacterDef characterDef, int inputX, bool jumpPressed, bool grounded)
        {
            if (inputX != 0) {
                player.velX = inputX * characterDef.walkSpeed;
                player.facing = inputX > 0 ? Facing.RIGHT : Facing.LEFT;
            } else {
                ApplyFriction(ref player, characterDef, grounded);
            }

            if (jumpPressed && grounded) {
                player.velY = characterDef.jumpForce; // Positive is UP
                player.grounded = 0;
            }
        }

        public static void ApplyFriction(ref PlayerState player, CharacterDef characterDef, bool grounded)
        {
            int friction = grounded ? characterDef.groundFriction : characterDef.airFriction;
            if (player.velX > 0) player.velX = System.Math.Max(0, player.velX - friction);
            else if (player.velX < 0) player.velX = System.Math.Min(0, player.velX + friction);
        }

        public static void ApplyGravity(ref PlayerState player, CharacterDef characterDef)
        {
            if (player.grounded == 0) {
                player.velY -= characterDef.gravity; // Subtract for Y-Up
                if (player.velY < -characterDef.maxFallSpeed) player.velY = -characterDef.maxFallSpeed;
            } else if (player.velY < 0) {
                player.velY = 0;
            }
        }

        public static void StepAndCollide(ref PlayerState player, CharacterDef characterDef, MapData map)
        {
            int newPosX = player.posX + player.velX;
            int newPosY = player.posY + player.velY;

            // Calculate hitbox bounds based on character definition
            int halfWidth = characterDef.hitboxWidth / 2;
            AABB playerBounds = new AABB {
                minX = newPosX - halfWidth,
                maxX = newPosX + halfWidth,
                minY = newPosY + characterDef.hitboxOffsetY,
                maxY = newPosY + characterDef.hitboxOffsetY + characterDef.hitboxHeight
            };

            player.grounded = 0;
            if (map.SolidBlocks != null) {
                for (int blockIndex = 0; blockIndex < map.SolidBlocks.Length; blockIndex++) {
                    var block = map.SolidBlocks[blockIndex];
                    if (AABB.Overlaps(playerBounds, block)) {
                        int overlapX = System.Math.Min(playerBounds.maxX - block.minX, block.maxX - playerBounds.minX);
                        int overlapY = System.Math.Min(playerBounds.maxY - block.minY, block.maxY - playerBounds.minY);

                        if (overlapX < overlapY) {
                            if (playerBounds.minX < block.minX) newPosX = block.minX - halfWidth;
                            else newPosX = block.maxX + halfWidth;
                            player.velX = 0;
                        } else {
                            if (playerBounds.minY < block.minY) { // Hit Ceiling
                                newPosY = block.minY - characterDef.hitboxHeight - characterDef.hitboxOffsetY;
                                player.velY = 0;
                            } else { // Land on Floor
                                newPosY = block.maxY - characterDef.hitboxOffsetY;
                                player.velY = 0;
                                player.grounded = 1;
                            }
                        }
                        // Update bounds for next check
                        playerBounds.minX = newPosX - halfWidth;
                        playerBounds.maxX = newPosX + halfWidth;
                        playerBounds.minY = newPosY + characterDef.hitboxOffsetY;
                        playerBounds.maxY = newPosY + characterDef.hitboxOffsetY + characterDef.hitboxHeight;
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
