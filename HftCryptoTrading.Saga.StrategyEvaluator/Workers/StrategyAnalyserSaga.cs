
using HftCryptoTrading.Saga.StrategyEvaluator.Indicators;
using Microsoft.Extensions.Options;

namespace HftCryptoTrading.Saga.StrategyEvaluator.Workers;

public class StrategyAnalyserSaga(IOptions<AppSettings> appSettings) : IStrategyAnalyserSaga
{
    private IndicatorLoaderService _indicatorLoaderService;
    private string _indicatorsPath;

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _indicatorsPath = appSettings.Value.Runtime.IndicatorsPath;
        IndicatorLoaderService.Service.LoadIndicators(_indicatorsPath);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
