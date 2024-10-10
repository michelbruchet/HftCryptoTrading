namespace HftCryptoTrading.Shared.Metrics;

public interface IMetricService
{
    IDisposable StartTracking(string operationName);
    void TrackSuccess(string operationName);
    void TrackFailure(string operationName, Exception exception);
    void TrackFailure(string operationName);
}
