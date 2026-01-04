/* JASON CHARACTER DEFINITIONS */
namespace NeuralDraft {
    public enum JasonMoveType { Neutral, Forward, Backward, Jump, ForwardJump, BackwardJump, Crouch, Light, Medium, Heavy, Special, Super, Throw, Tech, Guard, Counter, Cancel }
    public struct JasonComboChain { public string comboName; public int damage; public int meterGain; public JasonMoveType[] sequence; public int[] frameData; public int[] hitConfirmWindows; public bool links; public bool cancellable; }
    public enum GuardType { None, High, Mid, Low, All, Unblockable }
    public enum CounterType { None, WhiffPunish, HitParry, ThrowTech }
    public struct AnimationSequence { public string name; public int frameCount; public bool loop; public int[] frameEvents; }
    public struct MoveDefinition { public string name; public JasonMoveType type; public int startup; public int active; public int recovery; public int damage; public int meterGain; public int knockback; public int hitstun; public AABB hitbox; public GuardType guardType; public bool cancelable; public int chainPriority; public bool isProjectile; public int projectileSpeed; public int projectileLifetime; public bool isInvincible; public int invincibleStartup; public int invincibleActive; public bool isParry; public int parryWindow; public bool isCounter; public CounterType counterType; public bool guardBreak; public int meterCost; }
