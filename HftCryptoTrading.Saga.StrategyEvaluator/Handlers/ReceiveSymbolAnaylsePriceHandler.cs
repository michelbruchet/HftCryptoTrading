using HftCryptoTrading.Exchanges.Core.Events;
using HftCryptoTrading.Exchanges.Core.Exchange;
using HftCryptoTrading.Saga.StrategyEvaluator.Workers;
using HftCryptoTrading.Shared.Metrics;
using HftCryptoTrading.Shared.Models;
using MediatR;
using Microsoft.Extensions.Options;
using Skender.Stock.Indicators;
using StrategyExecution;

namespace HftCryptoTrading.Saga.StrategyEvaluator.Handlers;

public class ReceiveSymbolAnaylsePriceHandler(IOptions<AppSettings> appSettings, IMediator mediator,
        ILoggerFactory loggerFactory, ExchangeProviderFactory factory, IMetricService metricService,
        IStrategyAnalyserSaga saga
        ) : INotificationHandler<ReceiveSymbolAnaylsePrice>
{
    public async Task Handle(ReceiveSymbolAnaylsePrice notification, CancellationToken cancellationToken)
    {
        using var scope = metricService.StartTracking("ReceiveSymbolAnaylsePrice.Handle");

        var quotes = new List<Quote>();

        try
        {
            quotes.AddRange(notification.History.Select(k => new Quote
            {
                Close = (decimal)k.ClosePrice,
                Date = k.CloseTime.GetValueOrDefault(),
                High = (decimal)k.HighPrice,
                Low = (decimal)k.LowPrice,
                Open = (decimal)k.OpenPrice,
                Volume = (decimal)k.Volume
            }));

            quotes.Add(new Quote
            {
                Date = notification.Event.PublishedDate.GetValueOrDefault(),
                Close = (decimal)notification.Event.Price,
                High = (decimal)notification.Event.HighPrice,
                Low = (decimal)notification.Event.LowPrice,
                Open = (decimal)(notification.Event.Price - notification.Event.PriceChange),
                Volume = (decimal)notification.Event.Volume
            });

            var result = StrategyLoaderService.Service.Evaluate(quotes);
            await saga.PublishStrategyResult(result, notification);
            metricService.TrackSuccess("ReceiveSymbolAnaylsePrice.Handle");
        }
        catch (Exception ex)
        {
            metricService.TrackFailure("ReceiveSymbolAnaylsePrice.Handle", ex);
            loggerFactory.CreateLogger<ReceiveSymbolAnaylsePriceHandler>().LogError(ex, "Failed to evaluate strategy.");
            throw; // Re-throwing to maintain the original behavior.
        }
    }
}
