using HftCryptoTrading.Exchanges.Core.Exchange;
using HftCryptoTrading.Shared.Metrics;
using HftCryptoTrading.Shared.Models;
using Microsoft.Extensions.Caching.Distributed;
using MessagePack;
using Microsoft.Extensions.Logging;
using HftCryptoTrading.Shared.Saga;
using System.Diagnostics.CodeAnalysis;
using HftCryptoTrading.Shared.Events;
using Polly;

namespace HftCryptoTrading.Services.Processes;

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MessagePack;
using Polly;
using Microsoft.CodeAnalysis;
using System.Collections.Concurrent;
using HftCryptoTrading.Services.Commands;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.Extensions.Options;

public class AnalyseDownloadedSymbolCommand
{
    private readonly IDistributedCache _cacheService;
    private readonly ILogger<AnalyseDownloadedSymbolCommand> _logger;
    private readonly IMetricService _metricService;
    private readonly IMarketWatcherSaga _saga;
    private readonly ISymbolAnalysisHelper _helper;
    private readonly IExchangeProviderFactory _exchangeProviderFactory;
    private readonly AppSettings _appSettings;
    private readonly ILoggerFactory _loggerFactory;

    public AnalyseDownloadedSymbolCommand(
        IDistributedCache cacheService,
        ILoggerFactory loggerfactory,
        IMarketWatcherSaga saga,
        IMetricService metricService,
        ISymbolAnalysisHelper helper,
        IOptions<AppSettings> appSettings,
        IExchangeProviderFactory exchangeProviderFactory
        )
    {
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = loggerfactory?.CreateLogger<AnalyseDownloadedSymbolCommand>() ?? throw new ArgumentNullException(nameof(loggerfactory));
        _metricService = metricService ?? throw new ArgumentNullException(nameof(metricService));
        _saga = saga ?? throw new ArgumentNullException(nameof(saga));
        _helper = helper ?? throw new ArgumentNullException(nameof(helper));
        _exchangeProviderFactory = exchangeProviderFactory ?? throw new ArgumentNullException(nameof(exchangeProviderFactory));
        _appSettings = appSettings.Value;
        _loggerFactory = loggerfactory;
    }

    public async Task RunAsync(PublishedDownloadedSymbolsEvent @event)
    {
        using (_metricService.StartTracking("AnalyseDownloadedSymbol"))
        {
            ArgumentNullException.ThrowIfNull(@event);
            var abnormalVolumes = new ConcurrentDictionary<string, SymbolTickerData>();
            var abnormalPrices = new ConcurrentDictionary<string, SymbolTickerData>();
            var abnormalSpreads = new ConcurrentDictionary<string, SymbolTickerData>();
            var validSymbols = new ConcurrentDictionary<string, SymbolTickerData>();

            try
            {
                var symbols = @event.Data.Select(s=>s.Symbol.Name).ToList();
                var exchangeClient = _exchangeProviderFactory.GetExchange(@event.ExchangeName, _appSettings, _loggerFactory);
                var bookPrices = await exchangeClient.GetBookPricesAsync(symbols);

                var tasks = @event.Data.Select(async ticker =>
                {

                    ticker.BookPrice = bookPrices.FirstOrDefault(bp => bp.Symbol == ticker.Symbol.Name);

                    var isAbnormalVolume = await _helper.RetryPolicyWrapper(
                        async () => await _helper.IsAbnormalVolume(ticker));

                    var isAbnormalSpread = await _helper.RetryPolicyWrapper(
                        async () => await _helper.IsAbnormalSpreadBidAsk(ticker));

                    var isAbnormalPrice = await _helper.RetryPolicyWrapper(
                        async () => await _helper.IsAbnormalPrice(ticker));

                    if (isAbnormalVolume) abnormalVolumes.TryAdd(ticker.Symbol.Name, ticker);
                    if (isAbnormalSpread) abnormalSpreads.TryAdd(ticker.Symbol.Name, ticker);
                    if (isAbnormalPrice) abnormalPrices.TryAdd(ticker.Symbol.Name, ticker);
                    if (!isAbnormalVolume && !isAbnormalSpread && !isAbnormalPrice) validSymbols.TryAdd(ticker.Symbol.Name, ticker);
                });

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to analyze symbols: {ex.Message}");
                throw;
            }

            await  PublishAnalysisResults(@event.ExchangeName, abnormalVolumes, 
                abnormalPrices, abnormalSpreads, validSymbols);
        }
    }

    async Task PublishAnalysisResults(
    string exchangeName,
    ConcurrentDictionary<string, SymbolTickerData> abnormalVolumes,
    ConcurrentDictionary<string, SymbolTickerData> abnormalPrices,
    ConcurrentDictionary<string, SymbolTickerData> abnormalSpreads,
    ConcurrentDictionary<string, SymbolTickerData> validSymbols)
    {
        if (!abnormalVolumes.IsEmpty)
            await _saga.PublishDownloadedSymbolAnalysedVolumeFailed(exchangeName, abnormalVolumes.Values.ToList());

        if (!abnormalSpreads.IsEmpty)
            await _saga.PublishDownloadedSymbolAnalysedSpreadBidAskFailed(exchangeName, abnormalSpreads.Values.ToList());

        if (!abnormalPrices.IsEmpty)
            await _saga.PublishDownloadedSymbolAnalysedPriceFailed(exchangeName, abnormalPrices.Values.ToList());

        if (!validSymbols.IsEmpty)
            await _saga.PublishDownloadedSymbolAnalysedSuccessFully(exchangeName, validSymbols.Values.ToList());
    }

}