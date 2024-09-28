using HftCryptoTrading.Exchanges.Core.Events;
using HftCryptoTrading.Exchanges.Core.Exchange;
using HftCryptoTrading.Saga.MarketDownloader.Workers;
using HftCryptoTrading.Shared.Metrics;
using HftCryptoTrading.Shared.Models;
using Microsoft.Extensions.Caching.Distributed;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using MessagePack;
using Microsoft.Extensions.Logging;

namespace HftCryptoTrading.Saga.MarketDownloader.Processes;

public class AnalyseWorkerProcess
{
    private readonly IMetricService _metricService;
    private readonly IMarketDownloaderSaga _marketDownloaderSaga;
    private readonly ILogger _logger;
    private readonly ExchangeProviderFactory _exchanges;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IDistributedCache _distributedCache;

    private const string PriceHistoryKeyPrefix = "priceHistory_";
    private const string VolumeHistoryKeyPrefix = "volumeHistory_";
    private const string SpreadHistoryKeyPrefix = "spreadHistory_";
    private const string symbolKeyPrefix = "symbols";

    public AnalyseWorkerProcess(IMetricService metricService,
        IMarketDownloaderSaga saga,
        ILoggerFactory loggerFactory,
        ExchangeProviderFactory exchanges,
        IDistributedCache distributedCache)
    {
        _metricService = metricService;
        _marketDownloaderSaga = saga;
        _exchanges = exchanges;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<AnalyseWorkerProcess>();
        _distributedCache = distributedCache;
    }

    // Analyze market data
    public async Task AnalyseMarket(NewSymbolTickerDataEvent eventData, AppSettings appSettings)
    {
        using (_metricService.StartTracking("AnalyseMarket"))
        {
            var validSymbols = new List<SymbolTickerData>();
            var abnormalPriceSymbols = new List<SymbolTickerData>();
            var abnormalVolumeSymbols = new List<SymbolTickerData>();
            var abnormalSpreadSymbols = new List<SymbolTickerData>();

            try
            {
                var exchange = _exchanges.GetExchange(eventData.ExchangeName, appSettings, _loggerFactory);
                var bookPrices = await exchange.GetBookPricesAsync(eventData.Data.Select(e => e.Symbol.Symbol).Distinct().ToList());

                foreach (SymbolTickerData symbol in eventData.Data)
                {
                    var bookPrice = bookPrices.FirstOrDefault(b => b.Symbol == symbol.Symbol.Symbol);
                    if (bookPrice == null) continue;

                    symbol.BookPrice = bookPrice;

                    bool abnormalPriceSymbol = await IsAbnormalPrice(symbol);
                    bool abnormalVolumeSymbol = await IsAbnormalVolume(symbol);
                    bool abnormalSpreadSymbol = await IsAbnormalSpread(symbol);

                    symbol.BookPrice = bookPrice;

                    // Check abnormal price, volume, and spread
                    if (abnormalPriceSymbol)
                    {
                        abnormalPriceSymbols.Add(symbol);
                    }

                    if (abnormalVolumeSymbol)
                    {
                        abnormalVolumeSymbols.Add(symbol);
                    }

                    if (abnormalSpreadSymbol)
                    {
                        abnormalSpreadSymbols.Add(symbol);
                    }

                    if (!abnormalPriceSymbol && !abnormalVolumeSymbol && !abnormalSpreadSymbol)
                    {
                        validSymbols.Add(symbol);
                    }
                }

                // Publish analysis result
                if (validSymbols.Any())
                {
                    await SetToCacheAsync(symbolKeyPrefix, validSymbols.Select(e => e.Symbol).Distinct(new CompareSymbolInCache()).ToList());
                    await _marketDownloaderSaga.PublishAnalyseMarketDoneSuccessFully(exchange.ExchangeName, validSymbols);
                }
                if (abnormalPriceSymbols.Any())
                {
                    await _marketDownloaderSaga.PublishAbnormalPriceSymbols(exchange.ExchangeName, abnormalPriceSymbols);
                }
                if (abnormalVolumeSymbols.Any())
                {
                    await _marketDownloaderSaga.PublishAbnormalVolumeSymbols(exchange.ExchangeName, abnormalVolumeSymbols);
                }
                if (abnormalSpreadSymbols.Any())
                {
                    await _marketDownloaderSaga.PublishAbnormalSpreadSymbols(exchange.ExchangeName, abnormalSpreadSymbols);
                }

                _metricService.TrackSuccess("AnalyseMarket");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AnalyseMarket method");
                _metricService.TrackFailure("AnalyseMarket", ex);
                throw; // Re-throw the exception after logging
            }
        }
    }

    // Method to check if the price is abnormal
    private async Task<bool> IsAbnormalPrice(SymbolTickerData symbol)
    {
        using (_metricService.StartTracking("IsAbnormalPrice"))
        {
            if (symbol == null) throw new ArgumentNullException(nameof(symbol), "Symbol cannot be null.");
            if (symbol.Ticker == null) throw new ArgumentNullException(nameof(symbol.Ticker));
            if (symbol.Ticker.PriceChange == null) throw new ArgumentNullException(nameof(symbol.Ticker.PriceChange));

            string cacheKey = $"{PriceHistoryKeyPrefix}{symbol.Symbol.Symbol}";
            decimal previousPriceChange = await GetFromCacheAsync<decimal>(cacheKey);

            try
            {
                if (previousPriceChange != default)
                {
                    decimal priceChange = Math.Abs((symbol.Ticker.PriceChange.GetValueOrDefault() - previousPriceChange) / previousPriceChange);
                    if (priceChange > 0.05m) // Example: 5% movement
                    {
                        return true; // Abnormal price
                    }
                }

                await SetToCacheAsync(cacheKey, symbol.Ticker.PriceChange.GetValueOrDefault());
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in IsAbnormalPrice method");
                _metricService.TrackFailure("IsAbnormalPrice", ex);
                throw; // Re-throw the exception after logging
            }
        }
    }

    // Method to check if the volume is abnormal
    private async Task<bool> IsAbnormalVolume(SymbolTickerData symbol)
    {
        using (_metricService.StartTracking("IsAbnormalVolume"))
        {
            if (symbol == null) throw new ArgumentNullException(nameof(symbol), "Symbol cannot be null.");

            string cacheKey = $"{VolumeHistoryKeyPrefix}{symbol.Symbol.Symbol}";
            decimal previousVolume = await GetFromCacheAsync<decimal>(cacheKey);

            try
            {
                if (previousVolume != default)
                {
                    if (symbol.Volume > previousVolume * 2) // Example threshold
                    {
                        return true; // Abnormal volume
                    }
                }

                await SetToCacheAsync(cacheKey, symbol.Volume);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in IsAbnormalVolume method");
                _metricService.TrackFailure("IsAbnormalVolume", ex);
                throw; // Re-throw the exception after logging
            }
        }
    }

    // Method to check if the bid-ask spread is abnormal
    private async Task<bool> IsAbnormalSpread(SymbolTickerData symbol)
    {
        using (_metricService.StartTracking("IsAbnormalSpread"))
        {
            if (symbol == null) throw new ArgumentNullException(nameof(symbol), "Symbol cannot be null.");
            
            if (symbol.BookPrice.BestBidPrice <= 0 || symbol.BookPrice.BestAskPrice <= 0)
            {
                return true;
            }

            decimal spread = symbol.BookPrice.BestAskPrice - symbol.BookPrice.BestBidPrice;
            string cacheKey = $"{SpreadHistoryKeyPrefix}{symbol.Symbol.Symbol}";
            decimal previousSpread = await GetFromCacheAsync<decimal>(cacheKey);

            try
            {
                if (previousSpread != default)
                {
                    if (spread > previousSpread * 1.5m) // Example: spread increase threshold
                    {
                        return true; // Abnormal spread
                    }
                }

                await SetToCacheAsync(cacheKey, spread);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in IsAbnormalSpread method");
                _metricService.TrackFailure("IsAbnormalSpread", ex);
                throw; // Re-throw the exception after logging
            }
        }
    }

    // Helper method to get data from cache and deserialize with MessagePack
    private async Task<T> GetFromCacheAsync<T>(string cacheKey)
    {
        using (_metricService.StartTracking("GetFromCacheAsync"))
        {
            byte[] cachedData = await _distributedCache.GetAsync(cacheKey);
            if (cachedData != null)
            {
                return MessagePackSerializer.Deserialize<T>(cachedData);
            }
            return default;
        }
    }

    // Helper method to serialize data with MessagePack and set it in the cache
    private async Task SetToCacheAsync<T>(string cacheKey, T data)
    {
        using (_metricService.StartTracking("SetToCacheAsync"))
        {
            try
            {
                byte[] serializedData = MessagePackSerializer.Serialize(data);
                await _distributedCache.SetAsync(cacheKey, serializedData);
            }
            catch(Exception ex)
            {
                _metricService.TrackFailure("SetToCacheAsync", ex);
            }
        }
    }

    // New method to analyze price change notifications
    public async Task AnalysePrice(PriceChangeNotification notification, AppSettings appSettings)
    {
        using (_metricService.StartTracking("AnalysePrice"))
        {
            ArgumentNullException.ThrowIfNull(notification);

            try
            {
                var symbol = await CreateSymbolTickerData(notification);

                bool abnormalPriceSymbol = await IsAbnormalPrice(symbol);
                bool abnormalVolumeSymbol = await IsAbnormalVolume(symbol);
                bool abnormalSpreadSymbol = await IsAbnormalSpread(symbol);

                if (abnormalPriceSymbol)
                {
                    await _marketDownloaderSaga.PublishAbnormalPriceSymbols(notification.ExchangeName, new(new[] { symbol }));
                }
                if (abnormalVolumeSymbol)
                {
                    await _marketDownloaderSaga.PublishAbnormalVolumeSymbols(notification.ExchangeName, new(new[] { symbol }));
                }
                if (abnormalSpreadSymbol)
                {
                    await _marketDownloaderSaga.PublishAbnormalSpreadSymbols(notification.ExchangeName, new(new[] { symbol }));
                }

                if (!abnormalPriceSymbol && !abnormalVolumeSymbol && !abnormalSpreadSymbol)
                {
                    await _marketDownloaderSaga.PublishAnalysePriceDoneSuccessFully(notification.ExchangeName, symbol);
                }

                _metricService.TrackSuccess("AnalysePrice");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AnalysePrice method");
                _metricService.TrackFailure("AnalysePrice", ex);
                throw; // Re-throw the exception after logging
            }
        }
    }

    // New method to create SymbolTickerData from notification
    private async Task<SymbolTickerData> CreateSymbolTickerData(PriceChangeNotification notification)
    {
        var symbols = await GetFromCacheAsync<List<SymbolData>>(symbolKeyPrefix);
        var symbol = symbols.FirstOrDefault(s => s.Symbol == notification.Symbol);

        // Populate other properties as needed
        return new SymbolTickerData(notification.ExchangeName)
        {
            Symbol = symbol,
            Ticker = new TickerData(symbol.Symbol)
            {
                Price = notification.LastPrice,
                Volume = notification.Volume,
                Price24H = notification.PrevDayClosePrice,
                HighPrice = notification.HighPrice,
                LowPrice = notification.LowPrice,
                PriceChange = notification.PriceChange,
                PriceChangePercent = notification.PriceChangePercent,
                Bid = notification.BestBidPrice,
                Ask = notification.BestAskPrice
            },
            Volume = notification.Volume,
            BookPrice = new BookPriceData(symbol.Symbol)
            {
                BestBidPrice = notification.BestBidPrice,
                BestAskPrice = notification.BestAskPrice,
                BestBidQuantity = notification.BestBidQuantity,
                BestAskQuantity = notification.BestAskQuantity
            }
        };
    }
}