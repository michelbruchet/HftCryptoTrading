using HftCryptoTrading.Shared.Saga;

namespace HftCryptoTrading.Saga.StrategyEvaluator.Workers;

public class StrategyAnalyserSagaHost(IStrategyAnalyserSaga saga) : BackgroundService
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
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}