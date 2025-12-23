/* ================================================================================
   NEURAL DRAFT LLC | DEEPSEEK INSTRUCTION HEADER
================================================================================
   FILE:    ProjectileState.cs
   CONTEXT: Deterministic projectile snapshot.

   TASK:
   Write a C# struct 'ProjectileState'. Fields: int uid; byte active; int posX, posY; int velX, velY; short lifetimeRemaining. No methods.

   CONSTRAINTS:
   - Use Fixed-Point Math (Fx.SCALE = 1000) for all physics.
   - No Unity Engine references in this file (unless specified in Bridge).
   - Strict Determinism: No floats, no random execution order.
================================================================================

*/
namespace NeuralDraft
{
    public struct ProjectileState
    {
        public int uid;
        public byte active;
        public int posX;
        public int posY;
        public int velX;
        public int velY;
        public short lifetimeRemaining;

        public void CopyTo(ref ProjectileState dst)
        {
            dst.uid = uid;
            dst.active = active;
            dst.posX = posX;
            dst.posY = posY;
            dst.velX = velX;
            dst.velY = velY;
            dst.lifetimeRemaining = lifetimeRemaining;
        }
    }
}
