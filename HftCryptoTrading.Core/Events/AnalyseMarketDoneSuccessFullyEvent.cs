using HftCryptoTrading.Shared.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HftCryptoTrading.Exchanges.Core.Events;

public class AnalyseMarketDoneSuccessFullyEvent() : INotification
{
    public AnalyseMarketDoneSuccessFullyEvent(string exchangeName, List<SymbolTickerData> validSymbols):this()
    {
        ValidSymbols = validSymbols;
        ExchangeName = exchangeName;
    }

    public Guid Id { get; } = Guid.NewGuid();
    public DateTime PublishedDate { get; } = DateTime.UtcNow;
    public List<SymbolTickerData> ValidSymbols { get; }
    public string ExchangeName { get; }
}
