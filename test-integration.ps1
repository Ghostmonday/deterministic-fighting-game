# Integration Test Script for Neural Draft Game + Trading System
# This script tests the end-to-end integration between the fighting game and trading system

Write-Host "==============================================" -ForegroundColor Cyan
Write-Host "Neural Draft Integration Test" -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$GameSignalUrl = "http://localhost:7777/v1/signal/"
$TradingApiUrl = "http://localhost:5000"
$TestTimeoutSeconds = 30
$RetryDelaySeconds = 2

# Colors for output
$SuccessColor = "Green"
$ErrorColor = "Red"
$InfoColor = "Yellow"
$StepColor = "Cyan"

function Test-Step {
    param(
        [string]$Name,
        [scriptblock]$Action,
        [string]$SuccessMessage,
        [string]$ErrorMessage
    )

    Write-Host "`n[$Name]" -ForegroundColor $StepColor
    Write-Host "  Testing: $Name..." -NoNewline

    try {
        $result = & $Action
        Write-Host " ✓ " -ForegroundColor $SuccessColor -NoNewline
        Write-Host $SuccessMessage -ForegroundColor $SuccessColor

        if ($result -ne $null) {
            return $result
        }
        return $true
    }
    catch {
        Write-Host " ✗ " -ForegroundColor $ErrorColor -NoNewline
        Write-Host $ErrorMessage -ForegroundColor $ErrorColor
        Write-Host "  Error: $_" -ForegroundColor $ErrorColor
        return $false
    }
}

function Wait-ForService {
    param(
        [string]$Url,
        [int]$TimeoutSeconds = 30,
        [string]$ServiceName = "Service"
    )

    Write-Host "`nWaiting for $ServiceName to start..." -ForegroundColor $InfoColor
    $startTime = Get-Date
    $timeout = New-TimeSpan -Seconds $TimeoutSeconds

    while ((Get-Date) - $startTime -lt $timeout) {
        try {
            $response = Invoke-WebRequest -Uri $Url -Method Get -TimeoutSec 5 -ErrorAction Stop
            if ($response.StatusCode -eq 200) {
                Write-Host "  $ServiceName is ready!" -ForegroundColor $SuccessColor
                return $true
            }
        }
        catch {
            Write-Host "  Waiting... ($([math]::Round(($(Get-Date) - $startTime).TotalSeconds))s)" -ForegroundColor $InfoColor
            Start-Sleep -Seconds $RetryDelaySeconds
        }
    }

    Write-Host "  Timeout waiting for $ServiceName" -ForegroundColor $ErrorColor
    return $false
}

function Test-GameSignalEndpoint {
    Write-Host "`nTesting Game Signal Endpoint..." -ForegroundColor $StepColor

    try {
        $response = Invoke-WebRequest -Uri $GameSignalUrl -Method Get -TimeoutSec 10
        Write-Host "  Status: $($response.StatusCode)" -ForegroundColor $InfoColor

        if ($response.StatusCode -eq 200) {
            $json = $response.Content | ConvertFrom-Json
            Write-Host "  Response:" -ForegroundColor $InfoColor
            Write-Host "    Symbol: $($json.symbol)" -ForegroundColor $InfoColor
            Write-Host "    Frame: $($json.frame)" -ForegroundColor $InfoColor
            Write-Host "    P1 HP: $($json.p1Hp)" -ForegroundColor $InfoColor
            Write-Host "    P2 HP: $($json.p2Hp)" -ForegroundColor $InfoColor
            Write-Host "    Sentiment: $($json.sentimentMilli) milli" -ForegroundColor $InfoColor
            Write-Host "    State Hash: $($json.stateHash)" -ForegroundColor $InfoColor

            # Validate response structure
            $requiredFields = @("symbol", "frame", "p1Hp", "p2Hp", "sentimentMilli", "stateHash")
            $missingFields = @()

            foreach ($field in $requiredFields) {
                if (-not ($json.PSObject.Properties.Name -contains $field)) {
                    $missingFields += $field
                }
            }

            if ($missingFields.Count -gt 0) {
                Write-Host "  ✗ Missing fields: $($missingFields -join ', ')" -ForegroundColor $ErrorColor
                return $false
            }

            Write-Host "  ✓ Game signal endpoint is working correctly!" -ForegroundColor $SuccessColor
            return $true
        }
        else {
            Write-Host "  ✗ Unexpected status code: $($response.StatusCode)" -ForegroundColor $ErrorColor
            return $false
        }
    }
    catch {
        Write-Host "  ✗ Failed to connect to game signal endpoint" -ForegroundColor $ErrorColor
        Write-Host "  Error: $_" -ForegroundColor $ErrorColor
        return $false
    }
}

function Test-TradingApi {
    Write-Host "`nTesting Trading API Endpoints..." -ForegroundColor $StepColor

    $endpoints = @(
        @{Name = "Health Check"; Path = "/health"; Method = "GET"},
        @{Name = "Live Dashboard"; Path = "/paper/live"; Method = "GET"},
        @{Name = "Statistics"; Path = "/paper/stats"; Method = "GET"},
        @{Name = "Trade History"; Path = "/paper/trades?pageSize=5"; Method = "GET"}
    )

    $allPassed = $true

    foreach ($endpoint in $endpoints) {
        $url = "$TradingApiUrl$($endpoint.Path)"
        Write-Host "  Testing $($endpoint.Name)..." -NoNewline

        try {
            $response = Invoke-WebRequest -Uri $url -Method $endpoint.Method -TimeoutSec 10
            if ($response.StatusCode -eq 200) {
                Write-Host " ✓" -ForegroundColor $SuccessColor
            }
            else {
                Write-Host " ✗ (Status: $($response.StatusCode))" -ForegroundColor $ErrorColor
                $allPassed = $false
            }
        }
        catch {
            Write-Host " ✗ (Connection failed)" -ForegroundColor $ErrorColor
            $allPassed = $false
        }
    }

    return $allPassed
}

function Test-SignalProcessing {
    Write-Host "`nTesting Signal Processing..." -ForegroundColor $StepColor

    $testSignals = @(
        @{Symbol = "SDNA"; SentimentMilli = 500; P1Hp = 100; P2Hp = 50; Description = "Strong LONG signal"},
        @{Symbol = "SDNA"; SentimentMilli = -500; P1Hp = 50; P2Hp = 100; Description = "Strong SHORT signal"},
        @{Symbol = "SDNA"; SentimentMilli = 100; P1Hp = 80; P2Hp = 70; Description = "Weak LONG signal"},
        @{Symbol = "SDNA"; SentimentMilli = -100; P1Hp = 70; P2Hp = 80; Description = "Weak SHORT signal"}
    )

    $allPassed = $true

    foreach ($test in $testSignals) {
        $url = "$TradingApiUrl/paper/test-signal"
        $body = @{
            symbol = $test.Symbol
            sentimentMilli = $test.SentimentMilli
            frame = (Get-Date).Ticks % 1000000
            p1Hp = $test.P1Hp
            p2Hp = $test.P2Hp
            stateHash = 0
        } | ConvertTo-Json

        Write-Host "  Testing $($test.Description)..." -NoNewline

        try {
            $response = Invoke-WebRequest -Uri $url -Method POST -Body $body -ContentType "application/json" -TimeoutSec 10

            if ($response.StatusCode -eq 200) {
                $result = $response.Content | ConvertFrom-Json
                Write-Host " ✓ (Open trades: $($result.OpenTrades))" -ForegroundColor $SuccessColor

                # Log the decision
                if ($test.SentimentMilli -ge 200) {
                    Write-Host "    Expected: LONG position" -ForegroundColor $InfoColor
                }
                elseif ($test.SentimentMilli -le -200) {
                    Write-Host "    Expected: SHORT position" -ForegroundColor $InfoColor
                }
                else {
                    Write-Host "    Expected: FLAT (no position)" -ForegroundColor $InfoColor
                }
            }
            else {
                Write-Host " ✗ (Status: $($response.StatusCode))" -ForegroundColor $ErrorColor
                $allPassed = $false
            }
        }
        catch {
            Write-Host " ✗ (Connection failed)" -ForegroundColor $ErrorColor
            Write-Host "    Error: $_" -ForegroundColor $ErrorColor
            $allPassed = $false
        }

        # Small delay between tests
        Start-Sleep -Milliseconds 500
    }

    return $allPassed
}

function Show-IntegrationStatus {
    Write-Host "`nIntegration Status Summary" -ForegroundColor Cyan
    Write-Host "=========================" -ForegroundColor Cyan

    # Check game signal endpoint
    Write-Host "`n1. Game Signal Endpoint ($GameSignalUrl)" -ForegroundColor $StepColor
    try {
        $gameResponse = Invoke-WebRequest -Uri $GameSignalUrl -Method Get -TimeoutSec 5
        Write-Host "   Status: ONLINE" -ForegroundColor $SuccessColor
        $gameJson = $gameResponse.Content | ConvertFrom-Json
        Write-Host "   Latest Frame: $($gameJson.frame)" -ForegroundColor $InfoColor
        Write-Host "   Sentiment: $($gameJson.sentimentMilli) milli" -ForegroundColor $InfoColor
    }
    catch {
        Write-Host "   Status: OFFLINE" -ForegroundColor $ErrorColor
    }

    # Check trading API
    Write-Host "`n2. Trading API ($TradingApiUrl)" -ForegroundColor $StepColor
    try {
        $healthResponse = Invoke-WebRequest -Uri "$TradingApiUrl/health" -Method Get -TimeoutSec 5
        if ($healthResponse.StatusCode -eq 200) {
            Write-Host "   Status: ONLINE" -ForegroundColor $SuccessColor

            # Get live dashboard
            $dashboardResponse = Invoke-WebRequest -Uri "$TradingApiUrl/paper/live" -Method Get -TimeoutSec 5
            $dashboard = $dashboardResponse.Content | ConvertFrom-Json

            Write-Host "   Open Positions: $($dashboard.OpenPositions.Count)" -ForegroundColor $InfoColor
            Write-Host "   Win Rate: $($dashboard.Statistics.WinRate)%" -ForegroundColor $InfoColor
            Write-Host "   Total Return: $($dashboard.Statistics.TotalReturn)%" -ForegroundColor $InfoColor
        }
        else {
            Write-Host "   Status: ERROR (Status: $($healthResponse.StatusCode))" -ForegroundColor $ErrorColor
        }
    }
    catch {
        Write-Host "   Status: OFFLINE" -ForegroundColor $ErrorColor
    }

    # Check signal flow
    Write-Host "`n3. Signal Flow" -ForegroundColor $StepColor
    try {
        $gameSignal = Invoke-WebRequest -Uri $GameSignalUrl -Method Get -TimeoutSec 5
        $tradingHealth = Invoke-WebRequest -Uri "$TradingApiUrl/health" -Method Get -TimeoutSec 5

        if ($gameSignal.StatusCode -eq 200 -and $tradingHealth.StatusCode -eq 200) {
            Write-Host "   Status: CONNECTED" -ForegroundColor $SuccessColor
            Write-Host "   Game → Trading: ✓" -ForegroundColor $SuccessColor

            # Test a signal
            $testBody = @{
                symbol = "SDNA"
                sentimentMilli = 300
                frame = 999999
                p1Hp = 100
                p2Hp = 50
                stateHash = 12345
            } | ConvertTo-Json

            $testResponse = Invoke-WebRequest -Uri "$TradingApiUrl/paper/test-signal" -Method POST -Body $testBody -ContentType "application/json" -TimeoutSec 5
            if ($testResponse.StatusCode -eq 200) {
                Write-Host "   Signal Processing: ✓" -ForegroundColor $SuccessColor
            }
            else {
                Write-Host "   Signal Processing: ✗" -ForegroundColor $ErrorColor
            }
        }
        else {
            Write-Host "   Status: DISCONNECTED" -ForegroundColor $ErrorColor
        }
    }
    catch {
        Write-Host "   Status: ERROR" -ForegroundColor $ErrorColor
    }
}

function Show-Instructions {
    Write-Host "`nSetup Instructions" -ForegroundColor Cyan
    Write-Host "==================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "1. Start the Game (Unity):" -ForegroundColor $InfoColor
    Write-Host "   - Open the Unity project" -ForegroundColor White
    Write-Host "   - Run the game in the editor" -ForegroundColor White
    Write-Host "   - Signal endpoint will start automatically on port 7777" -ForegroundColor White
    Write-Host ""
    Write-Host "2. Start the Trading System:" -ForegroundColor $InfoColor
    Write-Host "   cd src/trading" -ForegroundColor White
    Write-Host "   dotnet run --environment Development" -ForegroundColor White
    Write-Host "   - API will start on port 5000" -ForegroundColor White
    Write-Host ""
    Write-Host "3. Run this test script:" -ForegroundColor $InfoColor
    Write-Host "   .\test-integration.ps1" -ForegroundColor White
    Write-Host ""
    Write-Host "4. Monitor the integration:" -ForegroundColor $InfoColor
    Write-Host "   - Game Console: Signal endpoint logs" -ForegroundColor White
    Write-Host "   - Trading Console: Signal processing logs" -ForegroundColor White
    Write-Host "   - Browser: http://localhost:5000/paper/live" -ForegroundColor White
}

# Main execution
Write-Host "Starting Integration Tests..." -ForegroundColor $InfoColor
Write-Host ""

# Show instructions first
Show-Instructions

Write-Host "`nPress any key to begin tests, or Ctrl+C to cancel..." -ForegroundColor $InfoColor
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

# Run tests
$testsPassed = 0
$testsTotal = 0

Write-Host "`n" + ("="*50) -ForegroundColor Cyan

# Test 1: Wait for services
$testsTotal++
if (Wait-ForService -Url $GameSignalUrl -TimeoutSeconds $TestTimeoutSeconds -ServiceName "Game Signal Endpoint") {
    $testsPassed++
}

$testsTotal++
if (Wait-ForService -Url "$TradingApiUrl/health" -TimeoutSeconds $TestTimeoutSeconds -ServiceName "Trading API") {
    $testsPassed++
}

# Test 2: Game signal endpoint
$testsTotal++
if (Test-GameSignalEndpoint) {
    $testsPassed++
}

# Test 3: Trading API
$testsTotal++
if (Test-TradingApi) {
    $testsPassed++
}

# Test 4: Signal processing
$testsTotal++
if (Test-SignalProcessing) {
    $testsPassed++
}

# Show summary
Write-Host "`n" + ("="*50) -ForegroundColor Cyan
Write-Host "Test Summary" -ForegroundColor Cyan
Write-Host "============" -ForegroundColor Cyan
Write-Host "Tests Passed: $testsPassed/$testsTotal" -ForegroundColor $(if ($testsPassed -eq $testsTotal) { $SuccessColor } else { $ErrorColor })

if ($testsPassed -eq $testsTotal) {
    Write-Host "`n🎉 All tests passed! Integration is working correctly." -ForegroundColor $SuccessColor
    Write-Host "`nNext steps:" -ForegroundColor $InfoColor
    Write-Host "1. Play the game and watch trades execute" -ForegroundColor White
    Write-Host "2. Check http://localhost:5000/paper/live for real-time updates" -ForegroundColor White
    Write-Host "3. Monitor console logs for signal processing" -ForegroundColor White
}
else {
    Write-Host "`n⚠️  Some tests failed. Check the errors above." -ForegroundColor $ErrorColor
    Write-Host "`nTroubleshooting:" -ForegroundColor $InfoColor
    Write-Host "1. Ensure both services are running" -ForegroundColor White
    Write-Host "2. Check firewall/port settings (7777, 5000)" -ForegroundColor White
    Write-Host "3. Verify Unity console for HttpListener errors" -ForegroundColor White
    Write-Host "4. Check trading system logs for connection errors" -ForegroundColor White
}

# Show current status
Write-Host "`n" + ("="*50) -ForegroundColor Cyan
Show-IntegrationStatus

Write-Host "`nTest completed at $(Get-Date -Format 'HH:mm:ss')" -ForegroundColor $InfoColor
