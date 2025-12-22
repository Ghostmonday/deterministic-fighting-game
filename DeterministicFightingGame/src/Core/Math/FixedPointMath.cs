/* ================================================================================
   NEURAL DRAFT LLC | DEEPSEEK INSTRUCTION HEADER
================================================================================
   FILE:    Fx.cs
   CONTEXT: Fixed-point math constants.

   TASK:
   Write a C# static class 'Fx' defining 'public const int SCALE = 1000'. This class exists solely to define the fixed-point scale. Do not add methods.

   CONSTRAINTS:
   - Use Fixed-Point Math (Fx.SCALE = 1000) for all physics.
   - No Unity Engine references in this file (unless specified in Bridge).
   - Strict Determinism: No floats, no random execution order.
================================================================================

*/
namespace NeuralDraft
{
    public static class Fx
    {
        public const int SCALE = 1000;
    }
}
