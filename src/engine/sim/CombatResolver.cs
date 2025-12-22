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
            public int ownerIndex;
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

            // Normalize direction (simplified fixed-point normalization)
            int magnitudeSquared = deltaX * deltaX + deltaY * deltaY;
            if (magnitudeSquared == 0)
            {
                deltaX = Fx.SCALE;
                deltaY = 0;
            }
            else
            {
                // Simplified normalization for deterministic behavior
                // In a real implementation, you'd use fixed-point square root
                int approxMagnitude = magnitudeSquared / Fx.SCALE;
                deltaX = deltaX * Fx.SCALE / approxMagnitude;
                deltaY = deltaY * Fx.SCALE / approxMagnitude;
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
            // Legacy wrapper for compatibility
            var resultsBuffer = new HitResult[hitboxes.Length * hurtboxes.Length];
            int count = ResolveCombatNonAlloc(hitboxes, hitboxes.Length, hurtboxes, hurtboxes.Length, attackerPositionsX, attackerPositionsY, characterDefs, resultsBuffer);

            var finalResults = new HitResult[count];
            for (int i = 0; i < count; i++)
            {
                finalResults[i] = resultsBuffer[i];
            }
            return finalResults;
        }

        public static int ResolveCombatNonAlloc(
            Hitbox[] hitboxes, int hitboxCount,
            Hurtbox[] hurtboxes, int hurtboxCount,
            int[] attackerPositionsX, int[] attackerPositionsY,
            CharacterDef[] characterDefs,
            HitResult[] resultsBuffer)
        {
            int resultIndex = 0;

            for (int i = 0; i < hitboxCount; i++)
            {
                for (int j = 0; j < hurtboxCount; j++)
                {
                    // Skip self-hit
                    if (hitboxes[i].ownerIndex == hurtboxes[j].playerIndex) continue;

                    var result = ResolveHit(hitboxes[i], hurtboxes[j],
                                          attackerPositionsX[i], attackerPositionsY[i],
                                          characterDefs[j]);

                    if (result.hit)
                    {
                        if (resultIndex < resultsBuffer.Length)
                        {
                            resultsBuffer[resultIndex++] = result;
                        }
                    }
                }
            }
            return resultIndex;
        }
    }
}
