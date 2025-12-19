# Deterministic Fighting Game Engine

A deterministic fighting game engine with rollback netcode implemented in C#. This project implements a complete fighting game simulation system with deterministic physics, networking, and Unity integration.

## Features

- **Deterministic Simulation**: All physics calculations use fixed-point math (Fx.SCALE=1000) for perfect determinism across different machines
- **Rollback Netcode**: Advanced networking system with prediction, rollback, and resimulation for smooth online play
- **Collision Detection**: AABB-based collision with swept collision for projectiles to prevent tunneling
- **Combat System**: Hitbox vs hurtbox resolution with weight-based knockback, disjoint weapons, and hit trading
- **Unity Integration**: MonoBehaviour-based bridge for rendering game state in Unity
- **Action System**: JSON-defined actions with timeline events and projectile spawning

## Architecture

The project is organized into four main layers:

### 1. Engine/Core (Data Structures)
- `Enums.cs` - Game enums (Facing, ProjectileType)
- `Fx.cs` - Fixed-point math constants
- `GameState.cs` - Authoritative simulation state
- `PlayerState.cs` - Deterministic player snapshot
- `ProjectileState.cs` - Deterministic projectile snapshot
- `StateHash.cs` - FNV-1a hashing for desync detection

### 2. Engine/Sim (Physics & Logic)
- `AABB.cs` - Axis-aligned bounding box collision primitive
- `CombatResolver.cs` - Hit resolution with knockback calculations
- `MapData.cs` - World definition with solid blocks and kill floor
- `PhysicsSystem.cs` - Player movement, gravity, and collision
- `ProjectileSystem.cs` - Anti-tunneling projectile movement with substeps

### 3. Net (Networking)
- `RollbackController.cs` - Prediction & rollback using ring buffers
- `UdpInputTransport.cs` - UDP transport with Windows SIO_UDP_CONNRESET fix

### 4. Bridge (Unity Integration)
- `BattleManager.cs` - MonoBehaviour for simulation and rendering

### Engine/Data (Action Definitions)
- `ActionDef.cs` - Runtime action data structures
- `ActionLoader.cs` - JSON deserialization for action definitions

## Getting Started

### Prerequisites
- Unity (for the bridge layer)
- .NET framework for C# development

### Building
1. Clone the repository:
   ```bash
   git clone https://github.com/Ghostmonday/deterministic-fighting-game.git
   ```

2. Open the project in Unity or your preferred C# IDE

3. The core engine can be used independently of Unity

### Usage Example

```csharp
// Initialize game state
var gameState = new GameState();

// Apply physics
PhysicsSystem.ApplyGravity(ref gameState.players[0]);
PhysicsSystem.StepAndCollide(ref gameState.players[0], mapData, deltaTime);

// Resolve combat
var hitResults = CombatResolver.ResolveCombat(hitboxes, hurtboxes, attackerPositions);

// Network rollback
var rollbackController = new RollbackController();
rollbackController.TickPrediction();
```

## Deterministic Design

The engine ensures perfect determinism through:

1. **Fixed-Point Math**: All physics uses integer arithmetic with Fx.SCALE=1000
2. **No Floats**: Avoids floating-point inconsistencies across platforms
3. **Deterministic Order**: All operations execute in predictable order
4. **State Hashing**: FNV-1a hashing for desync detection and validation

## Networking

The rollback netcode implements:

1. **Input Prediction**: Predict local inputs while waiting for remote inputs
2. **State Snapshots**: Save game state history in ring buffers
3. **Rollback & Resimulate**: Rewind and re-simulate when remote inputs arrive
4. **Desync Detection**: State hashing to detect and recover from desyncs

## Action System

Actions are defined in JSON format (see `src/specs/combat_contract.json`):

```json
{
  "action_id": "NINJA_SHURIKEN",
  "timeline": { "startup": 4, "active": 2, "recovery": 12 },
  "events": [
    { "frame": 4, "type": "SPAWN_PROJECTILE", "payload": { "type": "SHURIKEN", "speed_x": 1500 } }
  ]
}
```

## License

This project is available for use under standard open-source licensing terms.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Acknowledgments

- Inspired by fighting game netcode implementations like GGPO
- Built with deterministic principles for competitive gaming