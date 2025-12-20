@echo off
echo ================================================
echo NEURAL DRAFT - Minimal Integration Test
echo ================================================
echo.
echo Testing connectivity between:
echo   Game Signal: http://localhost:7777/v1/signal/
echo   Trading API: http://localhost:5000/paper/live
echo.

REM Test Game Signal Endpoint
echo 1. Testing Game Signal Endpoint...
powershell -Command "try { $r = Invoke-WebRequest -Uri 'http://localhost:7777/v1/signal/' -TimeoutSec 5; if ($r.StatusCode -eq 200) { echo '   Status: ONLINE (200 OK)'; exit 0 } else { echo '   Status: ERROR (' + $r.StatusCode + ')'; exit 1 } } catch { echo '   Status: OFFLINE'; echo '   Error: ' + $_.Exception.Message; exit 1 }"
set GAME_TEST=%ERRORLEVEL%

echo.

REM Test Trading API
echo 2. Testing Trading API...
powershell -Command "try { $r = Invoke-WebRequest -Uri 'http://localhost:5000/paper/live' -TimeoutSec 5; if ($r.StatusCode -eq 200) { echo '   Status: ONLINE (200 OK)'; exit 0 } else { echo '   Status: ERROR (' + $r.StatusCode + ')'; exit 1 } } catch { echo '   Status: OFFLINE'; echo '   Error: ' + $_.Exception.Message; exit 1 }"
set TRADING_TEST=%ERRORLEVEL%

echo.
echo ================================================
echo TEST RESULTS
echo ================================================
echo.

if %GAME_TEST% equ 0 (
    echo Game Signal: ✓ ONLINE
) else (
    echo Game Signal: ✗ OFFLINE
)

if %TRADING_TEST% equ 0 (
    echo Trading API: ✓ ONLINE
) else (
    echo Trading API: ✗ OFFLINE
)

echo.

if %GAME_TEST% equ 0 if %TRADING_TEST% equ 0 (
    echo ✅ INTEGRATION TEST PASSED!
    echo.
    echo Both systems are online and communicating.
    echo The bridge between game state and trading decisions is operational.
    exit /b 0
) else if %GAME_TEST% equ 0 (
    echo ⚠️  PARTIAL SUCCESS
    echo.
    echo Game is online but Trading System is offline.
    echo To start trading system:
    echo   cd src\trading
    echo   dotnet run
    exit /b 1
) else if %TRADING_TEST% equ 0 (
    echo ⚠️  PARTIAL SUCCESS
    echo.
    echo Trading System is online but Game is offline.
    echo To start Unity game:
    echo   Open Unity project and run BattleManager scene
    exit /b 1
) else (
    echo ❌ INTEGRATION TEST FAILED
    echo.
    echo Both systems are offline.
    echo.
    echo Setup instructions:
    echo   1. Start Unity game with BattleManager
    echo   2. Start trading system: cd src\trading ^&^& dotnet run
    echo   3. Check firewall permissions for ports 7777 and 5000
    exit /b 1
)
