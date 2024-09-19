using HftCryptoTrading.Saga.MarketDownloader.Services;
using System.Collections.Concurrent;

namespace HftCryptoTrading.Saga.MarketDownloader.Workers;

public class MarketDownloaderSagaHost(MarketDownloaderSaga saga, DownloadWorkerService downloadWorkerService) : BackgroundService
{
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await saga.StartAsync(cancellationToken);
        await base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await saga.StopAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromHours(5));
            await downloadWorkerService.DownloadSymbols();
        }
    }
}
