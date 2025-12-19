/* ================================================================================
   NEURAL DRAFT LLC | DEEPSEEK INSTRUCTION HEADER
================================================================================
   FILE:    MapData.cs
   CONTEXT: World definition.

   TASK:
   Write a struct 'MapData' containing: AABB[] SolidBlocks; int KillFloorY. No logic.

   CONSTRAINTS:
   - Use Fixed-Point Math (Fx.SCALE = 1000) for all physics.
   - No Unity Engine references in this file (unless specified in Bridge).
   - Strict Determinism: No floats, no random execution order.
================================================================================

*/
namespace NeuralDraft
{
    public struct MapData
    {
        public AABB[] SolidBlocks;
        public int KillFloorY;
    }
}
