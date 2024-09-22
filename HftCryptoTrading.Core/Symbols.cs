using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HftCryptoTrading.Exchanges.Core;

using MessagePack;
using System;

[MessagePackObject]
public class Symbol
{
    [Key(0)]
    public int Id { get; set; } // Primary Key

    [Key(1)]
    public string SymbolIdentifier { get; set; } // Symbol identifier (e.g., BTCUSDT)

    [Key(2)]
    public decimal LastPrice { get; set; } // Latest price of the symbol

    [Key(3)]
    public decimal Volume { get; set; } // Current trading volume

    [Key(4)]
    public decimal BestBidPrice { get; set; } // Best bid price

    [Key(5)]
    public decimal BestAskPrice { get; set; } // Best ask price

    [Key(6)]
    public decimal BestBidQuantity { get; set; } // Best bid quantity

    [Key(7)]
    public decimal BestAskQuantity { get; set; } // Best ask quantity

    [Key(8)]
    public DateTime CreatedAt { get; set; } // Timestamp of the symbol entry
}

[MessagePackObject]
public class Kline
{
    [Key(0)]
    public int Id { get; set; } // Primary Key

    [Key(1)]
    public int SymbolId { get; set; } // Foreign Key to Symbols table

    [Key(2)]
    public Symbol Symbol { get; set; } // Navigation property

    [Key(3)]
    public decimal OpenPrice { get; set; } // Opening price

    [Key(4)]
    public decimal ClosePrice { get; set; } // Closing price

    [Key(5)]
    public decimal HighPrice { get; set; } // Highest price in the interval

    [Key(6)]
    public decimal LowPrice { get; set; } // Lowest price in the interval

    [Key(7)]
    public decimal Volume { get; set; } // Volume during the interval

    [Key(8)]
    public DateTime Timestamp { get; set; } // Timestamp of the kline
}

[MessagePackObject]
public class VolumeAnomaly
{
    [Key(0)]
    public int Id { get; set; } // Primary Key

    [Key(1)]
    public int SymbolId { get; set; } // Foreign Key to Symbols table

    [Key(2)]
    public Symbol Symbol { get; set; } // Navigation property

    [Key(3)]
    public string AnomalyType { get; set; } // Type of volume anomaly detected

    [Key(4)]
    public DateTime DetectedAt { get; set; } // Timestamp of detection
}

[MessagePackObject]
public class PriceAnomaly
{
    [Key(0)]
    public int Id { get; set; } // Primary Key

    [Key(1)]
    public int SymbolId { get; set; } // Foreign Key to Symbols table

    [Key(2)]
    public Symbol Symbol { get; set; } // Navigation property

    [Key(3)]
    public string AnomalyType { get; set; } // Type of price anomaly detected

    [Key(4)]
    public DateTime DetectedAt { get; set; } // Timestamp of detection
}

[MessagePackObject]
public class SpreadAnomaly
{
    [Key(0)]
    public int Id { get; set; } // Primary Key

    [Key(1)]
    public int SymbolId { get; set; } // Foreign Key to Symbols table

    [Key(2)]
    public Symbol Symbol { get; set; } // Navigation property

    [Key(3)]
    public string AnomalyType { get; set; } // Type of spread anomaly detected

    [Key(4)]
    public DateTime DetectedAt { get; set; } // Timestamp of detection
}

[MessagePackObject]
public class PriceChange
{
    [Key(0)]
    public int Id { get; set; } // Primary Key

    [Key(1)]
    public int SymbolId { get; set; } // Foreign Key to Symbols table

    [Key(2)]
    public Symbol Symbol { get; set; } // Navigation property

    [Key(3)]
    public decimal OldPrice { get; set; } // Price before the change

    [Key(4)]
    public decimal NewPrice { get; set; } // Price after the change

    [Key(5)]
    public DateTime ChangedAt { get; set; } // Timestamp of the change
}