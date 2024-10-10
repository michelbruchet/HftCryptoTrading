using System.ComponentModel;
using System.Text.Json.Serialization;

namespace HftCryptoTrading.Shared.Models;

public class AccountPosition(string exchange, string symbol)
{
    public string Exchange => exchange;
    public string Symbol => symbol;
    /// <summary>
    /// Time of last account update
    /// </summary>
    [JsonPropertyName("u"), JsonConverter(typeof(DateTimeConverter))]
    public DateTime Timestamp { get; set; }
    /// <summary>
    /// The listen key the update was for
    /// </summary>
    public string ListenKey { get; set; } = string.Empty;
    /// <summary>
    /// Balances
    /// </summary>
    [JsonPropertyName("B")]
    public List<StreamBalance> Balances { get; set; } = [];
}
