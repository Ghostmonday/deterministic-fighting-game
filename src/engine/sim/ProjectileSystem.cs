/* ================================================================================
   NEURAL DRAFT LLC | DEEPSEEK INSTRUCTION HEADER
================================================================================
   FILE:    ProjectileSystem.cs
   CONTEXT: Anti-tunneling projectile movement.

   TASK:
   Implement integer 'Swept Collision' logic. Calculate substeps = Max(abs(velX), abs(velY)) / 175. Loop through steps to prevent tunneling at high speeds.

   CONSTRAINTS:
   - Use Fixed-Point Math (Fx.SCALE = 1000) for all physics.
   - No Unity Engine references in this file (unless specified in Bridge).
   - Strict Determinism: No floats, no random execution order.
================================================================================

*/
namespace NeuralDraft
{
    public static class ProjectileSystem
    {
        private const int SUBSTEP_THRESHOLD = 175;
        private const int PROJECTILE_SIZE = 20 * Fx.SCALE / 1000;

        public static void UpdateProjectile(ref ProjectileState projectile, MapData map, ref int activeCountDelta)
        {
            if (projectile.active == 0) return;

            projectile.lifetimeRemaining -= 1;
            if (projectile.lifetimeRemaining <= 0) {
                projectile.active = 0;
                activeCountDelta--;
                return;
            }

            int velXAbs = System.Math.Abs(projectile.velX);
            int velYAbs = System.Math.Abs(projectile.velY);
            int maxVel = System.Math.Max(velXAbs, velYAbs);
            int substeps = maxVel / SUBSTEP_THRESHOLD;
            if (substeps < 1) substeps = 1;
            if (substeps > 32) substeps = 32;

            int stepVelX = projectile.velX / substeps;
            int stepVelY = projectile.velY / substeps;

            for (int step = 0; step < substeps; step++)
            {
                int newPosX = projectile.posX + stepVelX;
                int newPosY = projectile.posY + stepVelY;

                AABB projBox = new AABB {
                    minX = newPosX - PROJECTILE_SIZE / 2, maxX = newPosX + PROJECTILE_SIZE / 2,
                    minY = newPosY - PROJECTILE_SIZE / 2, maxY = newPosY + PROJECTILE_SIZE / 2
                };

                bool collided = false;
                if (map.SolidBlocks != null) {
                    for (int blockIndex = 0; blockIndex < map.SolidBlocks.Length; blockIndex++) {
                        var block = map.SolidBlocks[blockIndex];
                        if (AABB.Overlaps(projBox, block)) { collided = true; break; }
                    }
                }

                if (collided || newPosY < map.KillFloorY) { // Check Y < Floor
                    projectile.active = 0;
                    activeCountDelta--;
                    return;
                }

                projectile.posX = newPosX; projectile.posY = newPosY;
            }
        }

        public static void UpdateAllProjectiles(GameState state, MapData map) {
            int activeCountDelta = 0;

            // Only iterate through active projectiles using swap-remove pattern
            for (int i = 0; i < state.activeProjectileCount; i++) {
                UpdateProjectile(ref state.projectiles[i], map, ref activeCountDelta);
            }

            // Update active count
            state.activeProjectileCount += activeCountDelta;

            // If we have negative delta (projectiles were deactivated),
            // we would normally compact the array here, but for determinism
            // we keep the current structure and rely on activeProjectileCount
        }

        public static int SpawnProjectile(GameState state, int posX, int posY, int velX, int velY, short lifetime, ProjectileType type) {
            // First try to find an inactive slot within the active range
            for (int i = 0; i < state.activeProjectileCount; i++) {
                if (state.projectiles[i].active == 0) {
                    InitializeProjectile(ref state.projectiles[i], state.nextProjectileUid++, posX, posY, velX, velY, lifetime);
                    return i;
                }
            }

            // If no inactive slots found and we have room, add to the end
            if (state.activeProjectileCount < GameState.MAX_PROJECTILES) {
                int index = state.activeProjectileCount;
                InitializeProjectile(ref state.projectiles[index], state.nextProjectileUid++, posX, posY, velX, velY, lifetime);
                state.activeProjectileCount++;
                return index;
            }

            return -1; // No available projectile slot
        }

        private static void InitializeProjectile(ref ProjectileState projectile, int uid, int posX, int posY, int velX, int velY, short lifetime) {
            projectile.uid = uid;
            projectile.active = 1;
            projectile.posX = posX;
            projectile.posY = posY;
            projectile.velX = velX;
            projectile.velY = velY;
            projectile.lifetimeRemaining = lifetime;
        }
    }
}
