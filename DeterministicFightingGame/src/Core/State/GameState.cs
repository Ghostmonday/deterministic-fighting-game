/* ================================================================================
   NEURAL DRAFT LLC | DEEPSEEK INSTRUCTION HEADER
================================================================================
   FILE:    GameState.cs
   CONTEXT: Authoritative simulation state.

   TASK:
   Write a sealed C# class 'GameState'. Constants: MAX_PLAYERS=2, MAX_PROJECTILES=64. Fields: int frameIndex, PlayerState[] players, ProjectileState[] projectiles. Include a 'CopyTo(GameState dst)' method for deep copy.

   CONSTRAINTS:
   - Use Fixed-Point Math (Fx.SCALE = 1000) for all physics.
   - No Unity Engine references in this file (unless specified in Bridge).
   - Strict Determinism: No floats, no random execution order.
================================================================================

*/
namespace NeuralDraft
{
    public sealed class GameState
    {
        public const int MAX_PLAYERS = 2;
        public const int MAX_PROJECTILES = 64;

        // ================================================================================
        // SIMULATION ORDER (CRITICAL FOR DETERMINISM - DO NOT CHANGE)
        // ================================================================================
        // 1. Input Processing (RollbackController)
        // 2. Movement (PhysicsSystem.ApplyMovementInput)
        // 3. Gravity (PhysicsSystem.ApplyGravity)
        // 4. Collision (PhysicsSystem.StepAndCollide)
        // 5. Projectile Update (ProjectileSystem.UpdateAllProjectiles)
        // 6. Combat Resolution (CombatResolver.ResolveCombat)
        // 7. Action/Animation Updates
        // ================================================================================

        public int frameIndex;
        public int nextProjectileUid;
        public int activeProjectileCount;
        public PlayerState[] players;
        public ProjectileState[] projectiles;

        public GameState()
        {
            frameIndex = 0;
            nextProjectileUid = 0;
            activeProjectileCount = 0;

            players = new PlayerState[MAX_PLAYERS];
            for (int i = 0; i < MAX_PLAYERS; i++)
            {
                players[i] = new PlayerState();
            }

            projectiles = new ProjectileState[MAX_PROJECTILES];
            for (int i = 0; i < MAX_PROJECTILES; i++)
            {
                projectiles[i] = new ProjectileState();
            }
        }

        public void CopyTo(GameState dst)
        {
            dst.frameIndex = frameIndex;
            dst.nextProjectileUid = nextProjectileUid;
            dst.activeProjectileCount = activeProjectileCount;

            for (int i = 0; i < MAX_PLAYERS; i++)
            {
                players[i].CopyTo(dst.players[i]);
            }

            for (int i = 0; i < MAX_PROJECTILES; i++)
            {
                projectiles[i].CopyTo(dst.projectiles[i]);
            }
        }
    }
}
