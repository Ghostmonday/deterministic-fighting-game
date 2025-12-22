/* ================================================================================
   NEURAL DRAFT LLC | ACTION LIBRARY
================================================================================
   FILE:    ActionLibrary.cs
   CONTEXT: Hardcoded action definitions for testing/fallback.
   STATUS:  RoninSlash implemented.
================================================================================ */

namespace NeuralDraft
{
    public static class ActionLibrary
    {
        private static ActionDef _roninSlash;

        public static ActionDef RoninSlash
        {
            get
            {
                if (_roninSlash == null)
                {
                    _roninSlash = new ActionDef();
                    _roninSlash.name = "RoninSlash";
                    _roninSlash.actionId = ActionDef.HashActionId("RoninSlash");
                    _roninSlash.totalFrames = 30; // 5 startup, 10 active, 15 recovery

                    // Create frames
                    _roninSlash.frames = new ActionFrame[_roninSlash.totalFrames];
                    for(int i=0; i<_roninSlash.totalFrames; i++)
                    {
                        // Slight forward movement during startup/active
                        int velX = (i < 15) ? 300 * Fx.SCALE / 1000 : 0;

                        _roninSlash.frames[i] = new ActionFrame
                        {
                            frameNumber = i,
                            velX = velX,
                            velY = 0,
                            // Cancelable in recovery
                            cancelable = (byte)(i > 20 ? 1 : 0),
                            hitstun = 0
                        };
                    }

                    // Create hitbox
                    _roninSlash.hitboxEvents = new HitboxEvent[]
                    {
                        new HitboxEvent
                        {
                            startFrame = 5,
                            endFrame = 15,
                            offsetX = 80 * Fx.SCALE / 1000,   // Forward
                            offsetY = 100 * Fx.SCALE / 1000,  // Center height
                            width = 150 * Fx.SCALE / 1000,    // Wide slash
                            height = 100 * Fx.SCALE / 1000,
                            damage = 15,
                            baseKnockback = 500 * Fx.SCALE / 1000,
                            knockbackGrowth = 50,
                            hitstun = 30, // Frames of hitstun
                            disjoint = 0
                        }
                    };

                    _roninSlash.projectileSpawns = new ProjectileSpawn[0];
                }
                return _roninSlash;
            }
        }

        public static ActionDef GetAction(int actionHash)
        {
            if (actionHash == RoninSlash.actionId)
            {
                return RoninSlash;
            }
            return null;
        }
    }
}
