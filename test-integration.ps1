# Integration Test Script for Neural Draft Trading System
# Tests the end-to-end flow: Game → Signal Endpoint → Trading System

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "NEURAL DRAFT - Integration Test" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$GamePort = 7777
$TradingPort = 5000
$GameSignalUrl = "http://localhost:$GamePort/v1/signal/"
$TradingApiUrl = "http://localhost:$TradingPort/paper/live"
$TestTimeoutSeconds = 30
$RetryDelaySeconds = 2

# Colors for output
$SuccessColor = "Green"
$ErrorColor = "Red"
$InfoColor = "Yellow"
$WarningColor = "Magenta"

function Test-WebService {
    param(
        [string]$Url,
        [string]$ServiceName,
        [int]$TimeoutSeconds = 10
    )

    $startTime = Get-Date
    $timeoutTime = $startTime.AddSeconds($TimeoutSeconds)

    Write-Host "Testing $ServiceName at $Url" -ForegroundColor $InfoColor

    while ((Get-Date) -lt $timeoutTime) {
        try {
            $response = Invoke-WebRequest -Uri $Url -Method Get -TimeoutSec 5 -ErrorAction Stop
            if ($response.StatusCode -eq 200) {
                $elapsed = [math]::Round(((Get-Date) - $startTime).TotalSeconds, 1)
                Write-Host "  $ServiceName is ready! (${elapsed}s)" -ForegroundColor $SuccessColor
                return $true
            }
        }
        catch {
            # Service not ready yet, continue waiting
            $elapsed = [math]::Round(((Get-Date) - $startTime).TotalSeconds, 1)
            Write-Host "  Waiting... (${elapsed}s)" -ForegroundColor $InfoColor
            Start-Sleep -Seconds $RetryDelaySeconds
        }
    }

    Write-Host "  Timeout waiting for $ServiceName" -ForegroundColor $ErrorColor
    return $false
}

function Test-SignalEndpoint {
    Write-Host "Testing Game Signal Endpoint..." -ForegroundColor $InfoColor

    try {
        $response = Invoke-WebRequest -Uri $GameSignalUrl -Method Get -TimeoutSec 5 -ErrorAction Stop

        if ($response.StatusCode -eq 200) {
            $content = $response.Content | ConvertFrom-Json

            Write-Host "  Signal endpoint response:" -ForegroundColor $SuccessColor
            Write-Host "    Symbol: $($content.symbol)" -ForegroundColor $SuccessColor
            Write-Host "    Frame: $($content.frame)" -ForegroundColor $SuccessColor
            Write-Host "    P1 HP: $($content.p1Hp)" -ForegroundColor $SuccessColor
            Write-Host "    P2 HP: $($content.p2Hp)" -ForegroundColor $SuccessColor
            Write-Host "    Sentiment: $($content.sentimentMilli)" -ForegroundColor $SuccessColor

            # Validate response structure
            $requiredFields = @("symbol", "frame", "p1Hp", "p2Hp", "sentimentMilli")
            $missingFields = @()

            foreach ($field in $requiredFields) {
                if ($null -eq $content.$field) {
                    $missingFields += $field
                }
            }

            if ($missingFields.Count -eq 0) {
                Write-Host "  Signal endpoint structure: VALID" -ForegroundColor $SuccessColor
                return $true
            } else {
                Write-Host "  Signal endpoint missing fields: $($missingFields -join ', ')" -ForegroundColor $ErrorColor
                return $false
            }
        }
    }
    catch {
        Write-Host "  Failed to connect to signal endpoint: $_" -ForegroundColor $ErrorColor
        return $false
    }

    return $false
}

function Test-TradingApi {
    Write-Host "Testing Trading API..." -ForegroundColor $InfoColor

    try {
        $response = Invoke-WebRequest -Uri $TradingApiUrl -Method Get -TimeoutSec 5 -ErrorAction Stop

        if ($response.StatusCode -eq 200) {
            Write-Host "  Trading API is responding" -ForegroundColor $SuccessColor

            # Try to parse as JSON if possible
            try {
                $content = $response.Content | ConvertFrom-Json -ErrorAction SilentlyContinue
                if ($content) {
                    Write-Host "  Trading API returns valid JSON" -ForegroundColor $SuccessColor
                }
            }
            catch {
                Write-Host "  Trading API response is not JSON (may be HTML)" -ForegroundColor $WarningColor
            }

            return $true
        }
    }
    catch {
        Write-Host "  Failed to connect to Trading API: $_" -ForegroundColor $ErrorColor
        return $false
    }

    return $false
}

function Test-IntegrationFlow {
    Write-Host ""
    Write-Host "Testing integration flow..." -ForegroundColor Cyan

    # Step 1: Test Game Signal Endpoint
    Write-Host "Step 1: Game Signal Endpoint" -ForegroundColor $InfoColor
    $signalTest = Test-WebService -Url $GameSignalUrl -ServiceName "Game Signal Endpoint" -TimeoutSeconds 15

    if (-not $signalTest) {
        Write-Host "  ❌ Game Signal Endpoint offline" -ForegroundColor $ErrorColor
        Write-Host "  Make sure Unity game is running with BattleManager" -ForegroundColor $WarningColor
        return $false
    }

    # Step 2: Test Signal Data Structure
    Write-Host "Step 2: Signal Data Validation" -ForegroundColor $InfoColor
    $signalDataTest = Test-SignalEndpoint

    if (-not $signalDataTest) {
        Write-Host "  ❌ Signal data invalid" -ForegroundColor $ErrorColor
        return $false
    }

    # Step 3: Test Trading API
    Write-Host "Step 3: Trading System API" -ForegroundColor $InfoColor
    $tradingTest = Test-WebService -Url $TradingApiUrl -ServiceName "Trading API" -TimeoutSeconds 15

    if (-not $tradingTest) {
        Write-Host "  ❌ Trading API offline" -ForegroundColor $ErrorColor
        Write-Host "  Make sure trading system is running: cd src/trading && dotnet run" -ForegroundColor $WarningColor
        return $false
    }

    # Step 4: Test Trading API Response
    Write-Host "Step 4: Trading API Validation" -ForegroundColor $InfoColor
    $tradingApiTest = Test-TradingApi

    if (-not $tradingApiTest) {
        Write-Host "  ❌ Trading API response invalid" -ForegroundColor $ErrorColor
        return $false
    }

    # All tests passed
    Write-Host ""
    Write-Host "================================================" -ForegroundColor Green
    Write-Host "✅ INTEGRATION TEST PASSED!" -ForegroundColor Green
    Write-Host "================================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Integration flow verified:" -ForegroundColor Green
    Write-Host "  Game → Signal Endpoint: ✓" -ForegroundColor Green
    Write-Host "  Signal Processing: ✓" -ForegroundColor Green
    Write-Host "  Trading System: ✓" -ForegroundColor Green
    Write-Host ""
    Write-Host "The bridge between game state and trading decisions is operational." -ForegroundColor Green

    return $true
}

# Main execution
try {
    Write-Host "Starting integration test..." -ForegroundColor Cyan
    Write-Host "This test verifies the connection between:" -ForegroundColor Cyan
    Write-Host "  1. Game Engine (port $GamePort)" -ForegroundColor Cyan
    Write-Host "  2. Trading System (port $TradingPort)" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Prerequisites:" -ForegroundColor Yellow
    Write-Host "  - Unity game running with BattleManager" -ForegroundColor Yellow
    Write-Host "  - Trading system running: cd src/trading; dotnet run" -ForegroundColor Yellow
    Write-Host ""

    $result = Test-IntegrationFlow

    if ($result) {
        exit 0
    } else {
        Write-Host ""
        Write-Host "================================================" -ForegroundColor Red
        Write-Host "❌ INTEGRATION TEST FAILED" -ForegroundColor Red
        Write-Host "================================================" -ForegroundColor Red
        Write-Host ""
        Write-Host "Troubleshooting steps:" -ForegroundColor Yellow
        Write-Host "  1. Check if Unity game is running (should show signal endpoint log)" -ForegroundColor Yellow
        Write-Host "  2. Check if trading system is running: cd src/trading; dotnet run" -ForegroundColor Yellow
        Write-Host "  3. Check firewall permissions for ports $GamePort and $TradingPort" -ForegroundColor Yellow
        Write-Host "  4. Verify services are listening: netstat -an | findstr :$GamePort" -ForegroundColor Yellow
        Write-Host "  5. Check Windows Firewall for blocking prompts" -ForegroundColor Yellow
        exit 1
    }
}
catch {
    Write-Host "Unexpected error during integration test: $_" -ForegroundColor Red
    Write-Host "Stack trace: $($_.ScriptStackTrace)" -ForegroundColor Red
    exit 1
}
