using Newtonsoft.Json;

namespace CryptoTask;

public struct TradeData
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