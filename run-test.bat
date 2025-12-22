@echo off
echo ================================================
echo NEURAL DRAFT - SimRunner Compilation Test
echo ================================================
echo.

REM Check for C# compiler
where csc >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: C# compiler (csc) not found in PATH.
    echo Please ensure you have .NET SDK or Visual Studio installed.
    echo.
    echo You can install .NET SDK from: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo Found C# compiler. Compiling SimRunner...
echo.

REM Create list of source files
set SOURCE_FILES=src\engine\core\Enums.cs src\engine\core\Fx.cs src\engine\core\GameState.cs src\engine\core\InputFrame.cs src\engine\core\PlayerState.cs src\engine\core\ProjectileState.cs src\engine\core\StateHash.cs src\engine\data\CharacterDef.cs src\engine\data\ActionDef.cs src\engine\data\ActionLoader.cs src\engine\sim\AABB.cs src\engine\sim\CombatResolver.cs src\engine\sim\MapData.cs src\engine\sim\PhysicsSystem.cs src\engine\sim\ProjectileSystem.cs src\engine\sim\Simulation.cs src\net\RollbackController.cs src\net\UdpInputTransport.cs src\SimRunner.cs

REM Compile the application
csc -out:SimRunner.exe -target:exe -platform:anycpu -optimize -warn:4 -nologo %SOURCE_FILES%

if %errorlevel% neq 0 (
    echo.
    echo ERROR: Compilation failed!
    pause
    exit /b 1
)

echo.
echo SUCCESS: SimRunner.exe compiled successfully!
echo.
echo ================================================
echo RUNNING DETERMINISM TEST...
echo ================================================
echo.

REM Run the test
SimRunner.exe

if %errorlevel% equ 0 (
    echo.
    echo ================================================
    echo ✅ SUCCESS: Determinism verified!
    echo ================================================
) else (
    echo.
    echo ================================================
    echo ❌ FAILURE: Test failed!
    echo ================================================
)

echo.
pause
