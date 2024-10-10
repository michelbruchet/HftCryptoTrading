using HftCryptoTrading.Exchanges.Core.Exchange;
using HftCryptoTrading.Shared.Metrics;
using HftCryptoTrading.Shared.Models;
using Polly.Retry;
using Polly;
using HftCryptoTrading.Shared.Saga;

namespace HftCryptoTrading.Services.Processes;

public class DownloadSymbolCommand
{
    private readonly AsyncRetryPolicy _retryPolicy;

    private readonly IExchangeClient _exchange;
    private readonly IMetricService _metricService;
    private readonly IMarketWatcherSaga _saga;
    private readonly AppSettings _appSettings;

    public DownloadSymbolCommand(IExchangeClient exchange, IMetricService metricService, IMarketWatcherSaga saga, AppSettings appSettings)
    {
        _exchange = exchange;
        _metricService = metricService;
        _saga = saga;
        _appSettings = appSettings;

        _retryPolicy = Policy
            .Handle<Exception>() // Gérer les exceptions spécifiques selon vos besoins
            .WaitAndRetryAsync(
                3, // Nombre de tentatives
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) // Backoff exponentiel
            );
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var symbols = await ExecuteWithTelemetryAsync("Download-symbols", _exchange.GetSymbolsAsync);
        var tickers = await ExecuteWithTelemetryAsync("Download-tickers", _exchange.GetCurrentTickersAsync);

        // Joindre les données des symboles et des tickers
        var joinedData = symbols.Join(
            tickers,
            symbol => symbol.Name,
            ticker => ticker.Symbol,
            (symbol, ticker) => new SymbolTickerData(_exchange.ExchangeName, symbol)
            {
                Ticker = ticker,
                PublishedDate = DateTime.UtcNow,
                PriceChangePercent = ticker.ChangePercentage.GetValueOrDefault(),
            }).OrderByDescending(s => s.PriceChangePercent).ThenBy(s => s.Ticker.Volume).ToList();

        var data = joinedData.Take(_appSettings.LimitSymbolsMarket).ToList();

        // Publier les symboles
        await PublishDataAsync(data);
    }

    private async Task<List<T>> ExecuteWithTelemetryAsync<T>(string operationName, Func<Task<List<T>>> action)
    {
        using (_metricService.StartTracking(operationName))
        {
            try
            {
                var result = await _retryPolicy.ExecuteAsync(action);
                if (result == null)
                {
                    _metricService.TrackFailure(operationName);
                    return new List<T>();
                }

                _metricService.TrackSuccess(operationName);
                return result;
            }
            catch (Exception ex)
            {
                _metricService.TrackFailure(operationName, ex);
                throw;
            }
        }
    }

    private async Task PublishDataAsync(IEnumerable<SymbolTickerData> data)
    {
        using (_metricService.StartTracking("publish-symbols"))
        {
            try
            {
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    await _saga.PublishDownloadedSymbols(_exchange.ExchangeName, data);
                });

                _metricService.TrackSuccess("publish-symbols");
            }
            catch (Exception ex)
            {
                _metricService.TrackFailure("publish-symbols", ex);
                throw;
            }
        }
    }
}
