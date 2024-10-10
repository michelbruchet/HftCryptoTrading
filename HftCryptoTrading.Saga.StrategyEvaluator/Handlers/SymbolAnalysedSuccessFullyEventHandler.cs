using HftCryptoTrading.Exchanges.Core.Exchange;
using HftCryptoTrading.Shared.Events;
using HftCryptoTrading.Shared.Metrics;
using HftCryptoTrading.Shared.Saga;
using MediatR;
using Microsoft.Extensions.Options;

namespace HftCryptoTrading.Saga.StrategyEvaluator.Handlers;

public class SymbolAnalysedSuccessFullyEventHandler(IOptions<AppSettings> appSettings, IMediator mediator,
        ILoggerFactory loggerFactory, ExchangeProviderFactory factory, IMetricService metricService,
        IStrategyAnalyserSaga saga
        ) : INotificationHandler<SymbolAnalysedSuccessFullyEvent>
{
    public async Task Handle(SymbolAnalysedSuccessFullyEvent notification, CancellationToken cancellationToken)
    {
        using var scope = metricService.StartTracking($"StartReceiveSymbolAnalysePrice.Handle for symbol {notification.SymbolData.Name}");

        var exchangeClient = factory.GetExchange(notification.ExchangeName, appSettings.Value, loggerFactory);

        if (exchangeClient == null)
        {
            var exception = new PlatformNotSupportedException($"exchange {notification.ExchangeName} not yet implemented");
            metricService.TrackFailure("StartReceiveSymbolAnalysePrice.Handle", exception);
            loggerFactory.CreateLogger<SymbolAnalysedSuccessFullyEventHandler>()
                .LogError(exception, exception.Message);
            throw exception;
        }

        try
        {
            var history = await exchangeClient.GetHistoricalKlinesAsync(notification.SymbolData.Name,
                appSettings.Value.Trading.Period,
                DateTime.UtcNow.AddMinutes(-appSettings.Value.Trading.StartElpasedTime),
                DateTime.UtcNow
            );

            await saga
                .PublishHistoricalKlines(notification, history);
            
            metricService.TrackSuccess("StartReceiveSymbolAnalysePrice.Handle");
        }
        catch (Exception ex)
        {
            metricService.TrackFailure("StartReceiveSymbolAnalysePrice.Handle", ex);
            loggerFactory.CreateLogger<SymbolAnalysedSuccessFullyEventHandler>().LogError(ex, "Failed to get historical klines.");
            throw; // Re-throwing to maintain the original behavior.
        }
    }
}