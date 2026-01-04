namespace NeuralDraft {
    public struct JasonMoveSet {
        public MoveDefinition lightAttack;
        public MoveDefinition mediumAttack;
        public MoveDefinition heavyAttack;
        public MoveDefinition special1;
        public MoveDefinition special2;
        public MoveDefinition super1;
        public MoveDefinition counterMove;
        
        public static JasonMoveSet GetRoninMoves() => new JasonMoveSet {
            lightAttack = new MoveDefinition { name = "Quick Slash", type = JasonMoveType.Light, startup = 5, active = 4, recovery = 8, damage = 15, meterGain = 10, knockback = 50, hitstun = 12, hitbox = new AABB { minX = -40, maxX = 60, minY = 20, maxY = 80 }, guardType = GuardType.Mid, cancelable = true, chainPriority = 1 },
            mediumAttack = new MoveDefinition { name = "Slash", type = JasonMoveType.Medium, startup = 10, active = 5, recovery = 14, damage = 25, meterGain = 15, knockback = 80, hitstun = 18, hitbox = new AABB { minX = -50, maxX = 90, minY = 10, maxY = 100 }, guardType = GuardType.Mid, cancelable = true, chainPriority = 2 },
            heavyAttack = new MoveDefinition { name = "Heavy Slash", type = JasonMoveType.Heavy, startup = 18, active = 6, recovery = 22, damage = 40, meterGain = 25, knockback = 150, hitstun = 25, hitbox = new AABB { minX = -60, maxX = 120, minY = 0, maxY = 120 }, guardType = GuardType.High, cancelable = true, chainPriority = 3 },
            special1 = new MoveDefinition { name = "Flame Strike", type = JasonMoveType.Special, startup = 12, active = 15, recovery = 20, damage = 50, meterGain = 30, knockback = 100, hitstun = 30, hitbox = new AABB { minX = -80, maxX = 150, minY = -20, maxY = 80 }, guardType = GuardType.Mid, cancelable = false, chainPriority = 5, isProjectile = true, projectileSpeed = 1500, projectileLifetime = 60 },
            special2 = new MoveDefinition { name = "Dragon Dash", type = JasonMoveType.Special, startup = 8, active = 20, recovery = 15, damage = 35, meterGain = 25, knockback = 80, hitstun = 20, hitbox = new AABB { minX = -30, maxX = 100, minY = 10, maxY = 60 }, guardType = GuardType.Mid, cancelable = false, chainPriority = 5, isInvincible = true, invincibleStartup = 5, invincibleActive = 15 },
            super1 = new MoveDefinition { name = "Ronin Super", type = JasonMoveType.Super, startup = 20, active = 10, recovery = 40, damage = 100, meterGain = 50, knockback = 300, hitstun = 50, hitbox = new AABB { minX = -100, maxX = 200, minY = -30, maxY = 150 }, guardType = GuardType.Unblockable, cancelable = false, chainPriority = 10, meterCost = 100 }
        };
        
        public static JasonMoveSet GetKnightMoves() => new JasonMoveSet {
            lightAttack = new MoveDefinition { name = "Shield Poke", type = JasonMoveType.Light, startup = 6, active = 4, recovery = 10, damage = 12, meterGain = 12, knockback = 40, hitstun = 10, hitbox = new AABB { minX = -30, maxX = 50, minY = 20, maxY = 70 }, guardType = GuardType.Mid, cancelable = true, chainPriority = 1 },
            mediumAttack = new MoveDefinition { name = "Shield Bash", type = JasonMoveType.Medium, startup = 11, active = 5, recovery = 16, damage = 22, meterGain = 18, knockback = 70, hitstun = 15, hitbox = new AABB { minX = -40, maxX = 80, minY = 10, maxY = 90 }, guardType = GuardType.Mid, cancelable = true, chainPriority = 2, guardBreak = true },
            heavyAttack = new MoveDefinition { name = "Knights Justice", type = JasonMoveType.Heavy, startup = 20, active = 6, recovery = 25, damage = 45, meterGain = 30, knockback = 180, hitstun = 22, hitbox = new AABB { minX = -70, maxX = 130, minY = 0, maxY = 130 }, guardType = GuardType.High, cancelable = true, chainPriority = 3 },
            special1 = new MoveDefinition { name = "Shield Wall", type = JasonMoveType.Special, startup = 5, active = 30, recovery = 25, damage = 20, meterGain = 35, knockback = 50, hitstun = 15, hitbox = new AABB { minX = -50, maxX = 60, minY = 0, maxY = 100 }, guardType = GuardType.All, cancelable = false, chainPriority = 5, isParry = true, parryWindow = 10 },
            counterMove = new MoveDefinition { name = "Counter Stance", type = JasonMoveType.Counter, startup = 3, active = 15, recovery = 20, damage = 60, meterGain = 40, knockback = 200, hitstun = 35, hitbox = new AABB { minX = -60, maxX = 60, minY = 0, maxY = 120 }, guardType = GuardType.None, cancelable = false, chainPriority = 8, isCounter = true, counterType = CounterType.WhiffPunish },
            super1 = new MoveDefinition { name = "Knight Super", type = JasonMoveType.Super, startup = 30, active = 15, recovery = 50, damage = 120, meterGain = 50, knockback = 350, hitstun = 60, hitbox = new AABB { minX = -120, maxX = 250, minY = -50, maxY = 180 }, guardType = GuardType.Unblockable, cancelable = false, chainPriority = 10, meterCost = 100 }
        };
    }
}
