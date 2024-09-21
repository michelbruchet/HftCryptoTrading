using Binance.Net.Clients;
using HftCryptoTrading.Exchanges.Core.Exchange;
using HftCryptoTrading.Shared;
using HftCryptoTrading.Shared.Models;
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
    private ILogger<BinanceDownloadMarketClient> _logger;

    public string ExchangeName => "Binance";

    public BinanceDownloadMarketClient(AppSettings appSettings, ILogger<BinanceDownloadMarketClient> logger)
    {
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

        _binanceExchangeClient = new BinanceRestClient();
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
}