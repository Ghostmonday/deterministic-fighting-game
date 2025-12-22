/* ================================================================================
   NEURAL DRAFT LLC | DEEPSEEK INSTRUCTION HEADER
================================================================================
   FILE:    CombatResolver.cs
   CONTEXT: Hit resolution.

   TASK:
   Implement hit resolution. Support Hitbox vs Hurtbox, Disjoint weapons, Trading hits, and Weight-based knockback. Pure logic, no VFX.

   CONSTRAINTS:
   - Use Fixed-Point Math (Fx.SCALE = 1000) for all physics.
   - No Unity Engine references in this file (unless specified in Bridge).
   - Strict Determinism: No floats, no random execution order.
================================================================================

*/
namespace NeuralDraft
{
    public static class CombatResolver
    {
        public struct Hitbox
        {
            public AABB bounds;
            public int damage;
            public int baseKnockback;
            public int knockbackGrowth;
            public int hitstun;
            public byte disjoint;
        }

        public struct Hurtbox
        {
            public AABB bounds;
            public int weight;
            public int playerIndex;
        }

        public struct HitResult
        {
            public bool hit;
            public int damageDealt;
            public int knockbackX;
            public int knockbackY;
            public int hitstun;
            public int hitPlayerIndex;
        }

        public static HitResult ResolveHit(Hitbox hitbox, Hurtbox hurtbox, int attackerPosX, int attackerPosY, CharacterDef defenderDef)
        {
            var result = new HitResult { hit = false };

            if (!AABB.Overlaps(hitbox.bounds, hurtbox.bounds))
            {
                return result;
            }

            // Calculate knockback based on damage, weight, and knockback stats
            int knockbackScalar = hitbox.baseKnockback + (hitbox.damage * hitbox.knockbackGrowth / Fx.SCALE);

            // Weight scaling: heavier characters take less knockback
            int weightFactor = Fx.SCALE * defenderDef.weightFactorBase / (hurtbox.weight + defenderDef.weightFactorBase);
            knockbackScalar = knockbackScalar * weightFactor / Fx.SCALE;

            // Determine knockback direction (simplified - always away from attacker)
            int dirX = hurtbox.bounds.minX + (hurtbox.bounds.maxX - hurtbox.bounds.minX) / 2;
            int dirY = hurtbox.bounds.minY + (hurtbox.bounds.maxY - hurtbox.bounds.minY) / 2;

            int deltaX = dirX - attackerPosX;
            int deltaY = dirY - attackerPosY;

            // Normalize direction (fixed-point normalization)
            long magnitudeSquared = (long)deltaX * deltaX + (long)deltaY * deltaY;
            if (magnitudeSquared == 0)
            {
                deltaX = Fx.SCALE;
                deltaY = 0;
            }
            else
            {
                int magnitude = Sqrt(magnitudeSquared);
                // Prevent division by zero if Sqrt returns 0 for non-zero input
                if (magnitude == 0) magnitude = 1;

                deltaX = (int)((long)deltaX * Fx.SCALE / magnitude);
                deltaY = (int)((long)deltaY * Fx.SCALE / magnitude);
            }

            result.hit = true;
            result.damageDealt = hitbox.damage;
            result.knockbackX = deltaX * knockbackScalar / Fx.SCALE;
            result.knockbackY = deltaY * knockbackScalar / Fx.SCALE;
            result.hitstun = hitbox.hitstun * defenderDef.hitstunMultiplier / Fx.SCALE;
            result.hitPlayerIndex = hurtbox.playerIndex;

            return result;
        }

        public static HitResult[] ResolveCombat(Hitbox[] hitboxes, Hurtbox[] hurtboxes, int[] attackerPositionsX, int[] attackerPositionsY, CharacterDef[] characterDefs)
        {
            var results = new HitResult[hitboxes.Length * hurtboxes.Length];
            int resultIndex = 0;

            for (int i = 0; i < hitboxes.Length; i++)
            {
                for (int j = 0; j < hurtboxes.Length; j++)
                {
                    // Skip self-hit (attacker hitting themselves)
                    if (i == j) continue;

                    var result = ResolveHit(hitboxes[i], hurtboxes[j],
                                          attackerPositionsX[i], attackerPositionsY[i],
                                          characterDefs[j]);

                    if (result.hit)
                    {
                        results[resultIndex++] = result;
                    }
                }
            }

            // Resize array to actual results
            var finalResults = new HitResult[resultIndex];
            for (int i = 0; i < resultIndex; i++)
            {
                finalResults[i] = results[i];
            }

            return finalResults;
        }

        private static int Sqrt(long n)
        {
            if (n <= 0) return 0;

            long x = n;
            long y = (x + 1) / 2;
            while (y < x)
            {
                x = y;
                y = (x + n / x) / 2;
            }
            return (int)x;
        }
    }
}
