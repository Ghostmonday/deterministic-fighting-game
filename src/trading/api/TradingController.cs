/* ================================================================================
   NEURAL DRAFT LLC | TRADING CONTROLLER
================================================================================
   FILE:    TradingController.cs
   CONTEXT: API endpoints for paper trading system

   TASK:
   Implement ASP.NET Core API controller with endpoints:
   1. GET /paper/live - Live trading dashboard with stats and open positions
   2. GET /paper/stats - Trading statistics (win rate, return, drawdown)
   3. GET /paper/trades - Trade history with pagination
   4. POST /paper/simulate - Simulate trading with custom parameters
   5. GET /paper/health - Service health and status

   CONSTRAINTS:
   - RESTful API design
   - JSON serialization
   - Error handling with appropriate HTTP status codes
   - Thread-safe access to TradingEngine
   - Support for CORS if needed
================================================================================

*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NeuralDraft.Trading.Api
{
    [ApiController]
    [Route("paper")]
    public class TradingController : ControllerBase
    {
        private readonly TradingEngine _tradingEngine;
        private readonly TradingService _tradingService;
        private readonly ILogger<TradingController> _logger;

        public TradingController(
            TradingEngine tradingEngine,
            TradingService tradingService,
            ILogger<TradingController> logger)
        {
            _tradingEngine = tradingEngine ?? throw new ArgumentNullException(nameof(tradingEngine));
            _tradingService = tradingService ?? throw new ArgumentNullException(nameof(tradingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// GET /paper/live
        /// Returns live trading dashboard with current stats and open positions
        /// </summary>
        [HttpGet("live")]
        public IActionResult GetLiveDashboard()
        {
            try
            {
                var openTrades = _tradingEngine.GetOpenTrades();
                var recentTrades = _tradingEngine.GetTradeHistory(10);
                var stats = _tradingEngine.Statistics;
                var serviceStatus = _tradingService?.GetStatus();

                var dashboard = new
                {
                    Timestamp = DateTime.UtcNow,
                    ServiceStatus = serviceStatus?.IsRunning ?? false,
                    LastUpdate = serviceStatus?.LastSuccessfulPoll ?? DateTime.MinValue,
                    NextUpdateIn = serviceStatus?.NextPollInSeconds ?? 0,

                    Statistics = new
                    {
                        TotalTrades = stats.TotalTrades,
                        WinningTrades = stats.WinningTrades,
                        LosingTrades = stats.LosingTrades,
                        WinRate = Math.Round(stats.WinRate, 2),
                        TotalReturn = Math.Round(stats.TotalReturn, 2),
                        TotalPnl = Math.Round(stats.TotalPnl, 2),
                        MaxDrawdown = Math.Round(stats.MaxDrawdown, 2),
                        CurrentDrawdown = Math.Round(stats.CurrentDrawdown, 2)
                    },

                    OpenPositions = openTrades.Select(t => new
                    {
                        t.tradeId,
                        t.symbol,
                        Direction = t.direction.ToString(),
                        EntryPrice = Math.Round(t.entryPrice, 2),
                        CurrentPrice = Math.Round(t.currentPrice, 2),
                        PnlPercent = Math.Round(t.pnl * 100, 2),
                        PnlAmount = Math.Round(t.pnlAmount, 2),
                        PositionValue = Math.Round(t.PositionValue, 2),
                        EntryTime = t.entryTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        HoursHeld = Math.Round((DateTime.UtcNow - t.entryTime).TotalHours, 1)
                    }),

                    RecentTrades = recentTrades.Where(t => t.status == TradeStatus.CLOSED).Select(t => new
                    {
                        t.tradeId,
                        t.symbol,
                        Direction = t.direction.ToString(),
                        EntryPrice = Math.Round(t.entryPrice, 2),
                        ExitPrice = Math.Round(t.exitPrice, 2),
                        PnlPercent = Math.Round(t.pnl * 100, 2),
                        PnlAmount = Math.Round(t.pnlAmount, 2),
                        EntryTime = t.entryTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        ExitTime = t.exitTime?.ToString("yyyy-MM-dd HH:mm:ss"),
                        t.closeReason,
                        DurationHours = t.exitTime.HasValue ?
                            Math.Round((t.exitTime.Value - t.entryTime).TotalHours, 1) : 0
                    })
                };

                _logger.LogDebug("Served live dashboard with {OpenPositions} open positions", openTrades.Count);
                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating live dashboard");
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// GET /paper/stats
        /// Returns detailed trading statistics
        /// </summary>
        [HttpGet("stats")]
        public IActionResult GetStatistics([FromQuery] string timeframe = "all")
        {
            try
            {
                var allTrades = _tradingEngine.GetTradeHistory(int.MaxValue);
                var closedTrades = allTrades.Where(t => t.status == TradeStatus.CLOSED).ToList();

                // Filter by timeframe if specified
                if (timeframe != "all")
                {
                    var cutoffDate = GetTimeframeCutoff(timeframe);
                    closedTrades = closedTrades.Where(t => t.entryTime >= cutoffDate).ToList();
                }

                var stats = CalculateDetailedStatistics(closedTrades);
                var serviceStats = _tradingEngine.Statistics;

                var response = new
                {
                    Timeframe = timeframe,
                    Timestamp = DateTime.UtcNow,
                    TradeCount = closedTrades.Count,

                    Summary = new
                    {
                        TotalTrades = serviceStats.TotalTrades,
                        WinRate = Math.Round(serviceStats.WinRate, 2),
                        TotalReturn = Math.Round(serviceStats.TotalReturn, 2),
                        TotalPnl = Math.Round(serviceStats.TotalPnl, 2),
                        MaxDrawdown = Math.Round(serviceStats.MaxDrawdown, 2)
                    },

                    DetailedStats = stats,

                    PerformanceBySymbol = closedTrades
                        .GroupBy(t => t.symbol)
                        .Select(g => new
                        {
                            Symbol = g.Key,
                            TradeCount = g.Count(),
                            WinRate = Math.Round((decimal)g.Count(t => t.pnl > 0) / g.Count() * 100, 2),
                            AvgReturn = Math.Round(g.Average(t => t.pnl * 100), 2),
                            TotalReturn = Math.Round(g.Sum(t => t.pnl * 100), 2),
                            TotalPnl = Math.Round(g.Sum(t => t.pnlAmount), 2)
                        })
                        .OrderByDescending(x => x.TotalReturn),

                    PerformanceByDirection = new[]
                    {
                        new
                        {
                            Direction = "LONG",
                            TradeCount = closedTrades.Count(t => t.direction == TradeDirection.LONG),
                            WinRate = Math.Round((decimal)closedTrades.Count(t => t.direction == TradeDirection.LONG && t.pnl > 0) /
                                Math.Max(1, closedTrades.Count(t => t.direction == TradeDirection.LONG)) * 100, 2),
                            AvgReturn = Math.Round(closedTrades.Where(t => t.direction == TradeDirection.LONG).Average(t => t.pnl * 100), 2)
                        },
                        new
                        {
                            Direction = "SHORT",
                            TradeCount = closedTrades.Count(t => t.direction == TradeDirection.SHORT),
                            WinRate = Math.Round((decimal)closedTrades.Count(t => t.direction == TradeDirection.SHORT && t.pnl > 0) /
                                Math.Max(1, closedTrades.Count(t => t.direction == TradeDirection.SHORT)) * 100, 2),
                            AvgReturn = Math.Round(closedTrades.Where(t => t.direction == TradeDirection.SHORT).Average(t => t.pnl * 100), 2)
                        }
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating statistics");
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// GET /paper/trades
        /// Returns trade history with pagination
        /// </summary>
        [HttpGet("trades")]
        public IActionResult GetTrades(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string symbol = null,
            [FromQuery] string direction = null,
            [FromQuery] string status = null,
            [FromQuery] string sortBy = "entryTime",
            [FromQuery] string sortOrder = "desc")
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var allTrades = _tradingEngine.GetTradeHistory(int.MaxValue);

                // Apply filters
                var filteredTrades = allTrades.AsEnumerable();

                if (!string.IsNullOrEmpty(symbol))
                    filteredTrades = filteredTrades.Where(t => t.symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrEmpty(direction) && Enum.TryParse<TradeDirection>(direction, true, out var dir))
                    filteredTrades = filteredTrades.Where(t => t.direction == dir);

                if (!string.IsNullOrEmpty(status) && Enum.TryParse<TradeStatus>(status, true, out var stat))
                    filteredTrades = filteredTrades.Where(t => t.status == stat);

                // Apply sorting
                filteredTrades = sortBy.ToLowerInvariant() switch
                {
                    "pnl" => sortOrder.ToLowerInvariant() == "asc" ?
                        filteredTrades.OrderBy(t => t.pnl) :
                        filteredTrades.OrderByDescending(t => t.pnl),
                    "entryprice" => sortOrder.ToLowerInvariant() == "asc" ?
                        filteredTrades.OrderBy(t => t.entryPrice) :
                        filteredTrades.OrderByDescending(t => t.entryPrice),
                    "exittime" => sortOrder.ToLowerInvariant() == "asc" ?
                        filteredTrades.OrderBy(t => t.exitTime) :
                        filteredTrades.OrderByDescending(t => t.exitTime),
                    _ => sortOrder.ToLowerInvariant() == "asc" ?
                        filteredTrades.OrderBy(t => t.entryTime) :
                        filteredTrades.OrderByDescending(t => t.entryTime)
                };

                var totalCount = filteredTrades.Count();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                // Apply pagination
                var pagedTrades = filteredTrades
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => new
                    {
                        t.tradeId,
                        t.symbol,
                        Direction = t.direction.ToString(),
                        Status = t.status.ToString(),
                        EntryPrice = Math.Round(t.entryPrice, 2),
                        CurrentPrice = Math.Round(t.currentPrice, 2),
                        ExitPrice = Math.Round(t.exitPrice, 2),
                        PnlPercent = Math.Round(t.pnl * 100, 2),
                        PnlAmount = Math.Round(t.pnlAmount, 2),
                        EntryTime = t.entryTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        ExitTime = t.exitTime?.ToString("yyyy-MM-dd HH:mm:ss"),
                        t.closeReason,
                        DurationHours = t.exitTime.HasValue ?
                            Math.Round((t.exitTime.Value - t.entryTime).TotalHours, 1) : 0,
                        PositionValue = Math.Round(t.PositionValue, 2)
                    });

                var response = new
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    HasNextPage = page < totalPages,
                    HasPreviousPage = page > 1,
                    Trades = pagedTrades
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving trades");
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// POST /paper/simulate
        /// Simulate trading with custom parameters
        /// </summary>
        [HttpPost("simulate")]
        public IActionResult SimulateTrading([FromBody] SimulationRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { error = "Request body is required" });

                // Validate request
                if (request.InitialCapital <= 0)
                    return BadRequest(new { error = "Initial capital must be positive" });

                if (request.Days <= 0 || request.Days > 365)
                    return BadRequest(new { error = "Days must be between 1 and 365" });

                // Create a simulation engine (in-memory, doesn't affect real trading)
                var simEngine = new TradingEngine();
                var random = new Random(42); // Deterministic seed for reproducible results

                var startDate = DateTime.UtcNow.AddDays(-request.Days);
                var results = new List<object>();
                decimal currentCapital = request.InitialCapital;
                decimal peakCapital = request.InitialCapital;
                decimal maxDrawdown = 0;

                // Simulate daily trading
                for (int day = 0; day < request.Days; day++)
                {
                    var currentDate = startDate.AddDays(day);

                    // Generate random sentiment for each symbol
                    foreach (var symbol in request.Symbols ?? new[] { "BTC", "ETH" })
                    {
                        // Random sentiment between -1 and 1
                        var sentiment = (decimal)(random.NextDouble() * 2 - 1);

                        // Random price movement ±5%
                        var price = 50000m * (1 + (decimal)(random.NextDouble() * 0.1 - 0.05));

                        // Process through simulation engine
                        simEngine.ProcessSignal(symbol, sentiment, price);
                    }

                    // Calculate daily P&L
                    var dailyPnl = simEngine.GetOpenTrades().Sum(t => t.pnlAmount);
                    currentCapital += dailyPnl;
                    peakCapital = Math.Max(peakCapital, currentCapital);

                    var drawdown = (peakCapital - currentCapital) / peakCapital * 100;
                    maxDrawdown = Math.Max(maxDrawdown, drawdown);

                    results.Add(new
                    {
                        Date = currentDate.ToString("yyyy-MM-dd"),
                        Capital = Math.Round(currentCapital, 2),
                        DailyPnl = Math.Round(dailyPnl, 2),
                        Drawdown = Math.Round(drawdown, 2),
                        OpenTrades = simEngine.GetOpenTrades().Count
                    });
                }

                var finalStats = simEngine.Statistics;
                var totalReturn = (currentCapital - request.InitialCapital) / request.InitialCapital * 100;

                var response = new
                {
                    SimulationParameters = request,
                    FinalCapital = Math.Round(currentCapital, 2),
                    TotalReturn = Math.Round(totalReturn, 2),
                    MaxDrawdown = Math.Round(maxDrawdown, 2),
                    TotalTrades = finalStats.TotalTrades,
                    WinRate = Math.Round(finalStats.WinRate, 2),
                    DailyResults = results
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running simulation");
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// GET /paper/health
        /// Returns service health status
        /// </summary>
        [HttpGet("health")]
        public IActionResult GetHealth()
        {
            try
            {
                var serviceStatus = _tradingService?.GetStatus();
                var openTrades = _tradingEngine.GetOpenTrades();
                var stats = _tradingEngine.Statistics;

                var health = new
                {
                    Timestamp = DateTime.UtcNow,
                    Status = "healthy",
                    ServiceRunning = serviceStatus?.IsRunning ?? false,
                    LastSuccessfulPoll = serviceStatus?.LastSuccessfulPoll,
                    NextPollInSeconds = serviceStatus?.NextPollInSeconds ?? 0,
                    Uptime = serviceStatus != null ?
                        DateTime.UtcNow - (serviceStatus.LastSuccessfulPoll != DateTime.MinValue ?
                            serviceStatus.LastSuccessfulPoll : DateTime.UtcNow) : TimeSpan.Zero,

                    TradingEngine = new
                    {
                        OpenTrades = openTrades.Count,
                        TotalTrades = stats.TotalTrades,
                        MemoryUsage = GC.GetTotalMemory(false) / 1024 / 1024 // MB
                    },

                    System = new
                    {
                        Environment.MachineName,
                        Environment.ProcessorCount,
                        Memory = Environment.WorkingSet / 1024 / 1024 // MB
                    }
                };

                return Ok(health);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking health");
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// POST /paper/manual
        /// Manually open or close a trade (for testing)
        /// </summary>
        [HttpPost("manual")]
        public IActionResult ManualTrade([FromBody] ManualTradeRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { error = "Request body is required" });

                // Note: In a real system, this would require authentication and authorization
                // For now, we'll log it but not actually modify the trading engine

                _logger.LogInformation("Manual trade request: {@Request}", request);

                return Ok(new
                {
                    Message = "Manual trade logged (not executed in paper trading mode)",
                    Request = request,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing manual trade");
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        #region Helper Methods

        private DateTime GetTimeframeCutoff(string timeframe)
        {
            return timeframe.ToLowerInvariant() switch
            {
                "24h" => DateTime.UtcNow.AddHours(-24),
                "7d" => DateTime.UtcNow.AddDays(-7),
                "30d" => DateTime.UtcNow.AddDays(-30),
                "90d" => DateTime.UtcNow.AddDays(-90),
                "1y" => DateTime.UtcNow.AddYears(-1),
                _ => DateTime.MinValue
            };
        }

        private object CalculateDetailedStatistics(List<Trade> trades)
        {
            if (trades.Count == 0)
                return new { Message = "No trades in selected timeframe" };

            var winningTrades = trades.Where(t => t.pnl > 0).ToList();
            var losingTrades = trades.Where(t => t.pnl < 0).ToList();

            return new
            {
                WinRate = Math.Round((decimal)winningTrades.Count / trades.Count * 100, 2),
                LossRate = Math.Round((decimal)losingTrades.Count / trades.Count * 100, 2),

                AverageWin = Math.Round(winningTrades.Average(t => t.pnl * 100), 2),
                AverageLoss = Math.Round(losingTrades.Average(t => t.pnl * 100), 2),
                AverageTrade = Math.Round(trades.Average(t => t.pnl * 100), 2),

                LargestWin = Math.Round(winningTrades.Max(t => t.pnl * 100), 2),
                LargestLoss = Math.Round(losingTrades.Min(t => t.pnl * 100), 2),

                WinLossRatio = losingTrades.Count > 0 ?
                    Math.Round((decimal)winningTrades.Average(t => t.pnl) / Math.Abs(losingTrades.Average(t => t.pnl)), 2) : 0,

                Expectancy = Math.Round(
                    ((decimal)winningTrades.Count / trades.Count) * winningTrades.Average(t => t.pnl) +
                    ((decimal)losingTrades.Count / trades.Count) * losingTrades.Average(t => t.pnl), 4),

                SharpeRatio = CalculateSharpeRatio(trades),
                SortinoRatio = CalculateSortinoRatio(trades)
            };
        }

        private decimal CalculateSharpeRatio(List<Trade> trades)
        {
            if (trades.Count < 2) return 0;

            var returns = trades.Select(t => (double)t.pnl).ToArray();
            var avgReturn = returns.Average();
            var stdDev = Math.Sqrt(returns.Sum(r => Math.Pow(r - avgReturn, 2)) / (returns.Length - 1));

            // Assuming risk-free rate of 0% for simplicity
            return stdDev != 0 ? (decimal)(avgReturn / stdDev * Math.Sqrt(252)) : 0;
        }

        private decimal CalculateSortinoRatio(List<Trade> trades)
        {
            if (trades.Count < 2) return 0;

            var returns = trades.Select(t => (double)t.pnl).ToArray();
            var avgReturn = returns.Average();
            var negativeReturns = returns.Where(r => r < 0).ToArray();

            if (negativeReturns.Length == 0) return 100; // Arbitrary high value for no downside

            var downsideDev = Math.Sqrt(negativeReturns.Sum(r => Math.Pow(r, 2)) / negativeReturns.Length);
            return downsideDev != 0 ? (decimal)(avgReturn / downsideDev * Math.Sqrt(252)) : 0;
        }

        #endregion
    }

    #region Request Models

    public class SimulationRequest
    {
        public decimal InitialCapital { get; set; } = 100000;
        public int Days { get; set; } = 30;
        public string[] Symbols { get; set; } = new[] { "BTC", "ETH" };
        public decimal LongThreshold { get; set; } = 0.35m;
        public decimal ShortThreshold { get; set; } = -0.35m;
        public decimal TakeProfit { get; set; } = 0.03m;
        public decimal StopLoss { get; set; } = -0.015m;
    }

    public class ManualTradeRequest
    {
        public string Action { get; set; } // "open" or "close"
        public string Symbol { get; set; }
        public string Direction { get; set; } // "long" or "short"
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public string Reason { get; set; }
    }

    #endregion
}
