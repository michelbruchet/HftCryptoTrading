namespace HftCryptoTrading.Saga.StrategyEvaluator.Workers;

public interface IStrategyAnalyserSaga
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}