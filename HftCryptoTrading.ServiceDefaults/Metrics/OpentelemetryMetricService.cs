using System.Diagnostics.Metrics;
using System.Diagnostics;
using HftCryptoTrading.Shared.Metrics;
using System.Reflection;

namespace HftCryptoTrading.ServiceDefaults.Metrics;

public class OpentelemetryMetricService : IMetricService
{
    public static string OpenTelemetryServiceName = "HftCryptoTrading";

    private static readonly ActivitySource ActivitySource = new(OpenTelemetryServiceName);
    private readonly Counter<long> _requestCounter;
    private readonly Histogram<double> _requestDuration;

    public OpentelemetryMetricService()
    {
        var meter = new Meter(OpenTelemetryServiceName, Assembly.GetEntryAssembly().GetName().Version.ToString());
        _requestCounter = meter.CreateCounter<long>("request_count", "requests", "Number of exchange API requests");
        _requestDuration = meter.CreateHistogram<double>("request_duration", "ms", "Duration of exchange API requests");
    }

    public IDisposable StartTracking(string operationName)
    {
        var activity = ActivitySource.StartActivity(operationName, ActivityKind.Internal);
        var stopwatch = Stopwatch.StartNew();

        return new TrackingScope(() =>
        {
            stopwatch.Stop();
            activity?.SetTag("duration_ms", stopwatch.ElapsedMilliseconds);
            _requestDuration.Record(stopwatch.ElapsedMilliseconds);
            activity?.Stop();
        });
    }

    public void TrackSuccess(string operationName)
    {
        using var activity = ActivitySource.StartActivity(operationName + ".success");
        _requestCounter.Add(1);
    }

    public void TrackFailure(string operationName, Exception exception)
    {
        using var activity = ActivitySource.StartActivity(operationName + ".failure");
        activity?.SetTag("error", true);
        activity?.SetTag("exception", exception.ToString());
        _requestCounter.Add(1);
    }

    public void TrackFailure(string operationName)
    {
        using var activity = ActivitySource.StartActivity(operationName + ".failure");
        activity?.SetTag("error", true);
        _requestCounter.Add(1);
    }

    private class TrackingScope : IDisposable
    {
        private readonly Action _onDispose;

        public TrackingScope(Action onDispose)
        {
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            _onDispose?.Invoke();
        }
    }
}
