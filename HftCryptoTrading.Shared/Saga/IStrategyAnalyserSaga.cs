using HftCryptoTrading.Shared.Events;
using HftCryptoTrading.Shared.Models;
using HftCryptoTrading.Shared.Strategies;

namespace HftCryptoTrading.Shared.Saga;

public interface IStrategyAnalyserSaga
{
    Task PublishHistoricalKlines(SymbolAnalysedSuccessFullyEvent @event, List<KlineData> history);
    Task PublishStrategyResult(ActionStrategy result, DownloadSymbolHistoryEvent notification);
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}