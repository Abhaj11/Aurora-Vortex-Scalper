using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Aurora.Vortex
{
    public class MarketScanner
    {
        // Jerin coins din da muke so mu yi scalping dasu (Top Liquidity)
        public static List<string> TargetAssets = new() { "SOL", "ETH", "BTC", "BNB", "ARB", "OP", "AVAX", "LINK", "PEPE", "SHIB" };
        
        // Adana bayanan Volume na minti 5 da suka wuce
        private static ConcurrentDictionary<string, decimal> _lastVolume = new();

        /// <summary>
        /// Gano coins din da Volume dinsa ya karu farat daya (Indicator na Breakout)
        /// </summary>
        public static bool IsHighVolumeSpike(string symbol, decimal currentVolume, decimal thresholdMultiplier = 1.5m)
        {
            if (!_lastVolume.TryGetValue(symbol, out var oldVolume))
            {
                _lastVolume[symbol] = currentVolume;
                return false;
            }

            // Idan Volume na yanzu ya ninka na baya da kashi 50% ko fiye
            if (currentVolume > (oldVolume * thresholdMultiplier))
            {
                _lastVolume[symbol] = currentVolume; // Update volume
                return true; 
            }

            _lastVolume[symbol] = currentVolume;
            return false;
        }

        /// <summary>
        /// Tace pairs din da zamu saka a Grid
        /// </summary>
        public static List<string> GetActivePairs(IEnumerable<string> allBinancePairs)
        {
            // Muna son Pairs din da suke karewa da USDT kawai kuma suna cikin TargetAssets dinmu
            return allBinancePairs
                .Where(p => p.EndsWith("USDT"))
                .Where(p => TargetAssets.Any(a => p.StartsWith(a)))
                .ToList();
        }
    }
}
