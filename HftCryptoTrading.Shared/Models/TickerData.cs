using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HftCryptoTrading.Shared.Models;

public record class TickerData(string symbol)
{
    public string Symbol { get; set; } = symbol;// The trading pair, e.g., BTC/USDT
    public decimal? Price { get; set; }
    public decimal? Volume { get; set; }
    public decimal? Price24H { get; set; }
    public decimal? HighPrice { get; set; }
    public decimal? LowPrice { get; set; }
    public decimal? PriceChange { get; set; }
    public decimal? PriceChangePercent { get; set; }
}
