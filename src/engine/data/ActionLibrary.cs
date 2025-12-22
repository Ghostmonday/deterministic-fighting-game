/* ================================================================================
   NEURAL DRAFT LLC | ACTION LIBRARY
================================================================================
   FILE:    ActionLibrary.cs
   CONTEXT: Static library of character actions.

   TASK:
   Provide ActionDefs for characters based on input.
   For now, we implement basic actions for testing.
================================================================================ */

using System.Collections.Generic;

namespace NeuralDraft
{
    public static class ActionLibrary
    {
        private static readonly Dictionary<int, Dictionary<InputBits, ActionDef>> _library;
        private static readonly Dictionary<int, ActionDef> _actionByHash;

        static ActionLibrary()
        {
            _library = new Dictionary<int, Dictionary<InputBits, ActionDef>>();
            _actionByHash = new Dictionary<int, ActionDef>();

            InitializeRonin();
        }

        private static void InitializeRonin()
        {
            // Initialize for Archetype 0 (Ronin) - CharacterId 0
            var roninActions = new Dictionary<InputBits, ActionDef>();

            // ATTACK (Light Slash) - Detailed version from feature branch
            var slash = new ActionDef();
            slash.name = "Ronin_Slash_Light";
            slash.actionId = ActionDef.HashActionId(slash.name);
            slash.totalFrames = 19; // 4 Startup, 3 Active, 12 Recovery
            slash.frames = CreateFrames(19);
            slash.ignoreGravity = false;

            // Hitbox active frames 4-6 (3 active frames)
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
            roninActions[InputBits.ATTACK] = slash;
            _actionByHash[slash.actionId] = slash;

            // SPECIAL (Shuriken Toss) - Detailed version from feature branch
            var toss = new ActionDef();
            toss.name = "Ronin_Shuriken_Toss";
            toss.actionId = ActionDef.HashActionId(toss.name);
            toss.totalFrames = 30; // Example timeline: 5 Startup, 25 Recovery
            toss.frames = CreateFrames(30);
            toss.ignoreGravity = false;

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
            roninActions[InputBits.SPECIAL] = toss;
            _actionByHash[toss.actionId] = toss;

            // DEFEND (Block) - simpler implementation for now, just a state
            var defend = new ActionDef();
            defend.name = "Ronin_Block";
            defend.actionId = ActionDef.HashActionId(defend.name);
            defend.totalFrames = 30;
            defend.frames = CreateFrames(30);
            defend.ignoreGravity = false;
            defend.hitboxEvents = new HitboxEvent[0];
            defend.projectileSpawns = new ProjectileSpawn[0];
            roninActions[InputBits.DEFEND] = defend;
            _actionByHash[defend.actionId] = defend;

            // DASH STRIKE (Alternative special from original implementation)
            var dashStrike = new ActionDef();
            dashStrike.name = "Ronin_DashStrike";
            dashStrike.actionId = ActionDef.HashActionId(dashStrike.name);
            dashStrike.totalFrames = 40;
            dashStrike.frames = CreateFrames(40);
            dashStrike.ignoreGravity = false;

            // Add some movement to frames (root motion)
            for (int i = 5; i < 20; i++)
            {
                dashStrike.frames[i].velX = 2000 * Fx.SCALE / 1000;
            }

            dashStrike.hitboxEvents = new HitboxEvent[]
            {
                new HitboxEvent
                {
                    startFrame = 10,
                    endFrame = 25,
                    offsetX = 50 * Fx.SCALE / 1000,
                    offsetY = 0,
                    width = 120 * Fx.SCALE / 1000,
                    height = 100 * Fx.SCALE / 1000,
                    damage = 15,
                    baseKnockback = 800 * Fx.SCALE / 1000,
                    knockbackGrowth = 80,
                    hitstun = 20,
                    disjoint = 0
                }
            };
            dashStrike.projectileSpawns = new ProjectileSpawn[0];
            // Note: DashStrike not mapped to input by default, available by hash

            _library[0] = roninActions;
        }

        private static ActionFrame[] CreateFrames(int count)
        {
            var frames = new ActionFrame[count];
            for (int i = 0; i < count; i++)
            {
                frames[i] = new ActionFrame
                {
                    frameNumber = i,
                    velX = 0,
                    velY = 0,
                    cancelable = 0,
                    hitstun = 0
                };
            }
            return frames;
        }

        public static ActionDef GetAction(int archetype, InputBits input)
        {
            // Fallback: If specific archetype not found, try to use Ronin (0) or return null
            if (!_library.ContainsKey(archetype)) archetype = 0;

            if (_library.TryGetValue(archetype, out var actions))
            {
                if (actions.TryGetValue(input, out var action))
                {
                    return action;
                }
            }
            return null;
        }

        public static ActionDef GetActionByHash(int archetype, int actionHash)
        {
            // Map all to 0 for testing if not present
            if (!_library.ContainsKey(archetype)) archetype = 0;

            if (_library.TryGetValue(archetype, out var actions))
            {
                foreach (var kvp in actions)
                {
                    if (kvp.Value.actionId == actionHash) return kvp.Value;
                }
            }
            return null;
        }

        // Simple hash-based lookup for compatibility
        public static ActionDef GetAction(int actionHash)
        {
            if (_actionByHash.TryGetValue(actionHash, out var action))
            {
                return action;
            }
            return null;
        }

        public static bool TryGetAction(int hash, out ActionDef def)
        {
            return _actionByHash.TryGetValue(hash, out def);
        }

        public static void RegisterAction(ActionDef action)
        {
            if (!_actionByHash.ContainsKey(action.actionId))
            {
                _actionByHash.Add(action.actionId, action);
            }
        }
    }
}
