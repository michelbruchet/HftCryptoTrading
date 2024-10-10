using System.ComponentModel;
using System.Text.Json.Serialization;

namespace HftCryptoTrading.Shared.Models;

public class AccountBalanceUpdate(string exchange, string symbol)
{
    public string Exchange => exchange;
    public string Symbol => symbol;

    /// <summary>
    /// The asset which changed
    /// </summary>
    [JsonPropertyName("a")]
    public string Asset { get; set; } = string.Empty;
    /// <summary>
    /// The balance delta
    /// </summary>
    [JsonPropertyName("d")]
    public decimal BalanceDelta { get; set; }
    /// <summary>
    /// The listen key the update was for
    /// </summary>
    public string ListenKey { get; set; } = string.Empty;
    /// <summary>
    /// The time the deposit/withdrawal was cleared
    /// </summary>
    [JsonPropertyName("T"), JsonConverter(typeof(DateTimeConverter))]
    public DateTime ClearTime { get; set; }

}