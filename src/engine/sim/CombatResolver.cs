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

            // Check for overlap
            if (!AABB.Overlaps(hitbox.bounds, hurtbox.bounds))
                return result;

            // Prevent self-hit
            if (hitbox.ownerIndex == hurtbox.playerIndex)
                return result;

            // Calculate knockback direction (from attacker to defender)
            int centerX = (hurtbox.bounds.minX + hurtbox.bounds.maxX) / 2;
            int centerY = (hurtbox.bounds.minY + hurtbox.bounds.maxY) / 2;

            int deltaX = centerX - attackerPosX;
            int deltaY = centerY - attackerPosY;

            // Normalize direction vector (fixed-point)
            long magnitudeSquared = (long)deltaX * deltaX + (long)deltaY * deltaY;
            if (magnitudeSquared > 0)
            {
                int magnitude = FixedMath.Sqrt(magnitudeSquared);
                // Prevent division by zero if Sqrt returns 0 for non-zero input
                if (magnitude == 0) magnitude = 1;

                deltaX = (int)((long)deltaX * Fx.SCALE / magnitude);
                deltaY = (int)((long)deltaY * Fx.SCALE / magnitude);
            }
            else
            {
                // Directly above/below or same position
                deltaX = 0;
                deltaY = Fx.SCALE;
            }

            // Calculate knockback (weight-based scaling)
            int knockbackMagnitude = hitbox.baseKnockback + (hitbox.damage * hitbox.knockbackGrowth);
            int weightFactor = Fx.SCALE * 100 / (100 + defenderDef.weight); // Heavier = less knockback

            result.knockbackX = (int)((long)deltaX * knockbackMagnitude * weightFactor / (Fx.SCALE * Fx.SCALE));
            result.knockbackY = (int)((long)deltaY * knockbackMagnitude * weightFactor / (Fx.SCALE * Fx.SCALE));

            // Apply results
            result.hit = true;
            result.damageDealt = hitbox.damage;
            result.hitstun = hitbox.hitstun;
            result.hitPlayerIndex = hurtbox.playerIndex;

            return result;
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
                    if (hitboxes[i].ownerIndex == hurtboxes[j].playerIndex)
                        continue;

                    var result = ResolveHit(
                        hitboxes[i], hurtboxes[j],
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
