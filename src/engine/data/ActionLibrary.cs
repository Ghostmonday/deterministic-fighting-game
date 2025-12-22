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

        static ActionLibrary()
        {
            _library = new Dictionary<int, Dictionary<InputBits, ActionDef>>();

            InitializeRonin();
        }

        private static void InitializeRonin()
        {
            // Initialize for Archetype 0 (Ronin) - CharacterId 0
            var roninActions = new Dictionary<InputBits, ActionDef>();

            // ATTACK (Light Slash)
            var attack = new ActionDef();
            attack.name = "Ronin_LightSlash";
            attack.actionId = ActionDef.HashActionId(attack.name);
            attack.totalFrames = 20;
            attack.frames = CreateFrames(20);

            // Hitbox active frames 5-10
            attack.hitboxEvents = new HitboxEvent[]
            {
                new HitboxEvent
                {
                    startFrame = 5,
                    endFrame = 10,
                    offsetX = 50 * Fx.SCALE / 1000,
                    offsetY = 0,
                    width = 100 * Fx.SCALE / 1000,
                    height = 100 * Fx.SCALE / 1000,
                    damage = 10,
                    baseKnockback = 500 * Fx.SCALE / 1000,
                    knockbackGrowth = 50,
                    hitstun = 15,
                    disjoint = 0
                }
            };
            roninActions[InputBits.ATTACK] = attack;

            // SPECIAL (Dash Strike)
            var special = new ActionDef();
            special.name = "Ronin_DashStrike";
            special.actionId = ActionDef.HashActionId(special.name);
            special.totalFrames = 40;
            special.frames = CreateFrames(40);
            // Add some movement to frames
            for(int i=5; i<20; i++) special.frames[i].velX = 2000 * Fx.SCALE / 1000;

            special.hitboxEvents = new HitboxEvent[]
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
            roninActions[InputBits.SPECIAL] = special;

            // DEFEND (Block) - simpler implementation for now, just a state
            var defend = new ActionDef();
            defend.name = "Ronin_Block";
            defend.actionId = ActionDef.HashActionId(defend.name);
            defend.totalFrames = 30;
            defend.frames = CreateFrames(30);
            roninActions[InputBits.DEFEND] = defend;

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
            // For this task we assume we are testing with valid archetypes or map to 0 for testing.

            // Map all to 0 for testing if not present
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
                foreach(var kvp in actions)
                {
                    if (kvp.Value.actionId == actionHash) return kvp.Value;
                }
            }
            return null;
        }
    }
}
