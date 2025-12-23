/* ================================================================================
   NEURAL DRAFT LLC | DEEPSEEK INSTRUCTION HEADER
================================================================================
   FILE:    PlayerState.cs
   CONTEXT: Deterministic player snapshot.

   TASK:
   Write a C# struct 'PlayerState'. Fields: int posX, posY; int velX, velY; Facing facing; byte grounded; short health; int currentActionHash; short actionFrameIndex; short hitstunRemaining. No methods.

   CONSTRAINTS:
   - Use Fixed-Point Math (Fx.SCALE = 1000) for all physics.
   - No Unity Engine references in this file (unless specified in Bridge).
   - Strict Determinism: No floats, no random execution order.
================================================================================

*/
namespace NeuralDraft
{
    public struct PlayerState
    {
        public int posX;
        public int posY;
        public int velX;
        public int velY;
        public Facing facing;
        public byte grounded;
        public short health;
        public int currentActionHash;
        public short actionFrameIndex;
        public short hitstunRemaining;

        public void CopyTo(ref PlayerState dst)
        {
            dst.posX = posX;
            dst.posY = posY;
            dst.velX = velX;
            dst.velY = velY;
            dst.facing = facing;
            dst.grounded = grounded;
            dst.health = health;
            dst.currentActionHash = currentActionHash;
            dst.actionFrameIndex = actionFrameIndex;
            dst.hitstunRemaining = hitstunRemaining;
        }
    }
}
