using MessagePack;

namespace HftCryptoTrading.Shared.Models;

[MessagePackObject]
public record class TickerData()
{
    public TickerData(string symbol, string exchange) : this()
    {
        Symbol = symbol;
        Exchange = exchange;
    }

    [Key(0)]
    public string Symbol { get; set; }

    [Key(1)]
    public string Exchange { get;set; }

    /// <summary>
    /// Last trade price
    /// </summary>
    [Key(2)]
    public decimal? LastPrice { get; set; }

    /// <summary>
    /// Highest price in last 24h
    /// </summary>
    [Key(3)]
    public decimal? HighPrice { get; set; }

    /// <summary>
    /// Lowest price in last 24h
    /// </summary>
    [Key(4)]
    public decimal? LowPrice { get; set; }

    /// <summary>
    /// Trade volume in base asset in the last 24h
    /// </summary>
    [Key(5)]
    public decimal Volume { get; set; }

    /// <summary>
    /// Change percentage in the last 24h
    /// </summary>
    [Key(6)]
    public decimal? ChangePercentage { get; set; }
    [Key(7)]
    public decimal Bid { get; set; }
    [Key(8)]
    public decimal Ask { get; set; }
}
