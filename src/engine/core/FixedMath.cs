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
        
        // Multiply two fixed-point values
        public static int Mul(int a, int b)
        {
            return (int)((long)a * b / Fx.SCALE);
        }
        
        // Divide two fixed-point values
        public static int Div(int a, int b)
        {
            if (b == 0) return 0;
            return (int)((long)a * Fx.SCALE / b);
        }
        
        // Floor for fixed-point
        public static int Floor(int a)
        {
            return a / Fx.SCALE;
        }
        
        // Round for fixed-point
        public static int Round(int a)
        {
            if (a >= 0)
                return (a + Fx.SCALE / 2) / Fx.SCALE;
            return -((-a + Fx.SCALE / 2) / Fx.SCALE);
        }
        
        // Abs for fixed-point
        public static int Abs(int a)
        {
            return a >= 0 ? a : -a;
        }
        
        // Max for fixed-point
        public static int Max(int a, int b)
        {
            return a > b ? a : b;
        }
        
        // Min for fixed-point
        public static int Min(int a, int b)
        {
            return a < b ? a : b;
        }
    }
}
