# DotNet-based Determinism Test for Neural Draft Engine
# Uses the local dotnet executable to build and run tests

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "NEURAL DRAFT - DotNet Determinism Test" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Set error handling
$ErrorActionPreference = "Stop"

# Check if dotnet is available
$dotnetPath = ".\dotnet\dotnet.exe"
if (-not (Test-Path $dotnetPath)) {
    Write-Host "ERROR: DotNet not found at $dotnetPath" -ForegroundColor Red
    Write-Host "Please ensure dotnet is installed in the dotnet directory" -ForegroundColor Yellow
    exit 1
}

Write-Host "Using dotnet from: $dotnetPath" -ForegroundColor Green

# Check dotnet version
Write-Host ""
Write-Host "Checking dotnet version..." -ForegroundColor Yellow
try {
    $versionOutput = & $dotnetPath --version
    Write-Host "DotNet version: $versionOutput" -ForegroundColor Green
} catch {
    Write-Host "Failed to get dotnet version: $_" -ForegroundColor Red
    exit 1
}

# Build the project
Write-Host ""
Write-Host "Building SimRunner project..." -ForegroundColor Yellow
try {
    & $dotnetPath build SimRunner.csproj --configuration Release

    if ($LASTEXITCODE -eq 0) {
        Write-Host "Build successful!" -ForegroundColor Green
    } else {
        Write-Host "Build failed with exit code: $LASTEXITCODE" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "Build error: $_" -ForegroundColor Red
    exit 1
}

# Run the SimRunner test
Write-Host ""
Write-Host "Running determinism test..." -ForegroundColor Yellow
Write-Host ""

try {
    # Run using dotnet run to ensure proper runtime
    & $dotnetPath run --project SimRunner.csproj --configuration Release --no-build

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
Write-Host "Test completed successfully!" -ForegroundColor Cyan
