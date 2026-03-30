using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aurora.Vortex
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("--- AURORA VORTEX SCALPER [ACTIVE MODE] ---");

            var socket = new VortexSocket();
            
            // Lokacin da sabon farashi ya shigo...
            socket.OnPriceUpdate += (symbol, price, volume) => 
            {
                // 1. Scanner yana duba idan akwai Volume Spike (Kudi ya shigo)
                if (MarketScanner.IsHighVolumeSpike(symbol, volume))
                {
                    // Idan kudi ya shigo, Engine zai duba idan zamu iya shiga
                    VortexEngine.PrepareEntry(symbol, price);
                }

                // 2. Engine yana duba idan muna da ciniki a bude don fita da riba
                VortexEngine.EvaluateMarket(symbol, price);
            };

            // Zaba manyan pairs guda 10 don farawa
            var pairs = MarketScanner.TargetAssets.Select(a => a + "USDT").ToList();
            
            try 
            {
                await socket.StartAsync(pairs);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Connection failed: {ex.Message}");
            }

            await Task.Delay(-1); 
        }
    }
}
