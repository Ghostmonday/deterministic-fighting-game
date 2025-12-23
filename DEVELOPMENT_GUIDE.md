# Deterministic Fighting Game Engine - Development Guide

## Overview

This is a deterministic fighting game engine with rollback netcode implemented in C#. The engine provides a complete simulation system with perfect determinism across different machines, enabling competitive online play with frame-perfect accuracy.

## Project Structure

### Core Organization

```
game/
├── README.md                    # Project overview and features
├── DEVELOPMENT_GUIDE.md         # This file - comprehensive development guide
├── INTEGRATION_GUIDE.md         # Game + Paper Trading System integration
├── UNITY_SETUP.md               # Unity project setup instructions
├── SimRunner.csproj            # Main C# project file
├── .gitignore                  # Git ignore rules
│
├── src/                        # Primary source code
│   ├── SimRunner.cs           # Main program entry point
│   ├── engine/                # Core game engine
│   │   ├── core/              # Data structures and enums
│   │   │   ├── Enums.cs       # Game enums (Facing, ProjectileType, etc.)
│   │   │   ├── Fx.cs          # Fixed-point math constants (SCALE=1000)
│   │   │   ├── GameState.cs   # Authoritative simulation state
│   │   │   ├── InputFrame.cs  # Player input representation
│   │   │   ├── PlayerState.cs # Deterministic player snapshot
│   │   │   ├── ProjectileState.cs # Deterministic projectile snapshot
│   │   │   ├── StateHash.cs   # FNV-1a hashing for desync detection
│   │   │   └── FixedMath.cs   # Fixed-point math utilities (Sqrt, etc.)
│   │   │
│   │   ├── data/              # Game data definitions
│   │   │   ├── CharacterDef.cs # Character definitions (weight, speed, etc.)
│   │   │   ├── ActionDef.cs   # Action definitions with timeline events
│   │   │   ├── ActionLoader.cs # JSON deserialization for actions
│   │   │   └── ActionLibrary.cs # Static library of character actions
│   │   │
│   │   └── sim/               # Simulation systems
│   │       ├── AABB.cs        # Axis-aligned bounding box collision
│   │       ├── CombatResolver.cs # Hit resolution with knockback
│   │       ├── MapData.cs     # World definition with collision
│   │       ├── PhysicsSystem.cs # Movement, gravity, and collision
│   │       ├── ProjectileSystem.cs # Anti-tunneling projectile movement
│   │       └── Simulation.cs  # Main deterministic simulation loop
│   │
│   ├── net/                    # Networking layer
│   │   ├── RollbackController.cs # Prediction & rollback using ring buffers
│   │   └── UdpInputTransport.cs # UDP transport with connection fixes
│   │
│   ├── bridge/                 # Unity integration
│   │   └── BattleManager.cs   # MonoBehaviour for simulation and rendering
│   │
│   ├── specs/                  # Specification files
│   │   └── combat_contract.json # JSON schema for action definitions
│   │
│   └── trading/               # Paper trading system integration
│       ├── Program.cs         # Trading system entry point
│       ├── Integration.cs     # Game signal integration
│       ├── api/TradingController.cs # REST API endpoints
│       ├── core/TradingEngine.cs # Trading logic engine
│       └── services/TradingService.cs # Background trading service
│
├── DeterministicFightingGame/ # Alternative organization (legacy/duplicate)
│   └── src/Core/              # Similar structure with different organization
│
├── bin/                       # Compiled binaries
├── obj/                       # Build objects
├── dotnet/                    # .NET runtime files
│
└── Scripts/                   # Build and test scripts
    ├── build-test.ps1         # PowerShell build script
    ├── dotnet-install.ps1     # .NET installation script
    ├── run-test.bat           # Windows batch test runner
    ├── test-dotnet.ps1        # .NET test script
    ├── test-determinism.ps1   # Determinism verification
    ├── test-integration.ps1   # Game+trading integration test
    ├── test-integration-simple.ps1 # Simplified integration test
    ├── test-integration-minimal.bat # Minimal integration test
    ├── test-minimal.ps1       # Minimal test script
    └── test-simple.bat        # Simple test runner
```

## Key Architectural Principles

### 1. Determinism
- **Fixed-Point Math**: All physics uses integer arithmetic with `Fx.SCALE = 1000`
- **No Floats**: Avoids floating-point inconsistencies across platforms
- **Deterministic Order**: All operations execute in predictable sequence
- **State Hashing**: FNV-1a hashing for desync detection and validation

### 2. Simulation Loop
The engine follows a strict execution order for determinism:
1. **Input Application** - Apply player inputs to game state
2. **Physics** - Gravity → Map Collision → Friction
3. **Combat** - Hitbox/Hurtbox interaction resolution
4. **Entities** - Projectile system updates
5. **State Updates** - Frame index increment and validation

### 3. Networking
- **Rollback Netcode**: Prediction, rollback, and resimulation
- **Input Prediction**: Predict local inputs while waiting for remote
- **State Snapshots**: Save game state history in ring buffers
- **Desync Recovery**: State hashing to detect and recover from desyncs

## Getting Started

### Prerequisites
- **.NET 9.0 SDK** - For building and running the engine
- **Unity 2021.3+** (optional) - For visual representation
- **PowerShell** (Windows) or **Bash** (Linux/macOS) - For build scripts

### Quick Start
1. **Clone the repository**
   ```bash
   git clone https://github.com/Ghostmonday/deterministic-fighting-game.git
   cd deterministic-fighting-game
   ```

2. **Build the engine**
   ```powershell
   .\build-test.ps1
   ```
   or
   ```bash
   dotnet build
   ```

3. **Run tests**
   ```powershell
   .\run-test.bat
   ```
   or
   ```bash
   dotnet run
   ```

### Unity Integration
See `UNITY_SETUP.md` for detailed Unity project setup instructions.

## Development Workflow

### 1. Understanding the Codebase

#### Core Data Structures
- **GameState**: Complete simulation state at a specific frame
- **PlayerState**: Individual player state (position, velocity, health, etc.)
- **InputFrame**: Packed player inputs for a single frame
- **ActionDef**: Action definitions with timeline events

#### Simulation Systems
- **PhysicsSystem**: Handles movement, gravity, and collision
- **CombatResolver**: Resolves hitbox/hurtbox interactions
- **ProjectileSystem**: Manages projectile movement with anti-tunneling
- **Simulation**: Main deterministic simulation loop

### 2. Adding New Features

#### Adding a New Character Action
1. Define the action in `ActionLibrary.cs`
2. Add hitbox events and/or projectile spawns
3. Map the action to input bits for specific archetypes
4. Test determinism with the action

#### Modifying Physics
1. Work in `PhysicsSystem.cs` or `CombatResolver.cs`
2. Always use fixed-point math (`Fx.SCALE`)
3. Maintain deterministic execution order
4. Add state hashing for validation

### 3. Testing Determinism

#### Built-in Tests
```powershell
# Run determinism tests
.\test-determinism.ps1

# Run integration tests
.\test-integration.ps1
```

#### Manual Testing
1. Run two instances of the simulation with identical inputs
2. Compare state hashes at regular intervals
3. Verify identical behavior across runs

## Key Components Deep Dive

### Fixed-Point Mathematics
```csharp
// All physics calculations use fixed-point math
int position = 1500; // Represents 1.5 units (1500 / Fx.SCALE)
int velocity = 2000; // Represents 2.0 units per frame

// Operations maintain precision
int newPosition = position + (velocity * deltaTime) / Fx.SCALE;
```

### Action System
Actions are defined with precise frame data:
- **Startup Frames**: Wind-up before the action becomes active
- **Active Frames**: When hitboxes are active
- **Recovery Frames**: Cooldown after the action
- **Hitbox Events**: Damage, knockback, and hitstun properties
- **Projectile Spawns**: Timing and properties of spawned projectiles

### Networking Architecture
```
┌─────────────┐    Inputs    ┌─────────────┐
│   Local     │─────────────►│  Simulation │
│   Player    │              │             │
└─────────────┘              └─────────────┘
       ▲                            │
       │                      Save State
       │                            ▼
┌─────────────┐              ┌─────────────┐
│   Remote    │◄─────────────│   Rollback  │
│   Player    │   State      │   Buffer    │
└─────────────┘   Sync       └─────────────┘
```

## Integration with Paper Trading System

### Overview
The engine integrates with a paper trading system where game state generates trading signals:
- **Game → Trading**: Health differentials create sentiment signals
- **Trading → Game**: Trading decisions can be displayed in-game

### Setup
1. **Start the game** (Unity or SimRunner)
2. **Start the trading system**
   ```bash
   cd src/trading
   dotnet run --environment Development
   ```
3. **Test the integration**
   ```powershell
   .\test-integration.ps1
   ```

### Signal Flow
- Game exposes HTTP endpoint: `http://localhost:7777/v1/signal/`
- Trading system polls every 100ms (10Hz)
- Sentiment based on player health differentials
- Trading rules: LONG when sentiment > +200, SHORT when < -200

See `INTEGRATION_GUIDE.md` for complete integration details.

## Performance Considerations

### Optimization Guidelines
1. **Avoid Allocations**: Use pre-allocated buffers in hot paths
2. **Fixed Update Rate**: Engine runs at 60 FPS fixed timestep
3. **Object Pooling**: Projectiles use object pooling (64 max)
4. **State Copying**: Use `CopyTo()` methods instead of new instances

### Memory Management
- GameState: ~1KB per frame
- Rollback Buffer: 120 frames = ~120KB
- Projectile Pool: 64 projectiles = ~4KB
- Total: < 256KB for typical usage

## Troubleshooting

### Common Issues

#### Determinism Violations
**Symptoms**: Different state hashes on identical inputs
**Solutions**:
1. Check for floating-point math
2. Verify execution order consistency
3. Validate all physics uses `Fx.SCALE`
4. Check for non-deterministic API calls

#### Compilation Errors
**Missing Dependencies**:
```bash
dotnet restore
dotnet build
```

**Unity Integration Issues**:
1. Verify assembly definitions
2. Check namespace references
3. Ensure all files are in correct folders

#### Networking Problems
**Desync Detection**:
1. Check state hashing at regular intervals
2. Verify input synchronization
3. Examine rollback buffer consistency

### Debugging Tools

#### Built-in Scripts
```powershell
# Test determinism
.\test-determinism.ps1

# Test integration
.\test-integration.ps1

# Minimal test
.\test-minimal.ps1
```

#### Manual Debugging
1. **State Inspection**: Use `StateHash.Compute()` to verify determinism
2. **Frame Debugging**: Compare game states frame by frame
3. **Input Logging**: Record and replay input sequences

## Best Practices

### Code Organization
1. **Keep engine code separate** from Unity-specific code
2. **Maintain deterministic core** without Unity dependencies
3. **Use clear namespaces**: `NeuralDraft` for engine, `NeuralDraft.Bridge` for Unity
4. **Document public APIs** with XML comments

### Testing
1. **Unit test** individual systems
2. **Integration test** system interactions
3. **Determinism test** across multiple runs
4. **Performance test** with realistic workloads

### Version Control
1. **Commit deterministic builds** with state hashes
2. **Tag releases** with version numbers
3. **Document breaking changes** in commit messages
4. **Maintain backward compatibility** for saved states

## Extension Points

### Custom Actions
Extend `ActionLibrary.cs` to add new character actions with:
- Custom frame timelines
- Complex hitbox patterns
- Projectile spawn sequences
- Special movement properties

### Advanced Networking
Extend `RollbackController.cs` for:
- Different prediction algorithms
- Adaptive rollback strategies
- Bandwidth optimization
- Lag compensation techniques

### Integration Hooks
- **Game State Observers**: React to game events
- **Input Processors**: Custom input handling
- **Rendering Plugins**: Alternative visualizations
- **Analysis Tools**: Game state analysis and statistics

## Contributing

### Development Process
1. **Fork the repository**
2. **Create a feature branch**
3. **Implement changes** with tests
4. **Verify determinism** across multiple runs
5. **Submit pull request** with documentation

### Code Standards
- **C# Coding Conventions**: Follow .NET design guidelines
- **Determinism First**: All changes must maintain determinism
- **Performance Aware**: Consider allocation and computation costs
- **Documentation**: Update relevant guides and add code comments

### Testing Requirements
- **Unit Tests**: For new functionality
- **Determinism Tests**: Verify identical behavior across runs
- **Integration Tests**: For system interactions
- **Performance Tests**: For performance-critical changes

## Resources

### Documentation
- `README.md`: Project overview and features
- `INTEGRATION_GUIDE.md`: Game + Trading system integration
- `UNITY_SETUP.md`: Unity project setup
- This guide: Comprehensive development reference

### Code Examples
- `src/SimRunner.cs`: Minimal engine runner
- `src/bridge/BattleManager.cs`: Unity integration example
- `src/trading/`: Trading system integration example

### External References
- [.NET Documentation](https://docs.microsoft.com/dotnet/)
- [Unity Manual](https://docs.unity3d.com/Manual/)
- [Deterministic Game Physics](https://gafferongames.com/post/deterministic_lockstep/)
- [Rollback Netcode](https://arxiv.org/abs/2004.04234)

## Support

### Getting Help
1. **Check documentation** first
2. **Review code examples** in the repository
3. **Test with provided scripts**
4. **Examine existing implementations**

### Reporting Issues
When reporting issues, include:
1. **Environment details**: OS, .NET version, Unity version
2. **Reproduction steps**: Clear steps to reproduce the issue
3. **Expected vs Actual behavior**: What should happen vs what does happen
4. **Logs and error messages**: Console output and stack traces

### Community
- **GitHub Issues**: For bug reports and feature requests
- **Pull Requests**: For contributions and improvements
- **Documentation Updates**: For corrections and enhancements

---

*Last Updated: $(date)*  
*Engine Version: 1.0.0*  
*Determinism Guarantee: Frame-perfect across all platforms*