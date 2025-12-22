/* ================================================================================
   NEURAL DRAFT LLC | DEEPSEEK INSTRUCTION HEADER
================================================================================
   FILE:    Enums.cs
   CONTEXT: Shared enums used by State and Sim.

   TASK:
   Write C# enums for 'Facing' (LEFT, RIGHT) and 'ProjectileType' (BULLET, ARROW, SHURIKEN). Enums must be byte-sized.

   CONSTRAINTS:
   - Use Fixed-Point Math (Fx.SCALE = 1000) for all physics.
   - No Unity Engine references in this file (unless specified in Bridge).
   - Strict Determinism: No floats, no random execution order.
================================================================================

*/
namespace NeuralDraft
{
    public enum Facing : byte
    {
        LEFT = 0,
        RIGHT = 1
    }

    public enum ProjectileType : byte
    {
        BULLET = 0,
        ARROW = 1,
        SHURIKEN = 2
    }

    [System.Flags]
    public enum InputBits : ushort
    {
        NONE = 0,
        UP = 1 << 0,
        DOWN = 1 << 1,
        LEFT = 1 << 2,
        RIGHT = 1 << 3,
        JUMP = 1 << 4,
        ATTACK = 1 << 5,
        SPECIAL = 1 << 6,
        DEFEND = 1 << 7
    }
}
