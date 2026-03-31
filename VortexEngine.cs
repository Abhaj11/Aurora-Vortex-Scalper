using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Aurora.Vortex
{
    public class TradeState
    {
        public string Symbol { get; set; } = "";
        public decimal EntryPrice { get; set; }
        public decimal Amount { get; set; }
        public bool IsActive { get; set; } = false;
    }

    public class VortexEngine
    {
        // Adana bayanan ciniki na yanzu
        public static ConcurrentDictionary<string, TradeState> ActiveTrades = new();

        private static decimal _totalProfit = 0;
        private static int _tradeCount = 0;
        
        // Risk Shield
        private static decimal _dailyLoss = 0;
        private static DateTime _lastTradeDate = DateTime.Now;

        // Dokokinmu na Scalping (40-50% Target)
        public const decimal TAKE_PROFIT_PERCENT = 0.008m; // 0.8% Riba
        public const decimal STOP_LOSS_PERCENT = 0.015m;  // 1.5% Kare Jari
        public const decimal ENTRY_DIP_PERCENT = 0.003m;  // Jira ya dan fado da 0.3% kafin shiga
        public const decimal MAX_DAILY_LOSS = 5.0m;       // Idan asara ta kai $5 a rana, bot ya tsaya

        public static void EvaluateMarket(string symbol, decimal currentPrice)
        {
            // 1. Idan muna da ciniki a bude (Active Trade)
            if (ActiveTrades.TryGetValue(symbol, out var trade) && trade.IsActive)
            {
                decimal priceChange = (currentPrice - trade.EntryPrice) / trade.EntryPrice;

                // A. Duba Take Profit (Riba ta samu)
                if (priceChange >= TAKE_PROFIT_PERCENT)
                {
                    Console.WriteLine($"[SELL-PROFIT] 💰 {symbol} at ${currentPrice} | Profit: {priceChange:P2}");
                    ExecuteExit(trade, currentPrice);
                }
                // B. Duba Stop Loss (Kasuwa ta juya)
                else if (priceChange <= -STOP_LOSS_PERCENT)
                {
                    Console.WriteLine($"[SELL-LOSS] 🛡️ {symbol} at ${currentPrice} | Loss: {priceChange:P2}");
                    ExecuteExit(trade, currentPrice);
                }
            }
        }

        private static async void ExecuteExit(TradeState trade, decimal exitPrice)
        {
            bool success = await BinanceExecution.SendMarketOrder(trade.Symbol, "SELL", trade.Amount);
            if (success) {
                decimal profit = (exitPrice - trade.EntryPrice) * trade.Amount;
                _totalProfit += profit;
                _tradeCount++;
                
                // Add to daily loss if it's a loss
                if (profit < 0)
                {
                    _dailyLoss += Math.Abs(profit);
                }
                
                trade.IsActive = false;
                ActiveTrades.TryRemove(trade.Symbol, out _);
                
                await FirebaseSync.UpdateDashboard(_totalProfit, _tradeCount, _dailyLoss, "Running");

                // Dashboard Monitoring Notification
                if (profit > 0)
                {
                    await FirebaseSync.SendNotification($"Alhamdulillah! An samu ${Math.Round(profit, 2)} riba a {trade.Symbol}.");
                }
            }
        }

        public static async void PrepareEntry(string symbol, decimal price)
        {
            // Check if it's a new day to reset Daily Loss
            if (DateTime.Now.Date != _lastTradeDate.Date)
            {
                _dailyLoss = 0;
                _lastTradeDate = DateTime.Now;
            }

            // Risk Shield: Kare bot din daga hauka a lokacin rugujewar kasuwa
            if (_dailyLoss >= MAX_DAILY_LOSS)
            {
                Console.WriteLine("[RISK SHIELD] 🛑 Daily drawdown ($5) reached. Trading stopped for today.");
                return;
            }

            if (!ActiveTrades.ContainsKey(symbol)) {
                // Small Size: $10 kowane ciniki
                decimal qty = 10 / price; 
                bool success = await BinanceExecution.SendMarketOrder(symbol, "BUY", qty);
                
                if (success) {
                    var newTrade = new TradeState { 
                        Symbol = symbol, EntryPrice = price, Amount = qty, IsActive = true 
                    };
                    ActiveTrades[symbol] = newTrade;

                    // Sync state immediately upon entry
                    await FirebaseSync.UpdateDashboard(_totalProfit, _tradeCount, _dailyLoss, "Running");
                }
            }
        }
    }
}
