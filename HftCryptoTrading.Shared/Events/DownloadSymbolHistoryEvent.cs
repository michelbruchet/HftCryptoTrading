using HftCryptoTrading.Shared.Models;
using MediatR;

namespace HftCryptoTrading.Shared.Events;

public class DownloadSymbolHistoryEvent : INotification
{
    public SymbolAnalysedSuccessFullyEvent Event { get; set; }
    public List<KlineData> History { get; set; }

    public DownloadSymbolHistoryEvent(SymbolAnalysedSuccessFullyEvent @event, List<KlineData> history)
    {
        Event = @event;
        History = history;
    }

    public static implicit operator LongTradeSymbolDetectedEvent(DownloadSymbolHistoryEvent @event)
    {
        return new LongTradeSymbolDetectedEvent
        {
            Event = @event.Event,
            History = @event.History
        };
    }

    public static implicit operator ShortTradeSymbolDetectedEvent(DownloadSymbolHistoryEvent @event)
    {
        return new ShortTradeSymbolDetectedEvent
        {
            Event = @event.Event,
            History = @event.History
        };
    }
}
