/* ================================================================================
   NEURAL DRAFT LLC | TRADING ENGINE
================================================================================
   FILE:    TradingEngine.cs
   CONTEXT: Core trading logic - converts sentiment signals to trading decisions

   TASK:
   Implement deterministic trading engine that:
   1. Monitors sentiment signals from /v1/physics/signal endpoint
   2. Opens LONG positions when sentiment > +0.35
   3. Opens SHORT positions when sentiment < -0.35
   4. Closes positions on: reversal, 3% take-profit, -1.5% stop-loss, or 24h max hold
   5. Maintains trade history with full transparency

   CONSTRAINTS:
   - Deterministic decision making
   - Thread-safe for concurrent access
   - JSON serializable trade history
   - Fixed-point math for financial calculations
================================================================================

*/
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NeuralDraft.Trading
{
    public enum TradeDirection : byte
    {
        LONG = 0,
        SHORT = 1
    }

    public enum TradeStatus : byte
    {
        OPEN = 0,
        CLOSED = 1,
        CANCELLED = 2
    }

    public struct Trade
    {
        public string tradeId;
        public string symbol;
        public TradeDirection direction;
        public decimal entryPrice;
        public decimal currentPrice;
        public decimal quantity;
        public DateTime entryTime;
        public DateTime? exitTime;
        public decimal exitPrice;
        public TradeStatus status;
        public decimal pnl; // Profit and Loss in percentage
        public decimal pnlAmount; // Profit and Loss in absolute value
        public string closeReason;

        public readonly bool IsOpen => status == TradeStatus.OPEN;
        public readonly decimal PositionValue => quantity * currentPrice;
        public readonly decimal EntryValue => quantity * entryPrice;
    }

    public class TradingEngine
    {
        private const decimal LONG_THRESHOLD = 0.35m;
        private const decimal SHORT_THRESHOLD = -0.35m;
        private const decimal TAKE_PROFIT_PCT = 0.03m; // 3%
        private const decimal STOP_LOSS_PCT = -0.015m; // -1.5%
        private const int MAX_HOLD_HOURS = 24;

        private readonly object _lock = new object();
        private readonly Dictionary<string, Trade> _openTrades = new Dictionary<string, Trade>();
        private readonly List<Trade> _tradeHistory = new List<Trade>();
        private int _tradeCounter = 0;

        public TradingStatistics Statistics { get; private set; } = new TradingStatistics();

        public class TradingStatistics
        {
            public int TotalTrades { get; set; }
            public int WinningTrades { get; set; }
            public int LosingTrades { get; set; }
            public decimal WinRate => TotalTrades > 0 ? (decimal)WinningTrades / TotalTrades * 100 : 0;
            public decimal TotalReturn { get; set; } // Percentage
            public decimal TotalPnl { get; set; } // Absolute value
            public decimal MaxDrawdown { get; set; }
            public decimal CurrentDrawdown { get; set; }
        }

        public TradingEngine()
        {
            Statistics = new TradingStatistics();
        }

        public void ProcessSignal(string symbol, decimal sentiment, decimal currentPrice)
        {
            lock (_lock)
            {
                // Update prices for all open trades
                UpdateOpenTradePrices(symbol, currentPrice);

                // Check for position exits
                CheckPositionExits(symbol, sentiment, currentPrice);

                // Check for new entries
                CheckNewEntries(symbol, sentiment, currentPrice);

                // Update statistics
                UpdateStatistics();
            }
        }

        private void UpdateOpenTradePrices(string symbol, decimal currentPrice)
        {
            var keysToUpdate = new List<string>();

            foreach (var kvp in _openTrades)
            {
                if (kvp.Value.symbol == symbol)
                {
                    keysToUpdate.Add(kvp.Key);
                }
            }

            foreach (var key in keysToUpdate)
            {
                var trade = _openTrades[key];
                trade.currentPrice = currentPrice;
                trade.pnl = CalculatePnlPercentage(trade);
                trade.pnlAmount = CalculatePnlAmount(trade);
                _openTrades[key] = trade;
            }
        }

        private void CheckPositionExits(string symbol, decimal sentiment, decimal currentPrice)
        {
            var tradesToClose = new List<(string tradeId, string reason)>();

            foreach (var kvp in _openTrades)
            {
                var trade = kvp.Value;

                if (trade.symbol != symbol || !trade.IsOpen)
                    continue;

                // Check exit conditions
                string closeReason = CheckExitConditions(trade, sentiment, currentPrice);

                if (!string.IsNullOrEmpty(closeReason))
                {
                    tradesToClose.Add((kvp.Key, closeReason));
                }
            }

            // Close identified trades
            foreach (var (tradeId, reason) in tradesToClose)
            {
                CloseTrade(tradeId, currentPrice, reason);
            }
        }

        private string CheckExitConditions(Trade trade, decimal sentiment, decimal currentPrice)
        {
            // 1. Reversal condition
            if (trade.direction == TradeDirection.LONG && sentiment < SHORT_THRESHOLD)
                return "REVERSAL";

            if (trade.direction == TradeDirection.SHORT && sentiment > LONG_THRESHOLD)
                return "REVERSAL";

            // 2. Take profit (3%)
            if (trade.pnl >= TAKE_PROFIT_PCT)
                return "TAKE_PROFIT";

            // 3. Stop loss (-1.5%)
            if (trade.pnl <= STOP_LOSS_PCT)
                return "STOP_LOSS";

            // 4. Max hold time (24 hours)
            var holdTime = DateTime.UtcNow - trade.entryTime;
            if (holdTime.TotalHours >= MAX_HOLD_HOURS)
                return "MAX_HOLD_TIME";

            return string.Empty;
        }

        private void CheckNewEntries(string symbol, decimal sentiment, decimal currentPrice)
        {
            // Check if we already have an open position for this symbol
            bool hasOpenPosition = false;
            foreach (var trade in _openTrades.Values)
            {
                if (trade.symbol == symbol && trade.IsOpen)
                {
                    hasOpenPosition = true;
                    break;
                }
            }

            if (hasOpenPosition)
                return;

            // Check entry conditions
            TradeDirection? direction = null;

            if (sentiment > LONG_THRESHOLD)
                direction = TradeDirection.LONG;
            else if (sentiment < SHORT_THRESHOLD)
                direction = TradeDirection.SHORT;

            if (direction.HasValue)
            {
                OpenTrade(symbol, direction.Value, currentPrice);
            }
        }

        private void OpenTrade(string symbol, TradeDirection direction, decimal entryPrice)
        {
            _tradeCounter++;
            var tradeId = $"SDNA-{_tradeCounter:D3}";

            var trade = new Trade
            {
                tradeId = tradeId,
                symbol = symbol,
                direction = direction,
                entryPrice = entryPrice,
                currentPrice = entryPrice,
                quantity = CalculatePositionSize(entryPrice),
                entryTime = DateTime.UtcNow,
                exitTime = null,
                exitPrice = 0,
                status = TradeStatus.OPEN,
                pnl = 0,
                pnlAmount = 0,
                closeReason = string.Empty
            };

            _openTrades[tradeId] = trade;
            _tradeHistory.Add(trade);
        }

        private void CloseTrade(string tradeId, decimal exitPrice, string reason)
        {
            if (!_openTrades.TryGetValue(tradeId, out var trade))
                return;

            trade.exitTime = DateTime.UtcNow;
            trade.exitPrice = exitPrice;
            trade.status = TradeStatus.CLOSED;
            trade.currentPrice = exitPrice;
            trade.pnl = CalculatePnlPercentage(trade);
            trade.pnlAmount = CalculatePnlAmount(trade);
            trade.closeReason = reason;

            // Update in collections
            _openTrades[tradeId] = trade;

            // Update in history
            for (int i = 0; i < _tradeHistory.Count; i++)
            {
                if (_tradeHistory[i].tradeId == tradeId)
                {
                    _tradeHistory[i] = trade;
                    break;
                }
            }

            // Remove from open trades
            _openTrades.Remove(tradeId);
        }

        private decimal CalculatePositionSize(decimal entryPrice)
        {
            // Simplified position sizing - in reality this would be based on account balance and risk management
            const decimal basePositionValue = 10000m; // $10,000 base position
            return basePositionValue / entryPrice;
        }

        private decimal CalculatePnlPercentage(Trade trade)
        {
            if (trade.direction == TradeDirection.LONG)
            {
                return (trade.currentPrice - trade.entryPrice) / trade.entryPrice;
            }
            else // SHORT
            {
                return (trade.entryPrice - trade.currentPrice) / trade.entryPrice;
            }
        }

        private decimal CalculatePnlAmount(Trade trade)
        {
            return trade.pnl * trade.EntryValue;
        }

        private void UpdateStatistics()
        {
            var stats = new TradingStatistics();
            decimal peakEquity = 0;
            decimal currentEquity = 100000m; // Starting with $100k demo account
            decimal maxDrawdown = 0;

            // Calculate from trade history
            foreach (var trade in _tradeHistory)
            {
                if (trade.status == TradeStatus.CLOSED)
                {
                    stats.TotalTrades++;

                    if (trade.pnl > 0)
                        stats.WinningTrades++;
                    else if (trade.pnl < 0)
                        stats.LosingTrades++;

                    stats.TotalPnl += trade.pnlAmount;
                    stats.TotalReturn += trade.pnl * 100; // Convert to percentage

                    // Update equity for drawdown calculation
                    currentEquity += trade.pnlAmount;
                    peakEquity = Math.Max(peakEquity, currentEquity);

                    decimal drawdown = (peakEquity - currentEquity) / peakEquity * 100;
                    maxDrawdown = Math.Max(maxDrawdown, drawdown);
                }
            }

            // Calculate current drawdown from open positions
            decimal openPnl = 0;
            foreach (var trade in _openTrades.Values)
            {
                openPnl += trade.pnlAmount;
            }

            currentEquity += openPnl;
            peakEquity = Math.Max(peakEquity, currentEquity);
            stats.CurrentDrawdown = (peakEquity - currentEquity) / peakEquity * 100;
            stats.MaxDrawdown = maxDrawdown;

            Statistics = stats;
        }

        public IReadOnlyList<Trade> GetOpenTrades()
        {
            lock (_lock)
            {
                return new List<Trade>(_openTrades.Values);
            }
        }

        public IReadOnlyList<Trade> GetTradeHistory(int count = 50)
        {
            lock (_lock)
            {
                var startIndex = Math.Max(0, _tradeHistory.Count - count);
                var resultCount = Math.Min(count, _tradeHistory.Count - startIndex);
                return _tradeHistory.GetRange(startIndex, resultCount);
            }
        }

        public string GetTradeHistoryJson()
        {
            lock (_lock)
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Converters = { new JsonStringEnumConverter() }
                };
                return JsonSerializer.Serialize(_tradeHistory, options);
            }
        }

        public void SaveTradeHistory(string filePath)
        {
            lock (_lock)
            {
                var json = GetTradeHistoryJson();
                System.IO.File.WriteAllText(filePath, json);
            }
        }

        public void LoadTradeHistory(string filePath)
        {
            lock (_lock)
            {
                if (!System.IO.File.Exists(filePath))
                    return;

                var json = System.IO.File.ReadAllText(filePath);
                var options = new JsonSerializerOptions
                {
                    Converters = { new JsonStringEnumConverter() }
                };

                var loadedHistory = JsonSerializer.Deserialize<List<Trade>>(json, options);
                if (loadedHistory != null)
                {
                    _tradeHistory.Clear();
                    _openTrades.Clear();

                    foreach (var trade in loadedHistory)
                    {
                        _tradeHistory.Add(trade);

                        if (trade.IsOpen)
                        {
                            _openTrades[trade.tradeId] = trade;
                        }

                        // Update trade counter
                        if (int.TryParse(trade.tradeId.Replace("SDNA-", ""), out var tradeNum))
                        {
                            _tradeCounter = Math.Max(_tradeCounter, tradeNum);
                        }
                    }

                    UpdateStatistics();
                }
            }
        }
    }
}
