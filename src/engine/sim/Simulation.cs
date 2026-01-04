/* SIMULATION - Deterministic game loop with all 10 characters */
namespace NeuralDraft {
    public static class Simulation {
        private const int HASH_FREQ_DEV = 1;
        private const int HASH_FREQ_PROD = 10;
        
        public static void Tick(ref GameState s, InputFrame inputs, MapData map, CharacterDef[] defs, bool dev = true) {
            // 1. INPUT APPLICATION
            for (int i = 0; i < GameState.MAX_PLAYERS; i++) {
                if (s.players[i].health <= 0) continue;
                ushort pins = inputs.GetPlayerInputs(i);
                int ix = 0;
                if ((pins & (ushort)InputBits.LEFT) != 0) ix = -1;
                if ((pins & (ushort)InputBits.RIGHT) != 0) ix = 1;
                bool jump = (pins & (ushort)InputBits.JUMP) != 0;
                bool attack = (pins & (ushort)InputBits.ATTACK) != 0;
                bool defend = (pins & (ushort)InputBits.DEFEND) != 0;
                bool special = (pins & (ushort)InputBits.SPECIAL) != 0;
                
                // Action selection
                if (s.players[i].currentActionHash == 0 || CanCancel(s.players[i], defs[i])) {
                    if (attack) StartAction(ref s.players[i], defs[i], "LIGHT");
                    else if (special) StartAction(ref s.players[i], defs[i], "SPECIAL");
                    else if (defend) StartAction(ref s.players[i], defs[i], "HEAVY");
                }
                
                PhysicsSystem.ApplyMovementInput(ref s.players[i], defs[i], ix, jump, s.players[i].grounded != 0);
            }
            
            // 2. PHYSICS
            for (int i = 0; i < GameState.MAX_PLAYERS; i++) {
                if (s.players[i].health <= 0) continue;
                bool ignoreGrav = s.players[i].currentActionHash != 0 && GetAction(s.players[i].currentActionHash)?.ignoreGravity == true;
                PhysicsSystem.ApplyGravity(ref s.players[i], defs[i], ignoreGrav);
            }
            for (int i = 0; i < GameState.MAX_PLAYERS; i++) {
                if (s.players[i].health <= 0) continue;
                PhysicsSystem.StepAndCollide(ref s.players[i], defs[i], map);
            }
            
            // 3. COMBAT
            UpdateActions(ref s, defs);
            ResolveCombat(ref s, defs);
            
            // 4. PROJECTILES
            ProjectileSystem.UpdateAllProjectiles(s, map);
            
            // 5. STATE UPDATE
            s.frameIndex++;
            
            // 6. VALIDATION
            int hashFreq = dev ? HASH_FREQ_DEV : HASH_FREQ_PROD;
            if (s.frameIndex % hashFreq == 0) {
                uint h = StateHash.Compute(ref s);
                if (s.lastValidatedFrame != -1 && h != s.lastValidatedHash) {
                    throw new Exception("DESYNC at frame " + s.frameIndex);
                }
                s.lastValidatedHash = h;
                s.lastValidatedFrame = s.frameIndex;
            }
        }
        
        static void StartAction(ref PlayerState p, CharacterDef def, string type) {
            int hash = ActionDef.HashActionId(def.name + "_" + type);
            p.currentActionHash = hash;
            p.actionFrameIndex = 0;
        }
        
        static bool CanCancel(PlayerState p, CharacterDef def) {
            var a = GetAction(p.currentActionHash);
            if (a == null) return true;
            int f = p.actionFrameIndex;
            return a.frames != null && f < a.frames.Length && a.frames[f].cancelable == 1;
        }
        
        static void UpdateActions(ref GameState s, CharacterDef[] defs) {
            for (int i = 0; i < GameState.MAX_PLAYERS; i++) {
                ref var p = ref s.players[i];
                if (p.hitstunRemaining > 0) {
                    p.hitstunRemaining--;
                    continue;
                }
                if (p.currentActionHash != 0) {
                    p.actionFrameIndex++;
                    var a = GetAction(p.currentActionHash);
                    if (a == null || p.actionFrameIndex >= a.totalFrames) {
                        p.currentActionHash = 0;
                        p.actionFrameIndex = 0;
                    }
                }
            }
        }
        
        static void ResolveCombat(ref GameState s, CharacterDef[] defs) {
            for (int a = 0; a < GameState.MAX_PLAYERS; a++) {
                if (s.players[a].health <= 0) continue;
                if (s.players[a].currentActionHash == 0) continue;
                
                var action = GetAction(s.players[a].currentActionHash);
                if (action?.hitboxEvents == null) continue;
                
                int frame = s.players[a].actionFrameIndex;
                foreach (var hb in action.hitboxEvents) {
                    if (frame < hb.startFrame || frame > hb.endFrame) continue;
                    
                    var hitbox = new CombatResolver.Hitbox {
                        bounds = new AABB {
                            minX = s.players[a].posX + hb.offsetX - hb.width / 2,
                            maxX = s.players[a].posX + hb.offsetX + hb.width / 2,
                            minY = s.players[a].posY + hb.offsetY - hb.height / 2,
                            maxY = s.players[a].posY + hb.offsetY + hb.height / 2
                        },
                        damage = hb.damage,
                        baseKnockback = hb.baseKnockback,
                        knockbackGrowth = hb.knockbackGrowth,
                        hitstun = hb.hitstun,
                        disjoint = hb.disjoint,
                        ownerIndex = a
                    };
                    
                    for (int d = 0; d < GameState.MAX_PLAYERS; d++) {
                        if (d == a || s.players[d].health <= 0) continue;
                        var hurtbox = new CombatResolver.Hurtbox {
                            bounds = new AABB {
                                minX = s.players[d].posX - defs[d].hitboxWidth / 2,
                                maxX = s.players[d].posX + defs[d].hitboxWidth / 2,
                                minY = s.players[d].posY + defs[d].hitboxOffsetY,
                                maxY = s.players[d].posY + defs[d].hitboxOffsetY + defs[d].hitboxHeight
                            },
                            weight = defs[d].weight,
                            playerIndex = d
                        };
                        
                        var res = CombatResolver.ResolveHit(hitbox, hurtbox, s.players[a].posX, s.players[a].posY, defs[d]);
                        if (res.hit) {
                            s.players[d].health -= (short)res.damageDealt;
                            s.players[d].velX += res.knockbackX;
                            s.players[d].velY += res.knockbackY;
                            s.players[d].hitstunRemaining = (short)res.hitstun;
                            s.players[d].currentActionHash = 0;
                            s.players[d].actionFrameIndex = 0;
                        }
                    }
                }
            }
        }
        
        static ActionDef GetAction(int hash) {
            return ActionLibrary.GetAction(hash);
        }
    }
}
