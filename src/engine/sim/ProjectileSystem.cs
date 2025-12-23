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
        // Anti-tunneling constants
        private const int SUBSTEP_THRESHOLD = 175; // Velocity threshold for substep calculation (units per frame)
        private const int MAX_SUBSTEPS = 32;       // Maximum number of substeps to prevent performance issues
        private const int PROJECTILE_SIZE = 20 * Fx.SCALE / 1000; // 20 world units diameter

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
            if (substeps > MAX_SUBSTEPS) substeps = MAX_SUBSTEPS;

            int stepVelX = projectile.velX / substeps;
            int stepVelY = projectile.velY / substeps;

            for (int step = 0; step < substeps; step++)
            {
                int newPosX = projectile.posX + stepVelX;
                int newPosY = projectile.posY + stepVelY;

                AABB projBox = new AABB {
                    minX = newPosX - PROJECTILE_SIZE / 2, // Center projectile horizontally
                    maxX = newPosX + PROJECTILE_SIZE / 2,
                    minY = newPosY - PROJECTILE_SIZE / 2, // Center projectile vertically
                    maxY = newPosY + PROJECTILE_SIZE / 2
                };

                bool collided = false;
                if (map.SolidBlocks != null) {
                    for (int blockIndex = 0; blockIndex < map.SolidBlocks.Length; blockIndex++) {
                        var block = map.SolidBlocks[blockIndex];
                        if (AABB.Overlaps(projBox, block)) { collided = true; break; }
                    }
                }

                if (collided || newPosY < map.KillFloorY) { // Check Y < Floor (death zone)
                    projectile.active = 0;
                    activeCountDelta--;
                    return;
                }

                projectile.posX = newPosX; projectile.posY = newPosY;
            }
        }

        public static void UpdateAllProjectiles(GameState state, MapData map) {
            int activeCountDelta = 0;
            int writeIndex = 0;

            // Iterate through all projectiles and compact array using swap-remove pattern
            for (int readIndex = 0; readIndex < state.activeProjectileCount; readIndex++) {
                // If projectile is active, update it
                if (state.projectiles[readIndex].active == 1) {
                    // Update projectile in place
                    UpdateProjectile(ref state.projectiles[readIndex], map, ref activeCountDelta);

                    // If still active after update, keep it in compacted array
                    if (state.projectiles[readIndex].active == 1) {
                        if (writeIndex != readIndex) {
                            // Copy to compacted position (swap-remove pattern)
                            state.projectiles[readIndex].CopyTo(ref state.projectiles[writeIndex]);
                        }
                        writeIndex++;
                    }
                }
            }

            // Update active count
            state.activeProjectileCount = writeIndex + activeCountDelta;

            // Clear remaining slots for determinism (ensure inactive projectiles have consistent state)
            for (int i = state.activeProjectileCount; i < GameState.MAX_PROJECTILES; i++) {
                if (state.projectiles[i].active == 1) {
                    state.projectiles[i].active = 0;
                    state.projectiles[i].uid = 0;
                    state.projectiles[i].posX = 0;
                    state.projectiles[i].posY = 0;
                    state.projectiles[i].velX = 0;
                    state.projectiles[i].velY = 0;
                    state.projectiles[i].lifetimeRemaining = 0;
                }
            }
        }

        public static int SpawnProjectile(GameState state, int posX, int posY, int velX, int velY, short lifetime, ProjectileType type) {
            // First try to find an inactive slot within the active range (reuse dead projectiles)
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

            return -1; // No available projectile slot (max projectiles reached)
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
