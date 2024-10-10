namespace HftCryptoTrading.Shared.Models;

public record class TickerData(string symbol, string exchange)
{
    public string Symbol => symbol;
    public string Exchange => exchange;

    /// <summary>
    /// Last trade price
    /// </summary>
    public decimal? LastPrice { get; set; }

    /// <summary>
    /// Highest price in last 24h
    /// </summary>
    public decimal? HighPrice { get; set; }

    /// <summary>
    /// Lowest price in last 24h
    /// </summary>
    public decimal? LowPrice { get; set; }

    /// <summary>
    /// Trade volume in base asset in the last 24h
    /// </summary>
    public decimal Volume { get; set; }

    /// <summary>
    /// Change percentage in the last 24h
    /// </summary>
    public decimal? ChangePercentage { get; set; }
    public decimal Bid { get; set; }
    public decimal Ask { get; set; }
}
