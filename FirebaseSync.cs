using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Linq;

namespace Aurora.Vortex
{
    public class FirebaseSync
    {
        private static readonly HttpClient client = new HttpClient();
        private const string FIREBASE_DB_URL = "https://aurora-vortex-scalper-60b9c-default-rtdb.asia-southeast1.firebasedatabase.app";

        public static async Task UpdateDashboard(decimal totalProfit, int tradeCount, decimal dailyLoss, string status)
        {
            try
            {
                var activeTrades = VortexEngine.ActiveTrades.Values.Where(t => t.IsActive).ToList();
                
                var data = new {
                    Profit = totalProfit,
                    Trades = tradeCount,
                    DailyLoss = dailyLoss,
                    LastUpdate = DateTime.Now.ToString("HH:mm:ss"),
                    EngineStatus = status,
                    ActiveTradesCount = activeTrades.Count,
                    ActiveTrades = activeTrades
                };
                
                string json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                // Use PUT to overwrite the stats node entirely
                var response = await client.PutAsync($"{FIREBASE_DB_URL}/dashboard/stats.json", content);
                
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[FIREBASE] Stats Synced: ${totalProfit} Profit, {activeTrades.Count} Active Trades.");
                }
                else
                {
                    Console.WriteLine($"[FIREBASE] Sync Failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FIREBASE ERROR] Could not sync stats: {ex.Message}");
            }
        }

        public static async Task SendRadarData(object radarData)
        {
            try
            {
                string json = JsonSerializer.Serialize(radarData);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                // Use PUT to overwrite the radar node
                await client.PutAsync($"{FIREBASE_DB_URL}/dashboard/radar.json", content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FIREBASE ERROR] Could not sync radar: {ex.Message}");
            }
        }

        public static async Task SendNotification(string message)
        {
            // Code din tura Firebase Cloud Messaging zai kasance a nan,
            // Ko kuma a tura sabon log zuwa logs.json a Realtime DB
            try 
            {
                var data = new { Message = message, Time = DateTime.Now.ToString("HH:mm:ss") };
                string json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                
                // Use POST to append to logs
                await client.PostAsync($"{FIREBASE_DB_URL}/dashboard/logs.json", content);
                Console.WriteLine($"[NOTIFICATION] 📲 {message}");
            }
            catch {}
        }
    }
}
