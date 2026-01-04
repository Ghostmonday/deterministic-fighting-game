/* COMBAT HARDENING - Throws, meter, reversals, cross-ups */
namespace NeuralDraft {
    public static class CombatHardening {
        
        // THROW SYSTEM
        public static ThrowResult ResolveThrow(PlayerState attacker, PlayerState defender, CharacterDef attackerDef, CharacterDef defenderDef, bool isGroundThrow) {
            var result = new ThrowResult { hit = false };
            
            // Check throw range
            int dist = System.Math.Abs(attacker.posX - defender.posX);
            int range = isGroundThrow ? 60 : 50;
            
            if (dist > range) return result;
            if (defender.grounded == 0 && isGroundThrow) return result;
            
            // Check tech window (8 frames)
            if (defender.hitstunRemaining > 0 && defender.hitstunRemaining < 8) {
                result.teched = true;
                return result;
            }
            
            result.hit = true;
            result.damage = isGroundThrow ? 80 : 60;
            result.knockbackX = (defender.posX > attacker.posX ? 1 : -1) * 100;
            result.knockbackY = isGroundThrow ? 50 : 30;
            result.hitstun = isGroundThrow ? 25 : 20;
            
            return result;
        }
        
        // METER SYSTEM
        public static void AddMeter(PlayerState p, int amount, int maxMeter) {
            p.meter += amount;
            if (p.meter > maxMeter) p.meter = maxMeter;
        }
        
        public static bool SpendMeter(PlayerState p, int cost) {
            if (p.meter >= cost) { p.meter -= cost; return true; }
            return false;
        }
        
        // REVERSAL SYSTEM
        public static bool IsReversalFrame(PlayerState p, int reversalStartup) {
            return p.hitstunRemaining == reversalStartup;
        }
        
        // CROSS-UP DETECTION
        public static bool IsCrossUp(PlayerState attacker, PlayerState defender, int hitboxMinX, int hitboxMaxX) {
            int attackerCenter = attacker.posX;
            int defenderCenter = defender.posX;
            int hitboxCenter = (hitboxMinX + hitboxMaxX) / 2;
            
            // Cross-up if hitbox center is on opposite side of defender
            return (attackerCenter > defenderCenter && hitboxCenter < defenderCenter) ||
                   (attackerCenter < defenderCenter && hitboxCenter > defenderCenter);
        }
        
        // HITBOX AMBIGUITY (Cross-up potential)
        public static int CalculateCrossUpRisk(PlayerState attacker, PlayerState defender, int jumpArc) {
            // Higher jumps = more cross-up potential
            return System.Math.Abs(jumpArc) > 100 ? 1 : 0;
        }
    }
    
    public struct ThrowResult {
        public bool hit;
        public bool teched;
        public int damage;
        public int knockbackX;
        public int knockbackY;
        public int hitstun;
    }
    
    public static class PlayerStateExtensions {
        public static int meter;
        public static int superCharge;
        
        public static void ResetMeter(this PlayerState p) { p.meter = 0; p.superCharge = 0; }
        public static bool CanSuper(this PlayerState p, int cost) => p.meter >= cost;
    }
}
