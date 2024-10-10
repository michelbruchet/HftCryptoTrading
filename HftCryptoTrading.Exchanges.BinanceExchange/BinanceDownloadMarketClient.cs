using Binance.Net.Clients;
using Binance.Net.Objects.Models.Spot;
using Binance.Net.Objects.Models.Spot.Socket;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;
using HftCryptoTrading.Exchanges.Core.Exchange;
using HftCryptoTrading.Shared.Events;
using HftCryptoTrading.Shared.Models;
using MediatR;
using MessagePack;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace HftCryptoTrading.Exchanges.BinanceExchange;

public class BinanceDownloadMarketClient : IExchangeClient
{
    private BinanceRestClient _binanceExchangeClient;
    private BinanceSocketClient _binanceSocketExchangeClient;
    private ILogger<BinanceDownloadMarketClient> _logger;
    private IMediator _mediator;
    private List<UpdateSubscription> _activeSubscriptions = new();
    private Timer _keepAliveTimer;
    private IDistributedCache _distributedCache;

    public event EventHandler<OrderUpdateEvent> OnOrderUpdated;
    public event EventHandler<AccountPositionUpdateEvent> OnAccountPositionUpdated;
    public event EventHandler<AccountBalanceUpdateEvent> OnAccountBalanceUpdated;

    public string ExchangeName => "Binance";

    public BinanceDownloadMarketClient(AppSettings appSettings, 
        ILogger<BinanceDownloadMarketClient> logger, 
        IMediator mediator,
        IDistributedCache distributedCache
        )
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

        _distributedCache = distributedCache;
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
        var key = $"symbols_{ExchangeName}";

        var cachedSymbols = await _distributedCache.GetAsync(key);
        
        if(cachedSymbols != null )
        {
            var symbols = MessagePackSerializer.Deserialize<List<SymbolData>>(cachedSymbols);
            return symbols;
        }

        var result = await _binanceExchangeClient.SpotApi.ExchangeData.GetExchangeInfoAsync();

        if (!result.Success)
            throw new Exception($"Error retrieving symbols: {result.Error?.Message}");

        // Extract the symbols from the exchange information
        var data = result.Data.Symbols.Select(s => new SymbolData(s.Name)
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
            OrderTypes = (s.OrderTypes.Any() ? s.OrderTypes.Select(o => Enum.GetName(o) ?? "unknow").ToArray() : []),
            OTOAllowed = s.OTOAllowed,
            QuoteAsset = s.QuoteAsset,
            QuoteAssetPrecision = s.QuoteAssetPrecision,
            QuoteFeePrecision = s.QuoteFeePrecision,
            QuoteOrderQuantityMarketAllowed = s.QuoteOrderQuantityMarketAllowed,
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

        await _distributedCache.SetAsync(key, MessagePackSerializer.Serialize(data), new DistributedCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromHours(5)
        });

        return data;
    }
    public async Task<List<TickerData>> GetCurrentTickersAsync()
    {
        // Retrieve current ticker prices from Binance
        var key = $"tickers_{ExchangeName}";

        var cacheData = await _distributedCache.GetAsync(key);

        if(cacheData != null)
        {
            return MessagePackSerializer.Deserialize<List<TickerData>>(cacheData);
        }

        var tickersResult = await _binanceExchangeClient.SpotApi
            .SharedClient.GetSpotTickersAsync(new CryptoExchange.Net.SharedApis.GetTickersRequest
        {
            TradingMode = CryptoExchange.Net.SharedApis.TradingMode.Spot
        });

        // Check if the request was successful
        if (!tickersResult.Success)
        {
            // Log the error and throw an exception if the request failed
            _logger.LogError($"Failed to retrieve current tickers: {tickersResult.Error}");
            throw new ApplicationException("Failed to retrieve current tickers");
        }

        // Map the retrieved ticker data to TickerData model
        var tickerDataList = tickersResult.Data.Select(t => new TickerData(t.Symbol, ExchangeName)
        {
            LastPrice = t.LastPrice,
            Volume = t.Volume,
            HighPrice = t.HighPrice,
            LowPrice = t.LowPrice,
            ChangePercentage = t.ChangePercentage
        }).ToList();

        await _distributedCache.SetAsync(key, 
            MessagePackSerializer.Serialize(tickerDataList), 
            new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = new DateTimeOffset(DateTime.Today.AddDays(1)),
        });

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
        return getOrderBook.Data.Select(p => new BookPriceData(p.Symbol)
        {
            BestBidPrice = p.BestBidPrice,
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

    public async Task RegisterPriceChangeHandlerAsync(PublishSymbolAnalysedSuccessFullyEvent notification)
    {
        var symboles = notification.ValidSymbols.Select(symbolTicker => symbolTicker.Symbol.Name).ToList();

        // Cancel any active subscriptions before creating new ones
        await CancelActiveSubscriptions();

        // Clear the list of active subscriptions since we are unsubscribing
        _activeSubscriptions.Clear();

        var subscriptionResult = await _binanceSocketExchangeClient.SpotApi.ExchangeData.SubscribeToTickerUpdatesAsync(symboles, async data =>
        {
            // When Binance sends a price update, publish the event via MediatR
            var symbolTicker = notification?.ValidSymbols?.FirstOrDefault(s => s.Symbol.Name == data.Data.Symbol);

            if (symbolTicker != null)
            {
                var priceChangeNotification = new PriceChangeDetectedEvent(symbolTicker.Symbol, ExchangeName)
                {
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
            }
        });

        if (subscriptionResult.Success)
        {
            // Add the subscription to the active list
            _activeSubscriptions.Add(subscriptionResult.Data);

            subscriptionResult.Data.ConnectionClosed += async () =>
            {
                Console.WriteLine("Connection closed. Attempting to reconnect...");
                await subscriptionResult.Data.ReconnectAsync();
            };
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

    public async Task<List<OpenOrder>> GetOpenedOrders()
    {
        var openOrders = await _binanceExchangeClient.SpotApi
            .Trading.GetOpenOrdersAsync();

        return openOrders.Data.Select(o => new OpenOrder(o.Symbol, ExchangeName)
        {
            ClientOrderId = o.ClientOrderId,
            UpdateTime = o.UpdateTime,
            IsIsolated = o.IsIsolated,
            Status = (OrderStatus)o.Status,
            QuoteQuantityFilled = o.QuoteQuantityFilled,
            QuoteQuantity = o.QuoteQuantity,
            QuantityFilled = o.QuantityFilled,
            Quantity = o.Quantity,
            CreateTime = o.CreateTime,
            IcebergQuantity = o.IcebergQuantity,
            Id = o.Id,
            IsWorking = o.IsWorking,
            OrderListId = o.OrderListId,
            OriginalClientOrderId = o.OriginalClientOrderId,
            Price = o.Price,
            SelfTradePreventionMode = (int)o.SelfTradePreventionMode,
            Side = (int)o.Side,
            StopPrice = o.StopPrice,
            TimeInForce = (int)o.TimeInForce,
            Type = (int)o.Type,
            WorkingTime = o.WorkingTime
        }).ToList();
    }

    public async Task<PlaceOrderResult> PlaceMarketOrder(PlaceOrder placeOrder)
    {
        WebCallResult<BinancePlacedOrder> placeOrderResult = await _binanceExchangeClient.SpotApi.Trading.PlaceOrderAsync(
            symbol: placeOrder.Symbol,
            side: placeOrder.Strategy == Shared.Strategies.ActionStrategy.Short
                ? Binance.Net.Enums.OrderSide.Sell : Binance.Net.Enums.OrderSide.Buy,
            type: Binance.Net.Enums.SpotOrderType.Market,
            quantity: placeOrder.Quantity,
            newClientOrderId: placeOrder.ClientOrderId
            );

        return new PlaceOrderResult(placeOrderResult.Success,
            placeOrderResult.Success ? new OpenOrder(placeOrder.Symbol, ExchangeName)
            {

            } : null, !placeOrderResult.Success ? placeOrderResult.Error.Message : null);
    }

    public async Task TrackPlaceOrder()
    {
        var listenKeyResult = await _binanceExchangeClient.SpotApi.Account.StartUserStreamAsync();

        if (!listenKeyResult.Success)
        {
            Console.WriteLine("Failed to start user stream: " + listenKeyResult.Error);
            return;
        }

        string listenKey = listenKeyResult.Data;

        // Subscribe to user data updates (order updates)
        var subscriptionResult = await _binanceSocketExchangeClient.SpotApi.Account.SubscribeToUserDataUpdatesAsync(
            listenKey,
            onOrderUpdateMessage: (orderUpdate) => OnOrderUpdated?.Invoke(this, ParseOrderUpdate(orderUpdate)),
           onAccountPositionMessage: (positionUpdate) => OnAccountPositionUpdated?.Invoke(this, ParseAccountPositionUpdate(positionUpdate)),
            onAccountBalanceUpdate: (balanceUpdate) => OnAccountBalanceUpdated?.Invoke(this, ParseAccountBalanceUpdate(balanceUpdate))
        );

        if (!subscriptionResult.Success)
        {
            Console.WriteLine("Failed to subscribe to user data stream: " + subscriptionResult.Error);
            return;
        }

        subscriptionResult.Data.ConnectionClosed += async () =>
        {
            Console.WriteLine("Connection closed. Attempting to reconnect...");
            await subscriptionResult.Data.ReconnectAsync();
        };
    }

    private AccountBalanceUpdateEvent ParseAccountBalanceUpdate(DataEvent<BinanceStreamBalanceUpdate> balanceUpdate)
    {
        return new AccountBalanceUpdateEvent(ExchangeName, balanceUpdate.Symbol, new
                AccountBalanceUpdate(ExchangeName, balanceUpdate.Symbol)
        {
            Asset = balanceUpdate.Data.Asset,
            BalanceDelta = balanceUpdate.Data.BalanceDelta,
            ClearTime = balanceUpdate.Data.ClearTime,
            ListenKey= balanceUpdate.Data.ListenKey
        });
    }

    private AccountPositionUpdateEvent ParseAccountPositionUpdate(DataEvent<BinanceStreamPositionsUpdate> positionUpdate)
    {
        return new AccountPositionUpdateEvent(ExchangeName, positionUpdate.Symbol, new
                AccountPosition(ExchangeName, positionUpdate.Symbol)
        {
            ListenKey = positionUpdate.Data.ListenKey,
            Timestamp = positionUpdate.Data.Timestamp,
            Balances = positionUpdate.Data.Balances.Select(x => new StreamBalance
            {
                Asset = x.Asset,
                Available= x.Available,
                Locked= x.Locked,
                Total = x.Total
            }).ToList()
        });
    }

    private OrderUpdateEvent ParseOrderUpdate(DataEvent<BinanceStreamOrderUpdate> orderUpdate)
    {
        return new OrderUpdateEvent(ExchangeName, orderUpdate.Symbol, new OrderUpdate(ExchangeName, orderUpdate.Symbol)
        {
            BuyerIsMaker = orderUpdate.Data.BuyerIsMaker,
            ClientOrderId = orderUpdate.Data.ClientOrderId,
            CounterOrderId = orderUpdate.Data.CounterOrderId,
            CreateTime = orderUpdate.Data.CreateTime,
            ExecutionType = (Shared.Models.ExecutionType)orderUpdate.Data.ExecutionType,
            Fee = orderUpdate.Data.Fee,
            FeeAsset = orderUpdate.Data.FeeAsset,
            IcebergQuantity = orderUpdate.Data.IcebergQuantity,
            Id = orderUpdate.Data.Id,
            IsWorking = orderUpdate.Data.IsWorking,
            LastPreventedQuantity = orderUpdate.Data.LastPreventedQuantity,
            LastPriceFilled = orderUpdate.Data.LastPriceFilled,
            LastQuantityFilled = orderUpdate.Data.LastQuantityFilled,
            LastQuoteQuantity = orderUpdate.Data.LastQuoteQuantity,
            OrderListId = orderUpdate.Data.OrderListId,
            OriginalClientOrderId = orderUpdate.Data.OriginalClientOrderId,
            PreventedMatchId = orderUpdate.Data.PreventedMatchId,
            PreventedQuantity = orderUpdate.Data.PreventedQuantity,
            Price = orderUpdate.Data.Price,
            Quantity = orderUpdate.Data.Quantity,
            QuantityFilled = orderUpdate.Data.QuantityFilled,
            QuoteQuantity = orderUpdate.Data.QuoteQuantity,
            QuoteQuantityFilled = orderUpdate.Data.QuoteQuantityFilled,
            RejectReason = (Shared.Models.OrderRejectReason)orderUpdate.Data.RejectReason,
            SelfTradePreventionMode = (Shared.Models.SelfTradePreventionMode)orderUpdate.Data.SelfTradePreventionMode,
            Side = (Shared.Models.OrderSide)orderUpdate.Data.Side,
            Status = (Shared.Models.OrderStatus)orderUpdate.Data.Status,
            StopPrice = orderUpdate.Data.StopPrice,
            TimeInForce = (Shared.Models.TimeInForce)orderUpdate.Data.TimeInForce,
            TradeGroupId = orderUpdate.Data.TradeGroupId,
            TradeId = orderUpdate.Data.TradeId,
            TrailingDelta = orderUpdate.Data.TrailingDelta,
            TrailingTime = orderUpdate.Data.TrailingTime,
            Type = (Shared.Models.SpotOrderType)orderUpdate.Data.Type,
            UpdateTime = orderUpdate.Data.UpdateTime,
            WorkingTime = orderUpdate.Data.WorkingTime,
            ListenKey = orderUpdate.Data.ListenKey
        });
    }

    public async Task<IEnumerable<AccountPosition>> GetCurrentPositions()
    {
        var getAccountInfo = await _binanceSocketExchangeClient.SpotApi.Account.GetAccountInfoAsync();

        if (!getAccountInfo.Success)
        {
            throw new Exception($"can not get account info : {getAccountInfo.Error}");
        }

        var accountPositions = new List<AccountPosition>();

        foreach (var balance in getAccountInfo.Data.Result.Balances)
        {
            if (balance.Total > 0)
            {
                var streamBalance = new StreamBalance
                {
                    Asset = balance.Asset,
                    Available = balance.Available,
                    Locked = balance.Locked,
                    Total = balance.Total
                };

                // Create an AccountPosition per balance
                var accountPosition = new AccountPosition(
                    exchange: ExchangeName, // Ou tout autre exchange si tu en utilises plusieurs
                    symbol: balance.Asset)
                {
                    Timestamp = DateTime.UtcNow, // Moment de la récupération
                    ListenKey = "", // Peut être mis à jour si nécessaire
                    Balances = new List<StreamBalance> { streamBalance }
                };

                accountPositions.Add(accountPosition);
            }
        }

        return accountPositions;

    }

    public async Task<List<AccountBalance>> GetCurrentAccountBalancesGroupedByBaseAsset()
    {
        // Récupère les informations des symboles de l'échange
        var exchangeInfo = await _binanceExchangeClient.SpotApi.ExchangeData.GetExchangeInfoAsync();

        if (!exchangeInfo.Success)
        {
            throw new Exception($"Erreur lors de la récupération des informations de l'échange : {exchangeInfo.Error}");
        }

        // Récupère les informations de compte pour les balances
        var accountInfo = await _binanceExchangeClient.SpotApi.Account.GetAccountInfoAsync();

        if (!accountInfo.Success)
        {
            throw new Exception($"Erreur lors de la récupération des informations du compte : {accountInfo.Error}");
        }

        // Dictionnaire pour regrouper les balances par base asset
        var groupedBalances = new Dictionary<string, AccountBalance>();

        // Parcours les balances du compte
        foreach (var balance in accountInfo.Data.Balances)
        {
            if (balance.Total > 0)
            {
                // Trouver les symboles où cet asset est utilisé comme base asset
                var relevantSymbols = exchangeInfo.Data.Symbols
                    .Where(s => s.BaseAsset == balance.Asset)
                    .ToList();

                // Si l'asset est utilisé dans des paires de trading, on le groupe
                foreach (var symbol in relevantSymbols)
                {
                    var baseAsset = symbol.BaseAsset;

                    if (!groupedBalances.ContainsKey(baseAsset))
                    {
                        groupedBalances[baseAsset] = new AccountBalance(
                            exchange: "Binance",
                            symbol: baseAsset)
                        {
                            Timestamp = DateTime.UtcNow,
                            Available = balance.Available, // Montant disponible
                            ListenKey = "" // Peut être mis à jour si nécessaire
                        };
                    }
                    else
                    {
                        groupedBalances[baseAsset].Available += balance.Available;
                    }
                }
            }
        }

        return groupedBalances.Values.ToList();
    }
}