/* ENUMS - Shared enums */
namespace NeuralDraft {
    public enum Facing : int { LEFT = -1, RIGHT = 1 }
    public enum ProjectileType : byte { BULLET, ARROW, SHURIKEN }
    [System.Flags] public enum InputBits : ushort { NONE = 0, UP = 1, DOWN = 2, LEFT = 4, RIGHT = 8, JUMP = 16, ATTACK = 32, SPECIAL = 64, DEFEND = 128 }
}
