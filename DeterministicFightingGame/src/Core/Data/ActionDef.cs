/* ================================================================================
   NEURAL DRAFT LLC | DEEPSEEK INSTRUCTION HEADER
================================================================================
   FILE:    ActionDef.cs
   CONTEXT: Runtime action data.

   TASK:
   Write a class representing an action definition. Fields: timeline, events, projectile parameters. Hash action_id string to int.

   CONSTRAINTS:
   - Use Fixed-Point Math (Fx.SCALE = 1000) for all physics.
   - No Unity Engine references in this file (unless specified in Bridge).
   - Strict Determinism: No floats, no random execution order.
================================================================================

*/
namespace NeuralDraft
{
    public class ActionDef
    {
        public int actionId;
        public string name;
        public int totalFrames;
        public ActionFrame[] frames;
        public ProjectileSpawn[] projectileSpawns;
        public HitboxEvent[] hitboxEvents;

        public static int HashActionId(string actionId)
        {
            // Simple deterministic hash function
            uint hash = 2166136261;
            foreach (char c in actionId)
            {
                hash ^= c;
                hash *= 16777619;
            }
            return (int)hash;
        }
    }

    public struct ActionFrame
    {
        public int frameNumber;
        public int velX;
        public int velY;
        public byte cancelable;
        public byte hitstun;
    }

    public struct ProjectileSpawn
    {
        public int frame;
        public int offsetX;
        public int offsetY;
        public int velX;
        public int velY;
        public ProjectileType type;
        public short lifetime;
    }

    public struct HitboxEvent
    {
        public int startFrame;
        public int endFrame;
        public int offsetX;
        public int offsetY;
        public int width;
        public int height;
        public int damage;
        public int baseKnockback;
        public int knockbackGrowth;
        public int hitstun;
        public byte disjoint;
    }
}
