
using HftCryptoTrading.Shared.Events;
using HftCryptoTrading.Shared.Metrics;
using HftCryptoTrading.Shared.Saga;
using Microsoft.Extensions.Logging;
using Skender.Stock.Indicators;
using StrategyExecution;
namespace HftCryptoTrading.Services.Processes;

public class EvaluateStrategyCommand(IMetricService metricService, 
    ILogger<EvaluateStrategyCommand> logger, 
    IStrategyAnalyserSaga saga,
    StrategyLoaderService strategyLoaderService
    )
{
    public async Task Evaluate(DownloadSymbolHistoryEvent notification, CancellationToken cancellationToken)
    {
        using var scope = metricService.StartTracking($"Start EvaluateStrategyCommand for {notification.Event.SymbolData.Name}");

        var quotes = new List<Quote>();

        try
        {
            quotes = notification.History.Select(k => new Quote
            {
                Date = k.CloseTime.GetValueOrDefault(),
                Close = (decimal)k.ClosePrice,
                High = (decimal)k.HighPrice,
                Low = (decimal)k.LowPrice,
                Open = (decimal)k.OpenPrice,
                Volume = (decimal)k.Volume
            }).ToList();

            quotes.Add(new Quote
            {
                Date = notification.Event.PublishedDate.GetValueOrDefault(),
                Close = (decimal)notification.Event.Price,
                High = (decimal)notification.Event.HighPrice,
                Low = (decimal)notification.Event.LowPrice,
                Open = (decimal)(notification.Event.Price - notification.Event.PriceChangePercent),
                Volume = (decimal)notification.Event.Volume
            });

            quotes = quotes.OrderBy(k => k.Date).ToList();

            var result = strategyLoaderService.Evaluate(quotes);

            await saga.PublishStrategyResult(result, notification);
            metricService.TrackSuccess($"Finish EvaluateStrategyCommand for {notification.Event.SymbolData.Name} successfully");
        }
        catch (Exception ex)
        {
            metricService.TrackFailure($"Run EvaluateStrategyCommand for {notification.Event.SymbolData.Name} failed", ex);
            logger.LogError(ex, "Failed to evaluate strategy.");
            throw; // Re-throwing to maintain the original behavior.
        }
    }
}