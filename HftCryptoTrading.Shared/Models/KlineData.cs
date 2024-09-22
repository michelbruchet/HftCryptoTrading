namespace HftCryptoTrading.Shared.Models;

public class KlineData(string name)
{
    public string Symbol { get; set; } = name;  // The trading pair, e.g., BTC/USDT
    public DateTime OpenTime { get; set; }  // The time when the kline started
    public decimal? OpenPrice { get; set; }  // Opening price
    public decimal? HighPrice { get; set; }  // Highest price within the time period
    public decimal? LowPrice { get; set; }  // Lowest price within the time period
    public decimal? ClosePrice { get; set; }  // Closing price
    public decimal? Volume { get; set; }  // Trading volume within the time period
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
