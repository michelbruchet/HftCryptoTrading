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

public class AnalyseDownloadedSymbolCommand
{
    private readonly IDistributedCache _cacheService;
    private readonly ILogger<AnalyseDownloadedSymbolCommand> _logger;
    private readonly IMetricService _metricService;
    private readonly IMarketWatcherSaga _saga;
    private readonly ISymbolAnalysisHelper _helper;

    public AnalyseDownloadedSymbolCommand(
        IDistributedCache cacheService,
        ILogger<AnalyseDownloadedSymbolCommand> logger,
        IMarketWatcherSaga saga,
        IMetricService metricService,
        ISymbolAnalysisHelper helper
        )
    {
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metricService = metricService ?? throw new ArgumentNullException(nameof(metricService));
        _saga = saga ?? throw new ArgumentNullException(nameof(saga));
        _helper = helper ?? throw new ArgumentNullException(nameof(helper));
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
                var tasks = @event.Data.Select(async ticker =>
                {
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

            await  _helper.PublishAnalysisResults(@event.ExchangeName, abnormalVolumes, 
                abnormalPrices, abnormalSpreads, validSymbols);
        }
    }
}