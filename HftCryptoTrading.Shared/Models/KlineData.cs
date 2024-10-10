using MessagePack;

namespace HftCryptoTrading.Shared.Models;

[MessagePackObject]
public class KlineData(string name)
{
    [Key(0)]
    public string Symbol { get; set; } = name;  // The trading pair, e.g., BTC/USDT
    [Key(1)]
    public DateTime OpenTime { get; set; }  // The time when the kline started
    [Key(2)]
    public decimal? OpenPrice { get; set; }  // Opening price
    [Key(3)]
    public decimal? HighPrice { get; set; }  // Highest price within the time period
    [Key(4)]
    public decimal? LowPrice { get; set; }  // Lowest price within the time period
    [Key(5)]
    public decimal? ClosePrice { get; set; }  // Closing price
    [Key(6)]
    public decimal? Volume { get; set; }  // Trading volume within the time period
    [Key(7)]
    public DateTime? CloseTime { get; set; }  // The time when the kline ended

    // Constructor for quick initialization
    public KlineData(string symbol, DateTime openTime, decimal? open, decimal? high,
            decimal? low, decimal? close, decimal? volume, DateTime? closeTime)
        : this(symbol)
    {
        OpenTime = openTime;
        OpenPrice = open;
        HighPrice = high;
        LowPrice = low;
        ClosePrice = close;
        Volume = volume;
        CloseTime = closeTime;
    }
}
