using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HftCryptoTrading.Shared.Models;

public record class BookPriceData
{
    public BookPriceData(string symbol)
    {
        this.Symbol = symbol;
    }

    public string Symbol { get; }
    public decimal BestBidPrice { get; set; }
    public decimal BestAskPrice { get; set; }
    public decimal BestBidQuantity { get; set; }
    public decimal BestAskQuantity { get; set; }
}

