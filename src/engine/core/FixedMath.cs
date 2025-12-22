/* ================================================================================
   NEURAL DRAFT LLC | DEEPSEEK INSTRUCTION HEADER
================================================================================
   FILE:    FixedMath.cs
   CONTEXT: Fixed-point math utilities.

   TASK:
   Implement fixed-point math helper functions.

   CONSTRAINTS:
   - Use Fixed-Point Math (Fx.SCALE = 1000) for all physics.
   - No Unity Engine references in this file.
   - Strict Determinism: No floats, no random execution order.
================================================================================

*/
namespace NeuralDraft
{
    public static class FixedMath
    {
        // Integer square root for fixed-point math
        public static int Sqrt(long n)
        {
            if (n <= 0) return 0;
            long x = n;
            long y = (x + 1) / 2;
            while (y < x)
            {
                x = y;
                y = (x + n / x) / 2;
            }
            return (int)x;
        }
    }
}
