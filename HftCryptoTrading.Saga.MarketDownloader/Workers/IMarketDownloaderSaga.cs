using HftCryptoTrading.Shared.Models;

namespace HftCryptoTrading.Saga.MarketDownloader.Workers
{
    public interface IMarketDownloaderSaga
    {
        Task PublishAbnormalPriceSymbols(string exchangeName, List<SymbolTickerData> abnormalPriceSymbols);
        Task PublishAbnormalSpreadSymbols(string exchangeName, List<SymbolTickerData> abnormalSpreadSymbols);
        Task PublishAbnormalVolumeSymbols(string exchangeName, List<SymbolTickerData> abnormalVolumeSymbols);
        Task PublishAnalyseMarketDoneSuccessFully(string exchangeName, List<SymbolTickerData> validSymbols);
        Task PublishAnalysePriceDoneSuccessFully(string exchangeName, SymbolTickerData symbol);
        Task PublishSymbols(string exchangeName, IEnumerable<SymbolTickerData> data);
        Task StartAsync(CancellationToken cancellationToken);
        Task StopAsync(CancellationToken cancellationToken);
    }
}