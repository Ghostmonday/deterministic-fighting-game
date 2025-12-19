/* ================================================================================
   NEURAL DRAFT LLC | DEEPSEEK INSTRUCTION HEADER
================================================================================
   FILE:    ActionLoader.cs
   CONTEXT: JSON deserialization.

   TASK:
   Deserialize 'combat_contract.json'. Validate schema. Map strings to enums. Reject malformed data.

   CONSTRAINTS:
   - Use Fixed-Point Math (Fx.SCALE = 1000) for all physics.
   - No Unity Engine references in this file (unless specified in Bridge).
   - Strict Determinism: No floats, no random execution order.
================================================================================

*/
namespace NeuralDraft
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public static class ActionLoader
    {
        public static ActionDef LoadFromJson(string json)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var contract = JsonSerializer.Deserialize<CombatContract>(json, options);

                if (contract == null)
                {
                    throw new ArgumentException("Failed to deserialize JSON");
                }

                return ConvertToActionDef(contract);
            }
            catch (JsonException ex)
            {
                throw new ArgumentException($"Invalid JSON format: {ex.Message}");
            }
        }

        private static ActionDef ConvertToActionDef(CombatContract contract)
        {
            var actionDef = new ActionDef();
            actionDef.actionId = ActionDef.HashActionId(contract.ActionId);
            actionDef.name = contract.ActionId;

            // Calculate total frames from timeline
            int totalFrames = contract.Timeline.Startup + contract.Timeline.Active + contract.Timeline.Recovery;
            actionDef.totalFrames = totalFrames;

            // Process events
            var hitboxEvents = new List<HitboxEvent>();
            var projectileSpawns = new List<ProjectileSpawn>();

            if (contract.Events != null)
            {
                foreach (var evt in contract.Events)
                {
                    if (evt.Type == "SPAWN_PROJECTILE" && evt.Payload != null)
                    {
                        var spawn = new ProjectileSpawn
                        {
                            frame = evt.Frame,
                            offsetX = 0,
                            offsetY = 0,
                            velX = evt.Payload.SpeedX * Fx.SCALE / 1000,
                            velY = 0,
                            type = ParseProjectileType(evt.Payload.Type),
                            lifetime = 60 // Default lifetime
                        };
                        projectileSpawns.Add(spawn);
                    }
                    // Add other event types as needed
                }
            }

            actionDef.projectileSpawns = projectileSpawns.ToArray();
            actionDef.hitboxEvents = hitboxEvents.ToArray();

            // Create frames array
            actionDef.frames = new ActionFrame[totalFrames];
            for (int i = 0; i < totalFrames; i++)
            {
                actionDef.frames[i] = new ActionFrame
                {
                    frameNumber = i,
                    velX = 0,
                    velY = 0,
                    cancelable = (byte)(i < contract.Timeline.Startup ? 0 : 1),
                    hitstun = 0
                };
            }

            return actionDef;
        }

        private static ProjectileType ParseProjectileType(string typeStr)
        {
            return typeStr.ToUpper() switch
            {
                "BULLET" => ProjectileType.BULLET,
                "ARROW" => ProjectileType.ARROW,
                "SHURIKEN" => ProjectileType.SHURIKEN,
                _ => throw new ArgumentException($"Unknown projectile type: {typeStr}")
            };
        }

        // JSON contract classes
        private class CombatContract
        {
            [JsonPropertyName("action_id")]
            public string ActionId { get; set; }

            [JsonPropertyName("timeline")]
            public Timeline Timeline { get; set; }

            [JsonPropertyName("events")]
            public List<ContractEvent> Events { get; set; }
        }

        private class Timeline
        {
            [JsonPropertyName("startup")]
            public int Startup { get; set; }

            [JsonPropertyName("active")]
            public int Active { get; set; }

            [JsonPropertyName("recovery")]
            public int Recovery { get; set; }
        }

        private class ContractEvent
        {
            [JsonPropertyName("frame")]
            public int Frame { get; set; }

            [JsonPropertyName("type")]
            public string Type { get; set; }

            [JsonPropertyName("payload")]
            public EventPayload Payload { get; set; }
        }

        private class EventPayload
        {
            [JsonPropertyName("type")]
            public string Type { get; set; }

            [JsonPropertyName("speed_x")]
            public int SpeedX { get; set; }
        }
    }
}
