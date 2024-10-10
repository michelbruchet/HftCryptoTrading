namespace HftCryptoTrading.Shared.Models;

public class AccountBalance(string exchange, string symbol)
{
    public string Exchange => exchange;
    public string Symbol => symbol;
    public DateTime Timestamp { get; set; }
    public string ListenKey { get; set; } = string.Empty;
    public decimal Available { get; set; }

}
