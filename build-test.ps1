# Build and Test Script for Neural Draft SimRunner
# This script compiles and runs the deterministic simulation test

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "NEURAL DRAFT - Build and Test Script" -ForegroundColor Cyan
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
    ".\src\engine\data\ActionLoader.cs",
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

# Compile the SimRunner
Write-Host ""
Write-Host "Compiling SimRunner..." -ForegroundColor Yellow

try {
    # Create a temporary .csproj file for compilation
    $csprojContent = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <OutputPath>$outputDir</OutputPath>
    <AssemblyName>SimRunner</AssemblyName>
  </PropertyGroup>
</Project>
"@

    $csprojPath = "$outputDir\SimRunner.csproj"
    $csprojContent | Out-File -FilePath $csprojPath -Encoding UTF8

    # Copy all source files to the output directory
    foreach ($sourceFile in $sourceFiles) {
        $destFile = Join-Path $outputDir (Split-Path $sourceFile -Leaf)
        Copy-Item $sourceFile $destFile -Force
    }

    # Compile using dotnet
    & dotnet build "$csprojPath" --configuration Release --no-restore

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
        Write-Host "✅ SUCCESS: All tests passed!" -ForegroundColor Green
        Write-Host "================================================" -ForegroundColor Green
    } else {
        Write-Host ""
        Write-Host "================================================" -ForegroundColor Red
        Write-Host "❌ FAILURE: Tests failed with exit code $LASTEXITCODE" -ForegroundColor Red
        Write-Host "================================================" -ForegroundColor Red
        exit $LASTEXITCODE
    }
} catch {
    Write-Host "Runtime error: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Build and test completed successfully!" -ForegroundColor Cyan
