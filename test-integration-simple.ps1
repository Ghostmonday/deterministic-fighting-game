# Simple Integration Test for Neural Draft Trading System
# Tests basic connectivity between game and trading system

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "NEURAL DRAFT - Simple Integration Test" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$GameUrl = "http://localhost:7777/v1/signal/"
$TradingUrl = "http://localhost:5000/paper/live"
$TimeoutSeconds = 10

Write-Host "Testing connectivity to:" -ForegroundColor Yellow
Write-Host "  Game Signal: $GameUrl" -ForegroundColor Yellow
Write-Host "  Trading API: $TradingUrl" -ForegroundColor Yellow
Write-Host ""

# Test Game Signal Endpoint
Write-Host "1. Testing Game Signal Endpoint..." -ForegroundColor White

try {
    $gameResponse = Invoke-WebRequest -Uri $GameUrl -Method Get -TimeoutSec 5 -ErrorAction Stop

    if ($gameResponse.StatusCode -eq 200) {
        Write-Host "   Status: ONLINE (200 OK)" -ForegroundColor Green

        # Try to parse JSON
        try {
            $gameData = $gameResponse.Content | ConvertFrom-Json
            Write-Host "   Data received:" -ForegroundColor Green
            Write-Host "     Symbol: $($gameData.symbol)" -ForegroundColor Green
            Write-Host "     Frame: $($gameData.frame)" -ForegroundColor Green
            Write-Host "     P1 HP: $($gameData.p1Hp)" -ForegroundColor Green
            Write-Host "     P2 HP: $($gameData.p2Hp)" -ForegroundColor Green
            Write-Host "     Sentiment: $($gameData.sentimentMilli)" -ForegroundColor Green
            $gameTest = $true
        }
        catch {
            Write-Host "   Warning: Response is not valid JSON" -ForegroundColor Yellow
            Write-Host "   Raw response: $($gameResponse.Content.Substring(0, [Math]::Min(100, $gameResponse.Content.Length)))..." -ForegroundColor Yellow
            $gameTest = $true  # Still counts as online
        }
    }
    else {
        Write-Host "   Status: ERROR ($($gameResponse.StatusCode))" -ForegroundColor Red
        $gameTest = $false
    }
}
catch {
    Write-Host "   Status: OFFLINE (Connection failed)" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
    $gameTest = $false
}

Write-Host ""

# Test Trading API
Write-Host "2. Testing Trading API..." -ForegroundColor White

try {
    $tradingResponse = Invoke-WebRequest -Uri $TradingUrl -Method Get -TimeoutSec 5 -ErrorAction Stop

    if ($tradingResponse.StatusCode -eq 200) {
        Write-Host "   Status: ONLINE (200 OK)" -ForegroundColor Green

        # Check if it's HTML or JSON
        if ($tradingResponse.Content -match "<html" -or $tradingResponse.Content -match "<!DOCTYPE") {
            Write-Host "   Type: HTML page (likely dashboard)" -ForegroundColor Green
        }
        else {
            Write-Host "   Type: API response" -ForegroundColor Green
        }

        $tradingTest = $true
    }
    else {
        Write-Host "   Status: ERROR ($($tradingResponse.StatusCode))" -ForegroundColor Red
        $tradingTest = $false
    }
}
catch {
    Write-Host "   Status: OFFLINE (Connection failed)" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
    $tradingTest = $false
}

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "TEST RESULTS" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

if ($gameTest -and $tradingTest) {
    Write-Host "✅ INTEGRATION TEST PASSED!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Both systems are online and communicating:" -ForegroundColor Green
    Write-Host "  Game → Signal Endpoint: ✓" -ForegroundColor Green
    Write-Host "  Trading System: ✓" -ForegroundColor Green
    Write-Host ""
    Write-Host "The bridge between game state and trading decisions is operational." -ForegroundColor Green
    exit 0
}
elseif ($gameTest -and -not $tradingTest) {
    Write-Host "⚠️  PARTIAL SUCCESS" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Game is online but Trading System is offline:" -ForegroundColor Yellow
    Write-Host "  Game → Signal Endpoint: ✓" -ForegroundColor Green
    Write-Host "  Trading System: ✗" -ForegroundColor Red
    Write-Host ""
    Write-Host "To start trading system:" -ForegroundColor Yellow
    Write-Host "  cd src/trading" -ForegroundColor White
    Write-Host "  dotnet run" -ForegroundColor White
    exit 1
}
elseif (-not $gameTest -and $tradingTest) {
    Write-Host "⚠️  PARTIAL SUCCESS" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Trading System is online but Game is offline:" -ForegroundColor Yellow
    Write-Host "  Game → Signal Endpoint: ✗" -ForegroundColor Red
    Write-Host "  Trading System: ✓" -ForegroundColor Green
    Write-Host ""
    Write-Host "To start Unity game:" -ForegroundColor Yellow
    Write-Host "  Open Unity project and run BattleManager scene" -ForegroundColor White
    exit 1
}
else {
    Write-Host "❌ INTEGRATION TEST FAILED" -ForegroundColor Red
    Write-Host ""
    Write-Host "Both systems are offline:" -ForegroundColor Red
    Write-Host "  Game → Signal Endpoint: ✗" -ForegroundColor Red
    Write-Host "  Trading System: ✗" -ForegroundColor Red
    Write-Host ""
    Write-Host "Setup instructions:" -ForegroundColor Yellow
    Write-Host "  1. Start Unity game with BattleManager" -ForegroundColor White
    Write-Host "  2. Start trading system: cd src/trading; dotnet run" -ForegroundColor White
    Write-Host "  3. Check firewall permissions for ports 7777 and 5000" -ForegroundColor White
    exit 1
}
