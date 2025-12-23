# Deterministic Fighting Game Engine

A deterministic fighting game engine with rollback netcode implemented in C#. This project implements a complete fighting game simulation system with deterministic physics, networking, and Unity integration.

## 🚀 Quick Start

### Prerequisites
- **.NET 9.0 SDK** - For building and running the engine
- **PowerShell 5.0+** (Windows) or **Bash** (Linux/macOS) - For build scripts
- **Unity 2021.3+** (optional) - For visual representation

### Installation & Setup
1. **Clone the repository**
   ```bash
   git clone https://github.com/Ghostmonday/deterministic-fighting-game.git
   cd deterministic-fighting-game
   ```

2. **Run the master test suite**
   ```powershell
   .\test.ps1
   ```
   This will run all tests including build, determinism verification, and integration tests.

3. **For Unity integration**, see `UNITY_SETUP.md`

## 📚 Documentation

### Comprehensive Guides
- **[DEVELOPMENT_GUIDE.md](DEVELOPMENT_GUIDE.md)** - Complete development reference with architecture, workflows, and best practices
- **[INTEGRATION_GUIDE.md](INTEGRATION_GUIDE.md)** - Game + Paper Trading System integration guide
- **[UNITY_SETUP.md](UNITY_SETUP.md)** - Unity project setup instructions

### Quick References
- **`scripts/README.md`** - Build and test script documentation
- **`test.ps1 -Help`** - Master test runner help
- **`test.ps1 -List`** - List all available tests

## 🏗️ Architecture

### Core Principles
- **Deterministic Simulation**: Fixed-point math (Fx.SCALE=1000) for perfect determinism
- **Rollback Netcode**: Advanced networking with prediction and resimulation
- **Data-Driven Design**: All tuning in JSON-defined character and action definitions
- **Unity Integration**: MonoBehaviour bridge for rendering game state

### Key Components
- **Simulation Loop**: Strict execution order for determinism
- **Physics System**: Movement, gravity, and collision with AABB detection
- **Combat Resolver**: Hitbox vs hurtbox resolution with weight-based knockback
- **Action System**: Timeline-based actions with hitbox events and projectile spawns
- **State Hashing**: FNV-1a hashing for desync detection and validation

## 🧪 Testing

### Master Test Runner
Use the unified test runner for all testing needs:
```powershell
# Run all tests
.\test.ps1

# Run specific tests
.\test.ps1 -Test build
.\test.ps1 -Test determinism
.\test.ps1 -Test integration

# List available tests
.\test.ps1 -List

# Show help
.\test.ps1 -Help
```

### Test Categories
- **Build Tests**: Project compilation and .NET environment
- **Determinism Tests**: Verify identical results across multiple runs
- **Integration Tests**: Game + Trading system connectivity
- **Environment Tests**: Development environment validation

## 🔗 Integration with Paper Trading System

The engine integrates with a paper trading system where:
- **Game → Trading**: Health differentials create sentiment signals
- **Trading → Game**: Trading decisions can be displayed in-game

### Quick Integration Test
```powershell
.\test.ps1 -Test integration
```
Requires both game and trading systems running.

## 📁 Project Structure

```
game/
├── README.md                    # This file
├── DEVELOPMENT_GUIDE.md         # Comprehensive development guide
├── INTEGRATION_GUIDE.md         # Game + Trading integration guide
├── UNITY_SETUP.md              # Unity project setup
├── SimRunner.csproj            # Main C# project file
├── test.ps1                    # Master test runner
│
├── src/                        # Primary source code
│   ├── engine/                 # Core game engine
│   ├── net/                    # Networking layer
│   ├── bridge/                 # Unity integration
│   ├── specs/                  # Specification files
│   └── trading/               # Paper trading system
│
├── scripts/                    # Build and test scripts
│   ├── build-test.ps1         # Build script
│   ├── test-determinism.ps1   # Determinism verification
│   ├── test-integration.ps1   # Integration tests
│   └── README.md              # Script documentation
│
├── bin/                        # Compiled binaries
└── obj/                        # Build objects
```

## 🎮 Features

### Deterministic Simulation
- Fixed-point mathematics for cross-platform consistency
- No floating-point arithmetic in core engine
- Strict execution order for reproducible results
- State hashing for desync detection

### Networking
- Rollback netcode with prediction and resimulation
- UDP transport with connection fixes
- Input synchronization and state reconciliation
- Desync recovery mechanisms

### Combat System
- Hitbox vs hurtbox resolution
- Weight-based knockback calculations
- Disjoint weapon support
- Hit trading mechanics
- Projectile system with anti-tunneling

### Action System
- JSON-defined actions with timeline events
- Startup/active/recovery frame specification
- Hitbox events with damage and knockback properties
- Projectile spawn events with type and velocity

## 🛠️ Development

### Adding New Features
1. **Understand the architecture** - Read `DEVELOPMENT_GUIDE.md`
2. **Maintain determinism** - Always use fixed-point math
3. **Test thoroughly** - Use the master test runner
4. **Update documentation** - Keep guides current

### Code Standards
- **C# Coding Conventions**: Follow .NET design guidelines
- **Determinism First**: All changes must maintain determinism
- **Performance Aware**: Consider allocation and computation costs
- **Documentation**: Update relevant guides and add code comments

## 🤝 Contributing

### Development Process
1. **Fork the repository**
2. **Create a feature branch**
3. **Implement changes** with tests
4. **Verify determinism** across multiple runs
5. **Submit pull request** with documentation

### Testing Requirements
- **Unit Tests**: For new functionality
- **Determinism Tests**: Verify identical behavior across runs
- **Integration Tests**: For system interactions
- **Performance Tests**: For performance-critical changes

## 🆘 Support

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

## 📄 License

This project is available for use under standard open-source licensing terms.

## 🙏 Acknowledgments

- Inspired by fighting game netcode implementations like GGPO
- Built with deterministic principles for competitive gaming
- Integration with financial trading systems for novel gameplay experiences

---

*Last Updated: December 2024*  
*Engine Version: 1.0.0*  
*Determinism Guarantee: Frame-perfect across all platforms*