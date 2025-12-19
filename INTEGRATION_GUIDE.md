# Neural Draft Integration Guide
## Game Engine + Paper Trading System

This guide explains how to connect the deterministic fighting game engine with the paper trading system for real-time financial combat.

## Overview

The integration creates a bidirectional flow:
1. **Game → Trading**: Game state (health, position, combos) generates trading signals
2. **Trading → Game**: Trading decisions (LONG/SHORT) are displayed in-game

## Architecture

```
┌─────────────────┐      HTTP      ┌─────────────────┐
│   Unity Game    │◄──────────────►│  Trading System │
│                 │    (7777)      │                 │
│  • BattleManager│                │  • TradingEngine│
│  • HttpListener │                │  • TradingService│
│  • Signal Gen   │                │  • REST API     │
└─────────────────┘                └─────────────────┘
         │                                   │
         ▼                                   ▼
   Game State Snapshot                Trade Execution
   (JSON over HTTP)                   (Based on signals)
```

## Quick Start

### 1. Start the Game (Unity)

```bash
# Open Unity project
# Run the game in editor
# Signal endpoint starts automatically on port 7777
```

**Expected Console Output:**
```
Signal endpoint listening: http://localhost:7777/v1/signal/
```

### 2. Start the Trading System

```bash
cd src/trading
dotnet run --environment Development
```

**Expected Console Output:**
```
TradingService initialized with 3 symbols
Game signal client initialized for http://localhost:7777/v1/signal/
Paper Trading System started successfully
API Documentation: https://localhost:5000/api-docs
```

### 3. Test the Connection

```bash
# Test game signal endpoint
curl http://localhost:7777/v1/signal/

# Test trading API
curl http://localhost:5000/paper/live
```

## Signal Flow

### Game State → Trading Signal

The game exposes a simple HTTP endpoint that provides:

```json
{
  "symbol": "SDNA",
  "frame": 12345,
  "p1Hp": 950,
  "p2Hp": 875,
  "sentimentMilli": 375,
  "stateHash": 123456789
}
```

**Signal Generation Logic:**
```csharp
// Simple deterministic mapping
int diff = player1.health - player2.health;
int sentimentMilli = Math.Clamp(diff * 5, -1000, 1000);
```

### Trading Decision Rules

| Sentiment | Action | Threshold |
|-----------|--------|-----------|
| ≥ +200 | LONG | Player 1 has ≥ 40 HP advantage |
| ≤ -200 | SHORT | Player 2 has ≥ 40 HP advantage |
| -199 to +199 | FLAT | No position |

## Configuration

### Game Configuration (BattleManager.cs)

```csharp
private const string SIGNAL_PREFIX = "http://localhost:7777/v1/signal/";
```

**To change port:** Modify the `SIGNAL_PREFIX` constant.

### Trading System Configuration

**Development (`appsettings.Development.json`):**
```json
{
  "TradingService": {
    "GameSignalEndpoint": "http://localhost:7777/v1/signal/",
    "EnableGameSignals": true,
    "GameSignalPollingMs": 100,
    "GameSignalThresholds": {
      "LongThresholdMilli": 200,
      "ShortThresholdMilli": -200,
      "FlatZoneMilli": 50
    }
  }
}
```

**Production (`appsettings.json`):**
```json
{
  "TradingService": {
    "GameSignalEndpoint": "http://localhost:7777/v1/signal/",
    "EnableGameSignals": true,
    "GameSignalPollingMs": 100
  }
}
```

## API Endpoints

### Game Signal Endpoint
- **URL**: `http://localhost:7777/v1/signal/`
- **Method**: GET
- **Response**: JSON game state snapshot
- **Polling**: Trading system polls every 100ms (10Hz)

### Trading System APIs
- **Dashboard**: `http://localhost:5000/paper/live`
- **Statistics**: `http://localhost:5000/paper/stats`
- **Trade History**: `http://localhost:5000/paper/trades`
- **Health Check**: `http://localhost:5000/health`
- **Test Signal**: `http://localhost:5000/paper/test-signal` (POST)
- **Swagger UI**: `http://localhost:5000/api-docs`

## Testing the Integration

### Manual Testing

1. **Check game signal:**
   ```bash
   curl http://localhost:7777/v1/signal/
   ```

2. **Send test signal:**
   ```bash
   curl -X POST http://localhost:5000/paper/test-signal \
     -H "Content-Type: application/json" \
     -d '{"symbol":"SDNA","sentimentMilli":500,"frame":999999,"p1Hp":100,"p2Hp":50}'
   ```

3. **Monitor dashboard:**
   ```bash
   curl http://localhost:5000/paper/live | jq .
   ```

### Automated Testing

Run the integration test script:
```powershell
.\test-integration.ps1
```

## Troubleshooting

### Common Issues

1. **Port conflicts:**
   ```
   Failed to start signal server: Access denied
   ```
   **Solution:** Change port in `SIGNAL_PREFIX` or run Unity as administrator.

2. **Connection refused:**
   ```
   Error fetching game signal
   ```
   **Solution:** Ensure Unity game is running before starting trading system.

3. **CORS errors:**
   ```
   Cross-Origin Request Blocked
   ```
   **Solution:** Trading system is configured for CORS in development mode.

4. **HttpListener permissions (Windows):**
   ```
   System.Net.HttpListenerException (5)
   ```
   **Solution:** Run this command as administrator:
   ```bash
   netsh http add urlacl url=http://localhost:7777/ user=Everyone
   ```

### Logging

**Game Logs (Unity Console):**
- Signal endpoint startup
- HTTP request handling
- Game state updates

**Trading System Logs:**
```
dotnet run --environment Development
```
- Game signal polling
- Trade decisions
- API requests
- Health checks

## Development Workflow

### 1. Prototype Phase
- ✅ Basic signal endpoint (health differential)
- ✅ Simple trading rules (LONG/SHORT thresholds)
- ✅ Console logging

### 2. Enhancement Phase
- [ ] Advanced signal generation (position, combos, meter)
- [ ] In-game trading display
- [ ] Real-time dashboard updates

### 3. Production Phase
- [ ] Error handling and retry logic
- [ ] Performance optimization
- [ ] Security hardening

## Signal Mapping Examples

### Current Implementation
```csharp
// Health differential only
sentimentMilli = (p1Hp - p2Hp) * 5;
```

### Advanced Mappings (Future)
```csharp
// Multi-factor sentiment
sentimentMilli = 
    (healthDiff * 3) +          // Health advantage
    (cornerControl * 2) +       // Stage control  
    (comboMomentum * 4) +       // Recent combos
    (meterAdvantage * 1);       // Special meter
```

## Performance Considerations

- **Polling Interval**: 100ms (10Hz) for real-time responsiveness
- **Response Size**: < 1KB per signal
- **Memory Usage**: Volatile snapshots prevent allocations
- **Thread Safety**: Lock-free reads with volatile variables

## Security Notes

### Development Mode
- CORS enabled for all origins
- No authentication required
- HTTP only (no HTTPS)

### Production Considerations
- Enable HTTPS
- Add API key authentication
- Restrict CORS origins
- Rate limiting
- Request validation

## Monitoring

### Health Checks
```bash
# Game signal health
curl http://localhost:7777/v1/signal/

# Trading system health
curl http://localhost:5000/health
```

### Metrics
- Signal latency (game → trade decision)
- Trade execution rate
- Win rate correlation with game performance
- System uptime

## Next Steps

### Immediate (Day 1)
1. Verify end-to-end signal flow
2. Test with actual gameplay
3. Monitor console logs

### Short-term (Week 1)
1. Add visual feedback in-game
2. Implement advanced signal factors
3. Create integration tests

### Long-term (Month 1)
1. Add WebSocket for real-time updates
2. Implement backtesting framework
3. Create admin dashboard

## Support

### Debugging Tools
- **Integration Test Script**: `test-integration.ps1`
- **API Documentation**: `http://localhost:5000/api-docs`
- **Log Files**: `logs/trading-*.log`

### Common Debug Commands
```bash
# Check if ports are listening
netstat -ano | findstr :7777
netstat -ano | findstr :5000

# Test connectivity
Test-NetConnection -ComputerName localhost -Port 7777
Test-NetConnection -ComputerName localhost -Port 5000

# View recent logs
tail -f logs/trading-$(date +%Y-%m-%d).log
```

## Contributing

When extending the integration:

1. **Maintain determinism**: All signals must be reproducible
2. **Keep it simple**: Start with minimal viable implementation
3. **Add tests**: Update `test-integration.ps1` for new features
4. **Update docs**: Keep this guide current with changes

## License

Proprietary - Neural Draft LLC

---

*Last Updated: $(date)*
*Integration Version: 1.0.0*