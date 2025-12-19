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
        private const int SUBSTEP_THRESHOLD = 175; // Fixed-point threshold for substep calculation
        private const int PROJECTILE_SIZE = 20 * Fx.SCALE / 1000; // Fixed-point projectile size (20 units)

        public static void UpdateProjectile(ref ProjectileState projectile, MapData map, int deltaTime)
        {
            if (projectile.active == 0)
            {
                return;
            }

            // Decrease lifetime
            projectile.lifetimeRemaining -= (short)deltaTime;
            if (projectile.lifetimeRemaining <= 0)
            {
                projectile.active = 0;
                return;
            }

            // Calculate substeps based on velocity to prevent tunneling
            int velXAbs = System.Math.Abs(projectile.velX);
            int velYAbs = System.Math.Abs(projectile.velY);
            int maxVel = System.Math.Max(velXAbs, velYAbs);

            int substeps = maxVel / SUBSTEP_THRESHOLD;
            if (substeps < 1)
            {
                substeps = 1;
            }

            // Calculate movement per substep
            int stepVelX = projectile.velX / substeps;
            int stepVelY = projectile.velY / substeps;
            int stepDeltaTime = deltaTime / substeps;

            // Process each substep
            for (int step = 0; step < substeps; step++)
            {
                int newPosX = projectile.posX + stepVelX * stepDeltaTime / Fx.SCALE;
                int newPosY = projectile.posY + stepVelY * stepDeltaTime / Fx.SCALE;

                // Create projectile AABB
                AABB projectileBounds = new AABB
                {
                    minX = newPosX - PROJECTILE_SIZE / 2,
                    maxX = newPosX + PROJECTILE_SIZE / 2,
                    minY = newPosY - PROJECTILE_SIZE / 2,
                    maxY = newPosY + PROJECTILE_SIZE / 2
                };

                // Check collision with solid blocks
                bool collided = false;

                if (map.SolidBlocks != null)
                {
                    foreach (var block in map.SolidBlocks)
                    {
                        if (AABB.Overlaps(projectileBounds, block))
                        {
                            collided = true;
                            break;
                        }
                    }
                }

                if (collided)
                {
                    // Projectile hit a wall - deactivate it
                    projectile.active = 0;
                    return;
                }

                // Check if projectile is below kill floor
                if (newPosY > map.KillFloorY)
                {
                    projectile.active = 0;
                    return;
                }

                // Update position for this substep
                projectile.posX = newPosX;
                projectile.posY = newPosY;
            }
        }

        public static void UpdateAllProjectiles(GameState state, MapData map, int deltaTime)
        {
            for (int i = 0; i < GameState.MAX_PROJECTILES; i++)
            {
                UpdateProjectile(ref state.projectiles[i], map, deltaTime);
            }
        }

        public static int SpawnProjectile(GameState state, int posX, int posY, int velX, int velY,
                                         short lifetime, ProjectileType type)
        {
            for (int i = 0; i < GameState.MAX_PROJECTILES; i++)
            {
                if (state.projectiles[i].active == 0)
                {
                    state.projectiles[i].uid = i;
                    state.projectiles[i].active = 1;
                    state.projectiles[i].posX = posX;
                    state.projectiles[i].posY = posY;
                    state.projectiles[i].velX = velX;
                    state.projectiles[i].velY = velY;
                    state.projectiles[i].lifetimeRemaining = lifetime;
                    return i;
                }
            }
            return -1; // No available projectile slot
        }
    }
}
