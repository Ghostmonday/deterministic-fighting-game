/* ================================================================================
   NEURAL DRAFT LLC | DEEPSEEK INSTRUCTION HEADER
================================================================================
   FILE:    CharacterDef.cs
   CONTEXT: Character configuration data for deterministic simulation.

   TASK:
   Create a struct 'CharacterDef' containing configurable character properties:
   hitbox width/height, weight, walk speed, jump force, etc. All values must use
   fixed-point math (Fx.SCALE).

   CONSTRAINTS:
   - Use Fixed-Point Math (Fx.SCALE = 1000) for all physics.
   - No Unity Engine references in this file.
   - Strict Determinism: No floats, no random execution order.
   - Must support Elemental/Titan vs Ninja character archetypes.
================================================================================

*/
namespace NeuralDraft
{
    public struct CharacterDef
    {
        // Basic character properties
        public int characterId;
        public string name;

        // Hitbox dimensions (fixed-point units)
        public int hitboxWidth;      // Horizontal size
        public int hitboxHeight;     // Vertical size
        public int hitboxOffsetY;    // Vertical offset from position (typically 0)

        // Physics properties (fixed-point units per frame)
        public int weight;           // Affects knockback resistance (higher = heavier)
        public int walkSpeed;        // Horizontal movement speed
        public int runSpeed;         // Optional: faster horizontal movement
        public int jumpForce;        // Initial upward velocity when jumping
        public int airSpeed;         // Horizontal control in air
        public int gravity;          // Character-specific gravity (default = 45)
        public int maxFallSpeed;     // Terminal velocity

        // Combat properties
        public int baseHealth;       // Starting health
        public int fastFallSpeed;    // Optional: faster falling when pressing down

        // Character archetype flags
        public byte archetype;       // 0 = Standard, 1 = Titan, 2 = Ninja, 3 = Elemental

        // Default character definitions
        public static CharacterDef CreateTitan()
        {
            return new CharacterDef
            {
                characterId = 1,
                name = "Titan",
                hitboxWidth = 120 * Fx.SCALE / 1000,    // 120 units wide
                hitboxHeight = 250 * Fx.SCALE / 1000,   // 250 units tall
                hitboxOffsetY = 0,
                weight = 120,                           // Very heavy
                walkSpeed = 600 * Fx.SCALE / 1000,      // Slower movement
                runSpeed = 900 * Fx.SCALE / 1000,
                jumpForce = 1200 * Fx.SCALE / 1000,     // Lower jump
                airSpeed = 400 * Fx.SCALE / 1000,
                gravity = 50,                           // Slightly heavier gravity
                maxFallSpeed = 2500 * Fx.SCALE / 1000,
                baseHealth = 120,
                fastFallSpeed = 3500 * Fx.SCALE / 1000,
                archetype = 1
            };
        }

        public static CharacterDef CreateNinja()
        {
            return new CharacterDef
            {
                characterId = 2,
                name = "Ninja",
                hitboxWidth = 80 * Fx.SCALE / 1000,     // 80 units wide
                hitboxHeight = 180 * Fx.SCALE / 1000,   // 180 units tall
                hitboxOffsetY = 0,
                weight = 80,                            // Light
                walkSpeed = 1000 * Fx.SCALE / 1000,     // Faster movement
                runSpeed = 1400 * Fx.SCALE / 1000,
                jumpForce = 1800 * Fx.SCALE / 1000,     // Higher jump
                airSpeed = 800 * Fx.SCALE / 1000,
                gravity = 40,                           // Lighter gravity
                maxFallSpeed = 2000 * Fx.SCALE / 1000,
                baseHealth = 80,
                fastFallSpeed = 3000 * Fx.SCALE / 1000,
                archetype = 2
            };
        }

        public static CharacterDef CreateElemental()
        {
            return new CharacterDef
            {
                characterId = 3,
                name = "Elemental",
                hitboxWidth = 100 * Fx.SCALE / 1000,    // 100 units wide
                hitboxHeight = 220 * Fx.SCALE / 1000,   // 220 units tall
                hitboxOffsetY = 0,
                weight = 90,                            // Medium weight
                walkSpeed = 800 * Fx.SCALE / 1000,      // Standard movement
                runSpeed = 1100 * Fx.SCALE / 1000,
                jumpForce = 1500 * Fx.SCALE / 1000,     // Standard jump
                airSpeed = 600 * Fx.SCALE / 1000,
                gravity = 45,                           // Standard gravity
                maxFallSpeed = 2200 * Fx.SCALE / 1000,
                baseHealth = 100,
                fastFallSpeed = 3200 * Fx.SCALE / 1000,
                archetype = 3
            };
        }

        // Helper method to get default character by archetype
        public static CharacterDef GetDefault(byte archetype)
        {
            return archetype switch
            {
                1 => CreateTitan(),
                2 => CreateNinja(),
                3 => CreateElemental(),
                _ => CreateElemental() // Default to Elemental
            };
        }

        // Copy method for deterministic state copying
        public void CopyTo(ref CharacterDef dest)
        {
            dest.characterId = characterId;
            dest.name = name;
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
            dest.baseHealth = baseHealth;
            dest.fastFallSpeed = fastFallSpeed;
            dest.archetype = archetype;
        }
    }
}
