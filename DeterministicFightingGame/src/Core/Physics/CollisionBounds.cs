/* ================================================================================
   NEURAL DRAFT LLC | DEEPSEEK INSTRUCTION HEADER
================================================================================
   FILE:    AABB.cs
   CONTEXT: Collision primitive.

   TASK:
   Write an 'AABB' struct with minX, maxX, minY, maxY and a static 'Overlaps' method. Integer math only.

   CONSTRAINTS:
   - Use Fixed-Point Math (Fx.SCALE = 1000) for all physics.
   - No Unity Engine references in this file (unless specified in Bridge).
   - Strict Determinism: No floats, no random execution order.
================================================================================

*/
namespace NeuralDraft
{
    public struct AABB
    {
        public int minX;
        public int maxX;
        public int minY;
        public int maxY;

        public static bool Overlaps(AABB a, AABB b)
        {
            return a.minX <= b.maxX && a.maxX >= b.minX &&
                   a.minY <= b.maxY && a.maxY >= b.minY;
        }
    }
}
