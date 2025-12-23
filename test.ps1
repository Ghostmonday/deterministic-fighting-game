# Master Test Runner for Deterministic Fighting Game Engine
# Provides a unified interface to all test scripts

param(
    [string]$Test = "all",
    [switch]$Help,
    [switch]$List
)

# Configuration
$ScriptsDir = "scripts"
$GamePort = 7777
$TradingPort = 5000

# Colors for output
$SuccessColor = "Green"
$ErrorColor = "Red"
$InfoColor = "Yellow"
$WarningColor = "Magenta"
$HeaderColor = "Cyan"

function Write-Header {
    param([string]$Message)
    Write-Host "`n================================================" -ForegroundColor $HeaderColor
    Write-Host $Message -ForegroundColor $HeaderColor
    Write-Host "================================================" -ForegroundColor $HeaderColor
    Write-Host ""
}

function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor $SuccessColor
}

function Write-Error {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor $ErrorColor
}

function Write-Info {
    param([string]$Message)
    Write-Host "ℹ️  $Message" -ForegroundColor $InfoColor
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠️  $Message" -ForegroundColor $WarningColor
}

function Show-Help {
    Write-Header "DETERMINISTIC FIGHTING GAME - TEST RUNNER"
    Write-Host "Usage: .\test.ps1 [-Test <test-name>] [-Help] [-List]`n" -ForegroundColor $InfoColor

    Write-Host "Available Tests:" -ForegroundColor $HeaderColor
    Write-Host "  all              - Run all tests in sequence" -ForegroundColor $InfoColor
    Write-Host "  build            - Build the project" -ForegroundColor $InfoColor
    Write-Host "  determinism      - Test engine determinism" -ForegroundColor $InfoColor
    Write-Host "  integration      - Full integration test (game + trading)" -ForegroundColor $InfoColor
    Write-Host "  integration-mini - Minimal integration connectivity test" -ForegroundColor $InfoColor
    Write-Host "  dotnet           - Test .NET environment" -ForegroundColor $InfoColor
    Write-Host "  simple           - Simple architecture verification" -ForegroundColor $InfoColor
    Write-Host "  minimal          - Minimal test suite" -ForegroundColor $InfoColor
    Write-Host "  run              - Run the compiled executable" -ForegroundColor $InfoColor

    Write-Host "`nOptions:" -ForegroundColor $HeaderColor
    Write-Host "  -Help            - Show this help message" -ForegroundColor $InfoColor
    Write-Host "  -List            - List available tests without running" -ForegroundColor $InfoColor
    Write-Host "  -Test <name>     - Run specific test" -ForegroundColor $InfoColor

    Write-Host "`nExamples:" -ForegroundColor $HeaderColor
    Write-Host "  .\test.ps1                       # Run all tests" -ForegroundColor $InfoColor
    Write-Host "  .\test.ps1 -Test build           # Build project only" -ForegroundColor $InfoColor
    Write-Host "  .\test.ps1 -Test integration     # Test game+trading integration" -ForegroundColor $InfoColor
    Write-Host "  .\test.ps1 -List                 # List available tests" -ForegroundColor $InfoColor

    Write-Host "`nTest Descriptions:" -ForegroundColor $HeaderColor
    Write-Host "  Build Test        - Compiles the C# project using build-test.ps1" -ForegroundColor $InfoColor
    Write-Host "  Determinism Test  - Verifies engine produces identical results across runs" -ForegroundColor $InfoColor
    Write-Host "  Integration Test  - Tests full game+trading system connectivity" -ForegroundColor $InfoColor
    Write-Host "  .NET Test         - Checks .NET SDK installation and environment" -ForegroundColor $InfoColor
    Write-Host "  Run Test          - Executes the compiled SimRunner.exe" -ForegroundColor $InfoColor

    exit 0
}

function Show-TestList {
    Write-Header "AVAILABLE TESTS"

    $tests = @(
        @{Name="build"; Description="Build the project"; Script="build-test.ps1"},
        @{Name="determinism"; Description="Test engine determinism"; Script="test-determinism.ps1"},
        @{Name="integration"; Description="Full integration test"; Script="test-integration.ps1"},
        @{Name="integration-mini"; Description="Minimal integration test"; Script="test-integration-minimal.bat"},
        @{Name="dotnet"; Description="Test .NET environment"; Script="test-dotnet.ps1"},
        @{Name="simple"; Description="Simple architecture verification"; Script="test-simple.bat"},
        @{Name="minimal"; Description="Minimal test suite"; Script="test-minimal.ps1"},
        @{Name="run"; Description="Run compiled executable"; Script="run-test.bat"}
    )

    foreach ($test in $tests) {
        Write-Host "  $($test.Name.PadRight(15)) - $($test.Description)" -ForegroundColor $InfoColor
        Write-Host "    Script: $ScriptsDir\$($test.Script)" -ForegroundColor "DarkGray"
    }

    exit 0
}

function Test-Prerequisites {
    Write-Info "Checking prerequisites..."

    # Check if scripts directory exists
    if (-not (Test-Path $ScriptsDir)) {
        Write-Error "Scripts directory '$ScriptsDir' not found"
        return $false
    }

    # Check PowerShell version
    $psVersion = $PSVersionTable.PSVersion.Major
    if ($psVersion -lt 5) {
        Write-Warning "PowerShell version $psVersion detected. Some features may require PowerShell 5+"
    } else {
        Write-Success "PowerShell version $psVersion"
    }

    return $true
}

function Run-Test {
    param(
        [string]$TestName,
        [string]$ScriptName
    )

    Write-Header "RUNNING TEST: $TestName"
    Write-Info "Executing: $ScriptsDir\$ScriptName"

    $scriptPath = Join-Path $ScriptsDir $ScriptName

    if (-not (Test-Path $scriptPath)) {
        Write-Error "Test script not found: $scriptPath"
        return $false
    }

    $startTime = Get-Date

    try {
        if ($ScriptName.EndsWith(".ps1")) {
            # Run PowerShell script
            & $scriptPath
            $result = $LASTEXITCODE -eq 0
        } elseif ($ScriptName.EndsWith(".bat")) {
            # Run batch file
            cmd /c $scriptPath
            $result = $LASTEXITCODE -eq 0
        } else {
            Write-Error "Unsupported script type: $ScriptName"
            return $false
        }

        $elapsed = [math]::Round(((Get-Date) - $startTime).TotalSeconds, 1)

        if ($result) {
            Write-Success "Test '$TestName' completed successfully (${elapsed}s)"
            return $true
        } else {
            Write-Error "Test '$TestName' failed (${elapsed}s)"
            return $false
        }
    }
    catch {
        Write-Error "Error running test '$TestName': $_"
        return $false
    }
}

function Run-AllTests {
    Write-Header "RUNNING ALL TESTS"

    $testSequence = @(
        @{Name="dotnet"; Script="test-dotnet.ps1"; Description=".NET Environment Check"},
        @{Name="build"; Script="build-test.ps1"; Description="Project Build"},
        @{Name="run"; Script="run-test.bat"; Description="Executable Run"},
        @{Name="determinism"; Script="test-determinism.ps1"; Description="Determinism Verification"},
        @{Name="simple"; Script="test-simple.bat"; Description="Architecture Verification"},
        @{Name="integration-mini"; Script="test-integration-minimal.bat"; Description="Integration Connectivity"},
        @{Name="integration"; Script="test-integration.ps1"; Description="Full Integration Test"},
        @{Name="minimal"; Script="test-minimal.ps1"; Description="Minimal Test Suite"}
    )

    $results = @()
    $totalTests = $testSequence.Count
    $passedTests = 0

    foreach ($test in $testSequence) {
        Write-Host "`n[$($results.Count + 1)/$totalTests] $($test.Description)..." -ForegroundColor $InfoColor

        $testResult = Run-Test -TestName $test.Name -ScriptName $test.Script
        $results += @{
            Test = $test.Name
            Description = $test.Description
            Passed = $testResult
        }

        if ($testResult) {
            $passedTests++
        }
    }

    # Summary
    Write-Header "TEST SUMMARY"

    Write-Host "Total Tests: $totalTests" -ForegroundColor $InfoColor
    Write-Host "Passed: $passedTests" -ForegroundColor $SuccessColor
    Write-Host "Failed: $($totalTests - $passedTests)" -ForegroundColor $ErrorColor

    Write-Host "`nDetailed Results:" -ForegroundColor $HeaderColor
    foreach ($result in $results) {
        $status = if ($result.Passed) { "✅ PASS" } else { "❌ FAIL" }
        $color = if ($result.Passed) { $SuccessColor } else { $ErrorColor }
        Write-Host "  $status - $($result.Description)" -ForegroundColor $color
    }

    if ($passedTests -eq $totalTests) {
        Write-Host "`n🎉 ALL TESTS PASSED!" -ForegroundColor $SuccessColor
        return 0
    } else {
        Write-Host "`n⚠️  SOME TESTS FAILED" -ForegroundColor $WarningColor
        return 1
    }
}

# Main execution
try {
    # Handle help and list options
    if ($Help) {
        Show-Help
    }

    if ($List) {
        Show-TestList
    }

    # Check prerequisites
    if (-not (Test-Prerequisites)) {
        Write-Error "Prerequisites check failed"
        exit 1
    }

    # Run tests based on parameter
    switch ($Test.ToLower()) {
        "all" {
            exit (Run-AllTests)
        }
        "build" {
            $result = Run-Test -TestName "Build" -ScriptName "build-test.ps1"
            exit $(if ($result) { 0 } else { 1 })
        }
        "determinism" {
            $result = Run-Test -TestName "Determinism" -ScriptName "test-determinism.ps1"
            exit $(if ($result) { 0 } else { 1 })
        }
        "integration" {
            $result = Run-Test -TestName "Integration" -ScriptName "test-integration.ps1"
            exit $(if ($result) { 0 } else { 1 })
        }
        "integration-mini" {
            $result = Run-Test -TestName "Integration Mini" -ScriptName "test-integration-minimal.bat"
            exit $(if ($result) { 0 } else { 1 })
        }
        "dotnet" {
            $result = Run-Test -TestName ".NET Test" -ScriptName "test-dotnet.ps1"
            exit $(if ($result) { 0 } else { 1 })
        }
        "simple" {
            $result = Run-Test -TestName "Simple Test" -ScriptName "test-simple.bat"
            exit $(if ($result) { 0 } else { 1 })
        }
        "minimal" {
            $result = Run-Test -TestName "Minimal Test" -ScriptName "test-minimal.ps1"
            exit $(if ($result) { 0 } else { 1 })
        }
        "run" {
            $result = Run-Test -TestName "Run Test" -ScriptName "run-test.bat"
            exit $(if ($result) { 0 } else { 1 })
        }
        default {
            Write-Error "Unknown test: $Test"
            Write-Host "`nUse -List to see available tests or -Help for usage information" -ForegroundColor $InfoColor
            exit 1
        }
    }
}
catch {
    Write-Error "Unexpected error: $_"
    Write-Host "Stack trace: $($_.ScriptStackTrace)" -ForegroundColor $ErrorColor
    exit 1
}
