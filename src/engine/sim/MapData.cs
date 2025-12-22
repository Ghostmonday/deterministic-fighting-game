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
        /// <summary>
        /// Axis-aligned bounding boxes representing solid collision geometry.
        /// All coordinates use Y-Up system (positive Y = up).
        /// </summary>
        public AABB[] SolidBlocks;

        /// <summary>
        /// Absolute world Y coordinate below which all entities are destroyed.
        /// Uses Y-Up coordinate system (negative values = below origin).
        ///
        /// SEMANTICS:
        /// - Any entity with position.Y < KillFloorY is immediately destroyed
        /// - Applies to both players and projectiles
        /// - Used for pit deaths, void zones, and stage boundaries
        /// - Value is in fixed-point units (multiply by Fx.SCALE for world units)
        ///
        /// EXAMPLE: KillFloorY = -2000 means entities die 2000 units below origin
        /// </summary>
        public int KillFloorY;

        /// <summary>
        /// Minimum X coordinate (left boundary) of the map.
        /// </summary>
        public int mapMinX;

        /// <summary>
        /// Maximum X coordinate (right boundary) of the map.
        /// </summary>
        public int mapMaxX;
    }
}
