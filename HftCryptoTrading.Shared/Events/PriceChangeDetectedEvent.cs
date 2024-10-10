using HftCryptoTrading.Shared.Models;
using MediatR;

namespace HftCryptoTrading.Shared.Events;

public class PriceChangeDetectedEvent(SymbolData symbol, string exchange) : INotification
{
    public SymbolData Symbol { get; } = symbol;
    public string ExchangeName { get; } = exchange;
    public decimal CurrentPrice { get; set; }
    public decimal OpenPrice { get; set; }
    public decimal HighPrice { get; set; }
    public decimal LowPrice { get; set; }
    public decimal LastQuantity { get; set; }
    public decimal BestBidQuantity { get; set; }
    public decimal BestAskQuantity { get; set; }
    public decimal BestAskPrice { get; set; }
    public decimal BestBidPrice { get; set; }
    public decimal LastPrice { get; set; }
    public DateTime CloseTime { get; set; }
    public DateTime OpenTime { get; set; }
    public decimal PrevDayClosePrice { get; set; }
    public decimal PriceChange { get; set; }
    public decimal PriceChangePercent { get; set; }
    public decimal QuoteVolume { get; set; }
    public decimal Volume { get; set; }
    public decimal WeightedAveragePrice { get; set; }
    public long TotalTrades { get; set; }
}
