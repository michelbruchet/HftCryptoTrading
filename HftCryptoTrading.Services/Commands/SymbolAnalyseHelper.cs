using HftCryptoTrading.Shared.Metrics;
using HftCryptoTrading.Shared.Models;
using HftCryptoTrading.Shared.Saga;
using MessagePack;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace HftCryptoTrading.Services.Commands;

public class SymbolAnalysisHelper(IDistributedCache cacheService, ILogger logger, IMetricService metricService, IMarketWatcherSaga saga) 
    : ISymbolAnalysisHelper
{
    private const string VolumeHistoryKeyPrefix = "VolumeHistory_";
    private const string SpreadHistoryKeyPrefix = "SpreadHistory_";
    private const string PriceHistoryKeyPrefix = "PriceHistory_";

    public async Task SetToCacheAsync<T>(string key, T value)
    {
        try
        {
            byte[] serializedData = MessagePackSerializer.Serialize(value);
            await cacheService.SetAsync(key, serializedData);
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to cache data for key {key}: {ex.Message}");
        }
    }

    public async Task<T> GetFromCacheAsync<T>(string key)
    {
        try
        {
            byte[] cachedData = await cacheService.GetAsync(key);
            return cachedData == null ? default : MessagePackSerializer.Deserialize<T>(cachedData);
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to retrieve cached data for key {key}: {ex.Message}");
            return default;
        }
    }

    public async Task<T> RetryPolicyWrapper<T>(Func<Task<T>> action, int maxRetries = 3)
    {
        int retryCount = 0;
        while (true)
        {
            try
            {
                return await action();
            }
            catch (Exception ex) when (retryCount < maxRetries)
            {
                retryCount++;
                logger.LogWarning($"Retrying ({retryCount}/{maxRetries}) after failure: {ex.Message}");
            }
        }
    }

    public async Task<bool> IsAbnormalPrice(SymbolTickerData symbol)
    {
        using (metricService.StartTracking("IsAbnormalPrice"))
        {
            ArgumentNullException.ThrowIfNull(symbol);
            string cacheKey = $"{PriceHistoryKeyPrefix}{symbol.Symbol.Name}";
            decimal previousPrice = await GetFromCacheAsync<decimal>(cacheKey);

            bool isAbnormalPrice = previousPrice != default && (
                    symbol.Ticker.LastPrice.GetValueOrDefault() > previousPrice * 1.5m ||
                    symbol.Ticker.LastPrice.GetValueOrDefault() < previousPrice * 0.5m);
    
            if(!isAbnormalPrice)
                await SetToCacheAsync(cacheKey, symbol.Ticker.LastPrice.GetValueOrDefault());
            
            return isAbnormalPrice;
        }
    }

    public async Task<bool> IsAbnormalVolume(SymbolTickerData symbol)
    {
        using (metricService.StartTracking("IsAbnormalVolume"))
        {
            ArgumentNullException.ThrowIfNull(symbol);
            string cacheKey = $"{VolumeHistoryKeyPrefix}{symbol.Symbol.Name}";
            decimal previousVolume = await GetFromCacheAsync<decimal>(cacheKey);

            bool isAbnormalVolume = previousVolume != default && (
                    symbol.Ticker.Volume > previousVolume * 1.5m ||
                    symbol.Ticker.Volume < previousVolume * 0.5m);

            if(!isAbnormalVolume)
                await SetToCacheAsync(cacheKey, symbol.Ticker.Volume);
    
            return isAbnormalVolume;
        }
    }

    public async Task<bool> IsAbnormalSpreadBidAsk(SymbolTickerData symbol)
    {
        using (metricService.StartTracking("IsAbnormalSpreadBidAsk"))
        {
            ArgumentNullException.ThrowIfNull(symbol);
            string cacheKey = $"{SpreadHistoryKeyPrefix}{symbol.Symbol.Name}";
            decimal previousSpread = await GetFromCacheAsync<decimal>(cacheKey);
            decimal currentSpread = Math.Abs(symbol.BookPrice.BestAskPrice - symbol.BookPrice.BestBidPrice);
            
            bool isAbnormalSpread = previousSpread != default && (
                    currentSpread > previousSpread * 1.5m ||
                    currentSpread < previousSpread * 0.5m);

            if(!isAbnormalSpread)
                await SetToCacheAsync(cacheKey, currentSpread);
    
            return isAbnormalSpread;
        }
    }

    public async Task PublishAnalysisResults(
        string exchangeName,
        ConcurrentDictionary<string, SymbolTickerData> abnormalVolumes,
        ConcurrentDictionary<string, SymbolTickerData> abnormalPrices,
        ConcurrentDictionary<string, SymbolTickerData> abnormalSpreads,
        ConcurrentDictionary<string, SymbolTickerData> validSymbols)
    {
        if (!abnormalVolumes.IsEmpty)
            await saga.PublishDownloadedSymbolAnalysedVolumeFailed(exchangeName, abnormalVolumes.Values.ToList());

        if (!abnormalSpreads.IsEmpty)
            await saga.PublishDownloadedSymbolAnalysedSpreadBidAskFailed(exchangeName, abnormalSpreads.Values.ToList());

        if (!abnormalPrices.IsEmpty)
            await saga.PublishDownloadedSymbolAnalysedPriceFailed(exchangeName, abnormalPrices.Values.ToList());

        if (!validSymbols.IsEmpty)
            await saga.PublishDownloadedSymbolAnalysedSuccessFully(exchangeName, validSymbols.Values.ToList());
    }
}
