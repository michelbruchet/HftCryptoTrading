using HftCryptoTrading.Shared.Metrics;
using HftCryptoTrading.Shared.Models;
using HftCryptoTrading.Shared.Saga;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HftCryptoTrading.Services.Commands;

public class AnalysePriceChangeDetectedCommand
{
    private readonly IDistributedCache _cacheService;
    private readonly ILogger<AnalysePriceChangeDetectedCommand> _logger;
    private readonly IMetricService _metricService;
    private readonly IMarketWatcherSaga _saga;
    private readonly ISymbolAnalysisHelper _symbolAnalysisHelper;

    public AnalysePriceChangeDetectedCommand(
        IDistributedCache cacheService,
        ILogger<AnalysePriceChangeDetectedCommand> logger,
        IMetricService metricService,
        IMarketWatcherSaga saga,
        ISymbolAnalysisHelper symbolAnalysisHelper)
    {
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metricService = metricService ?? throw new ArgumentNullException(nameof(metricService));
        _saga = saga ?? throw new ArgumentNullException(nameof(saga));
        _symbolAnalysisHelper = symbolAnalysisHelper;
    }

    public async Task RunAsync(SymbolTickerData symbol)
    {
        using (_metricService.StartTracking("AnalysePriceChangeDetected"))
        {
            ArgumentNullException.ThrowIfNull(symbol);

            bool isAbnormalVolume = false;
            bool isAbnormalPrice = false;
            bool isAbnormalSpread = false;

            try
            {
                // Analyse des changements de volume
                isAbnormalVolume = await _symbolAnalysisHelper.RetryPolicyWrapper(
                    async () => await _symbolAnalysisHelper.IsAbnormalVolume(symbol));

                // Analyse des changements de spread bid-ask
                isAbnormalSpread = await _symbolAnalysisHelper.RetryPolicyWrapper(
                    async () => await _symbolAnalysisHelper.IsAbnormalSpreadBidAsk(symbol));

                // Analyse des changements de prix
                isAbnormalPrice = await _symbolAnalysisHelper.RetryPolicyWrapper(
                    async () => await _symbolAnalysisHelper.IsAbnormalPrice(symbol));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to analyze symbol {symbol.Symbol.Name}: {ex.Message}");
                throw;
            }

            // Publication des résultats d'analyse pour traitement par d'autres parties du système
            await PublishAnalysisResults(symbol, isAbnormalVolume, isAbnormalPrice, isAbnormalSpread);
        }
    }

    private async Task PublishAnalysisResults(
        SymbolTickerData symbol,
        bool isAbnormalVolume,
        bool isAbnormalPrice,
        bool isAbnormalSpread)
    {
        // Log des résultats pour chaque cas
        if (isAbnormalVolume)
        {
            _logger.LogInformation($"Abnormal volume detected for {symbol.Symbol.Name}");
            await _saga.PublishStreamedSymbolAnalysedVolumeFailed(symbol.Exchange, symbol);
        }

        if (isAbnormalPrice)
        {
            _logger.LogInformation($"Abnormal price detected for {symbol.Symbol.Name}");
            await _saga.PublishStreamedSymbolAnalysedPriceFailed(symbol.Exchange, symbol);
        }

        if (isAbnormalSpread)
        {
            _logger.LogInformation($"Abnormal bid-ask spread detected for {symbol.Symbol.Name}");
            await _saga.PublishStreamedSymbolAnalysedSpreadBidAskFailed(symbol.Exchange, symbol);
        }

        if (!isAbnormalPrice && !isAbnormalVolume && !isAbnormalSpread)
        {
            _logger.LogInformation($"Valid symbol price changed detected for {symbol.Symbol.Name}");
            await _saga.PublishStreamedSymbolAnalysedSuccessFully(symbol.Exchange, symbol);
        }
    }
}