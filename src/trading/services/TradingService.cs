/* ================================================================================
   NEURAL DRAFT LLC | TRADING SERVICE
================================================================================
   FILE:    TradingService.cs
   CONTEXT: Background service that polls sentiment signals and processes trades

   TASK:
   Implement a background service that:
   1. Polls the /v1/physics/signal endpoint every 60 seconds
   2. Fetches current market prices for symbols
   3. Processes signals through TradingEngine
   4. Maintains trade history persistence
   5. Provides health monitoring and graceful shutdown

   CONSTRAINTS:
   - Thread-safe background processing
   - Configurable polling interval
   - Graceful startup/shutdown
   - Error handling with retry logic
   - Integration with TradingEngine
================================================================================

*/
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NeuralDraft.Trading.Services
{
    public class TradingService : BackgroundService
    {
        private readonly TradingEngine _tradingEngine;
        private readonly ILogger<TradingService> _logger;
        private readonly HttpClient _httpClient;
        private readonly TradingServiceConfig _config;
        private readonly Timer _healthTimer;
        private DateTime _lastSuccessfulPoll = DateTime.MinValue;

        public class TradingServiceConfig
        {
            public int PollingIntervalSeconds { get; set; } = 60;
            public string SignalEndpoint { get; set; } = "http://localhost:5000/v1/physics/signal";
            public string[] Symbols { get; set; } = new[] { "BTC", "ETH", "SDNA" };
            public string PriceApiEndpoint { get; set; } = "https://api.coingecko.com/api/v3/simple/price";
            public string TradeHistoryPath { get; set; } = "paper_trades.json";
            public int MaxRetryAttempts { get; set; } = 3;
            public int RetryDelaySeconds { get; set; } = 5;
        }

        public class SignalResponse
        {
            public string Symbol { get; set; }
            public decimal Sentiment { get; set; }
            public DateTime Timestamp { get; set; }
            public decimal Confidence { get; set; }
        }

        public class PriceResponse
        {
            public decimal Usd { get; set; }
        }

        public TradingService(
            TradingEngine tradingEngine,
            ILogger<TradingService> logger,
            HttpClient httpClient = null,
            TradingServiceConfig config = null)
        {
            _tradingEngine = tradingEngine ?? throw new ArgumentNullException(nameof(tradingEngine));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? new HttpClient();
            _config = config ?? new TradingServiceConfig();

            // Set reasonable timeout
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            // Health check timer (every 5 minutes)
            _healthTimer = new Timer(HealthCheck, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

            _logger.LogInformation("TradingService initialized with {SymbolCount} symbols", _config.Symbols.Length);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TradingService starting...");

            // Load existing trade history
            try
            {
                _tradingEngine.LoadTradeHistory(_config.TradeHistoryPath);
                _logger.LogInformation("Loaded trade history from {Path}", _config.TradeHistoryPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load trade history from {Path}", _config.TradeHistoryPath);
            }

            // Main polling loop
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PollAndProcessSignals(stoppingToken);
                    _lastSuccessfulPoll = DateTime.UtcNow;
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("TradingService stopping due to cancellation request");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in trading service polling cycle");
                }

                // Wait for next polling interval
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(_config.PollingIntervalSeconds), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("TradingService stopped");
        }

        private async Task PollAndProcessSignals(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Starting polling cycle");

            foreach (var symbol in _config.Symbols)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    // Step 1: Get sentiment signal
                    var signal = await GetSentimentSignal(symbol, cancellationToken);
                    if (signal == null)
                        continue;

                    // Step 2: Get current market price
                    var price = await GetCurrentPrice(symbol, cancellationToken);
                    if (price <= 0)
                        continue;

                    // Step 3: Process through trading engine
                    _tradingEngine.ProcessSignal(symbol, signal.Sentiment, price);

                    _logger.LogDebug("Processed signal for {Symbol}: Sentiment={Sentiment}, Price={Price}",
                        symbol, signal.Sentiment, price);

                    // Step 4: Save trade history periodically
                    if (DateTime.UtcNow.Second % 30 == 0) // Save every 30 seconds
                    {
                        _tradingEngine.SaveTradeHistory(_config.TradeHistoryPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing symbol {Symbol}", symbol);
                }
            }

            _logger.LogDebug("Completed polling cycle");
        }

        private async Task<SignalResponse> GetSentimentSignal(string symbol, CancellationToken cancellationToken)
        {
            for (int attempt = 1; attempt <= _config.MaxRetryAttempts; attempt++)
            {
                try
                {
                    var endpoint = $"{_config.SignalEndpoint}?symbol={symbol}";
                    var response = await _httpClient.GetAsync(endpoint, cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync(cancellationToken);
                        var signal = JsonSerializer.Deserialize<SignalResponse>(content, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (signal != null)
                        {
                            _logger.LogDebug("Retrieved sentiment for {Symbol}: {Sentiment} (confidence: {Confidence})",
                                symbol, signal.Sentiment, signal.Confidence);
                            return signal;
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Failed to get sentiment for {Symbol}: {StatusCode}",
                            symbol, response.StatusCode);
                    }
                }
                catch (Exception ex) when (attempt < _config.MaxRetryAttempts)
                {
                    _logger.LogWarning(ex, "Attempt {Attempt} failed for {Symbol}, retrying...", attempt, symbol);
                    await Task.Delay(TimeSpan.FromSeconds(_config.RetryDelaySeconds), cancellationToken);
                    continue;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "All attempts failed for {Symbol}", symbol);
                }
            }

            return null;
        }

        private async Task<decimal> GetCurrentPrice(string symbol, CancellationToken cancellationToken)
        {
            // Convert symbol to coingecko format (lowercase)
            var coinId = symbol.ToLowerInvariant();

            for (int attempt = 1; attempt <= _config.MaxRetryAttempts; attempt++)
            {
                try
                {
                    var endpoint = $"{_config.PriceApiEndpoint}?ids={coinId}&vs_currencies=usd";
                    var response = await _httpClient.GetAsync(endpoint, cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync(cancellationToken);
                        var priceData = JsonSerializer.Deserialize<Dictionary<string, PriceResponse>>(
                            content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (priceData != null && priceData.TryGetValue(coinId, out var priceResponse))
                        {
                            _logger.LogDebug("Retrieved price for {Symbol}: ${Price}", symbol, priceResponse.Usd);
                            return priceResponse.Usd;
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Failed to get price for {Symbol}: {StatusCode}",
                            symbol, response.StatusCode);
                    }
                }
                catch (Exception ex) when (attempt < _config.MaxRetryAttempts)
                {
                    _logger.LogWarning(ex, "Price fetch attempt {Attempt} failed for {Symbol}, retrying...",
                        attempt, symbol);
                    await Task.Delay(TimeSpan.FromSeconds(_config.RetryDelaySeconds), cancellationToken);
                    continue;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "All price fetch attempts failed for {Symbol}", symbol);
                }
            }

            // Fallback: Use a mock price for testing if API fails
            _logger.LogWarning("Using mock price for {Symbol} due to API failure", symbol);
            return GetMockPrice(symbol);
        }

        private decimal GetMockPrice(string symbol)
        {
            // Simple deterministic mock price based on symbol and time
            var basePrice = symbol switch
            {
                "BTC" => 50000m,
                "ETH" => 3000m,
                "SDNA" => 100m,
                _ => 50m
            };

            // Add some predictable variation based on time
            var hour = DateTime.UtcNow.Hour;
            var minute = DateTime.UtcNow.Minute;
            var variation = (decimal)(Math.Sin(hour + minute / 60.0) * 0.02); // ±2% variation

            return basePrice * (1 + variation);
        }

        private void HealthCheck(object state)
        {
            try
            {
                var timeSinceLastPoll = DateTime.UtcNow - _lastSuccessfulPoll;
                var isHealthy = timeSinceLastPoll.TotalSeconds < (_config.PollingIntervalSeconds * 2);

                if (!isHealthy)
                {
                    _logger.LogWarning("Health check failed: Last successful poll was {TimeAgo} ago",
                        timeSinceLastPoll);
                }
                else
                {
                    var stats = _tradingEngine.Statistics;
                    _logger.LogInformation(
                        "TradingService health: {OpenTrades} open trades, {TotalTrades} total, Win Rate: {WinRate}%, Return: {Return}%",
                        _tradingEngine.GetOpenTrades().Count,
                        stats.TotalTrades,
                        stats.WinRate.ToString("F1"),
                        stats.TotalReturn.ToString("F1"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in health check");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("TradingService stopping...");

            // Save final trade history
            try
            {
                _tradingEngine.SaveTradeHistory(_config.TradeHistoryPath);
                _logger.LogInformation("Saved trade history to {Path}", _config.TradeHistoryPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save trade history on shutdown");
            }

            // Clean up timers
            _healthTimer?.Dispose();

            await base.StopAsync(cancellationToken);
        }

        public TradingServiceStatus GetStatus()
        {
            return new TradingServiceStatus
            {
                IsRunning = !_stoppingToken.IsCancellationRequested,
                LastSuccessfulPoll = _lastSuccessfulPoll,
                NextPollInSeconds = GetNextPollSeconds(),
                Config = _config,
                Statistics = _tradingEngine.Statistics
            };
        }

        private int GetNextPollSeconds()
        {
            if (_lastSuccessfulPoll == DateTime.MinValue)
                return 0;

            var nextPoll = _lastSuccessfulPoll.AddSeconds(_config.PollingIntervalSeconds);
            var secondsUntilNext = (int)(nextPoll - DateTime.UtcNow).TotalSeconds;
            return Math.Max(0, secondsUntilNext);
        }
    }

    public class TradingServiceStatus
    {
        public bool IsRunning { get; set; }
        public DateTime LastSuccessfulPoll { get; set; }
        public int NextPollInSeconds { get; set; }
        public TradingService.TradingServiceConfig Config { get; set; }
        public TradingEngine.TradingStatistics Statistics { get; set; }
    }
}
