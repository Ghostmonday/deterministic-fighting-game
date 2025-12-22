/* ================================================================================
   NEURAL DRAFT LLC | DEEPSEEK INSTRUCTION HEADER
================================================================================
   FILE:    StateHash.cs
   CONTEXT: Desync detection.

   TASK:
   Implement deterministic FNV-1a hashing over all fields of GameState. Used for rollback validation only. No allocations.

   CONSTRAINTS:
   - Use Fixed-Point Math (Fx.SCALE = 1000) for all physics.
   - No Unity Engine references in this file (unless specified in Bridge).
   - Strict Determinism: No floats, no random execution order.
================================================================================

*/
namespace NeuralDraft
{
    public static class StateHash
    {
        private const uint FNV_PRIME = 16777619;
        private const uint FNV_OFFSET_BASIS = 2166136261;

        public static uint Compute(GameState state)
        {
            uint hash = FNV_OFFSET_BASIS;

            // Hash frameIndex
            hash = FNV1aHash(hash, (uint)state.frameIndex);

            // Hash players
            for (int i = 0; i < GameState.MAX_PLAYERS; i++)
            {
                var player = state.players[i];
                hash = FNV1aHash(hash, (uint)player.posX);
                hash = FNV1aHash(hash, (uint)player.posY);
                hash = FNV1aHash(hash, (uint)player.velX);
                hash = FNV1aHash(hash, (uint)player.velY);
                hash = FNV1aHash(hash, (uint)player.facing);
                hash = FNV1aHash(hash, (uint)player.grounded);
                hash = FNV1aHash(hash, (uint)player.health);
                hash = FNV1aHash(hash, (uint)player.currentActionHash);
                hash = FNV1aHash(hash, (uint)player.actionFrameIndex);
                hash = FNV1aHash(hash, (uint)player.hitstunRemaining);
            }

            // Hash projectiles
            for (int i = 0; i < GameState.MAX_PROJECTILES; i++)
            {
                var projectile = state.projectiles[i];
                hash = FNV1aHash(hash, (uint)projectile.uid);
                hash = FNV1aHash(hash, (uint)projectile.active);
                hash = FNV1aHash(hash, (uint)projectile.posX);
                hash = FNV1aHash(hash, (uint)projectile.posY);
                hash = FNV1aHash(hash, (uint)projectile.velX);
                hash = FNV1aHash(hash, (uint)projectile.velY);
                hash = FNV1aHash(hash, (uint)projectile.lifetimeRemaining);
            }

            return hash;
        }

        private static uint FNV1aHash(uint hash, uint data)
        {
            hash ^= data;
            hash *= FNV_PRIME;
            return hash;
        }
    }
}
