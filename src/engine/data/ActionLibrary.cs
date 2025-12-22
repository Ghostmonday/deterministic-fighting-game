using System.Collections.Generic;

namespace NeuralDraft
{
    public static class ActionLibrary
    {
        private static Dictionary<int, ActionDef> _actions;

        public static void Initialize()
        {
            if (_actions != null) return;

            _actions = new Dictionary<int, ActionDef>();

            // Ronin Slash Light
            var slash = new ActionDef();
            slash.name = "Ronin_Slash_Light";
            slash.actionId = ActionDef.HashActionId(slash.name);
            slash.totalFrames = 19; // 4 Startup, 3 Active, 12 Recovery
            slash.frames = new ActionFrame[19];
            for (int i = 0; i < 19; i++)
            {
                slash.frames[i] = new ActionFrame { frameNumber = i };
            }

            slash.hitboxEvents = new HitboxEvent[]
            {
                new HitboxEvent
                {
                    startFrame = 4,
                    endFrame = 6,
                    offsetX = 800, // Specific offset
                    offsetY = 1000,
                    width = 1000,
                    height = 800,
                    damage = 50,
                    baseKnockback = 400,
                    knockbackGrowth = 5,
                    hitstun = 12,
                    disjoint = 0
                }
            };
            slash.projectileSpawns = new ProjectileSpawn[0];
            _actions[slash.actionId] = slash;

            // Ronin Shuriken Toss
            var toss = new ActionDef();
            toss.name = "Ronin_Shuriken_Toss";
            toss.actionId = ActionDef.HashActionId(toss.name);
            toss.totalFrames = 30; // Example timeline: 5 Startup, 25 Recovery
            toss.frames = new ActionFrame[30];
            for (int i = 0; i < 30; i++)
            {
                toss.frames[i] = new ActionFrame { frameNumber = i };
            }

            toss.projectileSpawns = new ProjectileSpawn[]
            {
                new ProjectileSpawn
                {
                    frame = 5, // Spawns at end of startup
                    offsetX = 1000,
                    offsetY = 1200,
                    velX = 12000, // Fast projectile
                    velY = 0,
                    type = ProjectileType.SHURIKEN,
                    lifetime = 120
                }
            };
            toss.hitboxEvents = new HitboxEvent[0];
            _actions[toss.actionId] = toss;
        }

        public static bool TryGetAction(int hash, out ActionDef def)
        {
            if (_actions == null)
            {
                Initialize();
            }
            return _actions.TryGetValue(hash, out def);
        }
    }
}
