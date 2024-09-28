using HftCryptoTrading.Exchanges.Core.Events;
using HftCryptoTrading.Shared.Events;
using HftCryptoTrading.Shared.Models;

namespace HftCryptoTrading.Saga.StrategyEvaluator.Workers;

public interface IStrategyAnalyserSaga
{
    Task PublishHistoricalKlines(SymbolAnalysePriceEvent @event, List<KlineData> history);
    Task PublishStrategyResult(ActionStrategy result, ReceiveSymbolAnaylsePrice notification);
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}