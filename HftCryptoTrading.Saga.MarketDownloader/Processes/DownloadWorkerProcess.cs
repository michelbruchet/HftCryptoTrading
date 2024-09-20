using HftCryptoTrading.Exchanges.Core.Exchange;
using HftCryptoTrading.Saga.MarketDownloader.Workers;
using HftCryptoTrading.Shared.Metrics;
using HftCryptoTrading.Shared.Models;
using HftCryptoTrading.Shared;
using Polly.Retry;
using Polly;

namespace HftCryptoTrading.Saga.MarketDownloader.Processes;

public class DownloadWorkerProcess
{
    private readonly AsyncRetryPolicy _retryPolicy;

    IExchangeClient _exchange;
    IMetricService _metricService;
    IMarketDownloaderSaga _saga;
    AppSettings _appSettings;

    public DownloadWorkerProcess(IExchangeClient exchange, IMetricService metricService, IMarketDownloaderSaga saga, AppSettings appSettings)
    {
        _exchange = exchange;
        _metricService = metricService;
        _saga = saga;
        _appSettings = appSettings;

        _retryPolicy = Policy
            .Handle<Exception>() // Gérez les exceptions spécifiques selon vos besoins
            .WaitAndRetryAsync(
                3, // Nombre de tentatives
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) // Backoff exponentiel
            );
    }

    public async Task DownloadSymbols()
    {
        List<SymbolData> symbols;
        List<TickerData> tickers;
        List<SymbolTickerData> joinedData;

        // Tente de télécharger les symboles avec résilience
        using (_metricService.StartTracking("Download-symbols"))
        {
            try
            {
                symbols = await _retryPolicy.ExecuteAsync(async () =>
                {
                    return await _exchange.GetSymbolsAsync();
                });

                if (symbols == null)
                {
                    _metricService.TrackFailure("Download-symbols");
                    return;
                }

                _metricService.TrackSuccess("Download-symbols"); // Suivi du succès
            }
            catch (Exception ex)
            {
                _metricService.TrackFailure("Download-symbols", ex); // Suivi de l'échec uniquement à la dernière tentative
                throw;
            }
        }

        // Tente de récupérer les tickers avec résilience
        using (_metricService.StartTracking("Download-tickers"))
        {
            try
            {
                tickers = await _retryPolicy.ExecuteAsync(async () =>
                {
                    return await _exchange.GetCurrentTickersAsync();
                });

                if (tickers == null)
                {
                    _metricService.TrackFailure("Download-tickers");
                    return;
                }

                _metricService.TrackSuccess("Download-tickers"); // Suivi du succès
            }
            catch (Exception ex)
            {
                _metricService.TrackFailure("Download-tickers", ex); // Suivi de l'échec uniquement à la dernière tentative
                throw;
            }
        }


        // Joindre les données des symboles et des tickers
        joinedData = symbols.Join(
            tickers,
            symbol => symbol.Symbol,
            ticker => ticker.Symbol,
            (symbol, ticker) => new SymbolTickerData
            {
                Symbol = symbol,
                Ticker = ticker,
                PublishedDate = DateTime.UtcNow,
                PriceChangePercent = ticker.PriceChangePercent.GetValueOrDefault(),
                Volume = ticker.Volume.GetValueOrDefault()
            }).OrderByDescending(s => s.PriceChangePercent).ThenBy(s => s.Volume).ToList();

        var data = joinedData.Take(_appSettings.LimitSymbolsMarket);

        // Publier les symboles
        bool success = false;

        using (_metricService.StartTracking("publish-symbols"))
        {
            try
            {
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    await _saga.PublishSymbols(data);
                    success = true;
                });

                if (!success)
                    _metricService.TrackFailure("publish-symbols");
            }
            catch (Exception ex)
            {
                _metricService.TrackFailure("publish-symbols", ex);
                throw;
            }

            if (success)
                _metricService.TrackSuccess("publish-symbols");
        }
    }
}
