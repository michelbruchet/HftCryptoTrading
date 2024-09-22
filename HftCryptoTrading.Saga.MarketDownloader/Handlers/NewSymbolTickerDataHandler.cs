using HftCryptoTrading.Exchanges.Core.Events;
using HftCryptoTrading.Saga.MarketDownloader.Processes;
using HftCryptoTrading.Shared.Models;
using MediatR;
using Microsoft.Extensions.Options;

namespace HftCryptoTrading.Saga.MarketDownloader.Handlers;

public class NewSymbolTickerDataHandler(IServiceProvider serviceProvider, IOptions<AppSettings> appSettings, ILogger<NewSymbolTickerDataHandler> logger) : INotificationHandler<NewSymbolTickerDataEvent>
{
    public async Task Handle(NewSymbolTickerDataEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var analyseWorker = ActivatorUtilities.GetServiceOrCreateInstance<AnalyseWorkerProcess>(serviceProvider) ?? throw new PlatformNotSupportedException("can not instanciate analyse worker");
            await analyseWorker.AnalyseMarket(notification, appSettings.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }
}
