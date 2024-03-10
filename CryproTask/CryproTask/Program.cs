using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading; 
using Newtonsoft.Json;

namespace CryptoTask
{
    struct TradeData
    {
        [JsonProperty("e")]
        public string Event { get; set; }
        [JsonProperty("E")]
        public long EventTime { get; set; }
        [JsonProperty("s")]
        public string Symbol { get; set; }
        [JsonProperty("t")]
        public string TradeID { get; set; }
        [JsonProperty("p")]
        public string Price { get; set; }
        [JsonProperty("q")]
        public string Quantity { get; set; }
        [JsonProperty("b")]
        public long BuyerOrderID { get; set; }
        [JsonProperty("a")]
        public long SellerOrderID { get; set; }
        [JsonProperty("T")]
        public long TradeTime { get; set; }
        [JsonProperty("m")]
        public bool IsBuyerMaker { get; set; }
        [JsonProperty("M")]
        public bool IsBestMatch { get; set; }
    }

    class Program
    {
        private static readonly object lockObject = new object();
        private static Dictionary<string, List<TradeData>> tradesDataset = new Dictionary<string, List<TradeData>>();
        private static int numberOfTradesToKeep;
        
        private static string tradeId = "N/A";
        private static string tradeSymbol = "N/A";
        private static string price = "N/A";
        private static string quantity = "N/A";

        private static bool isSell;
        private static string side = "";

        private static ConsoleColor _consoleColor  = ConsoleColor.White;
        static void Main()
        {
            Console.Write("Enter the number of trades to keep for each trading pair: ");
            if (int.TryParse(Console.ReadLine(), out numberOfTradesToKeep) && numberOfTradesToKeep > 0)
            {
                List<string> tradingPairs = new List<string>
                {
                    "btcusdt", "ethusdt", "bnbusdt" // Add your trading pairs of interest
                };

                Thread clearOldValuesThread = new Thread(ClearOldValues);
                clearOldValuesThread.Start();

                Thread outputTradesThread = new Thread(OutputTrades);
                outputTradesThread.Start();

                foreach (var tradingPair in tradingPairs)
                {
                    Thread thread = new Thread(() => SubscribeToTrades(tradingPair));
                    thread.Start();
                }

                Console.ReadLine();
            }
            else
            {
                Console.WriteLine("Invalid input. Please enter a positive integer for the number of trades to keep.");
            }
        }

        static void SubscribeToTrades(string tradingPair)
        {
            string endpoint = $"wss://stream.binance.com:9443/ws/{tradingPair.ToLower()}@trade";

            using (ClientWebSocket socket = new ClientWebSocket())
            {
                socket.ConnectAsync(new Uri(endpoint), CancellationToken.None).Wait();

                while (socket.State == WebSocketState.Open)
                {
                    try
                    {
                        byte[] buffer = new byte[1024];
                        WebSocketReceiveResult result = socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None).Result;

                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            string tradeData = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            HandleTradeUpdate(tradingPair, tradeData);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                    }
                }
            }
        }

        static void HandleTradeUpdate(string tradingPair, string tradeData)
        {
            lock (lockObject)
            {
                if (!tradesDataset.TryGetValue(tradingPair, out var tradeList))
                {
                    tradeList = new List<TradeData>();
                    tradesDataset[tradingPair] = tradeList;
                }

                TradeData tradeObject = JsonConvert.DeserializeObject<TradeData>(tradeData);
                tradeList.Add(tradeObject);

                if (tradeList.Count > numberOfTradesToKeep)
                {
                    tradeList.RemoveRange(0, tradeList.Count - numberOfTradesToKeep);
                }
            }
        }

        static void ClearOldValues()
        {
            while (true)
            {
                Thread.Sleep(TimeSpan.FromMinutes(1));
                
                lock (lockObject)
                {
                    foreach (var pair in tradesDataset)
                    {
                        if (pair.Value.Count > numberOfTradesToKeep)
                        {
                            pair.Value.RemoveRange(0, pair.Value.Count - numberOfTradesToKeep);
                        }
                    }
                }
            }
        }

        static void OutputTrades()
        {
            while (true)
            {
                Thread.Sleep(200);

                lock (lockObject)
                {
                    Console.Clear();
                    Console.WriteLine("Trades:");
                    foreach (var pair in tradesDataset)
                    {
                        foreach (var tradeData in pair.Value)
                        {
                             tradeId = tradeData.TradeID ?? "N/A";
                             tradeSymbol = tradeData.Symbol ?? "N/A";
                             price = tradeData.Price ?? "N/A";
                             quantity = tradeData.Quantity ?? "N/A";

                             isSell = tradeData.IsBuyerMaker;
                             side = isSell ? "Sell" : "Buy";

                             _consoleColor  = isSell ? ConsoleColor.Red : ConsoleColor.Green;
                            Console.ForegroundColor = _consoleColor;

                            Console.WriteLine(
                                $"| {tradeSymbol,-10} | {tradeId,-20} | {price,-15} | {quantity,-15} | {side,-10} |");
                        }
                    }
                }
            }
        }
    }
}
