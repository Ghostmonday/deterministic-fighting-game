/* ================================================================================
   NEURAL DRAFT LLC | CHARACTER DEFINITIONS (EAST/WEST ROSTER)
================================================================================
   FILE:    CharacterDef.cs
   CONTEXT: Configuration data for the 10 Elemental Archetypes.
   STATUS:  Updated for Fire, Earth, Venom, Lightning, Void (East/West variants).
================================================================================ */

namespace NeuralDraft
{
    public struct CharacterDef
    {
        // --- Identity ---
        public int characterId;
        public string name;
        public byte archetype; // 0-9 mapping to the Matrix

        // --- Dimensions (Fixed-Point) ---
        public int hitboxWidth;
        public int hitboxHeight;
        public int hitboxOffsetY;

        // --- Movement Physics ---
        public int weight;           // Knockback resistance
        public int walkSpeed;        // Base movement
        public int runSpeed;         // Sprint speed
        public int jumpForce;        // Initial Y velocity
        public int airSpeed;         // Air control acceleration
        public int gravity;          // Gravity per frame
        public int maxFallSpeed;     // Terminal velocity
        public int groundFriction;   // Deceleration on ground
        public int airFriction;      // Deceleration in air

        // --- Combat Stats ---
        public int baseHealth;
        public int fastFallSpeed;
        public int weightFactorBase; // For knockback formula
        public int hitstunMultiplier;

        // ========================================================================
        // FACTORY METHODS
        // ========================================================================

        public static CharacterDef GetDefault(byte archetypeId)
        {
            return archetypeId switch
            {
                // --- FIRE (The Anchor) ---
                0 => CreateRonin(),      // East
                1 => CreateKnight(),     // West

                // --- EARTH (The Heavy) ---
                2 => CreateGuardian(),   // East
                3 => CreateTitan(),      // West

                // --- VENOM (The Speed) ---
                4 => CreateNinja(),      // East
                5 => CreateDoctor(),     // West

                // --- LIGHTNING (The Ranged) ---
                6 => CreateDancer(),     // East
                7 => CreateGunslinger(), // West

                // --- VOID (The Specialist) ---
                8 => CreateMystic(),     // East
                9 => CreateReaper(),     // West

                _ => CreateRonin()       // Fallback
            };
        }

        // 1. FIRE EAST: The Ronin (Balanced/Offense)
        private static CharacterDef CreateRonin() => new CharacterDef
        {
            characterId = 0,
            name = "Ronin",
            archetype = 0,
            hitboxWidth = 100 * Fx.SCALE / 1000,
            hitboxHeight = 200 * Fx.SCALE / 1000,
            hitboxOffsetY = 0,
            weight = 100,
            baseHealth = 100,
            walkSpeed = 800 * Fx.SCALE / 1000,
            runSpeed = 1200 * Fx.SCALE / 1000,
            jumpForce = 1500 * Fx.SCALE / 1000,
            airSpeed = 600 * Fx.SCALE / 1000,
            gravity = 45,
            maxFallSpeed = 2200 * Fx.SCALE / 1000,
            groundFriction = 150,
            airFriction = 20,
            fastFallSpeed = 3200 * Fx.SCALE / 1000,
            weightFactorBase = 100,
            hitstunMultiplier = 1000
        };

        // 2. FIRE WEST: The Knight (Balanced/Defense) - Heavier, slower
        private static CharacterDef CreateKnight() => new CharacterDef
        {
            characterId = 1,
            name = "Knight",
            archetype = 1,
            hitboxWidth = 110 * Fx.SCALE / 1000,
            hitboxHeight = 210 * Fx.SCALE / 1000,
            hitboxOffsetY = 0,
            weight = 115,
            baseHealth = 110,
            walkSpeed = 700 * Fx.SCALE / 1000,
            runSpeed = 1000 * Fx.SCALE / 1000,
            jumpForce = 1400 * Fx.SCALE / 1000,
            airSpeed = 500 * Fx.SCALE / 1000,
            gravity = 50,
            maxFallSpeed = 2300 * Fx.SCALE / 1000,
            groundFriction = 200,
            airFriction = 25,
            fastFallSpeed = 3300 * Fx.SCALE / 1000,
            weightFactorBase = 110,
            hitstunMultiplier = 950 // Recover slightly faster
        };

        // 3. EARTH EAST: The Guardian (Sumo) - Impossible to push
        private static CharacterDef CreateGuardian() => new CharacterDef
        {
            characterId = 2,
            name = "Guardian",
            archetype = 2,
            hitboxWidth = 140 * Fx.SCALE / 1000,
            hitboxHeight = 220 * Fx.SCALE / 1000,
            hitboxOffsetY = 0,
            weight = 140,
            baseHealth = 130,
            walkSpeed = 500 * Fx.SCALE / 1000,
            runSpeed = 800 * Fx.SCALE / 1000,
            jumpForce = 1100 * Fx.SCALE / 1000,
            airSpeed = 300 * Fx.SCALE / 1000,
            gravity = 60,
            maxFallSpeed = 2500 * Fx.SCALE / 1000,
            groundFriction = 600, // MASSIVE FRICTION
            airFriction = 100,
            fastFallSpeed = 3500 * Fx.SCALE / 1000,
            weightFactorBase = 150,
            hitstunMultiplier = 1000
        };

        // 4. EARTH WEST: The Titan (Golem) - Massive Health
        private static CharacterDef CreateTitan() => new CharacterDef
        {
            characterId = 3,
            name = "Titan",
            archetype = 3,
            hitboxWidth = 150 * Fx.SCALE / 1000,
            hitboxHeight = 250 * Fx.SCALE / 1000,
            hitboxOffsetY = 0,
            weight = 150,
            baseHealth = 150, // HIGHEST HEALTH
            walkSpeed = 450 * Fx.SCALE / 1000,
            runSpeed = 750 * Fx.SCALE / 1000,
            jumpForce = 1000 * Fx.SCALE / 1000,
            airSpeed = 250 * Fx.SCALE / 1000,
            gravity = 65,
            maxFallSpeed = 2600 * Fx.SCALE / 1000,
            groundFriction = 200,
            airFriction = 50,
            fastFallSpeed = 3600 * Fx.SCALE / 1000,
            weightFactorBase = 160,
            hitstunMultiplier = 1000
        };

        // 5. VENOM EAST: The Ninja - Fast, Wall Jumps
        private static CharacterDef CreateNinja() => new CharacterDef
        {
            characterId = 4,
            name = "Ninja",
            archetype = 4,
            hitboxWidth = 80 * Fx.SCALE / 1000,
            hitboxHeight = 180 * Fx.SCALE / 1000,
            hitboxOffsetY = 0,
            weight = 80,
            baseHealth = 85, // LOW HEALTH
            walkSpeed = 1100 * Fx.SCALE / 1000,
            runSpeed = 1600 * Fx.SCALE / 1000,
            jumpForce = 1700 * Fx.SCALE / 1000,
            airSpeed = 800 * Fx.SCALE / 1000,
            gravity = 40,
            maxFallSpeed = 2000 * Fx.SCALE / 1000,
            groundFriction = 150,
            airFriction = 15,
            fastFallSpeed = 3000 * Fx.SCALE / 1000,
            weightFactorBase = 80,
            hitstunMultiplier = 1000
        };

        // 6. VENOM WEST: The Plague Doctor - Slippery
        private static CharacterDef CreateDoctor() => new CharacterDef
        {
            characterId = 5,
            name = "Plague Doctor",
            archetype = 5,
            hitboxWidth = 90 * Fx.SCALE / 1000,
            hitboxHeight = 190 * Fx.SCALE / 1000,
            hitboxOffsetY = 0,
            weight = 90,
            baseHealth = 95,
            walkSpeed = 900 * Fx.SCALE / 1000,
            runSpeed = 1300 * Fx.SCALE / 1000,
            jumpForce = 1500 * Fx.SCALE / 1000,
            airSpeed = 700 * Fx.SCALE / 1000,
            gravity = 42,
            maxFallSpeed = 2100 * Fx.SCALE / 1000,
            groundFriction = 20, // SLIPPERY (Low Friction)
            airFriction = 20,
            fastFallSpeed = 3100 * Fx.SCALE / 1000,
            weightFactorBase = 90,
            hitstunMultiplier = 1000
        };

        // 7. LIGHTNING EAST: Storm Dancer - Aerial Mobility
        private static CharacterDef CreateDancer() => new CharacterDef
        {
            characterId = 6,
            name = "Storm Dancer",
            archetype = 6,
            hitboxWidth = 90 * Fx.SCALE / 1000,
            hitboxHeight = 190 * Fx.SCALE / 1000,
            hitboxOffsetY = 0,
            weight = 85,
            baseHealth = 90,
            walkSpeed = 1000 * Fx.SCALE / 1000,
            runSpeed = 1400 * Fx.SCALE / 1000,
            jumpForce = 1800 * Fx.SCALE / 1000,
            airSpeed = 1200 * Fx.SCALE / 1000, // HIGH AIR CONTROL
            gravity = 35, // FLOATY
            maxFallSpeed = 1900 * Fx.SCALE / 1000,
            groundFriction = 150,
            airFriction = 10,
            fastFallSpeed = 2900 * Fx.SCALE / 1000,
            weightFactorBase = 85,
            hitstunMultiplier = 1000
        };

        // 8. LIGHTNING WEST: Gunslinger - Standard
        private static CharacterDef CreateGunslinger() => new CharacterDef
        {
            characterId = 7,
            name = "Gunslinger",
            archetype = 7,
            hitboxWidth = 100 * Fx.SCALE / 1000,
            hitboxHeight = 200 * Fx.SCALE / 1000,
            hitboxOffsetY = 0,
            weight = 95,
            baseHealth = 100,
            walkSpeed = 950 * Fx.SCALE / 1000,
            runSpeed = 1350 * Fx.SCALE / 1000,
            jumpForce = 1500 * Fx.SCALE / 1000,
            airSpeed = 650 * Fx.SCALE / 1000,
            gravity = 45,
            maxFallSpeed = 2200 * Fx.SCALE / 1000,
            groundFriction = 150,
            airFriction = 20,
            fastFallSpeed = 3200 * Fx.SCALE / 1000,
            weightFactorBase = 100,
            hitstunMultiplier = 1000
        };

        // 9. VOID EAST: The Mystic - Standard
        private static CharacterDef CreateMystic() => new CharacterDef
        {
            characterId = 8,
            name = "Mystic",
            archetype = 8,
            hitboxWidth = 95 * Fx.SCALE / 1000,
            hitboxHeight = 195 * Fx.SCALE / 1000,
            hitboxOffsetY = 0,
            weight = 90,
            baseHealth = 95,
            walkSpeed = 900 * Fx.SCALE / 1000,
            runSpeed = 1250 * Fx.SCALE / 1000,
            jumpForce = 1600 * Fx.SCALE / 1000,
            airSpeed = 700 * Fx.SCALE / 1000,
            gravity = 40,
            maxFallSpeed = 2100 * Fx.SCALE / 1000,
            groundFriction = 150,
            airFriction = 20,
            fastFallSpeed = 3100 * Fx.SCALE / 1000,
            weightFactorBase = 90,
            hitstunMultiplier = 1000
        };

        // 10. VOID WEST: The Reaper - Disjointed
        private static CharacterDef CreateReaper() => new CharacterDef
        {
            characterId = 9,
            name = "Reaper",
            archetype = 9,
            hitboxWidth = 110 * Fx.SCALE / 1000,
            hitboxHeight = 210 * Fx.SCALE / 1000,
            hitboxOffsetY = 0,
            weight = 105,
            baseHealth = 105,
            walkSpeed = 850 * Fx.SCALE / 1000,
            runSpeed = 1150 * Fx.SCALE / 1000,
            jumpForce = 1450 * Fx.SCALE / 1000,
            airSpeed = 550 * Fx.SCALE / 1000,
            gravity = 48,
            maxFallSpeed = 2300 * Fx.SCALE / 1000,
            groundFriction = 180,
            airFriction = 22,
            fastFallSpeed = 3300 * Fx.SCALE / 1000,
            weightFactorBase = 105,
            hitstunMultiplier = 1000
        };

        // Copy utility
        public void CopyTo(ref CharacterDef dest)
        {
            dest.characterId = characterId;
            dest.name = name;
            dest.archetype = archetype;
            dest.hitboxWidth = hitboxWidth;
            dest.hitboxHeight = hitboxHeight;
            dest.hitboxOffsetY = hitboxOffsetY;
            dest.weight = weight;
            dest.walkSpeed = walkSpeed;
            dest.runSpeed = runSpeed;
            dest.jumpForce = jumpForce;
            dest.airSpeed = airSpeed;
            dest.gravity = gravity;
            dest.maxFallSpeed = maxFallSpeed;
            dest.groundFriction = groundFriction;
            dest.airFriction = airFriction;
            dest.baseHealth = baseHealth;
            dest.fastFallSpeed = fastFallSpeed;
            dest.weightFactorBase = weightFactorBase;
            dest.hitstunMultiplier = hitstunMultiplier;
        }
    }
}
