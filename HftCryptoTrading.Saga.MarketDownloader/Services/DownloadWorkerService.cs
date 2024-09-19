using HftCryptoTrading.Exchanges.Core.Exchange;
using HftCryptoTrading.Shared.Metrics;
using HftCryptoTrading.Shared;
using HftCryptoTrading.Shared.Models;
using HftCryptoTrading.Saga.MarketDownloader.Workers;

namespace HftCryptoTrading.Saga.MarketDownloader.Services;

public class DownloadWorkerService(IExchangeClient exchange, IMetricService metricService,  MarketDownloaderSaga saga, AppSettings appSettings)
{
    public async Task DownloadSymbols()
    {
        List<SymbolData> symbols;
        List<TickerData> tickers;
        List<SymbolTickerData> joinedData;

        using (metricService.StartTracking("Download-symbols"))
        {
            symbols = await exchange.GetSymbolsAsync();
            metricService.TrackSuccess("Download-symbols");
        }

        using (metricService.StartTracking("Get-current-Tickers"))
        {
            tickers = await exchange.GetCurrentTickersAsync();
            metricService.TrackSuccess("Get-current-Tickers");
        }

        using (metricService.StartTracking("Join-Symbols-Tickers"))
        {
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
        }

        var data = joinedData.Take(appSettings.LimitSymbolsMarket);

        using (metricService.StartTracking("publish-symbols"))
        {
            await saga.PublishSymbols(data);
        }
    }
}