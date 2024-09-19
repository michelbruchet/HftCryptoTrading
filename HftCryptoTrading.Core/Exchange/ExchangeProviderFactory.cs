using HftCryptoTrading.Shared;
using HftCryptoTrading.Shared.Metrics;
using HftCryptoTrading.Shared.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using System.Collections.Concurrent;

namespace HftCryptoTrading.Exchanges.Core.Exchange;

public class ExchangeProviderFactory(ILoggerFactory loggerFactory)
{
    private static ConcurrentDictionary<string, Func<AppSettings, ILoggerFactory, IExchangeClient>> _clients = new();

    public static void RegisterExchange(string name, ILoggerFactory loggerFactory, Func<AppSettings, ILoggerFactory,
        IExchangeClient> registrationFactory)
    {
        _clients.TryAdd(name, registrationFactory);
    }

    public static IExchangeClient?
        GetExchange(string name, AppSettings settings, ILoggerFactory loggerFactory) =>
        _clients.TryGetValue(name, out var clientFactory) ?
            clientFactory.Invoke(settings, loggerFactory) : null;
}

public class ExchangeClientProxy(string name, ILoggerFactory loggerFactory, AppSettings settings) : IExchangeClient
{
    private IExchangeClient? _client;
    private readonly ILogger _logger;
    private readonly IDistributedCache _cache;
    private readonly IMetricService _metricService;

    public ExchangeClientProxy(string name, ILoggerFactory loggerFactory, AppSettings settings,
        IDistributedCache cache, IMetricService metricService) : this(name, loggerFactory, settings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(settings);

        _client = ExchangeProviderFactory.GetExchange(name, settings, loggerFactory);
        ArgumentNullException.ThrowIfNull(nameof(_client));

        _logger = loggerFactory.CreateLogger<ExchangeClientProxy>();
        _cache = cache;
        _metricService = metricService;
    }

    public void Dispose()
    {
        _client!.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _client!.DisposeAsync();
    }

    public async Task<List<TickerData>> GetCurrentTickersAsync()
    {
        var cacheKey = $"Tickers_{_client.GetType().Name}";
        return await ExecuteWithRetryAndCacheAsync(
            () => _client!.GetCurrentTickersAsync(),
            cacheKey,
            TimeSpan.FromMinutes(5),
            "GetCurrentTickersAsync");
    }

    public async Task<List<KlineData>> GetHistoricalKlinesAsync(string symbol, TimeSpan period, DateTime startTime, DateTime endTime)
    {
        var cacheKey = $"Klines_{symbol}_{period}_{startTime}_{endTime}";
        return (await ExecuteWithRetryAndCacheAsync(
            () => _client!.GetHistoricalKlinesAsync(symbol, period, startTime, endTime),
            cacheKey,
            TimeSpan.FromMinutes(10),
            "GetHistoricalKlinesAsync")).ToList();
    }

    public async Task<List<SymbolData>> GetSymbolsAsync()
    {
        var cacheKey = "Symbols";
        return (await ExecuteWithRetryAndCacheAsync(
            () => _client!.GetSymbolsAsync(),
            cacheKey,
            TimeSpan.FromHours(1),
            "GetSymbolsAsync")).ToList();
    }

    // Helper function for handling retry, caching, and telemetry
    private async Task<T> ExecuteWithRetryAndCacheAsync<T>(Func<Task<T>> apiCall, string cacheKey, TimeSpan cacheDuration, string methodName)
    {
        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning($"Attempt {retryCount} failed for {methodName}. Retrying in {timeSpan.TotalSeconds}s. Error: {exception.Message}");
                });

        var fallbackPolicy = Policy<T>
            .Handle<Exception>()
            .FallbackAsync(async (cancellationToken) =>
            {
                _logger.LogWarning($"Falling back to cache for {methodName} due to repeated failures.");

                var data = await _cache.GetStringAsync(cacheKey);

                if (!string.IsNullOrEmpty(data))
                    return JsonConvert.DeserializeObject<T>(data);
                else
                {
                    throw new Exception("No cache available for fallback.");
                }
            });

        return await fallbackPolicy.WrapAsync(retryPolicy).ExecuteAsync(async () =>
        {
            // Log start of the method
            using var activity = _metricService.StartTracking($"ExchangeClientProxy.{methodName}");
            _logger.LogInformation($"Calling {methodName} at {DateTime.UtcNow}");

            // Call the API
            var result = await apiCall();

            //Serialize
            var json = JsonConvert.SerializeObject(result);

            // Cache the result
            await _cache.SetStringAsync(cacheKey, json, new DistributedCacheEntryOptions
            {
                SlidingExpiration = cacheDuration
            });

            // Log the successful result
            _logger.LogInformation($"{methodName} succeeded at {DateTime.UtcNow} with {result}");

            return result;
        });
    }
}
