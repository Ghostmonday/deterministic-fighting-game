/* PHYSICS SYSTEM - Deterministic player physics */
namespace NeuralDraft {
    public static class PhysicsSystem {
        public static void ApplyMovementInput(ref PlayerState player, CharacterDef def, int inputX, bool jumpPressed, bool grounded) {
            if (inputX != 0) {
                player.velX = inputX * def.walkSpeed;
                player.facing = inputX > 0 ? Facing.RIGHT : Facing.LEFT;
            }
            ApplyFriction(ref player, def, grounded);
            if (jumpPressed && grounded != 0) {
                player.velY = def.jumpForce;
                player.grounded = 0;
            }
        }
        public static void ApplyFriction(ref PlayerState player, CharacterDef def, bool grounded) {
            int friction = grounded != 0 ? def.groundFriction : def.airFriction;
            if (player.velX > 0) player.velX = System.Math.Max(0, player.velX - friction);
            else if (player.velX < 0) player.velX = System.Math.Min(0, player.velX + friction);
        }
        public static void ApplyGravity(ref PlayerState player, CharacterDef def, bool ignoreGravity) {
            if (ignoreGravity) return;
            if (player.grounded == 0) {
                player.velY -= def.gravity;
                if (player.velY < -def.maxFallSpeed) player.velY = -def.maxFallSpeed;
            } else if (player.velY < 0) {
                player.velY = 0;
            }
        }
        public static void StepAndCollide(ref PlayerState player, CharacterDef def, MapData map) {
            int newX = player.posX + player.velX;
            int newY = player.posY + player.velY;
            
            // Stage boundary collision
            if (map.leftWall != 0 || map.rightWall != 0) {
                int halfW = def.hitboxWidth / 2;
                if (newX - halfW < map.leftWall) { newX = map.leftWall + halfW; player.velX = 0; }
                if (newX + halfW > map.rightWall) { newX = map.rightWall - halfW; player.velX = 0; }
            }
            
            // Floor collision
            int floorY = map.floorY;
            if (newY < floorY) {
                newY = floorY;
                player.velY = 0;
                player.grounded = 1;
            }
            
            // Kill floor
            if (newY < map.KillFloorY) {
                newX = 0; newY = 2000; player.velX = 0; player.velY = 0;
            }
            
            player.posX = newX;
            player.posY = newY;
        }
    }
}
