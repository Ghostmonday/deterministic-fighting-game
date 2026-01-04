/* STAGE ENVIRONMENT - Destructible walls, lighting, reactive objects */
namespace NeuralDraft.Environment {
    public class StageEnvironment {
        public DestructibleWall[] walls = new DestructibleWall[16];
        public DynamicLight[] lights = new DynamicLight[8];
        public ReactiveObject[] objects = new ReactiveObject[32];
        public Platform[] platforms = new Platform[8];
        public int currentFrame;
        public uint stateHash;
        
        public StageEnvironment(int width, int height) {
            currentFrame = 0;
            for (int i = 0; i < 16; i++) walls[i] = DestructibleWall.Empty(i);
            for (int i = 0; i < 8; i++) lights[i] = DynamicLight.Empty(i);
            for (int i = 0; i < 32; i++) objects[i] = ReactiveObject.Empty(i);
            for (int i = 0; i < 8; i++) platforms[i] = Platform.Empty(i);
        }
        
        public void Update(int frame) {
            currentFrame = frame;
            for (int i = 0; i < 16; i++) walls[i].Update(frame);
            for (int i = 0; i < 8; i++) lights[i].Update(frame);
            for (int i = 0; i < 32; i++) objects[i].Update(frame);
            for (int i = 0; i < 8; i++) platforms[i].Update(frame);
            stateHash = ComputeHash();
        }
        
        public uint ComputeHash() {
            uint h = 2166136261;
            h = FNV(h, (uint)currentFrame);
            for (int i = 0; i < 16; i++) h = FNV(h, walls[i].ComputeHash());
            for (int i = 0; i < 8; i++) h = FNV(h, lights[i].ComputeHash());
            for (int i = 0; i < 32; i++) h = FNV(h, objects[i].ComputeHash());
            return h;
        }
        
        static uint FNV(uint h, uint v) { h ^= v; h *= 16777619; return h; }
    }
    
    public struct DestructibleWall {
        public int id; public bool active; public bool destroyed;
        public int x, y, w, h, health, maxHealth;
        public int respawnTimer, respawnTime;
        public static DestructibleWall Empty(int i) => new DestructibleWall { id = i, active = false };
        public static DestructibleWall Create(int i, int X, int Y, int W, int H, int hp, int respawn) {
            return new DestructibleWall { id = i, active = true, x = X, y = Y, w = W, h = H, health = hp, maxHealth = hp, respawnTime = respawn };
        }
        public void Update(int frame) {
            if (destroyed && respawnTimer > 0) {
                respawnTimer--;
                if (respawnTimer <= 0) { destroyed = false; health = maxHealth; }
            }
        }
        public void TakeDamage(int dmg) {
            if (!active || destroyed) return;
            health -= dmg;
            if (health <= 0) { destroyed = true; respawnTimer = respawnTime; }
        }
        public uint ComputeHash() {
            uint h = 2166136261; h ^= (uint)id; h *= 16777619;
            h ^= (uint)(active ? 1 : 0); h *= 16777619;
            h ^= (uint)(destroyed ? 1 : 0); h *= 16777619;
            h ^= (uint)health; return h;
        }
    }
    
    public struct DynamicLight {
        public int id; public bool active;
        public int x, y, radius, intensity;
        public int colorR, colorG, colorB;
        public LightType type; public int animPhase;
        public static DynamicLight Empty(int i) => new DynamicLight { id = i, active = false };
        public static DynamicLight Create(int i, int X, int Y, int R, int I, byte[] C, LightType T) {
            return new DynamicLight { id = i, active = true, x = X, y = Y, radius = R, intensity = I, colorR = C[0], colorG = C[1], colorB = C[2], type = T };
        }
        public void Update(int frame) {
            if (!active) return;
            animPhase = frame % 60;
            if (type == LightType.Pulsing) {
                intensity = 50 + (int)(50 * System.Math.Sin(animPhase * 0.1));
            }
        }
        public uint ComputeHash() {
            uint h = 2166136261; h ^= (uint)id; h *= 16777619;
            h ^= (uint)(active ? 1 : 0); h *= 16777619;
            h ^= (uint)x; h ^= (uint)y; h ^= (uint)intensity;
            return h;
        }
    }
    
    public struct ReactiveObject {
        public int id; public bool active;
        public int x, y, w, h, threshold, damage;
        public ReactiveType type; public int currentDmg;
        public static ReactiveObject Empty(int i) => new ReactiveObject { id = i, active = false };
        public static ReactiveObject Create(int i, int X, int Y, int W, int H, int T, int D, ReactiveType tp) {
            return new ReactiveObject { id = i, active = true, x = X, y = Y, w = W, h = H, threshold = T, damage = D, type = tp };
        }
        public void Update(int frame) { }
        public void Trigger(int dmg) {
            if (!active) return;
            currentDmg += dmg;
            if (currentDmg >= threshold) { active = false; }
        }
        public uint ComputeHash() {
            uint h = 2166136261; h ^= (uint)id; h ^= (uint)(active ? 1 : 0);
            h ^= (uint)currentDmg; return h;
        }
    }
    
    public struct Platform {
        public int id; public bool active;
        public int x, y, w, h, centerX, centerY, rangeX, rangeY, speed, phase;
        public PlatformType type;
        public static Platform Empty(int i) => new Platform { id = i, active = false };
        public static Platform Create(int i, int X, int Y, int W, int RX, int RY, int S, PlatformType T) {
            return new Platform { id = i, active = true, x = X, y = Y, w = W, centerX = X, centerY = Y, rangeX = RX, rangeY = RY, speed = S, type = T };
        }
        public void Update(int frame) {
            if (!active) return;
            phase = frame % speed;
            float t = (float)phase / speed;
            if (type == PlatformType.Horizontal) x = centerX + (int)(rangeX * System.Math.Sin(t * 6.28f));
            else if (type == PlatformType.Vertical) y = centerY + (int)(rangeY * System.Math.Sin(t * 6.28f));
            else if (type == PlatformType.Diagonal) { x = centerX + (int)(rangeX * System.Math.Sin(t * 6.28f)); y = centerY + (int)(rangeY * System.Math.Cos(t * 6.28f)); }
        }
        public uint ComputeHash() {
            uint h = 2166136261; h ^= (uint)id; h ^= (uint)(active ? 1 : 0);
            h ^= (uint)x; h ^= (uint)y; return h;
        }
    }
    
    public enum LightType { Static, Pulsing, Orbiting, Flickering }
    public enum ReactiveType { Barrel, Crate, TNT, LightFixture }
    public enum PlatformType { Horizontal, Vertical, Diagonal, Circular }
}
