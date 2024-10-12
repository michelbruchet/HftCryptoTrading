using HftCryptoTrading.Shared.Models;

namespace HftCryptoTrading.Shared.Saga;

public interface IOpenPositionMonitorSaga
{
    Task StartAsync(CancellationToken cancellationToken);
    Task ExecuteAsync(CancellationToken stoppingToken);
}