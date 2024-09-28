using HftCryptoTrading.Shared.Events;
using HftCryptoTrading.Shared.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HftCryptoTrading.Exchanges.Core.Events;

public class StartReceiveSymbolAnalysePrice : INotification
{
    public SymbolAnalysePriceEvent Event { get; private set; }

    public StartReceiveSymbolAnalysePrice(SymbolAnalysePriceEvent e)
    {
        this.Event = e;
    }
}

public class ReceiveSymbolAnaylsePrice : INotification
{
    public SymbolAnalysePriceEvent Event { get; set; }
    public List<KlineData> History { get; set; }

    public ReceiveSymbolAnaylsePrice(SymbolAnalysePriceEvent @event, List<KlineData> history)
    {
        Event = @event;
        History = history;
    }

    public static implicit operator LongSymbolDetected(ReceiveSymbolAnaylsePrice @event)
    {
        return new LongSymbolDetected
        {
            Symbol = @event.Event,
            History = @event.History
        };
    }

    public static implicit operator ShortSymbolDetected(ReceiveSymbolAnaylsePrice @event)
    {
        return new ShortSymbolDetected
        {
            Symbol = @event.Event,
            History = @event.History
        };
    }
}
