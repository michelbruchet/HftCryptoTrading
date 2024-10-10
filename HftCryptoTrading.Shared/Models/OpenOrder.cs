﻿using System.ComponentModel;
using System.Text.Json.Serialization;

namespace HftCryptoTrading.Shared.Models;

public class OpenOrder(string symbol, string exchange)
{
    /// <summary>
    /// The symbol the order is for
    /// </summary>
    [JsonPropertyName("symbol")]
    public string Symbol { get; private set; } = symbol;
    public string Exchange { get; private set; } = exchange;
    /// <summary>
    /// The order id generated by Binance
    /// </summary>
    [JsonPropertyName("orderId")]
    public long Id { get; set; }

    /// <summary>
    /// Id of the order list this order belongs to
    /// </summary>
    [JsonPropertyName("orderListId")]
    public long OrderListId { get; set; }

    /// <summary>
    /// Original order id
    /// </summary>
    [JsonPropertyName("origClientOrderId")]
    public string OriginalClientOrderId { get; set; } = string.Empty;

    /// <summary>
    /// The order id as assigned by the client
    /// </summary>
    [JsonPropertyName("clientOrderId")]
    public string ClientOrderId { get; set; } = string.Empty;

    private decimal _price;

    /// <summary>
    /// The price of the order
    /// </summary>
    [JsonPropertyName("price")]
    public decimal Price
    {
        get;
        set;
    }

    /// <summary>
    /// The original quantity of the order, as specified in the order parameters by the user
    /// </summary>
    [JsonPropertyName("origQty")]
    public decimal Quantity { get; set; }
    /// <summary>
    /// The currently executed quantity of the order
    /// </summary>
    [JsonPropertyName("executedQty")]
    public decimal QuantityFilled { get; set; }
    /// <summary>
    /// The currently executed amount of quote asset. Amounts to Sum(quantity * price) of executed trades for this order
    /// </summary>
    [JsonPropertyName("cummulativeQuoteQty")]
    public decimal QuoteQuantityFilled { get; set; }
    /// <summary>
    /// The original quote order quantity of the order, as specified in the order parameters by the user
    /// </summary>
    [JsonPropertyName("origQuoteOrderQty")]
    public decimal QuoteQuantity { get; set; }

    /// <summary>
    /// The status of the order
    /// </summary>
    [JsonPropertyName("status")]
    public OrderStatus Status { get; set; }

    /// <summary>
    /// How long the order is active
    /// </summary>
    [JsonPropertyName("timeInForce")]
    public int TimeInForce { get; set; }
    /// <summary>
    /// The type of the order
    /// </summary>
    [JsonPropertyName("type")]
    public int Type { get; set; }
    /// <summary>
    /// The side of the order
    /// </summary>
    [JsonPropertyName("side")]
    public int Side { get; set; }
    /// <summary>
    /// The stop price
    /// </summary>
    [JsonPropertyName("stopPrice")]
    public decimal? StopPrice { get; set; }

    /// <summary>
    /// The iceberg quantity
    /// </summary>
    [JsonPropertyName("icebergQty")]
    public decimal? IcebergQuantity { get; set; }
    /// <summary>
    /// The time the order was submitted
    /// </summary>
    [JsonPropertyName("time"), JsonConverter(typeof(DateTimeConverter))]
    public DateTime CreateTime { get; set; }
    /// <summary>
    /// The time the order was last updated
    /// </summary>
    [JsonConverter(typeof(DateTimeConverter))]
    [JsonPropertyName("updateTime")]
    public DateTime? UpdateTime { get; set; }
    /// <summary>
    /// The time the transaction was executed (when canceling order)
    /// </summary>
    [JsonConverter(typeof(DateTimeConverter))]
    [JsonPropertyName("transactTime")]
    public TimeSpan? TransactTime => UpdateTime.HasValue ? CreateTime.Subtract(UpdateTime.Value) : null;

    /// <summary>
    /// When the order started working
    /// </summary>
    [JsonConverter(typeof(DateTimeConverter))]
    [JsonPropertyName("workingTime")]
    public DateTime? WorkingTime { get; set; }
    /// <summary>
    /// Is working
    /// </summary>
    [JsonPropertyName("isWorking")]
    public bool? IsWorking { get; set; }
    /// <summary>
    /// If isolated margin (for margin account orders)
    /// </summary>
    [JsonPropertyName("isIsolated")]
    public bool? IsIsolated { get; set; }
    /// <summary>
    /// Quantity which is still open to be filled
    /// </summary>
    public decimal QuantityRemaining => Quantity - QuantityFilled;

    /// <summary>
    /// The average price the order was filled
    /// </summary>
    public decimal? AverageFillPrice
    {
        get
        {
            if (QuantityFilled == 0)
                return null;

            return QuoteQuantityFilled / QuantityFilled;
        }
    }

    /// <summary>
    /// Self trade prevention mode
    /// </summary>
    [JsonPropertyName("selfTradePreventionMode"), JsonConverter(typeof(EnumConverter))]
    public int SelfTradePreventionMode { get; set; }
}

public enum OrderSide
{
    /// <summary>
    /// Buy
    /// </summary>
    Buy,
    /// <summary>
    /// Sell
    /// </summary>
    Sell
}

public enum SpotOrderType
{
    /// <summary>
    /// Limit orders will be placed at a specific price. If the price isn't available in the order book for that asset the order will be added in the order book for someone to fill.
    /// </summary>
    Limit,
    /// <summary>
    /// Market order will be placed without a price. The order will be executed at the best price available at that time in the order book.
    /// </summary>
    Market,
    /// <summary>
    /// Stop loss order. Will execute a market order when the price drops below a price to sell and therefor limit the loss
    /// </summary>
    StopLoss,
    /// <summary>
    /// Stop loss order. Will execute a limit order when the price drops below a price to sell and therefor limit the loss
    /// </summary>
    StopLossLimit,
    /// <summary>
    /// Take profit order. Will execute a market order when the price rises above a price to sell and therefor take a profit
    /// </summary>
    TakeProfit,
    /// <summary>
    /// Take profit limit order. Will execute a limit order when the price rises above a price to sell and therefor take a profit
    /// </summary>
    TakeProfitLimit,
    /// <summary>
    /// Same as a limit order, however it will fail if the order would immediately match, therefor preventing taker orders
    /// </summary>
    LimitMaker
}

/// <summary>
/// The time the order will be active for
/// </summary>
public enum TimeInForce
{
    /// <summary>
    /// GoodTillCanceled orders will stay active until they are filled or canceled
    /// </summary>
    GoodTillCanceled,
    /// <summary>
    /// ImmediateOrCancel orders have to be at least partially filled upon placing or will be automatically canceled
    /// </summary>
    ImmediateOrCancel,
    /// <summary>
    /// FillOrKill orders have to be entirely filled upon placing or will be automatically canceled
    /// </summary>
    FillOrKill,
    /// <summary>
    /// GoodTillCrossing orders will post only
    /// </summary>
    GoodTillCrossing,
    /// <summary>
    /// Good til the order expires or is canceled
    /// </summary>
    GoodTillExpiredOrCanceled,
    /// <summary>
    /// Good til date
    /// </summary>
    GoodTillDate
}
public enum ExecutionType
{
    /// <summary>
    /// New
    /// </summary>
    New,
    /// <summary>
    /// Canceled
    /// </summary>
    Canceled,
    /// <summary>
    /// Replaced
    /// </summary>
    Replaced,
    /// <summary>
    /// Rejected
    /// </summary>
    Rejected,
    /// <summary>
    /// Trade
    /// </summary>
    Trade,
    /// <summary>
    /// Expired
    /// </summary>
    Expired,
    /// <summary>
    /// Amendment
    /// </summary>
    Amendment,
    /// <summary>
    /// Self trade prevented
    /// </summary>
    TradePrevention
}

/// <summary>
/// The status of an orderн
/// </summary>
public enum OrderStatus
{
    /// <summary>
    /// Order is not yet active
    /// </summary>
    PendingNew,
    /// <summary>
    /// Order is new
    /// </summary>
    New,
    /// <summary>
    /// Order is partly filled, still has quantity left to fill
    /// </summary>
    PartiallyFilled,
    /// <summary>
    /// The order has been filled and completed
    /// </summary>
    Filled,
    /// <summary>
    /// The order has been canceled
    /// </summary>
    Canceled,
    /// <summary>
    /// The order is in the process of being canceled  (currently unused)
    /// </summary>
    PendingCancel,
    /// <summary>
    /// The order has been rejected
    /// </summary>
    Rejected,
    /// <summary>
    /// The order has expired
    /// </summary>
    Expired,
    /// <summary>
    /// Liquidation with Insurance Fund
    /// </summary>
    Insurance,
    /// <summary>
    /// Counterparty Liquidation
    /// </summary>
    Adl,
    /// <summary>
    /// Expired because of trigger SelfTradePrevention
    /// </summary>
    ExpiredInMatch
}
/// <summary>
/// The reason the order was rejected
/// </summary>
public enum OrderRejectReason
{
    /// <summary>
    /// Not rejected
    /// </summary>
    None,
    /// <summary>
    /// Unknown instrument
    /// </summary>
    UnknownInstrument,
    /// <summary>
    /// Closed market
    /// </summary>
    MarketClosed,
    /// <summary>
    /// Quantity out of bounds
    /// </summary>
    PriceQuantityExceedsHardLimits,
    /// <summary>
    /// Unknown order
    /// </summary>
    UnknownOrder,
    /// <summary>
    /// Duplicate
    /// </summary>
    DuplicateOrder,
    /// <summary>
    /// Unkown account
    /// </summary>
    UnknownAccount,
    /// <summary>
    /// Not enough balance
    /// </summary>
    InsufficientBalance,
    /// <summary>
    /// Account not active
    /// </summary>
    AccountInactive,
    /// <summary>
    /// Cannot settle
    /// </summary>
    AccountCannotSettle,
    /// <summary>
    /// Stop price would trigger immediately
    /// </summary>
    StopPriceWouldTrigger
}
/// <summary>
/// Self trade prevention mode
/// </summary>
public enum SelfTradePreventionMode
{
    /// <summary>
    /// Expire taker
    /// </summary>
    ExpireTaker,
    /// <summary>
    /// Exire maker
    /// </summary>
    ExpireMaker,
    /// <summary>
    /// Exire both
    /// </summary>
    ExpireBoth,
    /// <summary>
    /// None
    /// </summary>
    None
}
