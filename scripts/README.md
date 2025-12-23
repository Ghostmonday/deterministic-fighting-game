# Scripts Directory

This directory contains all build, test, and utility scripts for the Deterministic Fighting Game Engine.

## Overview

The scripts are organized to provide a comprehensive testing and development workflow for the engine. Use the master test runner (`../test.ps1`) for easy access to all scripts.

## Available Scripts

### Build Scripts
- **`build-test.ps1`** - Builds the C# project and compiles SimRunner.exe
- **`dotnet-install.ps1`** - Helps install .NET SDK if missing

### Test Scripts
- **`run-test.bat`** - Runs the compiled SimRunner.exe executable
- **`test-determinism.ps1`** - Verifies engine determinism across multiple runs
- **`test-dotnet.ps1`** - Tests .NET environment and dependencies
- **`test-integration.ps1`** - Full integration test between game and trading systems
- **`test-integration-simple.ps1`** - Simplified integration test
- **`test-integration-minimal.bat`** - Minimal connectivity test (game + trading)
- **`test-minimal.ps1`** - Minimal test suite for basic verification
- **`test-simple.bat`** - Simple architecture verification without compilation

## Master Test Runner

Use the master test runner in the project root for easy access:

```powershell
# Run all tests
.\test.ps1

# Run specific test
.\test.ps1 -Test build
.\test.ps1 -Test integration
.\test.ps1 -Test determinism

# List available tests
.\test.ps1 -List

# Show help
.\test.ps1 -Help
```

## Test Categories

### 1. Build & Compilation
- **Purpose**: Verify the project compiles successfully
- **Scripts**: `build-test.ps1`, `dotnet-install.ps1`
- **Prerequisites**: .NET 9.0 SDK

### 2. Determinism Verification
- **Purpose**: Ensure identical results across multiple simulation runs
- **Scripts**: `test-determinism.ps1`, `run-test.bat`
- **Key Test**: State hashes must match across identical input sequences

### 3. Integration Testing
- **Purpose**: Test connectivity between game engine and trading system
- **Scripts**: `test-integration.ps1`, `test-integration-simple.ps1`, `test-integration-minimal.bat`
- **Ports**: Game (7777), Trading (5000)
- **Requirements**: Both systems must be running

### 4. Environment Validation
- **Purpose**: Verify development environment setup
- **Scripts**: `test-dotnet.ps1`, `test-simple.bat`, `test-minimal.ps1`
- **Checks**: .NET installation, file structure, basic functionality

## Usage Examples

### Basic Development Workflow
```powershell
# 1. Check environment
.\test.ps1 -Test dotnet

# 2. Build project
.\test.ps1 -Test build

# 3. Run determinism test
.\test.ps1 -Test determinism

# 4. Run integration test (requires both systems running)
.\test.ps1 -Test integration
```

### Quick Verification
```powershell
# Run minimal test suite
.\test.ps1 -Test minimal

# Simple architecture check
.\test.ps1 -Test simple
```

### Full Test Suite
```powershell
# Run all tests in sequence
.\test.ps1
```

## Script Details

### build-test.ps1
Builds the C# project using `dotnet build`. Creates `SimRunner.exe` in the `bin/` directory.

**Usage**:
```powershell
.\scripts\build-test.ps1
```

### test-integration.ps1
Comprehensive integration test that:
1. Verifies game signal endpoint is running
2. Validates signal data structure
3. Tests trading API connectivity
4. Validates trading API responses

**Requirements**:
- Unity game running with BattleManager (port 7777)
- Trading system running (port 5000)

### test-determinism.ps1
Runs the simulation multiple times with identical inputs and compares state hashes to verify determinism.

**Key Verification**:
- Identical inputs produce identical state hashes
- No floating-point inconsistencies
- Perfect reproducibility across runs

## Troubleshooting

### Common Issues

#### "Script cannot be loaded because running scripts is disabled"
**Solution**: Run PowerShell as administrator and execute:
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

#### "dotnet command not found"
**Solution**: Run `.\scripts\dotnet-install.ps1` or install .NET SDK manually.

#### Integration Test Fails
**Check**:
1. Game is running (port 7777)
2. Trading system is running (port 5000)
3. Firewall allows connections on both ports
4. Services are properly configured

#### Determinism Test Fails
**Investigate**:
1. Check for floating-point math in engine code
2. Verify all physics uses `Fx.SCALE`
3. Examine state hashing implementation
4. Check for non-deterministic API calls

### Script Dependencies

- **PowerShell 5.0+**: Most scripts require PowerShell 5 or later
- **.NET 9.0 SDK**: Required for building and running the engine
- **Web Access**: Integration tests require HTTP connectivity
- **Administrator Rights**: Some scripts may require elevated privileges

## Adding New Scripts

When adding new scripts:
1. Place them in this directory
2. Update the master test runner (`../test.ps1`)
3. Document the script in this README
4. Follow existing naming conventions:
   - PowerShell: `*.ps1`
   - Batch files: `*.bat`
   - Use descriptive names

## Best Practices

1. **Error Handling**: All scripts should include proper error handling
2. **Logging**: Provide clear output and progress indicators
3. **Idempotency**: Scripts should be safe to run multiple times
4. **Documentation**: Keep this README updated with changes
5. **Testing**: Test scripts in different environments

## Support

For script-related issues:
1. Check script output for error messages
2. Verify prerequisites are met
3. Consult the main `DEVELOPMENT_GUIDE.md`
4. Review script source code for implementation details

## Version History

- **v1.0.0**: Initial script organization and master test runner
- **All scripts**: Maintain compatibility with engine version 1.0.0

---
*Last Updated: $(date)*  
*Scripts Version: 1.0.0*