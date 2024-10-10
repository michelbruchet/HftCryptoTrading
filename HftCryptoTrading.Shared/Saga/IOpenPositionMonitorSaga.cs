using HftCryptoTrading.Shared.Models;

namespace HftCryptoTrading.Shared.Saga;

public interface IOpenPositionMonitorSaga
{
    Task PlaceCloseOrderResult(OpenOrder openPosition);
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}