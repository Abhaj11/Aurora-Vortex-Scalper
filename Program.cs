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
            var radarPrices = new System.Collections.Concurrent.ConcurrentDictionary<string, decimal>();
            
            // Lokacin da sabon farashi ya shigo...
            socket.OnPriceUpdate += (symbol, price, volume) => 
            {
                // Update slowing radar tracking
                radarPrices[symbol] = price;

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
            
            // Start Background Heartbeat for Slow Radar and Bot Status
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        await FirebaseSync.SendRadarData(radarPrices);
                        await VortexEngine.SyncDashboardState("Running");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[RADAR SYNC ERROR] {ex.Message}");
                    }
                    await Task.Delay(TimeSpan.FromSeconds(15));
                }
            });

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
