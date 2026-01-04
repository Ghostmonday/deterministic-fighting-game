/* ACTION LIBRARY - All 10 character actions */
namespace NeuralDraft {
    public static class ActionLibrary {
        static Dictionary<int, ActionDef> _actions = new Dictionary<int, ActionDef>();
        
        static ActionLibrary() {
            // RONIN (Fire East) - Fast, balanced
            AddCharacter("Ronin", 0, 10, 8, 50, 5, 12);
            
            // KNIGHT (Fire West) - Defensive, heavy
            AddCharacter("Knight", 1, 12, 10, 40, 4, 10);
            
            // GUARDIAN (Earth East) - Heavy, slow
            AddCharacter("Guardian", 2, 15, 12, 80, 8, 18);
            
            // TITAN (Earth West) - Tank, max HP
            AddCharacter("Titan", 3, 18, 14, 100, 10, 22);
            
            // NINJA (Venom East) - Fast, air mobility
            AddCharacter("Ninja", 4, 7, 5, 35, 3, 8);
            
            // DOCTOR (Venom West) - Slippery, meter focus
            AddCharacter("Doctor", 5, 9, 7, 45, 5, 12);
            
            // DANCER (Lightning East) - Aerial mobility
            AddCharacter("Dancer", 6, 8, 6, 40, 4, 10);
            
            // GUNSLINGER (Lightning West) - Projectile focus
            AddCharacter("Gunslinger", 7, 10, 8, 50, 5, 14);
            
            // MYSTIC (Void East) - Teleports
            AddCharacter("Mystic", 8, 12, 10, 60, 6, 16);
            
            // REAPER (Void West) - Disjointed range
            AddCharacter("Reaper", 9, 14, 11, 70, 7, 18);
        }
        
        static void AddCharacter(string name, int id, int dmg, int kb, int hs, int startup, int totalFrames) {
            // Light Attack
            var light = new ActionDef {
                name = name + "_LIGHT",
                actionId = ActionDef.HashActionId(name + "_LIGHT"),
                totalFrames = totalFrames,
                ignoreGravity = false
            };
            light.frames = CreateFrames(totalFrames, startup);
            light.hitboxEvents = new HitboxEvent[] {
                new HitboxEvent { startFrame = startup, endFrame = startup + 3, offsetX = 80, offsetY = 100, width = 100, height = 80, damage = dmg, baseKnockback = kb, knockbackGrowth = 5, hitstun = hs, disjoint = 0 }
            };
            _actions[light.actionId] = light;
            
            // Heavy Attack
            var heavy = new ActionDef {
                name = name + "_HEAVY",
                actionId = ActionDef.HashActionId(name + "_HEAVY"),
                totalFrames = totalFrames + 15,
                ignoreGravity = false
            };
            heavy.frames = CreateFrames(totalFrames + 15, startup + 5);
            heavy.hitboxEvents = new HitboxEvent[] {
                new HitboxEvent { startFrame = startup + 5, endFrame = startup + 8, offsetX = 120, offsetY = 100, width = 150, height = 100, damage = dmg * 2, baseKnockback = kb * 2, knockbackGrowth = 8, hitstun = hs + 10, disjoint = 1 }
            };
            _actions[heavy.actionId] = heavy;
            
            // Special Move
            var special = new ActionDef {
                name = name + "_SPECIAL",
                actionId = ActionDef.HashActionId(name + "_SPECIAL"),
                totalFrames = 45,
                ignoreGravity = name == "Ninja" || name == "Dancer"
            };
            special.frames = CreateFrames(45, 10);
            special.hitboxEvents = new HitboxEvent[] {
                new HitboxEvent { startFrame = 12, endFrame = 25, offsetX = 150, offsetY = 100, width = 200, height = 120, damage = dmg * 2, baseKnockback = kb * 2, knockbackGrowth = 10, hitstun = hs + 15, disjoint = 1 }
            };
            _actions[special.actionId] = special;
        }
        
        static ActionFrame[] CreateFrames(int count, int cancelStart) {
            var frames = new ActionFrame[count];
            for (int i = 0; i < count; i++) {
                frames[i] = new ActionFrame { frameNumber = i, velX = 0, velY = 0, cancelable = (byte)(i < cancelStart ? 0 : 1), hitstun = 0 };
            }
            return frames;
        }
        
        public static ActionDef GetAction(int hash) {
            _actions.TryGetValue(hash, out var action);
            return action;
        }
        
        public static ActionDef GetAction(int archetype, InputBits input) {
            string name = archetype switch {
                0 => "Ronin", 1 => "Knight", 2 => "Guardian", 3 => "Titan",
                4 => "Ninja", 5 => "Doctor", 6 => "Dancer", 7 => "Gunslinger",
                8 => "Mystic", 9 => "Reaper", _ => "Ronin"
            };
            string suffix = "";
            if ((input & InputBits.ATTACK) != 0) suffix = "_LIGHT";
            else if ((input & InputBits.SPECIAL) != 0) suffix = "_SPECIAL";
            else if ((input & InputBits.DEFEND) != 0) suffix = "_HEAVY";
            return GetAction(ActionDef.HashActionId(name + suffix));
        }
    }
}
