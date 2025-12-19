/* ================================================================================
   NEURAL DRAFT LLC | TRADING INTEGRATION
================================================================================
   FILE:    Integration.cs
   CONTEXT: Integration bridge between paper trading system and main application

   TASK:
   Create integration layer that connects the paper trading system with the
   main deterministic fighting game engine. This includes:
   1. Signal generation from game state to trading sentiment
   2. Trading visualization in game UI
   3. Bidirectional communication between systems
   4. Configuration management for integrated mode

   CONSTRAINTS:
   - Thread-safe integration
   - Minimal performance impact on game simulation
   - JSON-based communication
   - Support for both embedded and external trading service
================================================================================

*/
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NeuralDraft.Trading
{
    /// <summary>
    /// Integration bridge between deterministic fighting game and paper trading system
    /// </summary>
    public class TradingIntegration
    {
        private readonly ILogger<TradingIntegration> _logger;
        private readonly HttpClient _httpClient;
        private readonly TradingIntegrationConfig _config;
        private readonly object _syncLock = new object();
        private bool _isInitialized = false;
        private DateTime _lastSignalTime = DateTime.MinValue;

        /// <summary>
        /// Configuration for trading integration
        /// </summary>
        public class TradingIntegrationConfig
        {
            public IntegrationMode Mode { get; set; } = IntegrationMode.Disabled;
            public string TradingServiceUrl { get; set; } = "http://localhost:5000";
            public int SignalGenerationIntervalFrames { get; set; } = 60; // Once per second at 60 FPS
            public bool EnableVisualization { get; set; } = true;
            public SignalMapping[] SignalMappings { get; set; } = DefaultSignalMappings;
            public string[] TradingSymbols { get; set; } = new[] { "SDNA", "BTC", "ETH" };

            public static SignalMapping[] DefaultSignalMappings => new[]
            {
                new SignalMapping
                {
                    GameMetric = "player_health_differential",
                    TradingSymbol = "SDNA",
                    Weight = 1.0m,
                    Invert = false,
                    Description = "Health difference between players affects SDNA sentiment"
                },
                new SignalMapping
                {
                    GameMetric = "combo_count",
                    TradingSymbol = "BTC",
                    Weight = 0.5m,
                    Invert = false,
                    Description = "Combo frequency affects BTC sentiment"
                },
                new SignalMapping
                {
                    GameMetric = "projectile_intensity",
                    TradingSymbol = "ETH",
                    Weight = 0.3m,
                    Invert = false,
                    Description = "Projectile activity affects ETH sentiment"
                },
                new SignalMapping
                {
                    GameMetric = "match_duration",
                    TradingSymbol = "SDNA",
                    Weight = -0.2m,
                    Invert = true,
                    Description = "Longer matches reduce SDNA sentiment"
                }
            };
        }

        /// <summary>
        /// Integration mode
        /// </summary>
        public enum IntegrationMode
        {
            Disabled = 0,
            Embedded = 1,      // Trading service runs in same process
            External = 2,      // Trading service runs as separate service
            Simulation = 3     // Use simulated trading without real service
        }

        /// <summary>
        /// Maps game metrics to trading signals
        /// </summary>
        public class SignalMapping
        {
            public string GameMetric { get; set; }
            public string TradingSymbol { get; set; }
            public decimal Weight { get; set; }
            public bool Invert { get; set; }
            public string Description { get; set; }
        }

        /// <summary>
        /// Game state snapshot for signal generation
        /// </summary>
        public class GameStateSnapshot
        {
            public int FrameIndex { get; set; }
            public DateTime Timestamp { get; set; }
            public Dictionary<string, decimal> Metrics { get; set; } = new Dictionary<string, decimal>();
            public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

            // Common metrics
            public decimal PlayerHealthDifferential => GetMetric("player_health_differential");
            public decimal ComboCount => GetMetric("combo_count");
            public decimal ProjectileIntensity => GetMetric("projectile_intensity");
            public decimal MatchDuration => GetMetric("match_duration");
            public decimal InputComplexity => GetMetric("input_complexity");
            public decimal StageControl => GetMetric("stage_control");

            private decimal GetMetric(string key)
            {
                return Metrics.TryGetValue(key, out var value) ? value : 0;
            }
        }

        /// <summary>
        /// Trading signal generated from game state
        /// </summary>
        public class TradingSignal
        {
            public string Symbol { get; set; }
            public decimal Sentiment { get; set; } // Range: -1.0 to +1.0
            public decimal Confidence { get; set; } // Range: 0.0 to 1.0
            public DateTime Timestamp { get; set; }
            public string Source { get; set; } = "game_engine";
            public Dictionary<string, decimal> Contributors { get; set; } = new Dictionary<string, decimal>();
        }

        /// <summary>
        /// Trading dashboard data for in-game display
        /// </summary>
        public class TradingDashboard
        {
            public bool IsConnected { get; set; }
            public DateTime LastUpdate { get; set; }
            public TradingEngine.TradingStatistics Statistics { get; set; }
            public List<Trade> OpenTrades { get; set; } = new List<Trade>();
            public List<Trade> RecentTrades { get; set; } = new List<Trade>();
            public Dictionary<string, decimal> CurrentSignals { get; set; } = new Dictionary<string, decimal>();
        }

        /// <summary>
        /// Integration status
        /// </summary>
        public class IntegrationStatus
        {
            public IntegrationMode Mode { get; set; }
            public bool IsConnected { get; set; }
            public DateTime LastSignalTime { get; set; }
            public int SignalsGenerated { get; set; }
            public int SignalsSent { get; set; }
            public DateTime LastDashboardUpdate { get; set; }
            public Exception LastError { get; set; }
            public Dictionary<string, string> Configuration { get; set; } = new Dictionary<string, string>();
        }

        // Events for UI updates
        public event EventHandler<TradingDashboard> DashboardUpdated;
        public event EventHandler<TradingSignal> SignalGenerated;
        public event EventHandler<IntegrationStatus> StatusChanged;

        // Internal state
        private int _signalsGenerated = 0;
        private int _signalsSent = 0;
        private TradingDashboard _currentDashboard = new TradingDashboard();
        private IntegrationStatus _currentStatus = new IntegrationStatus();
        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Initialize trading integration
        /// </summary>
        public TradingIntegration(
            ILogger<TradingIntegration> logger = null,
            HttpClient httpClient = null,
            TradingIntegrationConfig config = null)
        {
            _logger = logger ?? CreateDefaultLogger();
            _httpClient = httpClient ?? new HttpClient();
            _config = config ?? new TradingIntegrationConfig();

            _httpClient.Timeout = TimeSpan.FromSeconds(10);
            _currentStatus.Mode = _config.Mode;
            _currentStatus.Configuration["Mode"] = _config.Mode.ToString();
            _currentStatus.Configuration["ServiceUrl"] = _config.TradingServiceUrl;

            _logger.LogInformation("TradingIntegration initialized in {Mode} mode", _config.Mode);
        }

        /// <summary>
        /// Start the integration service
        /// </summary>
        public void Start()
        {
            lock (_syncLock)
            {
                if (_isInitialized)
                    return;

                _cancellationTokenSource = new CancellationTokenSource();
                _isInitialized = true;

                // Start background tasks based on mode
                if (_config.Mode == IntegrationMode.External)
                {
                    Task.Run(() => HealthCheckLoop(_cancellationTokenSource.Token));
                    Task.Run(() => DashboardUpdateLoop(_cancellationTokenSource.Token));
                }

                _logger.LogInformation("TradingIntegration started");
                UpdateStatus();
            }
        }

        /// <summary>
        /// Stop the integration service
        /// </summary>
        public void Stop()
        {
            lock (_syncLock)
            {
                if (!_isInitialized)
                    return;

                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                _isInitialized = false;

                _logger.LogInformation("TradingIntegration stopped");
                UpdateStatus();
            }
        }

        /// <summary>
        /// Process game frame and generate trading signals if needed
        /// </summary>
        public void ProcessGameFrame(GameStateSnapshot gameState)
        {
            if (_config.Mode == IntegrationMode.Disabled || !_isInitialized)
                return;

            // Check if it's time to generate a signal
            bool shouldGenerateSignal = _config.Mode == IntegrationMode.Simulation ||
                (gameState.FrameIndex % _config.SignalGenerationIntervalFrames == 0);

            if (!shouldGenerateSignal)
                return;

            try
            {
                var signals = GenerateSignals(gameState);
                _signalsGenerated += signals.Count;

                foreach (var signal in signals)
                {
                    SignalGenerated?.Invoke(this, signal);
                    SendSignalToTradingService(signal).ConfigureAwait(false);
                }

                _lastSignalTime = DateTime.UtcNow;
                UpdateStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing game frame for trading signals");
                _currentStatus.LastError = ex;
                UpdateStatus();
            }
        }

        /// <summary>
        /// Generate trading signals from game state
        /// </summary>
        private List<TradingSignal> GenerateSignals(GameStateSnapshot gameState)
        {
            var signals = new List<TradingSignal>();
            var symbolContributors = new Dictionary<string, Dictionary<string, decimal>>();

            // Initialize contributors for each symbol
            foreach (var symbol in _config.TradingSymbols)
            {
                symbolContributors[symbol] = new Dictionary<string, decimal>();
            }

            // Apply signal mappings
            foreach (var mapping in _config.SignalMappings)
            {
                if (!gameState.Metrics.TryGetValue(mapping.GameMetric, out var metricValue))
                    continue;

                if (!symbolContributors.ContainsKey(mapping.TradingSymbol))
                    continue;

                var contribution = metricValue * mapping.Weight;
                if (mapping.Invert)
                    contribution = -contribution;

                symbolContributors[mapping.TradingSymbol][mapping.GameMetric] = contribution;
            }

            // Create signals from contributors
            foreach (var kvp in symbolContributors)
            {
                var symbol = kvp.Key;
                var contributors = kvp.Value;

                if (contributors.Count == 0)
                    continue;

                // Calculate weighted sentiment
                decimal totalSentiment = 0;
                decimal totalWeight = 0;

                foreach (var contributor in contributors)
                {
                    totalSentiment += contributor.Value;
                    totalWeight += Math.Abs(contributor.Value);
                }

                decimal sentiment = totalWeight > 0 ? totalSentiment / totalWeight : 0;
                sentiment = Math.Clamp(sentiment, -1.0m, 1.0m);

                // Calculate confidence based on contributor diversity and magnitude
                decimal confidence = Math.Min(1.0m, totalWeight * 2); // Simple confidence calculation

                var signal = new TradingSignal
                {
                    Symbol = symbol,
                    Sentiment = sentiment,
                    Confidence = confidence,
                    Timestamp = DateTime.UtcNow,
                    Contributors = new Dictionary<string, decimal>(contributors)
                };

                signals.Add(signal);
                _logger.LogDebug("Generated signal for {Symbol}: {Sentiment} (confidence: {Confidence})",
                    symbol, sentiment, confidence);
            }

            return signals;
        }

        /// <summary>
        /// Send signal to trading service
        /// </summary>
        private async Task SendSignalToTradingService(TradingSignal signal)
        {
            if (_config.Mode != IntegrationMode.External)
                return;

            try
            {
                var endpoint = $"{_config.TradingServiceUrl}/api/signals";
                var json = JsonSerializer.Serialize(signal, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(endpoint, content);

                if (response.IsSuccessStatusCode)
                {
                    _signalsSent++;
                    _logger.LogDebug("Signal sent successfully for {Symbol}", signal.Symbol);
                }
                else
                {
                    _logger.LogWarning("Failed to send signal: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending signal to trading service");
            }
        }

        /// <summary>
        /// Get current trading dashboard for in-game display
        /// </summary>
        public TradingDashboard GetDashboard()
        {
            lock (_syncLock)
            {
                return _currentDashboard;
            }
        }

        /// <summary>
        /// Get integration status
        /// </summary>
        public IntegrationStatus GetStatus()
        {
            lock (_syncLock)
            {
                return _currentStatus;
            }
        }

        /// <summary>
        /// Health check loop for external service
        /// </summary>
        private async Task HealthCheckLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);

                    var response = await _httpClient.GetAsync($"{_config.TradingServiceUrl}/health", cancellationToken);
                    _currentStatus.IsConnected = response.IsSuccessStatusCode;

                    if (!_currentStatus.IsConnected)
                    {
                        _logger.LogWarning("Trading service health check failed");
                    }
                }
                catch (Exception ex)
                {
                    _currentStatus.IsConnected = false;
                    _currentStatus.LastError = ex;
                    _logger.LogError(ex, "Health check error");
                }

                UpdateStatus();
            }
        }

        /// <summary>
        /// Dashboard update loop
        /// </summary>
        private async Task DashboardUpdateLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);

                    if (!_currentStatus.IsConnected)
                        continue;

                    var response = await _httpClient.GetAsync($"{_config.TradingServiceUrl}/paper/live", cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync(cancellationToken);
                        // Parse and update dashboard (simplified - would need proper deserialization)
                        _currentDashboard.LastUpdate = DateTime.UtcNow;
                        _currentDashboard.IsConnected = true;
                        DashboardUpdated?.Invoke(this, _currentDashboard);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Dashboard update error");
                }
            }
        }

        /// <summary>
        /// Update integration status and notify listeners
        /// </summary>
        private void UpdateStatus()
        {
            lock (_syncLock)
            {
                _currentStatus.LastSignalTime = _lastSignalTime;
                _currentStatus.SignalsGenerated = _signalsGenerated;
                _currentStatus.SignalsSent = _signalsSent;
                _currentStatus.LastDashboardUpdate = _currentDashboard.LastUpdate;

                StatusChanged?.Invoke(this, _currentStatus);
            }
        }

        /// <summary>
        /// Create default logger if none provided
        /// </summary>
        private static ILogger<TradingIntegration> CreateDefaultLogger()
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            return loggerFactory.CreateLogger<TradingIntegration>();
        }

        /// <summary>
        /// Helper method to create game state snapshot from fighting game state
        /// </summary>
        public static GameStateSnapshot CreateSnapshotFromGameState(
            NeuralDraft.GameState gameState,
            Dictionary<string, decimal> additionalMetrics = null)
        {
            var snapshot = new GameStateSnapshot
            {
                FrameIndex = gameState.frameIndex,
                Timestamp = DateTime.UtcNow,
                Metadata =
                {
                    ["player_count"] = NeuralDraft.GameState.MAX_PLAYERS.ToString(),
                    ["projectile_count"] = gameState.activeProjectileCount.ToString()
                }
            };

            // Calculate basic metrics from game state
            if (gameState.players.Length >= 2)
            {
                var player1 = gameState.players[0];
                var player2 = gameState.players[1];

                // Health differential (normalized)
                decimal healthDiff = (player1.health - player2.health) / 100.0m;
                snapshot.Metrics["player_health_differential"] = healthDiff;

                // Stage control (position-based)
                decimal stageControl = (player1.posX - player2.posX) / 1000.0m;
                snapshot.Metrics["stage_control"] = stageControl;
            }

            // Add any additional metrics
            if (additionalMetrics != null)
            {
                foreach (var kvp in additionalMetrics)
                {
                    snapshot.Metrics[kvp.Key] = kvp.Value;
                }
            }

            return snapshot;
        }
    }
}
