using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using Newtonsoft.Json;

namespace CryptoTask
{
    // Structure representing trade data received from the WebSocket

    class Program
    {
        private static readonly object _lockObject = new object();
        private static Dictionary<string, List<TradeData>> _tradesDataset = new Dictionary<string, List<TradeData>>();
        private static int _numberOfTradesToKeep;
        
        // Variables to store trade information for display

        private static string _tradeData = "";
        private static string _tradeId = "N/A";
        private static string _tradeSymbol = "N/A";
        private static string _price = "N/A";
        private static string _quantity = "N/A";
        
        // Variables for trade side (Buy/Sell) and console color

        private static bool _isSell;
        private static string _side = "";
        private static byte[] _buffer = new byte[1024];
        private static ConsoleColor _consoleColor = ConsoleColor.White;
        private static StringBuilder _sb = new StringBuilder();

        static void Main()
        {
            Console.Write("Enter the number of trades to keep for each trading pair: ");
            if (int.TryParse(Console.ReadLine(), out _numberOfTradesToKeep) && _numberOfTradesToKeep > 0)
            {
                string? inputString = Console.ReadLine();
                List<string> tradingPairs = ParseInput(inputString);
            //    List<string> tradingPairs = new List<string> { "btcusdt", "ethusdt", "bnbusdt", "USDTUSDC".ToLower(), "DOGESHIB".ToLower() // Add your trading pairs of interest};

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
                        WebSocketReceiveResult result = socket.ReceiveAsync(new ArraySegment<byte>(_buffer), CancellationToken.None).Result;

                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            _tradeData = Encoding.UTF8.GetString(_buffer, 0, result.Count);
                            HandleTradeUpdate(tradingPair, _tradeData);
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
            lock (_lockObject)
            {
                if (!_tradesDataset.TryGetValue(tradingPair, out var tradeList))
                {
                    tradeList = new List<TradeData>();
                    _tradesDataset[tradingPair] = tradeList;
                }

                TradeData tradeObject = JsonConvert.DeserializeObject<TradeData>(tradeData);
                tradeList.Add(tradeObject);

                if (tradeList.Count > _numberOfTradesToKeep)
                {
                    tradeList.RemoveRange(0, tradeList.Count - _numberOfTradesToKeep);
                }
            }
        }

        static void ClearOldValues()
        {
            while (true)
            {
                Thread.Sleep(TimeSpan.FromMinutes(1));

                lock (_lockObject)
                {
                    foreach (var pair in _tradesDataset)
                    {
                        if (pair.Value.Count > _numberOfTradesToKeep)
                        {
                            pair.Value.RemoveRange(0, pair.Value.Count - _numberOfTradesToKeep);
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

                lock (_lockObject)
                {
                    Console.Clear();
                    GC.Collect();
                    Console.WriteLine("Trades:");
                    foreach (var pair in _tradesDataset)
                    {
                        foreach (var tradeData in pair.Value)
                        {
                            _tradeId = tradeData.TradeID ?? "N/A";
                            _tradeSymbol = tradeData.Symbol ?? "N/A";
                            _price = tradeData.Price ?? "N/A";
                            _quantity = tradeData.Quantity ?? "N/A";

                            _isSell = tradeData.IsBuyerMaker;
                            _side = _isSell ? "Sell" : "Buy";

                            _consoleColor = _isSell ? ConsoleColor.Red : ConsoleColor.Green;
                            Console.ForegroundColor = _consoleColor;
                            _sb.Clear();
                            _sb.Append(($"| {_tradeSymbol,-10} | {_tradeId,-10} | {_price,-15} | {_quantity,-15} | {_side,-5} |"));
                            Console.WriteLine(_sb);
                        }
                    }
                }
            }
        }
        static List<string> ParseInput(string? input)
        {
            List<string> resultList = new List<string>();

            // Split the input based on spaces or commas
            string[] splitValues = input.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);

            // Add the split values to the result list
            resultList.AddRange(splitValues);

            return resultList;
        }
    }
}
