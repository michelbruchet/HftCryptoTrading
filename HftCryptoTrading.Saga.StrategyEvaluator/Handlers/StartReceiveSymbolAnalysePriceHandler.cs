using HftCryptoTrading.Exchanges.Core.Events;
using HftCryptoTrading.Exchanges.Core.Exchange;
using HftCryptoTrading.Saga.StrategyEvaluator.Workers;
using HftCryptoTrading.Shared.Metrics;
using MediatR;
using Microsoft.Extensions.Options;

namespace HftCryptoTrading.Saga.StrategyEvaluator.Handlers;

public class StartReceiveSymbolAnalysePriceHandler(IOptions<AppSettings> appSettings, IMediator mediator,
        ILoggerFactory loggerFactory, ExchangeProviderFactory factory, IMetricService metricService,
        IStrategyAnalyserSaga saga
        ) : INotificationHandler<StartReceiveSymbolAnalysePrice>
{
    public async Task Handle(StartReceiveSymbolAnalysePrice notification, CancellationToken cancellationToken)
    {
        using var scope = metricService.StartTracking("StartReceiveSymbolAnalysePrice.Handle");

        var exchangeClient = factory.GetExchange(notification.Event.ExchangeName, appSettings.Value, loggerFactory);

        if (exchangeClient == null)
        {
            var exception = new PlatformNotSupportedException($"exchange {notification.Event.ExchangeName} not yet implemented");
            metricService.TrackFailure("StartReceiveSymbolAnalysePrice.Handle", exception);
            loggerFactory.CreateLogger<StartReceiveSymbolAnalysePriceHandler>().LogError(exception, exception.Message);
            throw exception;
        }

        try
        {
            var history = await exchangeClient.GetHistoricalKlinesAsync(notification.Event.SymbolName,
                appSettings.Value.Trading.Period,
                DateTime.UtcNow.AddMinutes(-appSettings.Value.Trading.StartElpasedTime),
                DateTime.UtcNow
            );

            await saga.PublishHistoricalKlines(notification.Event, history);
            metricService.TrackSuccess("StartReceiveSymbolAnalysePrice.Handle");
        }
        catch (Exception ex)
        {
            metricService.TrackFailure("StartReceiveSymbolAnalysePrice.Handle", ex);
            loggerFactory.CreateLogger<StartReceiveSymbolAnalysePriceHandler>().LogError(ex, "Failed to get historical klines.");
            throw; // Re-throwing to maintain the original behavior.
        }
    }
}

