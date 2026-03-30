using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Aurora.Vortex
{
    public class BinanceExecution
    {
        private static string _apiKey = "SAKA_API_KEY_ANAN";
        private static string _apiSecret = "SAKA_SECRET_KEY_ANAN";
        private static string _baseUrl = "https://api.binance.com";

        public static async Task<bool> SendMarketOrder(string symbol, string side, decimal quantity)
        {
            using var client = new HttpClient();
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string queryString = $"symbol={symbol}&side={side}&type=MARKET&quantity={quantity}&timestamp={timestamp}";
            string signature = CreateSignature(queryString, _apiSecret);
            
            client.DefaultRequestHeaders.Add("X-MBX-APIKEY", _apiKey);
            var response = await client.PostAsync($"{_baseUrl}/api/v3/order?{queryString}&signature={signature}", null);
            
            string result = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode) {
                Console.WriteLine($"[ORDER SUCCESS] {side} {symbol}");
                return true;
            }
            Console.WriteLine($"[ORDER FAILED] {result}");
            return false;
        }

        private static string CreateSignature(string message, string secret)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(secret);
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            using var hmac = new HMACSHA256(keyBytes);
            byte[] hash = hmac.ComputeHash(messageBytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}
