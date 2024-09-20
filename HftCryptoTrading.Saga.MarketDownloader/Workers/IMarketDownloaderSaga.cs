using HftCryptoTrading.Shared.Models;

namespace HftCryptoTrading.Saga.MarketDownloader.Workers
{
    public interface IMarketDownloaderSaga
    {
        Task PublishSymbols(IEnumerable<SymbolTickerData> data);
        Task StartAsync(CancellationToken cancellationToken);
        Task StopAsync(CancellationToken cancellationToken);
    }
}