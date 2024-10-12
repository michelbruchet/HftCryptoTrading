using HftCryptoTrading.Services.Processes;
using HftCryptoTrading.Shared.Events;
using MediatR;
using Microsoft.Extensions.Options;

namespace HftCryptoTrading.Saga.MarketWatcher.Handlers;

public class PublishedDownloadedSymbolsHandler(IServiceProvider serviceProvider, IOptions<AppSettings> appSettings, ILogger<PublishedDownloadedSymbolsHandler> logger) : IRequestHandler<PublishedDownloadedSymbolsEvent>
{
    public async Task Handle(PublishedDownloadedSymbolsEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var analyseProcess = ActivatorUtilities
                .GetServiceOrCreateInstance<AnalyseDownloadedSymbolCommand>(serviceProvider) 
                    ?? throw new PlatformNotSupportedException("can not instanciate analyse worker");
            
            await analyseProcess.RunAsync(notification);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }
}
