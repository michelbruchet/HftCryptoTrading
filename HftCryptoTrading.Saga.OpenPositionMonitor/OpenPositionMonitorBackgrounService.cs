using HftCryptoTrading.Shared.Saga;

namespace HftCryptoTrading.Saga.OpenPositionMonitor;

public class OpenPositionMonitorBackgrounService(IOpenPositionMonitorSaga saga) : BackgroundService
{
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await saga.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await saga.ExecuteAsync(stoppingToken);
    }
}
