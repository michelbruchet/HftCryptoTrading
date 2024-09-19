using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HftCryptoTrading.Shared.Models;

public class SymbolData
{
    public string Symbol { get; set; }
    public bool AllowTrailingStop { get; set; }
    public string BaseAsset { get; set; }
    public int BaseAssetPrecision { get; set; }
    public int BaseFeePrecision { get; set; }
    public bool CancelReplaceAllowed { get; set; }
    public bool IcebergAllowed { get; set; }
    public bool IsMarginTradingAllowed { get; set; }
    public bool IsSpotTradingAllowed { get; set; }
    public bool OCOAllowed { get; set; }
    public string[]? OrderTypes { get; set; }
    public bool OTOAllowed { get; set; }
    public string QuoteAsset { get; set; }
    public int QuoteAssetPrecision { get; set; }
    public int QuoteFeePrecision { get; set; }
    public bool QuoteOrderQuantityMarketAllowed { get; set; }
    public string Status { get; set; }

    // Optional filters
    public MarketLotSizeFilterData? MarketLotSizeFilter { get; set; }
    public IceBergPartsFilterData? IceBergPartsFilter { get; set; }
    public MaxAlgorithmicOrdersFilterData? MaxAlgorithmicOrdersFilter { get; set; }
    public MaxOrdersFilterData? MaxOrdersFilter { get; set; }
    public MaxPositionFilterData? MaxPositionFilter { get; set; }
    public MinNotionalFilterData? MinNotionalFilter { get; set; }
    public PriceFilterData? PriceFilter { get; set; }
    public PricePercentFilterData? PricePercentFilter { get; set; }
    public TrailingDeltaFilterData? TrailingDeltaFilter { get; set; }

    public SymbolData(string symbol)
    {
        Symbol = symbol;
    }
}

public class MarketLotSizeFilterData
{
    public decimal StepSize { get; set; }
    public decimal MinQuantity { get; set; }
    public decimal MaxQuantity { get; set; }

    public MarketLotSizeFilterData(decimal stepSize, decimal minQuantity, decimal maxQuantity)
    {
        StepSize = stepSize;
        MinQuantity = minQuantity;
        MaxQuantity = maxQuantity;
    }
}

public class IceBergPartsFilterData
{
    public int Limit { get; set; }

    public IceBergPartsFilterData(int limit)
    {
        Limit = limit;
    }
}

public class MaxAlgorithmicOrdersFilterData
{
    public int MaxNumberAlgorithmicOrders { get; set; }

    public MaxAlgorithmicOrdersFilterData(int maxNumberAlgorithmicOrders)
    {
        MaxNumberAlgorithmicOrders = maxNumberAlgorithmicOrders;
    }
}

public class MaxOrdersFilterData
{
    public int MaxNumberOrders { get; set; }

    public MaxOrdersFilterData(int maxNumberOrders)
    {
        MaxNumberOrders = maxNumberOrders;
    }
}

public class MaxPositionFilterData
{
    public decimal MaxPosition { get; set; }

    public MaxPositionFilterData(decimal maxPosition)
    {
        MaxPosition = maxPosition;
    }
}

public class MinNotionalFilterData
{
    /// <summary>
    /// The minimal total quote quantity of an order. This is calculated by Price * Quantity.
    /// </summary>
    public decimal MinNotional { get; set; }

    /// <summary>
    /// Whether or not this filter is applied to market orders. If so the average trade price is used.
    /// </summary>
    public bool? ApplyToMarketOrders { get; set; }

    /// <summary>
    /// The amount of minutes the average price of trades is calculated over for market orders. 0 means the last price is used
    /// </summary>
    public int? AveragePriceMinutes { get; set; }

    public MinNotionalFilterData(decimal minNotional, int? averagePriceMinutes, bool? applyToMarketOrders)
    {
        MinNotional = minNotional;
        AveragePriceMinutes = averagePriceMinutes;
        ApplyToMarketOrders = applyToMarketOrders;
    }
}

public class PriceFilterData
{
    /// <summary>
    /// The minimal price the order can be for
    /// </summary>
    public decimal MinPrice { get; set; }
    /// <summary>
    /// The max price the order can be for
    /// </summary>
    public decimal MaxPrice { get; set; }
    /// <summary>
    /// The tick size of the price. The price can not have more precision as this and can only be incremented in steps of this.
    /// </summary>
    public decimal TickSize { get; set; }

    public PriceFilterData(decimal minPrice, decimal maxPrice, decimal tickSize)
    {
        MinPrice = minPrice;
        MaxPrice = maxPrice;
        TickSize = tickSize;
    }
}

public class PricePercentFilterData
{
    /// <summary>
    /// The max factor the price can deviate up
    /// </summary>
    public decimal MultiplierUp { get; set; }
    /// <summary>
    /// The max factor the price can deviate down
    /// </summary>
    public decimal MultiplierDown { get; set; }

    /// <summary>
    /// The amount of minutes the average price of trades is calculated over. 0 means the last price is used
    /// </summary>
    public int? MultiplierDecimal { get; set; }
    /// <summary>
    /// The amount of minutes the average price of trades is calculated over. 0 means the last price is used
    /// </summary>
    public int? AveragePriceMinutes { get; set; }

    public PricePercentFilterData(int? averagePriceMinutes, decimal multiplierUp, decimal multiplierDown, int? multiplierDecimal)
    {
        AveragePriceMinutes = averagePriceMinutes;
        MultiplierUp = multiplierUp;
        MultiplierDown = multiplierDown;
        MultiplierDecimal = multiplierDecimal;
    }
}

public class TrailingDeltaFilterData
{
    /// <summary>
    /// The MinTrailingAboveDelta filter defines the minimum amount in Basis Point or BIPS above the price to activate the order.
    /// </summary>
    public int MinTrailingAboveDelta { get; set; }
    /// <summary>
    /// The MaxTrailingAboveDelta filter defines the maximum amount in Basis Point or BIPS above the price to activate the order.
    /// </summary>
    public int MaxTrailingAboveDelta { get; set; }
    /// <summary>
    /// The MinTrailingBelowDelta filter defines the minimum amount in Basis Point or BIPS below the price to activate the order.
    /// </summary>
    public int MinTrailingBelowDelta { get; set; }
    /// <summary>
    /// The MaxTrailingBelowDelta filter defines the minimum amount in Basis Point or BIPS below the price to activate the order.
    /// </summary>
    public int MaxTrailingBelowDelta { get; set; }

    public TrailingDeltaFilterData(int minTrailingBelowDelta, int maxTrailingBelowDelta, int minTrailingAboveDelta, int maxTrailingAboveDelta)
    {
        MinTrailingBelowDelta = minTrailingBelowDelta;
        MaxTrailingBelowDelta = maxTrailingBelowDelta;
        MinTrailingAboveDelta = minTrailingAboveDelta;
        MaxTrailingAboveDelta = maxTrailingAboveDelta;
    }
}
