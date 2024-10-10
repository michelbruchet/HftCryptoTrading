using HftCryptoTrading.Shared.Models;

namespace HftCryptoTrading.Shared.Saga
{
    public interface IMarketWatcherSaga
    {
        Task PublishDownloadedSymbolAnalysedPriceFailed(string exchangeName, List<SymbolTickerData> abnormalPriceSymbols);
        Task PublishDownloadedSymbolAnalysedSpreadBidAskFailed(string exchangeName, List<SymbolTickerData> abnormalSpreadSymbols);
        Task PublishDownloadedSymbolAnalysedVolumeFailed(string exchangeName, List<SymbolTickerData> abnormalVolumeSymbols);
        Task PublishDownloadedSymbolAnalysedSuccessFully(string exchangeName, List<SymbolTickerData> validSymbols);
        Task PublishDownloadedSymbols(string exchangeName, IEnumerable<SymbolTickerData> data);
        Task PublishStreamedSymbolAnalysedPriceFailed(string exchangeName, SymbolTickerData abnormalPriceSymbols);
        Task PublishStreamedSymbolAnalysedSpreadBidAskFailed(string exchangeName, SymbolTickerData abnormalSpreadSymbol);
        Task PublishStreamedSymbolAnalysedVolumeFailed(string exchangeName, SymbolTickerData abnormalVolumeSymbol);
        Task PublishStreamedSymbolAnalysedSuccessFully(string exchangeName, SymbolTickerData validSymbols);
        Task StartAsync(CancellationToken cancellationToken);
        Task StopAsync(CancellationToken cancellationToken);
    }
}