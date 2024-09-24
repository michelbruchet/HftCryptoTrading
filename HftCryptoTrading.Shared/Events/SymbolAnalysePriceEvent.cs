using MediatR;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HftCryptoTrading.Shared.Events;

[MessagePackObject]
public class SymbolAnalysePriceEvent(string exchangeName, string symbolName) : INotification
{
    [Key(0)]
    public string ExchangeName { get; } = exchangeName;

    [Key(1)]
    public string SymbolName { get; } = symbolName;

    [Key(2)]
    public Symbol? Symbol { get; set; }
    [Key(3)]
    public decimal? Price { get; set; }
    [Key(4)]
    public decimal? Volume { get; set; }
    [Key(5)]
    public decimal? Price24H { get; set; }
    [Key(6)]
    public decimal? HighPrice { get; set; }
    [Key(7)]
    public decimal? LowPrice { get; set; }
    [Key(8)]
    public decimal? PriceChange { get; set; }
    [Key(9)]
    public decimal? PriceChangePercent { get; set; }
    [Key(10)]
    public decimal? Bid { get; set; } // Add Bid
    [Key(11)]
    public decimal? Ask { get; set; } // Add Ask
    [Key(12)]
    public DateTime? PublishedDate { get; set; }
    [Key(13)]
    public decimal? BestBidPrice { get; set; }
    [Key(14)]
    public decimal? BestAskPrice { get; set; }
    [Key(15)]
    public decimal? BestBidQuantity { get; set; }
    [Key(16)]
    public decimal? BestAskQuantity { get; set; }
}

[MessagePackObject]
public class Symbol
{
    /// <summary>
    /// Gets or sets the name of the symbol.
    /// </summary>
    [Key(0)]
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether trailing stop is allowed.
    /// </summary>
    [Key(1)]
    public bool AllowTrailingStop { get; set; }

    /// <summary>
    /// Gets or sets the base asset for the symbol.
    /// </summary>
    [Key(2)]
    public string BaseAsset { get; set; }

    /// <summary>
    /// Gets or sets the precision of the base asset.
    /// </summary>
    [Key(3)]
    public int BaseAssetPrecision { get; set; }

    /// <summary>
    /// Gets or sets the precision of the base fee.
    /// </summary>
    [Key(4)]
    public int BaseFeePrecision { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether cancel replace is allowed.
    /// </summary>
    [Key(5)]
    public bool CancelReplaceAllowed { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether iceberg orders are allowed.
    /// </summary>
    [Key(6)]
    public bool IcebergAllowed { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether margin trading is allowed.
    /// </summary>
    [Key(7)]
    public bool IsMarginTradingAllowed { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether spot trading is allowed.
    /// </summary>
    [Key(8)]
    public bool IsSpotTradingAllowed { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether OCO (One Cancels Other) orders are allowed.
    /// </summary>
    [Key(9)]
    public bool OCOAllowed { get; set; }

    /// <summary>
    /// Gets or sets the allowed order types for the symbol.
    /// </summary>
    [Key(10)]
    public string[]? OrderTypes { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether OTO (One Triggers Other) orders are allowed.
    /// </summary>
    [Key(11)]
    public bool OTOAllowed { get; set; }

    /// <summary>
    /// Gets or sets the quote asset for the symbol.
    /// </summary>
    [Key(12)]
    public string QuoteAsset { get; set; }

    /// <summary>
    /// Gets or sets the precision of the quote asset.
    /// </summary>
    [Key(13)]
    public int QuoteAssetPrecision { get; set; }

    /// <summary>
    /// Gets or sets the precision of the quote fee.
    /// </summary>
    [Key(14)]
    public int QuoteFeePrecision { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether quantity market orders for the quote are allowed.
    /// </summary>
    [Key(15)]
    public bool QuoteOrderQuantityMarketAllowed { get; set; }

    /// <summary>
    /// Gets or sets the status of the symbol.
    /// </summary>
    [Key(16)]
    public string? Status { get; set; }

    /// <summary>
    /// Gets or sets the step size for the symbol.
    /// </summary>
    [Key(17)]
    public decimal? StepSize { get; set; }

    /// <summary>
    /// Gets or sets the minimum quantity for orders.
    /// </summary>
    [Key(18)]
    public decimal? MinQuantity { get; set; }

    /// <summary>
    /// Gets or sets the maximum quantity for orders.
    /// </summary>
    [Key(19)]
    public decimal? MaxQuantity { get; set; }

    /// <summary>
    /// Gets or sets the limit for orders.
    /// </summary>
    [Key(20)]
    public int? IceBergPartsLimit { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of algorithmic orders allowed.
    /// </summary>
    [Key(21)]
    public int? MaxNumberAlgorithmicOrders { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of orders allowed.
    /// </summary>
    [Key(22)]
    public int? MaxNumberOrders { get; set; }

    /// <summary>
    /// Gets or sets the maximum position for the symbol.
    /// </summary>
    [Key(23)]
    public decimal? MaxPosition { get; set; }

    /// <summary>
    /// Gets or sets the minimum notional value for orders.
    /// </summary>
    [Key(24)]
    public decimal? MinNotional { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to apply settings to market orders.
    /// </summary>
    [Key(25)]
    public bool? ApplyToMarketOrders { get; set; }

    /// <summary>
    /// Gets or sets the average price minutes.
    /// </summary>
    [Key(26)]
    public int? AveragePriceMinutes { get; set; }

    /// <summary>
    /// Gets or sets the minimum price allowed for the symbol.
    /// </summary>
    [Key(27)]
    public decimal? MinPrice { get; set; }

    /// <summary>
    /// Gets or sets the maximum price allowed for the symbol.
    /// </summary>
    [Key(28)]
    public decimal? MaxPrice { get; set; }

    /// <summary>
    /// Gets or sets the tick size for the symbol.
    /// </summary>
    [Key(29)]
    public decimal? TickSize { get; set; }

    /// <summary>
    /// Gets or sets the multiplier up value.
    /// </summary>
    [Key(30)]
    public decimal? MultiplierUp { get; set; }

    /// <summary>
    /// Gets or sets the multiplier down value.
    /// </summary>
    [Key(31)]
    public decimal? MultiplierDown { get; set; }

    /// <summary>
    /// Gets or sets the number of decimal places for the multiplier.
    /// </summary>
    [Key(32)]
    public int? MultiplierDecimal { get; set; }

    /// <summary>
    /// Gets or sets the minimum trailing above delta.
    /// </summary>
    [Key(33)]
    public int? MinTrailingAboveDelta { get; set; }

    /// <summary>
    /// Gets or sets the maximum trailing above delta.
    /// </summary>
    [Key(34)]
    public int? MaxTrailingAboveDelta { get; set; }

    /// <summary>
    /// Gets or sets the minimum trailing below delta.
    /// </summary>
    [Key(35)]
    public int? MinTrailingBelowDelta { get; set; }

    /// <summary>
    /// Gets or sets the maximum trailing below delta.
    /// </summary>
    [Key(36)]
    public int? MaxTrailingBelowDelta { get; set; }
}
