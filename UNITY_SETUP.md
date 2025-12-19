# Unity Project Setup Guide

This guide explains how to set up the Deterministic Fighting Game Engine in a Unity project.

## Prerequisites

- Unity 2021.3 LTS or later (tested with 2021.3.34f1)
- Basic knowledge of Unity and C#

## Step 1: Create a New Unity Project

1. Open Unity Hub
2. Click "New Project"
3. Select "3D Core" template
4. Name your project (e.g., "DeterministicFightingGame")
5. Choose a location and click "Create"

## Step 2: Import the Engine Code

1. Create the following folder structure in your Unity project's `Assets` folder:
   ```
   Assets/
   ├── Scripts/
   │   ├── Engine/
   │   │   ├── Core/
   │   │   ├── Sim/
   │   │   └── Data/
   │   ├── Net/
   │   └── Bridge/
   └── Resources/
   ```

2. Copy the C# files from this repository into the corresponding folders:
   - `src/engine/core/*.cs` → `Assets/Scripts/Engine/Core/`
   - `src/engine/sim/*.cs` → `Assets/Scripts/Engine/Sim/`
   - `src/engine/data/*.cs` → `Assets/Scripts/Engine/Data/`
   - `src/net/*.cs` → `Assets/Scripts/Net/`
   - `src/bridge/*.cs` → `Assets/Scripts/Bridge/`
   - `src/specs/combat_contract.json` → `Assets/Resources/`

## Step 3: Configure Assembly Definitions (Recommended)

For better compilation performance and namespace isolation, create assembly definition files:

1. In `Assets/Scripts/Engine/`, create `Engine.asmdef`:
   ```json
   {
     "name": "NeuralDraft.Engine",
     "references": [],
     "includePlatforms": [],
     "excludePlatforms": [],
     "allowUnsafeCode": false,
     "overrideReferences": false,
     "precompiledReferences": [],
     "autoReferenced": true,
     "defineConstraints": [],
     "versionDefines": [],
     "noEngineReferences": false
   }
   ```

2. In `Assets/Scripts/Net/`, create `Net.asmdef`:
   ```json
   {
     "name": "NeuralDraft.Net",
     "references": ["NeuralDraft.Engine"],
     "includePlatforms": [],
     "excludePlatforms": [],
     "allowUnsafeCode": false,
     "overrideReferences": false,
     "precompiledReferences": [],
     "autoReferenced": true,
     "defineConstraints": [],
     "versionDefines": [],
     "noEngineReferences": false
   }
   ```

3. In `Assets/Scripts/Bridge/`, create `Bridge.asmdef`:
   ```json
   {
     "name": "NeuralDraft.Bridge",
     "references": ["NeuralDraft.Engine", "NeuralDraft.Net"],
     "includePlatforms": [],
     "excludePlatforms": [],
     "allowUnsafeCode": false,
     "overrideReferences": false,
     "precompiledReferences": [],
     "autoReferenced": true,
     "defineConstraints": [],
     "versionDefines": [],
     "noEngineReferences": false
   }
   ```

## Step 4: Set Up the Scene

1. Create a new scene: `File → New Scene`
2. Save it as `MainBattleScene`

3. Create GameObjects:
   - Create an empty GameObject named "BattleManager"
   - Add the `BattleManager` component to it
   - Create two GameObjects named "Player1" and "Player2" as children
   - Add SpriteRenderer or 3D models to represent players
   - Assign these transforms to the `playerTransforms` array in BattleManager

4. For projectiles:
   - Create a prefab for projectiles (simple sphere or sprite)
   - Create an empty GameObject named "ProjectilePool"
   - Instantiate 64 projectile instances (matching MAX_PROJECTILES)
   - Assign these transforms to the `projectileTransforms` array

## Step 5: Configure Input System

### Using Unity's Legacy Input System:

Edit `Edit → Project Settings → Input Manager`:

1. Add/configure axes:
   - Horizontal: Positive = "right", Negative = "left"
   - Vertical: Positive = "up", Negative = "down"
   - Jump: Positive = "space"
   - Fire1: Positive = "z" or "joystick button 0"
   - Fire2: Positive = "x" or "joystick button 1"
   - Fire3: Positive = "c" or "joystick button 2"

### Using Unity's New Input System:

1. Install Input System Package: `Window → Package Manager → Input System`
2. Create Input Actions asset: `Create → Input Actions`
3. Define actions for:
   - Move (Vector2)
   - Jump (Button)
   - Attack1, Attack2, Attack3 (Buttons)

## Step 6: Configure Physics Settings

1. Go to `Edit → Project Settings → Physics`
2. Disable auto-simulation if using deterministic physics:
   - Set `Auto Simulation` to false
   - Set `Auto Sync Transforms` to false

3. The engine uses its own collision system, so Unity physics is only for visualization.

## Step 7: Test the Setup

1. Create a test script to verify the engine works:

```csharp
using UnityEngine;
using NeuralDraft;

public class EngineTest : MonoBehaviour
{
    void Start()
    {
        // Test fixed-point math
        Debug.Log($"Fixed-point scale: {Fx.SCALE}");
        
        // Test game state initialization
        var gameState = new GameState();
        Debug.Log($"GameState initialized with {GameState.MAX_PLAYERS} players");
        
        // Test action loading
        var jsonText = Resources.Load<TextAsset>("combat_contract").text;
        var actionDef = ActionLoader.LoadFromJson(jsonText);
        Debug.Log($"Loaded action: {actionDef.name}");
    }
}
```

## Step 8: Build Settings

1. Go to `File → Build Settings`
2. Add your scene to the build
3. Configure platform settings as needed
4. For deterministic builds, ensure:
   - Scripting Backend: IL2CPP
   - API Compatibility Level: .NET Standard 2.1
   - Strip Engine Code: Disabled (for development)

## Common Issues and Solutions

### Issue: "The type or namespace name 'NeuralDraft' could not be found"
**Solution**: Ensure all files are in the correct folders and assembly definitions are properly referenced.

### Issue: Input not detected in BattleManager
**Solution**: Check Input Manager settings and ensure axis names match those in `BattleManager.CaptureLocalInput()`.

### Issue: Projectiles not appearing
**Solution**: Verify projectileTransforms array is properly assigned and projectile prefabs are active.

### Issue: Non-deterministic behavior
**Solution**: 
- Ensure no Unity Physics components are interfering
- Check that all physics uses Fx.SCALE for calculations
- Verify no floating-point math in engine code

## Performance Considerations

1. **Fixed Update Rate**: The engine runs at 60 FPS fixed timestep
2. **Object Pooling**: Projectiles use object pooling (64 max)
3. **State Copying**: Use `CopyTo()` methods instead of creating new instances
4. **Network Optimization**: Rollback buffer size is configurable (default 120 frames)

## Next Steps

1. Implement visual effects for hits and projectiles
2. Add character animations tied to action frames
3. Create UI for health bars and game state
4. Implement matchmaking and lobby system
5. Add sound effects and music

## Support

For issues or questions:
1. Check the [GitHub repository](https://github.com/Ghostmonday/deterministic-fighting-game)
2. Review the engine source code comments
3. Test with the provided example JSON contract

The engine is designed to be modular - you can replace or extend any component while maintaining determinism.