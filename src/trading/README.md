# Neural Draft Paper Trading System

A C# implementation of a paper trading system that converts sentiment signals from the deterministic fighting game engine into simulated trading decisions.

## Overview

This system monitors sentiment signals from the `/v1/physics/signal` endpoint and executes paper trades based on predefined rules:
- **LONG** positions when sentiment > +0.35
- **SHORT** positions when sentiment < -0.35
- **Exit conditions**: reversal, 3% take-profit, -1.5% stop-loss, or 24h max hold

## Architecture

```
src/trading/
├── core/
│   └── TradingEngine.cs          # Core trading logic and decision engine
├── services/
│   └── TradingService.cs         # Background service for polling and processing
├── api/
│   └── TradingController.cs      # REST API endpoints for dashboard and stats
├── Program.cs                    # Application entry point and configuration
├── Integration.cs                # Bridge between game engine and trading system
├── Trading.csproj               # Project configuration
├── appsettings.json             # Production configuration
├── appsettings.Development.json # Development configuration
└── windows-service.xml          # Windows Service configuration
```

## Features

### Trading Engine
- Deterministic trade decision making
- Position sizing and risk management
- Trade history with full transparency
- Performance statistics (win rate, return, drawdown)
- JSON serialization for persistence

### Background Service
- Configurable polling interval (default: 60 seconds)
- Automatic retry logic for failed API calls
- Health monitoring and graceful shutdown
- Trade history auto-save

### REST API
- `GET /paper/live` - Live trading dashboard
- `GET /paper/stats` - Detailed trading statistics
- `GET /paper/trades` - Trade history with pagination
- `POST /paper/simulate` - Trading simulation with custom parameters
- `GET /paper/health` - Service health status

### Integration Layer
- Real-time signal generation from game state
- Configurable signal mappings
- In-game trading dashboard display
- Multiple integration modes (Embedded, External, Simulation)

## Getting Started

### Prerequisites
- .NET 8.0 SDK or later
- ASP.NET Core runtime
- (Optional) Docker for containerized deployment

### Installation

1. **Clone and build:**
   ```bash
   cd src/trading
   dotnet restore
   dotnet build
   ```

2. **Configure settings:**
   - Edit `appsettings.json` for production
   - Edit `appsettings.Development.json` for development
   - Set signal endpoint and trading symbols

3. **Run the service:**
   ```bash
   # Development mode
   dotnet run --environment Development
   
   # Production mode
   dotnet run --environment Production
   ```

4. **Access the API:**
   - Swagger UI: `http://localhost:5000/api-docs`
   - Live dashboard: `http://localhost:5000/paper/live`
   - Health check: `http://localhost:5000/health`

### Docker Deployment

```bash
# Build the image
docker build -t neuraldraft-trading .

# Run the container
docker run -d \
  -p 5000:5000 \
  -v ./data:/app/Data \
  -v ./logs:/app/logs \
  --name trading-service \
  neuraldraft-trading
```

## Configuration

### Trading Service Settings
```json
{
  "TradingService": {
    "PollingIntervalSeconds": 60,
    "SignalEndpoint": "http://localhost:5000/v1/physics/signal",
    "Symbols": ["BTC", "ETH", "SDNA"],
    "PriceApiEndpoint": "https://api.coingecko.com/api/v3/simple/price",
    "TradeHistoryPath": "paper_trades.json",
    "MaxRetryAttempts": 3,
    "EnableMockData": false
  }
}
```

### Integration Settings
```json
{
  "Integration": {
    "Mode": "External", // Disabled, Embedded, External, Simulation
    "TradingServiceUrl": "http://localhost:5000",
    "SignalGenerationIntervalFrames": 60,
    "EnableVisualization": true
  }
}
```

## API Reference

### Live Dashboard
```http
GET /paper/live
```
Returns current trading dashboard with:
- Open positions and recent trades
- Performance statistics
- Service status and next poll time

### Trading Statistics
```http
GET /paper/stats?timeframe=7d
```
Returns detailed statistics for specified timeframe:
- Win rate and return metrics
- Performance by symbol and direction
- Risk-adjusted metrics (Sharpe ratio, Sortino ratio)

### Trade History
```http
GET /paper/trades?page=1&pageSize=20&symbol=BTC
```
Returns paginated trade history with filtering and sorting options.

### Trading Simulation
```http
POST /paper/simulate
```
Simulates trading with custom parameters and returns backtest results.

## Integration with Fighting Game Engine

### Signal Generation
The system can generate trading signals from game state metrics:
- Player health differential → SDNA sentiment
- Combo frequency → BTC sentiment  
- Projectile intensity → ETH sentiment
- Match duration → Inverse SDNA sentiment

### Game State Integration
```csharp
// Create trading integration
var integration = new TradingIntegration(config);

// Process game frames
integration.ProcessGameFrame(gameStateSnapshot);

// Get dashboard for in-game display
var dashboard = integration.GetDashboard();
```

### Visualization
The integration provides events for real-time updates:
- `DashboardUpdated` - Trading dashboard updates
- `SignalGenerated` - New trading signals
- `StatusChanged` - Integration status changes

## Performance Metrics

### Trading Statistics
- **Win Rate**: Percentage of profitable trades
- **Total Return**: Cumulative return percentage
- **Max Drawdown**: Largest peak-to-trough decline
- **Sharpe Ratio**: Risk-adjusted returns
- **Sortino Ratio**: Downside risk-adjusted returns

### System Metrics
- API response time < 100ms
- Memory usage < 100MB
- Uptime > 99.9%
- Trade processing < 10ms

## Monitoring and Maintenance

### Health Checks
- Service connectivity
- API endpoint availability
- Trade history persistence
- Memory and CPU usage

### Logging
- Structured logging with Serilog
- Log levels configurable by environment
- File and console outputs
- Log rotation and retention

### Backup and Recovery
- Automatic trade history backup
- Configurable backup intervals
- Point-in-time recovery capability
- Disaster recovery procedures

## Development

### Building from Source
```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build --configuration Release

# Run tests
dotnet test

# Create deployment package
dotnet publish --configuration Release --output ./publish
```

### Testing
```bash
# Unit tests
dotnet test tests/UnitTests

# Integration tests  
dotnet test tests/IntegrationTests

# API tests
dotnet test tests/ApiTests
```

### Code Quality
- Static analysis with Roslyn analyzers
- Code coverage > 80%
- Integration test coverage > 70%
- Performance benchmarking

## Deployment

### Windows Service
```powershell
# Install as Windows Service
sc create NeuralDraftPaperTrader binPath="C:\Path\To\Trading.exe"
sc start NeuralDraftPaperTrader

# Monitor service
Get-Service NeuralDraftPaperTrader
```

### Linux Systemd Service
```bash
# Create service file
sudo cp sdna-paper-trader.service /etc/systemd/system/

# Enable and start service
sudo systemctl enable sdna-paper-trader
sudo systemctl start sdna-paper-trader

# View logs
sudo journalctl -u sdna-paper-trader -f
```

### Kubernetes Deployment
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: trading-service
spec:
  replicas: 2
  template:
    spec:
      containers:
      - name: trading
        image: neuraldraft/trading:latest
        ports:
        - containerPort: 5000
```

## Troubleshooting

### Common Issues

1. **Service not starting**
   - Check port availability (default: 5000)
   - Verify configuration file permissions
   - Check log files for errors

2. **API connection failures**
   - Verify signal endpoint URL
   - Check network connectivity
   - Validate API response format

3. **Trade history not saving**
   - Verify write permissions to data directory
   - Check disk space
   - Validate JSON serialization

### Log Analysis
```bash
# View recent errors
grep -i error logs/trading-*.log

# Monitor live logs
tail -f logs/trading-$(date +%Y-%m-%d).log

# Analyze performance
grep "Processing time" logs/trading-*.log | awk '{print $NF}' | sort -n
```

## Security Considerations

### API Security
- Rate limiting enabled by default
- IP whitelisting configurable
- API key authentication (optional)
- HTTPS enforcement in production

### Data Security
- Trade history encrypted at rest
- Secure configuration management
- Regular security updates
- Audit logging enabled

### Network Security
- Firewall rules for service ports
- VPN for internal communications
- DDoS protection configuration
- Regular security audits

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make changes with tests
4. Submit a pull request
5. Ensure CI/CD passes

### Code Standards
- Follow C# coding conventions
- Include XML documentation
- Write unit tests for new features
- Update documentation as needed

## License

Proprietary - Neural Draft LLC

## Support

- Documentation: [docs.neuraldraft.com](https://docs.neuraldraft.com)
- Issues: GitHub Issues
- Email: support@neuraldraft.com
- Discord: [Neural Draft Community](https://discord.gg/neuraldraft)

## Acknowledgments

- Built on .NET 8 and ASP.NET Core
- Inspired by algorithmic trading systems
- Integrated with deterministic fighting game engine
- Designed for high-performance and reliability