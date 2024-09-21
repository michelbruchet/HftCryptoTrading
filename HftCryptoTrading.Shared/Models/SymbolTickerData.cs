using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HftCryptoTrading.Shared.Models;

public class SymbolTickerData(string exchange)
{
    public string Exchange { get; } = exchange;
    public SymbolData Symbol { get; set; }
    public TickerData Ticker { get; set; }
    public DateTime PublishedDate { get; set; }
    public decimal PriceChangePercent { get; set; }
    public decimal Volume { get; set; }
}
