using HftCryptoTrading.Exchanges.Core.Events;
using HftCryptoTrading.Shared.Models;
using MediatR;

namespace HftCryptoTrading.Saga.MarketDownloader.Handlers;

public class NewSymbolTickerDataHandler : INotificationHandler<NewSymbolTickerDataEvent>
{
    public Task Handle(NewSymbolTickerDataEvent notification, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
