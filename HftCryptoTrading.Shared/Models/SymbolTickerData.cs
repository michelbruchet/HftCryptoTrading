namespace HftCryptoTrading.Shared.Models;

public class SymbolTickerData(string exchange, SymbolData symbol)
{
    public string Exchange { get; } = exchange;
    public SymbolData Symbol { get; } = symbol;
    public TickerData Ticker { get; set; }
    public DateTime PublishedDate { get; set; }
    public decimal PriceChangePercent { get; set; }
    public BookPriceData? BookPrice { get; set; }
}