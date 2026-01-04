/* ACTION LIBRARY - Character actions */
namespace NeuralDraft {
    public static class ActionLibrary {
        static Dictionary<int, ActionDef> _actions = new Dictionary<int, ActionDef>();
        
        static ActionLibrary() {
            // Ronin Light Attack
            var slash = new ActionDef {
                name = "Ronin_ATTACK",
                actionId = ActionDef.HashActionId("Ronin_ATTACK"),
                totalFrames = 19,
                ignoreGravity = false
            };
            slash.frames = new ActionFrame[19];
            for (int i = 0; i < 19; i++) {
                slash.frames[i] = new ActionFrame { frameNumber = i, velX = 0, velY = 0, cancelable = (byte)(i < 5 ? 0 : 1), hitstun = 0 };
            }
            slash.hitboxEvents = new HitboxEvent[] {
                new HitboxEvent { startFrame = 4, endFrame = 7, offsetX = 80, offsetY = 100, width = 100, height = 80, damage = 10, baseKnockback = 50, knockbackGrowth = 5, hitstun = 12, disjoint = 0 }
            };
            _actions[slash.actionId] = slash;
            
            // Ronin Special
            var special = new ActionDef {
                name = "Ronin_SPECIAL",
                actionId = ActionDef.HashActionId("Ronin_SPECIAL"),
                totalFrames = 40,
                ignoreGravity = false
            };
            special.frames = new ActionFrame[40];
            for (int i = 0; i < 40; i++) {
                special.frames[i] = new ActionFrame { frameNumber = i, velX = 0, velY = 0, cancelable = 0, hitstun = 0 };
            }
            special.hitboxEvents = new HitboxEvent[] {
                new HitboxEvent { startFrame = 10, endFrame = 20, offsetX = 120, offsetY = 100, width = 150, height = 100, damage = 25, baseKnockback = 100, knockbackGrowth = 10, hitstun = 20, disjoint = 1 }
            };
            _actions[special.actionId] = special;
            
            // Knight Light Attack
            var kSlash = new ActionDef {
                name = "Knight_ATTACK",
                actionId = ActionDef.HashActionId("Knight_ATTACK"),
                totalFrames = 20,
                ignoreGravity = false
            };
            kSlash.frames = new ActionFrame[20];
            for (int i = 0; i < 20; i++) {
                kSlash.frames[i] = new ActionFrame { frameNumber = i, velX = 0, velY = 0, cancelable = (byte)(i < 6 ? 0 : 1), hitstun = 0 };
            }
            kSlash.hitboxEvents = new HitboxEvent[] {
                new HitboxEvent { startFrame = 5, endFrame = 8, offsetX = 90, offsetY = 105, width = 110, height = 85, damage = 12, baseKnockback = 40, knockbackGrowth = 4, hitstun = 10, disjoint = 0 }
            };
            _actions[kSlash.actionId] = kSlash;
            
            // Knight Special (Guard)
            var kSpecial = new ActionDef {
                name = "Knight_SPECIAL",
                actionId = ActionDef.HashActionId("Knight_SPECIAL"),
                totalFrames = 30,
                ignoreGravity = false
            };
            kSpecial.frames = new ActionFrame[30];
            for (int i = 0; i < 30; i++) {
                kSpecial.frames[i] = new ActionFrame { frameNumber = i, velX = 0, velY = 0, cancelable = 0, hitstun = 0 };
            }
            kSpecial.hitboxEvents = new HitboxEvent[0];
            _actions[kSpecial.actionId] = kSpecial;
        }
        
        public static ActionDef GetAction(int hash) {
            _actions.TryGetValue(hash, out var action);
            return action;
        }
        
        public static ActionDef GetAction(int archetype, InputBits input) {
            string suffix = "";
            if ((input & InputBits.ATTACK) != 0) suffix = "_ATTACK";
            else if ((input & InputBits.SPECIAL) != 0) suffix = "_SPECIAL";
            else if ((input & InputBits.DEFEND) != 0) suffix = "_SPECIAL";
            
            string name = archetype == 0 ? "Ronin" : "Knight";
            return GetAction(ActionDef.HashActionId(name + suffix));
        }
    }
}
