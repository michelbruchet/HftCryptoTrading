namespace HftCryptoTrading.Shared.Models;

using MessagePack;

[MessagePackObject]
public class SymbolData()
{
    [Key(0)] public string Name { get; set; }
    [Key(1)] public bool AllowTrailingStop { get; set; }
    [Key(2)] public string BaseAsset { get; set; }
    [Key(3)] public int BaseAssetPrecision { get; set; }
    [Key(4)] public int BaseFeePrecision { get; set; }
    [Key(5)] public bool CancelReplaceAllowed { get; set; }
    [Key(6)] public bool IcebergAllowed { get; set; }
    [Key(7)] public bool IsMarginTradingAllowed { get; set; }
    [Key(8)] public bool IsSpotTradingAllowed { get; set; }
    [Key(9)] public bool OCOAllowed { get; set; }
    [Key(10)] public string[]? OrderTypes { get; set; }
    [Key(11)] public bool OTOAllowed { get; set; }
    [Key(12)] public string QuoteAsset { get; set; }
    [Key(13)] public int QuoteAssetPrecision { get; set; }
    [Key(14)] public int QuoteFeePrecision { get; set; }
    [Key(15)] public bool QuoteOrderQuantityMarketAllowed { get; set; }
    [Key(16)] public string Status { get; set; }

    // Optional filters
    [Key(17)] public MarketLotSizeFilterData? MarketLotSizeFilter { get; set; } = new();
    [Key(18)] public IceBergPartsFilterData? IceBergPartsFilter { get; set; } = new();
    [Key(19)] public MaxAlgorithmicOrdersFilterData? MaxAlgorithmicOrdersFilter { get; set; } = new();
    [Key(20)] public MaxOrdersFilterData? MaxOrdersFilter { get; set; } = new();
    [Key(21)] public MaxPositionFilterData? MaxPositionFilter { get; set; } = new();
    [Key(22)] public MinNotionalFilterData? MinNotionalFilter { get; set; } = new();
    [Key(23)] public PriceFilterData? PriceFilter { get; set; } = new();
    [Key(24)] public PricePercentFilterData? PricePercentFilter { get; set; } = new();
    [Key(25)] public TrailingDeltaFilterData? TrailingDeltaFilter { get; set; } = new();

    public SymbolData(string symbol):this()
    {
        Name = symbol;
    }
}

[MessagePackObject]
public class MarketLotSizeFilterData()
{
    [Key(0)] public decimal StepSize { get; set; }
    [Key(1)] public decimal MinQuantity { get; set; }
    [Key(2)] public decimal MaxQuantity { get; set; }

    public MarketLotSizeFilterData(decimal stepSize, decimal minQuantity, decimal maxQuantity):this()
    {
        StepSize = stepSize;
        MinQuantity = minQuantity;
        MaxQuantity = maxQuantity;
    }
}

[MessagePackObject]
public class IceBergPartsFilterData()
{
    [Key(0)] public int Limit { get; set; }

    public IceBergPartsFilterData(int limit):this()
    {
        Limit = limit;
    }
}

[MessagePackObject]
public class MaxAlgorithmicOrdersFilterData()
{
    [Key(0)] public int MaxNumberAlgorithmicOrders { get; set; }

    public MaxAlgorithmicOrdersFilterData(int maxNumberAlgorithmicOrders):this()
    {
        MaxNumberAlgorithmicOrders = maxNumberAlgorithmicOrders;
    }
}

[MessagePackObject]
public class MaxOrdersFilterData()
{
    [Key(0)] public int MaxNumberOrders { get; set; }

    public MaxOrdersFilterData(int maxNumberOrders):this()
    {
        MaxNumberOrders = maxNumberOrders;
    }
}

[MessagePackObject]
public class MaxPositionFilterData()
{
    [Key(0)] public decimal MaxPosition { get; set; }

    public MaxPositionFilterData(decimal maxPosition):this()
    {
        MaxPosition = maxPosition;
    }
}

[MessagePackObject]
public class MinNotionalFilterData()
{
    [Key(0)] public decimal MinNotional { get; set; }
    [Key(1)] public bool? ApplyToMarketOrders { get; set; }
    [Key(2)] public int? AveragePriceMinutes { get; set; }

    public MinNotionalFilterData(decimal minNotional, int? averagePriceMinutes, bool? applyToMarketOrders):this()
    {
        MinNotional = minNotional;
        AveragePriceMinutes = averagePriceMinutes;
        ApplyToMarketOrders = applyToMarketOrders;
    }
}

[MessagePackObject]
public class PriceFilterData()
{
    [Key(0)] public decimal MinPrice { get; set; }
    [Key(1)] public decimal MaxPrice { get; set; }
    [Key(2)] public decimal TickSize { get; set; }

    public PriceFilterData(decimal minPrice, decimal maxPrice, decimal tickSize):this()
    {
        MinPrice = minPrice;
        MaxPrice = maxPrice;
        TickSize = tickSize;
    }
}

[MessagePackObject]
public class PricePercentFilterData()
{
    [Key(0)] public int? AveragePriceMinutes { get; set; }
    [Key(1)] public decimal MultiplierUp { get; set; }
    [Key(2)] public decimal MultiplierDown { get; set; }
    [Key(3)] public int? MultiplierDecimal { get; set; }

    public PricePercentFilterData(int? averagePriceMinutes, decimal multiplierUp, decimal multiplierDown, int? multiplierDecimal):this()
    {
        AveragePriceMinutes = averagePriceMinutes;
        MultiplierUp = multiplierUp;
        MultiplierDown = multiplierDown;
        MultiplierDecimal = multiplierDecimal;
    }
}

[MessagePackObject]
public class TrailingDeltaFilterData()
{
    [Key(0)] public int MinTrailingAboveDelta { get; set; }
    [Key(1)] public int MaxTrailingAboveDelta { get; set; }
    [Key(2)] public int MinTrailingBelowDelta { get; set; }
    [Key(3)] public int MaxTrailingBelowDelta { get; set; }

    public TrailingDeltaFilterData(int minTrailingBelowDelta, int maxTrailingBelowDelta, int minTrailingAboveDelta, int maxTrailingAboveDelta):this()
    {
        MinTrailingBelowDelta = minTrailingBelowDelta;
        MaxTrailingBelowDelta = maxTrailingBelowDelta;
        MinTrailingAboveDelta = minTrailingAboveDelta;
        MaxTrailingAboveDelta = maxTrailingAboveDelta;
    }
}
