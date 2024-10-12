    using HftCryptoTrading.Exchanges.Core.Exchange;
using HftCryptoTrading.Services.Processes;
using HftCryptoTrading.Shared.Events;
using HftCryptoTrading.Shared.Metrics;
using HftCryptoTrading.Shared.Saga;
using MediatR;
using Microsoft.Extensions.Options;

namespace HftCryptoTrading.Saga.StrategyEvaluator.Handlers;

public class DownloadSymbolHistoryEventHandler(IServiceProvider serviceProvider) : 
    INotificationHandler<DownloadSymbolHistoryEvent>
{
    public async Task Handle(DownloadSymbolHistoryEvent notification, CancellationToken cancellationToken)
    {
        var command = ActivatorUtilities.GetServiceOrCreateInstance<EvaluateStrategyCommand>(serviceProvider);
        await command.Evaluate(notification, cancellationToken);
    }
}
