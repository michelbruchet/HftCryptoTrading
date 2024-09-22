using Binance.Net.Clients;
using Binance.Net.Interfaces.Clients;
using CryptoExchange.Net.Objects.Sockets;
using HftCryptoTrading.Exchanges.Core.Events;
using HftCryptoTrading.Exchanges.Core.Exchange;
using HftCryptoTrading.Shared;
using HftCryptoTrading.Shared.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HftCryptoTrading.Exchanges.BinanceExchange;

public class BinanceDownloadMarketClient : IExchangeClient
{
    private BinanceRestClient _binanceExchangeClient;
    private BinanceSocketClient _binanceSocketExchangeClient;
    private ILogger<BinanceDownloadMarketClient> _logger;
    private IMediator _mediator;
    private List<UpdateSubscription> _activeSubscriptions = new();

    public string ExchangeName => "Binance";

    public BinanceDownloadMarketClient(AppSettings appSettings, ILogger<BinanceDownloadMarketClient> logger, IMediator mediator)
    {
        _mediator = mediator;

        BinanceRestClient.SetDefaultOptions(options =>
        {
            options.ApiCredentials = new CryptoExchange.Net.Authentication.ApiCredentials(appSettings.Binance.ApiKey, appSettings.Binance.ApiSecret);
            options.CachingEnabled = true;
            options.Environment = appSettings.Binance.IsBackTest ?
                Binance.Net.BinanceEnvironment.Testnet : Binance.Net.BinanceEnvironment.Live;
            options.AutoTimestamp = true;
            options.CachingMaxAge = TimeSpan.FromMinutes(15);
            options.RateLimiterEnabled = true;
        });

        BinanceSocketClient.SetDefaultOptions(options =>
        {
            options.ApiCredentials = new CryptoExchange.Net.Authentication.ApiCredentials(appSettings.Binance.ApiKey, appSettings.Binance.ApiSecret);
            options.Environment = appSettings.Binance.IsBackTest ?
                Binance.Net.BinanceEnvironment.Testnet : Binance.Net.BinanceEnvironment.Live;
            options.RateLimiterEnabled = true;
        });

        _binanceExchangeClient = new BinanceRestClient();
        _binanceSocketExchangeClient = new BinanceSocketClient();
        _logger = logger;
    }

    public void Dispose()
    {
        _binanceExchangeClient.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        _binanceExchangeClient.Dispose();
        return ValueTask.CompletedTask;
    }

    public async Task<List<SymbolData>> GetSymbolsAsync()
    {
        var result = await _binanceExchangeClient.SpotApi.ExchangeData.GetExchangeInfoAsync();

        if (!result.Success)
            throw new Exception($"Error retrieving symbols: {result.Error?.Message}");

        // Extract the symbols from the exchange information
        return result.Data.Symbols.Select(s => new SymbolData(s.Name)
        {
            AllowTrailingStop = s.AllowTrailingStop,
            BaseAsset = s.BaseAsset,
            BaseAssetPrecision = s.BaseAssetPrecision,
            BaseFeePrecision = s.BaseFeePrecision,
            CancelReplaceAllowed = s.CancelReplaceAllowed,
            IcebergAllowed = s.IcebergAllowed,
            IsMarginTradingAllowed = s.IsMarginTradingAllowed,
            IsSpotTradingAllowed = s.IsSpotTradingAllowed,
            OCOAllowed = s.OCOAllowed,
            OrderTypes = (s.OrderTypes.Any() ? s.OrderTypes.Select(o=>Enum.GetName(o) ?? "unknow").ToArray() : []),
            OTOAllowed = s.OTOAllowed,
            QuoteAsset = s.QuoteAsset,
            QuoteAssetPrecision = s.QuoteAssetPrecision,
            QuoteFeePrecision=s.QuoteFeePrecision,
            QuoteOrderQuantityMarketAllowed=s.QuoteOrderQuantityMarketAllowed,
            Status = Enum.GetName(s.Status) ?? "unknow",
            MarketLotSizeFilter = s.MarketLotSizeFilter != null ? 
                new(s.MarketLotSizeFilter.StepSize, s.MarketLotSizeFilter.MinQuantity, s.MarketLotSizeFilter.MaxQuantity) : null,
            IceBergPartsFilter = s.IceBergPartsFilter != null ? new(s.IceBergPartsFilter.Limit) : null,
            MaxAlgorithmicOrdersFilter = s.MaxAlgorithmicOrdersFilter != null ?
                new(s.MaxAlgorithmicOrdersFilter.MaxNumberAlgorithmicOrders) : null,
            MaxOrdersFilter = s.MaxOrdersFilter != null ? new(s.MaxOrdersFilter.MaxNumberOrders) : null,
            MaxPositionFilter = s.MaxPositionFilter != null ? new(s.MaxPositionFilter.MaxPosition) : null,
            MinNotionalFilter = s.MinNotionalFilter != null ? 
                new(s.MinNotionalFilter.MinNotional, s.MinNotionalFilter.AveragePriceMinutes, s.MinNotionalFilter.ApplyToMarketOrders) : null,
            PriceFilter = s.PriceFilter != null ? new(s.PriceFilter.MinPrice, s.PriceFilter.MaxPrice, s.PriceFilter.TickSize) : null,
            PricePercentFilter = s.PricePercentFilter != null ? new(s.PricePercentFilter.AveragePriceMinutes, 
                s.PricePercentFilter.MultiplierUp, s.PricePercentFilter.MultiplierDown, s.PricePercentFilter.MultiplierDecimal) : null,
            TrailingDeltaFilter = s.TrailingDeltaFilter != null ? new(s.TrailingDeltaFilter.MinTrailingBelowDelta, s.TrailingDeltaFilter.MaxTrailingBelowDelta,
                s.TrailingDeltaFilter.MinTrailingAboveDelta, s.TrailingDeltaFilter.MaxTrailingAboveDelta) : null
        }).ToList();
    }
    public async Task<List<TickerData>> GetCurrentTickersAsync()
    {
        // Retrieve current ticker prices from Binance
        var tickersResult = await _binanceExchangeClient.SpotApi.CommonSpotClient.GetTickersAsync();

        // Check if the request was successful
        if (!tickersResult.Success)
        {
            // Log the error and throw an exception if the request failed
            _logger.LogError($"Failed to retrieve current tickers: {tickersResult.Error}");
            throw new ApplicationException("Failed to retrieve current tickers");
        }

        // Map the retrieved ticker data to TickerData model
        var tickerDataList = tickersResult.Data.Select(t => new TickerData(t.Symbol)
        {
            Price = t.LastPrice,
            Volume = t.Volume,
            Price24H = t.Price24H,
            HighPrice = t.HighPrice,
            LowPrice = t.LowPrice,
            PriceChange = t.Price24H > 0 ? t.LastPrice - t.Price24H : 0,
            PriceChangePercent = t.Price24H > 0 ? ((t.LastPrice - t.Price24H) / t.Price24H) * 100 : 0,
        }).ToList();

        // Return the list of mapped ticker data
        return tickerDataList;
    }

    public async Task<List<BookPriceData>> GetBookPricesAsync(IEnumerable<string> symboles)
    {
        var getOrderBook = await _binanceExchangeClient.SpotApi.ExchangeData.GetBookPricesAsync(symboles);

        if (!getOrderBook.Success)
        {
            _logger.LogError($"Failed to retreive book data error:{getOrderBook.Error}");
            throw new ApplicationException("Failed to retreive book data");
        }

        //Map the retrieved book prices to BookPriceData
        return getOrderBook.Data.Select(p=>new BookPriceData(p.Symbol)
        {
            BestBidPrice= p.BestBidPrice,
            BestAskPrice = p.BestAskPrice,
            BestAskQuantity = p.BestAskQuantity,
            BestBidQuantity = p.BestBidQuantity
        }).ToList();
    }

    public async Task<List<KlineData>> GetHistoricalKlinesAsync(string symbol, TimeSpan period, DateTime startTime, DateTime endTime)
    {
        var klinesResult = await _binanceExchangeClient.SpotApi.CommonSpotClient.GetKlinesAsync(symbol, period, startTime, endTime);

        // Check if the result is successful
        if (!klinesResult.Success)
        {
            throw new Exception($"Error fetching klines: {klinesResult.Error?.Message}");
        }

        // Map the result to KlineData model
        var klinesData = klinesResult.Data.Select(k => new KlineData(symbol)
        {
            OpenTime = k.OpenTime,
            OpenPrice = k.OpenPrice,
            HighPrice = k.HighPrice,
            LowPrice = k.LowPrice,
            ClosePrice = k.ClosePrice,
            Volume = k.Volume
        });

        return klinesData.ToList();
    }

    public async Task RegisterPriceChangeHandlerAsync(AnalyseMarketDoneSuccessFullyEvent notification)
    {
        var symboles = notification.ValidSymbols.Select(symbol => symbol.Symbol.Symbol).ToList();

        // Cancel any active subscriptions before creating new ones
        await CancelActiveSubscriptions();

        // Clear the list of active subscriptions since we are unsubscribing
        _activeSubscriptions.Clear();

        var subscriptionResult = await _binanceSocketExchangeClient.SpotApi.ExchangeData.SubscribeToTickerUpdatesAsync(symboles, async data =>
        {
            // When Binance sends a price update, publish the event via MediatR
            var priceChangeNotification = new PriceChangeNotification
            {
                Symbol = data.Data.Symbol,
                ExchangeName = ExchangeName,
                CurrentPrice = data.Data.LastPrice,
                OpenPrice = data.Data.OpenPrice,
                HighPrice = data.Data.HighPrice,
                LowPrice = data.Data.LowPrice,
                LastQuantity = data.Data.LastQuantity,
                BestBidQuantity = data.Data.BestBidQuantity,
                BestAskQuantity = data.Data.BestAskQuantity,
                BestAskPrice = data.Data.BestAskPrice,
                BestBidPrice = data.Data.BestBidPrice,
                LastPrice = data.Data.LastPrice,
                CloseTime = data.Data.CloseTime,
                OpenTime = data.Data.OpenTime,
                PrevDayClosePrice = data.Data.PrevDayClosePrice,
                PriceChange = data.Data.PriceChange,
                PriceChangePercent = data.Data.PriceChangePercent,
                QuoteVolume = data.Data.QuoteVolume,
                Volume = data.Data.Volume,
                WeightedAveragePrice = data.Data.WeightedAveragePrice,
                TotalTrades = data.Data.TotalTrades
            };

            await _mediator.Publish(priceChangeNotification);
        });

        if (subscriptionResult.Success)
        {
            // Add the subscription to the active list
            _activeSubscriptions.Add(subscriptionResult.Data);
        }
        else
        {
            // Handle subscription failure (log or throw an exception if necessary)
            Console.WriteLine($"Failed to subscribe to {symboles}: {subscriptionResult.Error?.Message}");
        }
    }

    private async Task CancelActiveSubscriptions()
    {
        await _binanceSocketExchangeClient.UnsubscribeAllAsync();
    }
}