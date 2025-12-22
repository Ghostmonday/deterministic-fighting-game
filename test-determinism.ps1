# Determinism Test Script for Neural Draft Engine
# This script compiles and runs the SimRunner to verify determinism

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "NEURAL DRAFT - Determinism Test Runner" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Set error handling
$ErrorActionPreference = "Stop"

# Create output directory
$outputDir = ".\bin"
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    Write-Host "Created output directory: $outputDir" -ForegroundColor Green
}

# Find all C# source files
Write-Host "Collecting source files..." -ForegroundColor Yellow
$sourceFiles = @(
    ".\src\engine\core\Enums.cs",
    ".\src\engine\core\Fx.cs",
    ".\src\engine\core\GameState.cs",
    ".\src\engine\core\InputFrame.cs",
    ".\src\engine\core\PlayerState.cs",
    ".\src\engine\core\ProjectileState.cs",
    ".\src\engine\core\StateHash.cs",
    ".\src\engine\data\CharacterDef.cs",
    ".\src\engine\data\ActionDef.cs",

    ".\src\engine\sim\AABB.cs",
    ".\src\engine\sim\CombatResolver.cs",
    ".\src\engine\sim\MapData.cs",
    ".\src\engine\sim\PhysicsSystem.cs",
    ".\src\engine\sim\ProjectileSystem.cs",
    ".\src\engine\sim\Simulation.cs",
    ".\src\net\RollbackController.cs",
    ".\src\net\UdpInputTransport.cs",
    ".\src\SimRunner.cs"
)

Write-Host "Found $($sourceFiles.Count) source files" -ForegroundColor Green

# Try to find C# compiler
Write-Host ""
Write-Host "Looking for C# compiler..." -ForegroundColor Yellow

$cscPath = $null
$possiblePaths = @(
    "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe",
    "C:\Program Files\Microsoft Visual Studio\*\*\MSBuild\Current\Bin\Roslyn\csc.exe",
    "C:\Program Files (x86)\Microsoft Visual Studio\*\*\MSBuild\Current\Bin\Roslyn\csc.exe"
)

foreach ($path in $possiblePaths) {
    $expandedPath = [System.Environment]::ExpandEnvironmentVariables($path)
    $foundPaths = Get-ChildItem -Path $expandedPath -ErrorAction SilentlyContinue
    if ($foundPaths) {
        $cscPath = $foundPaths[0].FullName
        Write-Host "Found C# compiler at: $cscPath" -ForegroundColor Green
        break
    }
}

if (-not $cscPath) {
    Write-Host "ERROR: C# compiler not found!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please install one of the following:" -ForegroundColor Yellow
    Write-Host "1. .NET SDK: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
    Write-Host "2. Visual Studio Build Tools" -ForegroundColor Yellow
    Write-Host "3. Or ensure csc.exe is in your PATH" -ForegroundColor Yellow
    exit 1
}

# Compile the SimRunner
Write-Host ""
Write-Host "Compiling SimRunner..." -ForegroundColor Yellow

try {
    # Build the compilation command
    $compileArgs = @(
        "-out:$outputDir\SimRunner.exe",
        "-target:exe",
        "-platform:anycpu",
        "-optimize",
        "-warn:4",
        "-nologo"
    ) + $sourceFiles

    & $cscPath $compileArgs

    if ($LASTEXITCODE -eq 0) {
        Write-Host "Compilation successful!" -ForegroundColor Green
        Write-Host "Executable: $outputDir\SimRunner.exe" -ForegroundColor Green
    } else {
        Write-Host "Compilation failed with exit code: $LASTEXITCODE" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "Compilation error: $_" -ForegroundColor Red
    exit 1
}

# Run the SimRunner
Write-Host ""
Write-Host "Running SimRunner test..." -ForegroundColor Yellow
Write-Host ""

try {
    & "$outputDir\SimRunner.exe"

    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "================================================" -ForegroundColor Green
        Write-Host "✅ SUCCESS: Determinism verified!" -ForegroundColor Green
        Write-Host "================================================" -ForegroundColor Green
        Write-Host ""
        Write-Host "Engine core is solid. Deterministic chassis confirmed." -ForegroundColor Green
        Write-Host "Ready for Task 6: Unity integration." -ForegroundColor Green
    } else {
        Write-Host ""
        Write-Host "================================================" -ForegroundColor Red
        Write-Host "❌ FAILURE: Determinism test failed!" -ForegroundColor Red
        Write-Host "Exit code: $LASTEXITCODE" -ForegroundColor Red
        Write-Host "================================================" -ForegroundColor Red
        Write-Host ""
        Write-Host "Engine has determinism violations." -ForegroundColor Red
        Write-Host "Check logs above for desync details." -ForegroundColor Red
        exit $LASTEXITCODE
    }
} catch {
    Write-Host "Runtime error: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Test completed!" -ForegroundColor Cyan
