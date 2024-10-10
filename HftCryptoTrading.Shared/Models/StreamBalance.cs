using System.Text.Json.Serialization;

namespace HftCryptoTrading.Shared.Models;

public class StreamBalance
{
    /// <summary>
    /// The asset this balance is for
    /// </summary>
    [JsonPropertyName("a")]
    public string Asset { get; set; } = string.Empty;
    /// <summary>
    /// The quantity that isn't locked in a trade
    /// </summary>
    [JsonPropertyName("f")]
    public decimal Available { get; set; }
    /// <summary>
    /// The quantity that is currently locked in a trade
    /// </summary>
    [JsonPropertyName("l")]
    public decimal Locked { get; set; }
    /// <summary>
    /// The total balance of this asset (Free + Locked)
    /// </summary>
    public decimal Total { get; set; }
}
