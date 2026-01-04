/* ACTION DEFINITION */
namespace NeuralDraft {
    public class ActionDef {
        public int actionId;
        public string name;
        public int totalFrames;
        public ActionFrame[] frames;
        public HitboxEvent[] hitboxEvents;
        public bool ignoreGravity;
        public static int HashActionId(string s) {
            uint h = 2166136261;
            for (int i = 0; i < s.Length; i++) { h ^= s[i]; h *= 16777619; }
            return (int)h;
        }
    }
    public struct ActionFrame { public int frameNumber; public int velX; public int velY; public byte cancelable; public byte hitstun; }
    public struct HitboxEvent { public int startFrame; public int endFrame; public int offsetX; public int offsetY; public int width; public int height; public int damage; public int baseKnockback; public int knockbackGrowth; public int hitstun; public byte disjoint; }
    public enum ProjectileType { SHURIKEN, FIREBALL }
    public struct ProjectileSpawn { public int frame; public int offsetX; public int offsetY; public int velX; public int velY; public ProjectileType type; public short lifetime; }
}
