using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

namespace Aurora.Vortex
{
    public class VortexSocket
    {
        private ClientWebSocket _ws = new();
        private readonly Uri _uri = new Uri("wss://stream.binance.com/ws");

        // Wannan Event din zai sanar da mu duk lokacin da sabon farashi ya zo
        public event Action<string, decimal, decimal>? OnPriceUpdate;

        public async Task StartAsync(List<string> pairs)
        {
            await _ws.ConnectAsync(_uri, CancellationToken.None);
            Console.WriteLine("[WS] Connected to Binance Vortex Stream.");

            // Subscribe ga "Mini Ticker" domin yana da sauri kuma bayanan sa basu da nauyi
            var subRequest = new
            {
                method = "SUBSCRIBE",
                @params = pairs.Select(p => $"{p.ToLower()}@miniTicker").ToArray(),
                id = 1
            };

            string json = JsonSerializer.Serialize(subRequest);
            await _ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(json)), 
                WebSocketMessageType.Text, true, CancellationToken.None);

            _ = ReceiveLoop(); // Fara karbar data a background
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[1024 * 8];
            while (_ws.State == WebSocketState.Open)
            {
                var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                string message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                using (JsonDocument doc = JsonDocument.Parse(message))
                {
                    JsonElement root = doc.RootElement;
                    // Tabbatar cewa sakon ticker ne (yana da "s" na symbol da "c" na close price)
                    if (root.TryGetProperty("s", out var symbol) && root.TryGetProperty("c", out var price))
                    {
                        decimal lastPrice = decimal.Parse(price.GetString()!);
                        decimal volume = decimal.Parse(root.GetProperty("v").GetString()!);
                        
                        // Sanar da sauran sassan bot din
                        OnPriceUpdate?.Invoke(symbol.GetString()!, lastPrice, volume);
                    }
                }
            }
        }
    }
}
