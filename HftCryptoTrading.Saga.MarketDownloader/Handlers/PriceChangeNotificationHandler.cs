using HftCryptoTrading.Exchanges.Core.Events;
using HftCryptoTrading.Saga.MarketDownloader.Processes;
using MediatR;
using Microsoft.Extensions.Options;
using System.Reactive;

namespace HftCryptoTrading.Saga.MarketDownloader.Handlers;

public class PriceChangeNotificationHandler(
    IServiceProvider serviceProvider, 
    IOptions<AppSettings> appSettings, 
    ILogger<NewSymbolTickerDataHandler> logger)
    : INotificationHandler<PriceChangeNotification>
{
    public async Task Handle(PriceChangeNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            var analyseWorker = ActivatorUtilities.GetServiceOrCreateInstance<AnalyseWorkerProcess>(serviceProvider) ?? throw new PlatformNotSupportedException("can not instanciate analyse worker");
            await analyseWorker.AnalysePrice(notification, appSettings.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }
}
